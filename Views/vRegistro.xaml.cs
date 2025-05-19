using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vRegistro : ContentPage
{
    public vRegistro()
    {
        InitializeComponent();
    }

    private async void btnRegistrar_Clicked(object sender, EventArgs e)
    {
        var nuevoUsuario = new Usuario
        {
            nombre = txtNombre.Text,
            apellido = txtApellido.Text,
            fechanac = dtpFechaNacimiento.Date,
            correo = txtCorreo.Text,
            contrase�a = txtPassword.Text
        };

        var api = new ApiService();
        var resultado = await api.RegistrarUsuarioAsync(nuevoUsuario);

        if (resultado == "OK")
            await DisplayAlert("�xito", "Usuario registrado correctamente", "OK");
        else
            await DisplayAlert("Error", resultado, "OK"); // ? Ahora te mostrar� el mensaje exacto de Supabase
    }
}
