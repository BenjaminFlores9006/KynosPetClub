using KynosPetClub.Models;

namespace KynosPetClub.Views;

public partial class vRegistro : ContentPage
{
	public vRegistro()
	{
		InitializeComponent();
	}

    private async void Button_Clicked(object sender, EventArgs e)
    {
        lblMensaje.Text = "";

        if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
            string.IsNullOrWhiteSpace(txtApellido.Text) ||
            string.IsNullOrWhiteSpace(txtCorreo.Text) ||
            string.IsNullOrWhiteSpace(txtPassword.Text) ||
            string.IsNullOrWhiteSpace(txtRepetirPassword.Text))
        {
            lblMensaje.TextColor = Colors.Red;
            lblMensaje.Text = "Todos los campos son obligatorios.";
            return;
        }

        if (txtPassword.Text != txtRepetirPassword.Text)
        {
            lblMensaje.TextColor = Colors.Red;
            lblMensaje.Text = "Las contraseñas no coinciden.";
            return;
        }

        // Guardar temporalmente el usuario
        UsuarioTemporal.Correo = txtCorreo.Text.Trim();
        UsuarioTemporal.Password = txtPassword.Text;

        lblMensaje.TextColor = Colors.Green;
        lblMensaje.Text = "Usuario registrado correctamente.";

        await Task.Delay(1000);

        await Navigation.PopAsync(); // Regresar al login
    }
}
