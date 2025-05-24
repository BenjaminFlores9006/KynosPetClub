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
        var contrase�a = txtPassword.Text;

        // Validaci�n b�sica
        if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contrase�a))
        {
            await DisplayAlert("Error", "Por favor ingresa tu correo y contrase�a", "OK");
            return;
        }

        // Validaci�n de formato de correo
        if (!correo.Contains("@") || !correo.Contains("."))
        {
            await DisplayAlert("Error", "Por favor ingresa un correo electr�nico v�lido", "Ok");
            return;
        }

        // Mostrar indicador de carga
        btnIniciarSesion.IsEnabled = false;
        btnIniciarSesion.Text = "Iniciando sesi�n...";

        try
        {
            // *** NUEVO: Primero autenticar con Supabase para obtener JWT ***
            var resultadoAuth = await _apiService.AutenticarConSupabaseAsync(correo, contrase�a);

            if (resultadoAuth != "OK")
            {
                // Si falla la autenticaci�n JWT, intentar el login normal (backwards compatibility)
                Console.WriteLine($"Autenticaci�n JWT fall�, intentando login normal: {resultadoAuth}");
            }

            // Tu l�gica original de login (sin cambios)
            var usuario = await _apiService.LoginUsuarioAsync(correo, contrase�a);

            if (usuario != null)
            {
                // Guardar datos de sesi�n (tu c�digo original)
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

                // Tu navegaci�n original (sin cambios)
                await Navigation.PushAsync(new vInicio(usuario));
                Navigation.RemovePage(this);
            }
            else
            {
                await DisplayAlert("Error", "Credenciales inv�lidas. Por favor verifica tu correo y contrase�a.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurri� un error: {ex.Message}", "OK");
        }
        finally
        {
            // Restaurar el bot�n (tu c�digo original)
            btnIniciarSesion.IsEnabled = true;
            btnIniciarSesion.Text = "Iniciar sesi�n";
        }
    }

    private async void btnRegistrar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRegistro());
    }
}