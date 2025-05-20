using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;

namespace KynosPetClub.Views;

public partial class vHistorial : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Usuario _usuario;
    public ObservableCollection<Reserva> Reservas { get; set; } = new();
    public vHistorial(Usuario usuario)
	{
		InitializeComponent();
        _apiService = new ApiService();
        _usuario = usuario;
        BindingContext = this;

        CargarReservas();
    }

    private async void CargarReservas()
    {
        try
        {
            var reservas = await _apiService.ObtenerReservasUsuarioAsync(_usuario.Id.Value);
            if (reservas != null)
            {
                Reservas.Clear();
                foreach (var reserva in reservas.OrderByDescending(r => r.FechaServicio))
                {
                    Reservas.Add(reserva);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar historial: {ex.Message}", "OK");
        }
    }

    private async void btnFiltroSeleccionado_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            var filtro = button.Text.ToLower();
            var reservas = await _apiService.ObtenerReservasUsuarioAsync(_usuario.Id.Value);

            if (reservas != null)
            {
                Reservas.Clear();
                var reservasFiltradas = filtro switch
                {
                    "todos" => reservas,
                    "completados" => reservas.Where(r => r.Estado == "completado"),
                    "cancelados" => reservas.Where(r => r.Estado == "cancelado"),
                    _ => reservas
                };

                foreach (var reserva in reservasFiltradas.OrderByDescending(r => r.FechaServicio))
                {
                    Reservas.Add(reserva);
                }
            }
        }
    }
}