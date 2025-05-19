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

        // Mostrar indicador de carga
        btnIniciarSesion.IsEnabled = false;
        btnIniciarSesion.Text = "Iniciando sesi�n...";

        if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contrase�a))
        {
            await DisplayAlert("Error", "Por favor ingresa tu correo y contrase�a", "Ok");
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
            var usuario = await _apiService.LoginUsuarioAsync(correo, contrase�a);

            if (usuario != null)
            {
                // Guardar datos de sesi�n (podr�a usarse SecureStorage)
                await SecureStorage.SetAsync("user_id", usuario.Id.ToString());
                await SecureStorage.SetAsync("user_name", $"{usuario.nombre} {usuario.apellido}");
                await SecureStorage.SetAsync("user_email", usuario.correo);

                // Redirigir a la p�gina de inicio
                await Navigation.PushAsync(new vInicio(usuario));

                // Limpiar la p�gina de login de la pila de navegaci�n
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
            // Restaurar el bot�n
            btnIniciarSesion.IsEnabled = true;
            btnIniciarSesion.Text = "Iniciar sesi�n";
        }
    }

    private async void btnRegistrar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRegistro());
    }
}