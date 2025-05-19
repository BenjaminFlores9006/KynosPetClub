using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vLogIn : ContentPage
{
    private readonly ApiService _apiService;

    public vLogIn()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void btnIniciarSesion_Clicked(object sender, EventArgs e)
    {
        var correo = txtCorreo.Text?.Trim();
        var contraseña = txtPassword.Text;

        // Validación básica
        if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contraseña))
        {
            await DisplayAlert("Error", "Por favor ingresa tu correo y contraseña", "OK");
            return;
        }

        // Mostrar indicador de carga
        btnIniciarSesion.IsEnabled = false;
        btnIniciarSesion.Text = "Iniciando sesión...";

        if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contraseña))
        {
            await DisplayAlert("Error", "Por favor ingresa tu correo y contraseña", "Ok");
            return;
        }

        // Validación de formato de correo
        if (!correo.Contains("@") || !correo.Contains("."))
        {
            await DisplayAlert("Error", "Por favor ingresa un correo electrónico válido", "Ok");
            return;
        }

        // Mostrar indicador de carga
        btnIniciarSesion.IsEnabled = false;
        btnIniciarSesion.Text = "Iniciando sesión...";

        try
        {
            var usuario = await _apiService.LoginUsuarioAsync(correo, contraseña);

            if (usuario != null)
            {
                // Guardar datos de sesión (podría usarse SecureStorage)
                await SecureStorage.SetAsync("user_id", usuario.Id.ToString());
                await SecureStorage.SetAsync("user_name", $"{usuario.nombre} {usuario.apellido}");
                await SecureStorage.SetAsync("user_email", usuario.correo);

                // Redirigir a la página de inicio
                await Navigation.PushAsync(new vInicio(usuario));

                // Limpiar la página de login de la pila de navegación
                Navigation.RemovePage(this);
            }
            else
            {
                await DisplayAlert("Error", "Credenciales inválidas. Por favor verifica tu correo y contraseña.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
        }
        finally
        {
            // Restaurar el botón
            btnIniciarSesion.IsEnabled = true;
            btnIniciarSesion.Text = "Iniciar sesión";
        }
    }

    private async void btnRegistrar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRegistro());
    }
}