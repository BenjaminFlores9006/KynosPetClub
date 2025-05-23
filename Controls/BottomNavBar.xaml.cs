using KynosPetClub.Models;
using KynosPetClub.Services;
using KynosPetClub.Views;

namespace KynosPetClub.Controls;

public partial class BottomNavBar : ContentView
{
    // Propiedad bindable para el usuario
    public static readonly BindableProperty UsuarioProperty =
        BindableProperty.Create(nameof(Usuario), typeof(Usuario), typeof(BottomNavBar), null);

    public Usuario Usuario
    {
        get => (Usuario)GetValue(UsuarioProperty);
        set => SetValue(UsuarioProperty, value);
    }

    private readonly ApiService _apiService;

    public BottomNavBar()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void btnInicio_Clicked(object sender, EventArgs e)
    {
        if (Usuario != null)
        {
            // Verificar si ya estamos en la página de Inicio
            if (!(Application.Current?.MainPage?.Navigation?.NavigationStack?.LastOrDefault() is vInicio))
            {
                await Application.Current.MainPage.Navigation.PushAsync(new Views.vInicio(Usuario));
            }
        }
    }

    private async void btnReservar_Clicked(object sender, EventArgs e)
    {
        if (Usuario != null)
        {
            try
            {
                // Verificar si el usuario tiene mascotas antes de navegar
                var mascotas = await _apiService.ObtenerMascotasUsuarioAsync(Usuario.Id.Value);
                if (mascotas == null || !mascotas.Any())
                {
                    await Application.Current.MainPage.DisplayAlert("Advertencia",
                        "No tienes mascotas registradas. Por favor agrega una mascota primero.", "OK");
                    // Navegar a perfil para agregar mascota
                    await Application.Current.MainPage.Navigation.PushAsync(new Views.vPerfil(Usuario));
                    return;
                }

                // Navegar a la vista de reservas (ahora solo necesita el usuario)
                await Application.Current.MainPage.Navigation.PushAsync(new Views.vReserva(Usuario));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al verificar mascotas: {ex.Message}", "OK");
            }
        }
    }

    private async void btnPagos_Clicked(object sender, EventArgs e)
    {
        if (Usuario != null)
        {
            // Crear un servicio temporal para mostrar los pagos
            var servicioTemp = new Servicio { Nombre = "Consulta General", Precio = 50 };
            var mascotaTemp = new Mascota { Nombre = "Sin especificar" };
            await Application.Current.MainPage.Navigation.PushAsync(new Views.vPagos(Usuario, servicioTemp, mascotaTemp, DateTime.Now));
        }
    }

    private async void btnMembresia_Clicked(object sender, EventArgs e)
    {
        if (Usuario != null)
        {
            await Application.Current.MainPage.Navigation.PushAsync(new Views.vPlanes(Usuario));
        }
    }

    private async void btnHistorial_Clicked(object sender, EventArgs e)
    {
        if (Usuario != null)
        {
            await Application.Current.MainPage.Navigation.PushAsync(new Views.vHistorial(Usuario));
        }
    }

    private async void btnPerfil_Clicked(object sender, EventArgs e)
    {
        if (Usuario != null)
        {
            await Application.Current.MainPage.Navigation.PushAsync(new Views.vPerfil(Usuario));
        }
    }
}