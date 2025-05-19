using KynosPetClub.Models;

namespace KynosPetClub.Views;

public partial class vLogIn : ContentPage
{
	public vLogIn()
	{
		InitializeComponent();
	}

    private async void Button_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRegistro());
    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        string correoIngresado = txtCorreo.Text;
        string passwordIngresado = txtPassword.Text;

        if (correoIngresado == UsuarioTemporal.Correo && passwordIngresado == UsuarioTemporal.Password)
        {
            await Navigation.PushAsync(new vInicio());
        }
        else
        {
            await DisplayAlert("Error", "Correo o contraseña incorrectos", "OK");
        }
    }
}