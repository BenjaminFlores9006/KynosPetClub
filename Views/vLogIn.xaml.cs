using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vLogIn : ContentPage
{
    public vLogIn()
    {
        InitializeComponent();
    }

    private async void btnIniciarSesion_Clicked(object sender, EventArgs e)
    {
        var correo = txtCorreo.Text;
        var contrase�a = txtPassword.Text;

        var api = new ApiService();
        var usuario = await api.LogInUsuarioAsync(correo, contrase�a);

        if (usuario != null)
            await DisplayAlert("Bienvenido", $"Hola {usuario.nombre}!", "OK");
        else
            await DisplayAlert("Error", "Credenciales inv�lidas", "OK");
    }

    private async void btnRegistrar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRegistro());
    }
}
