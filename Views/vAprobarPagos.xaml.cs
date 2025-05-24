using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;

namespace KynosPetClub.Views;

public partial class vAprobarPagos : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Usuario _usuario;
    public ObservableCollection<Comprobante> Comprobantes { get; set; } = new();
    public vAprobarPagos(Usuario usuario)
	{
		InitializeComponent();
        _apiService = new ApiService();
        _usuario = usuario;
        BindingContext = this;
        CargarComprobantes();
    }

    private async void CargarComprobantes()
    {
        try
        {
            var comprobantes = await _apiService.ObtenerComprobantesPendientesAsync();
            Comprobantes.Clear();
            foreach (var comp in comprobantes)
            {
                Comprobantes.Add(comp);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar comprobantes: {ex.Message}", "OK");
        }
    }

    private async void btnVolver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void btnRevisarPagos_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Comprobante comprobante)
        {
            await Navigation.PushAsync(new vDetalleComprobante(comprobante, _usuario));
        }
    }
}