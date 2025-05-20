using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vReserva : ContentPage
{
    private readonly Servicio _servicio;
    private readonly Usuario _usuario;
    private readonly List<Mascota> _mascotas;
    private readonly ApiService _apiService;
    public vReserva(Servicio servicio, Usuario usuario, List<Mascota> mascotas)
	{
		InitializeComponent();
        _servicio = servicio;
        _usuario = usuario;
        _mascotas = mascotas;
        _apiService = new ApiService();

        CargarDatosServicio();
        CargarMascotas();
    }

    private void CargarDatosServicio()
    {
        lblNombreServicio.Text = _servicio.Nombre;
        lblPrecioServicio.Text = $"{_servicio.Precio:C}";
        lblDescripcionServicio.Text = _servicio.Descripcion;
        

        // Opcional: Cambiar imagen según el servicio
        imgServicio.Source = _servicio.Nombre switch
        {
            "Veterinaria" => "veterinaria.jpg",
            "Peluqueria" => "peluqueria.png",
            "Guarderia" => "guarderia.png",
            "Hospedaje" => "hospedaje.png",
            _ => "servicio_generico.png"
        };

        dpFecha.MinimumDate = DateTime.Today;
        dpFecha.Date = DateTime.Today;
    }

    private async void CargarMascotas()
    {
        pkMascotas.ItemsSource = _mascotas;
        if (_mascotas.Any())
        {
            pkMascotas.SelectedIndex = 0;
        }
    }

    private async void btnAgendar_Clicked(object sender, EventArgs e)
    {
        if (pkMascotas.SelectedItem == null)
        {
            await DisplayAlert("Error", "Por favor selecciona una mascota", "OK");
            return;
        }

        var mascotaSeleccionada = (Mascota)pkMascotas.SelectedItem;
        var fechaServicio = dpFecha.Date.Add(tpHora.Time);

        if (fechaServicio < DateTime.Now)
        {
            await DisplayAlert("Error", "No puedes seleccionar una fecha/hora en el pasado", "OK");
            return;
        }

        var reserva = new Reserva
        {
            FechaReserva = DateTime.Now,
            FechaServicio = fechaServicio,
            Estado = "pendiente",
            UsuarioId = _usuario.Id.Value,
            MascotaId = mascotaSeleccionada.Id,
            ServicioId = _servicio.Id,
            Servicio = _servicio,
            Mascota = mascotaSeleccionada
        };

        try
        {
            btnAgendar.IsEnabled = false;
            btnAgendar.Text = "Procesando...";

            var resultado = await _apiService.CrearReservaAsync(reserva);

            if (resultado != "ERROR")
            {
                await Navigation.PushAsync(new vPagos(_usuario, _servicio, mascotaSeleccionada, fechaServicio));
            }
            else
            {
                await DisplayAlert("Error", "No se pudo crear la reserva", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al crear reserva: {ex.Message}", "OK");
        }
        finally
        {
            btnAgendar.IsEnabled = true;
            btnAgendar.Text = "Agendar cita";
        }
    }
}