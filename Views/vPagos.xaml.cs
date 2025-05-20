using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vPagos : ContentPage
{
    private readonly Usuario _usuario;
    private readonly Servicio _servicio;
    private readonly Mascota _mascota;
    private readonly DateTime _fechaServicio;
    private readonly ApiService _apiService;
    private byte[] _comprobanteData;
    private string _comprobanteFileName;

    public List<string> MetodosPago { get; } = new List<string>
        {
            "Transferencia bancaria",
            "Depósito",
            "Efectivo"
        };
    public vPagos(Usuario usuario, Servicio servicio, Mascota mascota, DateTime fechaServicio)
	{
		InitializeComponent();
        _usuario = usuario;
        _servicio = servicio;
        _mascota = mascota;
        _fechaServicio = fechaServicio;
        _apiService = new ApiService();

        BindingContext = this;
        CargarDatosPago();
    }

    private void CargarDatosPago()
    {
        lblNombreServicio.Text = _servicio.Nombre;
        lblDescripcionServicio.Text = _servicio.Descripcion;
        lblFechaHora.Text = _fechaServicio.ToString("dd/MM/yyyy HH:mm");
        lblTotal.Text = _servicio.Precio.ToString("C");
        // imgServicio.Source = ImageSource.FromFile($"{_servicio.Nombre.ToLower()}.png");
    }

    private async void btnSeleccionarComprobante_Clicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                var stream = await result.OpenReadAsync();
                _comprobanteData = new byte[stream.Length];
                await stream.ReadAsync(_comprobanteData, 0, (int)stream.Length);
                _comprobanteFileName = result.FileName;

                imgComprobante.Source = ImageSource.FromStream(() => new MemoryStream(_comprobanteData));
                imgComprobante.IsVisible = true;
                btnPagar.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al seleccionar imagen: {ex.Message}", "OK");
        }
    }

    private async void btnPagar_Clicked(object sender, EventArgs e)
    {
        if (pkMetodoPago.SelectedItem == null)
        {
            await DisplayAlert("Error", "Por favor selecciona un método de pago", "OK");
            return;
        }

        if (_comprobanteData == null)
        {
            await DisplayAlert("Error", "Por favor sube un comprobante de pago", "OK");
            return;
        }

        try
        {
            btnPagar.IsEnabled = false;
            btnPagar.Text = "Procesando...";

            using var stream = new MemoryStream(_comprobanteData);
            var comprobante = new Comprobante
            {
                Descripcion = $"Pago por servicio {_servicio.Nombre}",
                UsuarioId = _usuario.Id.Value,
                ReservaId = 0 // Se actualizará después
            };

            var resultado = await _apiService.SubirComprobanteAsync(comprobante, stream, _comprobanteFileName);

            if (resultado == "OK")
            {
                await DisplayAlert("Éxito", "Pago procesado correctamente", "OK");
                await Navigation.PopToRootAsync();
            }
            else
            {
                await DisplayAlert("Error", resultado, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al procesar pago: {ex.Message}", "OK");
        }
        finally
        {
            btnPagar.IsEnabled = true;
            btnPagar.Text = "Pagar";
        }
    }
}