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

    private async void btnMascotas_Clicked(object sender, EventArgs e)
    {
        // Aquí iría la navegación a la página de mascotas
        // Por ejemplo:
        // await Navigation.PushAsync(new vMascotas(_usuarioActual));

        // Por ahora, solo mostraremos un mensaje
        await DisplayAlert("Próximamente", "La gestión de mascotas estará disponible pronto", "OK");
    }

    // También podrías agregar handlers para los botones del menú inferior
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Configurar eventos de los botones del menú inferior si es necesario
        btnHome.Clicked += (sender, e) => { /* Acción para Home */ };
        btnCalendar.Clicked += (sender, e) => { /* Acción para Calendar */ };
        btnCart.Clicked += (sender, e) => { /* Acción para Cart */ };
        btnUser.Clicked += (sender, e) => { /* Acción para User */ };
    }
}