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
        // Validaci�n b�sica de campos
        if (string.IsNullOrEmpty(txtNombre.Text) ||
            string.IsNullOrEmpty(txtApellido.Text) ||
            string.IsNullOrEmpty(txtCorreo.Text) ||
            string.IsNullOrEmpty(txtPassword.Text))
        {
            await DisplayAlert("Error", "Por favor completa todos los campos", "OK");
            return;
        }

        // Mostrar indicador de carga
        btnRegistrar.IsEnabled = false;
        btnRegistrar.Text = "Registrando...";

        try
        {
            // Crear el objeto usuario con los datos del formulario
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
            {
                // Mostrar alerta de �xito
                await DisplayAlert("�xito", "Usuario registrado correctamente", "OK");

                // Volver a la p�gina de login
                await Navigation.PopAsync();
            }
            else
            {
                // Mostrar mensaje de error
                await DisplayAlert("Error", resultado, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Ocurri� un error: {ex.Message}", "OK");
        }
        finally
        {
            // Restaurar el bot�n
            btnRegistrar.IsEnabled = true;
            btnRegistrar.Text = "Registrar";
        }
    }
}
