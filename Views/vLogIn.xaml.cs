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
            // *** NUEVO: Primero autenticar con Supabase para obtener JWT ***
            var resultadoAuth = await _apiService.AutenticarConSupabaseAsync(correo, contraseña);

            if (resultadoAuth != "OK")
            {
                // Si falla la autenticación JWT, intentar el login normal (backwards compatibility)
                Console.WriteLine($"Autenticación JWT falló, intentando login normal: {resultadoAuth}");
            }

            // Tu lógica original de login (sin cambios)
            var usuario = await _apiService.LoginUsuarioAsync(correo, contraseña);

            if (usuario != null)
            {
                // Guardar datos de sesión (tu código original)
                await SecureStorage.SetAsync("user_id", usuario.Id.ToString());
                await SecureStorage.SetAsync("user_name", $"{usuario.nombre} {usuario.apellido}");
                await SecureStorage.SetAsync("user_email", usuario.correo);

                // *** NUEVO: Si no se pudo obtener JWT, crear un token temporal ***
                var existeJWT = await SecureStorage.GetAsync("supabase_jwt");
                if (string.IsNullOrEmpty(existeJWT))
                {
                    // Crear un token temporal usando tu API key (solo para desarrollo)
                    await SecureStorage.SetAsync("supabase_jwt", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNmd3licWF5a3llcmxqcWZxdHprIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDc2MTU1MTcsImV4cCI6MjA2MzE5MTUxN30.0NvvKf7vF_SLMB4OvpxgatIACDStEWu6MR83LCkn5C0");
                    Console.WriteLine("JWT temporal configurado para desarrollo");
                }

                // Tu navegación original (sin cambios)
                await Navigation.PushAsync(new vInicio(usuario));
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
            // Restaurar el botón (tu código original)
            btnIniciarSesion.IsEnabled = true;
            btnIniciarSesion.Text = "Iniciar sesión";
        }
    }

    private async void btnRegistrar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRegistro());
    }
}