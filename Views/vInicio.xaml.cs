using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vInicio : ContentPage
{
    private Usuario _usuarioActual;
    private readonly ApiService _apiService;

    public vInicio()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    public vInicio(Usuario usuario)
    {
        InitializeComponent();
        _usuarioActual = usuario;
        _apiService = new ApiService();

        if (_usuarioActual != null)
        {
            lblSaludo.Text = $"Hola, {_usuarioActual.nombre}";
        }

        CargarServicios();
    }

    private async void CargarServicios()
    {
        try
        {
            var servicios = await _apiService.ObtenerServiciosAsync();

            if (servicios != null && servicios.Any())
            {
                // Aquí podrías bindear los servicios a una lista o colección
                // Por ahora solo mostramos en consola
                foreach (var servicio in servicios)
                {
                    Console.WriteLine($"Servicio: {servicio.Nombre} - ${servicio.Precio}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar servicios: {ex.Message}");
        }
    }

    // También podrías agregar handlers para los botones del menú inferior
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Configurar eventos de los botones del menú inferior si es necesario
        btnInicio.Clicked += (sender, e) => { /* Acción para Home */ };
        btnReserva.Clicked += (sender, e) => { /* Acción para Calendar */ };
        btnPagos.Clicked += (sender, e) => { /* Acción para Cart */ };
        btnPerfil.Clicked += (sender, e) => { /* Acción para User */ };
    }

    private async void btnInicio_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vInicio(_usuarioActual));
    }

    private async void btnReserva_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vReserva());
    }

    private async void btnPagos_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vPagos());
    }

    private async void btnPerfil_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vPerfil(_usuarioActual));
    }
}