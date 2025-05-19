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

        try
        {
            // Agregamos un mensaje de depuraci�n
            Console.WriteLine($"Intentando iniciar sesi�n con correo: {correo}");

            var usuario = await _apiService.LogInUsuarioAsync(correo, contrase�a);

            if (usuario != null)
            {
                Console.WriteLine($"Login exitoso para: {usuario.nombre} {usuario.apellido}");

                // Redirigir a la p�gina de inicio
                await Navigation.PushAsync(new vInicio(usuario));

                // Opcional: Limpiar la p�gina de login de la pila de navegaci�n
                Navigation.RemovePage(this);
            }
            else
            {
                Console.WriteLine("Login fallido: usuario no encontrado");
                await DisplayAlert("Error", "Credenciales inv�lidas. Por favor verifica tu correo y contrase�a.", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Excepci�n durante login: {ex.Message}");
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