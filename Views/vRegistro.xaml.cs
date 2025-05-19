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
        // Validación básica de campos
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
                contraseña = txtPassword.Text
            };

            var api = new ApiService();
            var resultado = await api.RegistrarUsuarioAsync(nuevoUsuario);

            if (resultado == "OK")
            {
                // Mostrar alerta de éxito
                await DisplayAlert("Éxito", "Usuario registrado correctamente", "OK");

                // Volver a la página de login
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
            await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
        }
        finally
        {
            // Restaurar el botón
            btnRegistrar.IsEnabled = true;
            btnRegistrar.Text = "Registrar";
        }
    }
}
