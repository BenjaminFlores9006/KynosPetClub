using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vDetalleComprobante : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Usuario _admin;
    public Comprobante Comprobante { get; set; }
    public Color EstadoColor { get; set; }
    public string MensajeError { get; set; }
    public vDetalleComprobante(Comprobante comprobante, Usuario admin)
	{
		InitializeComponent();
        _apiService = new ApiService();
        _admin = admin;
        Comprobante = comprobante;
        BindingContext = this;

        // Configurar color según estado
        EstadoColor = Comprobante.Estado == "Aprobado" ? Colors.Green :
                     Comprobante.Estado == "Rechazado" ? Colors.Red :
                     Colors.Orange;
    }

    private async void btnGuardar_Clicked(object sender, EventArgs e)
    {
        try
        {
            var resultado = await _apiService.ActualizarComprobanteAsync(Comprobante);

            if (resultado)
            {
                await DisplayAlert("Éxito", "Comprobante actualizado correctamente", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                MensajeError = "No se pudo actualizar el comprobante";
                OnPropertyChanged(nameof(MensajeError));
            }
        }
        catch (Exception ex)
        {
            MensajeError = $"Error: {ex.Message}";
            OnPropertyChanged(nameof(MensajeError));
        }
    }

    private async void btnCancelar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void pickerEstado_SelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        if (selectedIndex != -1)
        {
            Comprobante.Estado = picker.Items[selectedIndex];
            EstadoColor = Comprobante.Estado == "Aprobado" ? Colors.Green : Colors.Red;
            OnPropertyChanged(nameof(Comprobante));
            OnPropertyChanged(nameof(EstadoColor));
        }
    }
}