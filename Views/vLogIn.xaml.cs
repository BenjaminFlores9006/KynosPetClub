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

        try
        {
            // Agregamos un mensaje de depuración
            Console.WriteLine($"Intentando iniciar sesión con correo: {correo}");

            var usuario = await _apiService.LogInUsuarioAsync(correo, contraseña);

            if (usuario != null)
            {
                Console.WriteLine($"Login exitoso para: {usuario.nombre} {usuario.apellido}");

                // Redirigir a la página de inicio
                await Navigation.PushAsync(new vInicio(usuario));

                // Opcional: Limpiar la página de login de la pila de navegación
                Navigation.RemovePage(this);
            }
            else
            {
                Console.WriteLine("Login fallido: usuario no encontrado");
                await DisplayAlert("Error", "Credenciales inválidas. Por favor verifica tu correo y contraseña.", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Excepción durante login: {ex.Message}");
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