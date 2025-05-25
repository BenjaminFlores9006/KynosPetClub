using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vInicio : ContentPage
{
    private Usuario _usuarioActual;
    private readonly ApiService _apiService;
    public List<Servicio> ServiciosDisponibles { get; set; }

    // Propiedad para binding del Usuario al BottomNavBar
    public Usuario Usuario => _usuarioActual;

    public vInicio(Usuario usuario)
    {
        InitializeComponent();
        _usuarioActual = usuario;
        _apiService = new ApiService();
        BindingContext = this;
        lblSaludo.Text = $"Hola, {_usuarioActual.nombre}";
        CargarServicios();
    }

    private async void CargarServicios()
    {
        try
        {
            var servicios = await _apiService.ObtenerServiciosAsync();
            if (servicios != null && servicios.Any())
            {
                ServiciosDisponibles = servicios;
                // Aquí podrías bindear los servicios a una CollectionView
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar servicios: {ex.Message}", "OK");
        }
    }

    // Botones de acceso rápido en la parte superior
    private async void btnAccesoRapidoHistorial_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vHistorial(_usuarioActual));
    }

    private async void btnAccesoRapidoSeguimiento_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vReserva(_usuarioActual));
    }

    private async void btnAccesoRapidoPlanes_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vPlanes(_usuarioActual));
    }

    // Evento para cuando se toca un servicio
    private async void btnServicios_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is Frame frame)
        {
            try
            {
                // Verificar si el usuario tiene mascotas antes de navegar a reservas
                var mascotas = await _apiService.ObtenerMascotasUsuarioAsync(_usuarioActual.Id.Value);
                if (mascotas == null || !mascotas.Any())
                {
                    await DisplayAlert("Advertencia",
                        "No tienes mascotas registradas. Por favor agrega una mascota primero.", "OK");
                    // Navegar a perfil para agregar mascota
                    await Navigation.PushAsync(new vPerfil(_usuarioActual));
                    return;
                }
                // Navegar a vHacerReserva en lugar de vReserva
                await Navigation.PushAsync(new vHacerReserva(_usuarioActual));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al navegar: {ex.Message}", "OK");
            }
        }
    }
}