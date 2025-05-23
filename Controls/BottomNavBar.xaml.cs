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
            try
            {
                // Verificar si ya estamos en la página de Inicio
                var currentPage = Application.Current?.MainPage?.Navigation?.NavigationStack?.LastOrDefault();
                if (!(currentPage is vInicio))
                {
                    await Application.Current.MainPage.Navigation.PushAsync(new vInicio(Usuario));
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al navegar a Inicio: {ex.Message}", "OK");
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
                    await Application.Current.MainPage.Navigation.PushAsync(new vPerfil(Usuario));
                    return;
                }

                // Navegar a la vista de reservas
                await Application.Current.MainPage.Navigation.PushAsync(new vReserva(Usuario));
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
            try
            {
                // Navegar a una vista específica para mostrar pagos pendientes
                await Application.Current.MainPage.Navigation.PushAsync(new vPagosPendientes(Usuario));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al navegar a Pagos: {ex.Message}", "OK");
            }
        }
    }

    private async void btnMembresia_Clicked(object sender, EventArgs e)
    {
        if (Usuario != null)
        {
            try
            {
                await Application.Current.MainPage.Navigation.PushAsync(new vPlanes(Usuario));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al navegar a Membresía: {ex.Message}", "OK");
            }
        }
    }

    private async void btnHistorial_Clicked(object sender, EventArgs e)
    {
        if (Usuario != null)
        {
            try
            {
                await Application.Current.MainPage.Navigation.PushAsync(new vHistorial(Usuario));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al navegar a Historial: {ex.Message}", "OK");
            }
        }
    }

    private async void btnPerfil_Clicked(object sender, EventArgs e)
    {
        if (Usuario != null)
        {
            try
            {
                await Application.Current.MainPage.Navigation.PushAsync(new vPerfil(Usuario));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al navegar a Perfil: {ex.Message}", "OK");
            }
        }
    }
}