using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vRegistro : ContentPage
{
    public vRegistro()
    {
        InitializeComponent();
        dtpFechaNacimiento.Date = DateTime.Today.AddYears(-18);
        dtpFechaNacimiento.MaximumDate = DateTime.Today;
    }

    private async void btnRegistrar_Clicked(object sender, EventArgs e)
    {
        // Validación de campos
        if (string.IsNullOrEmpty(txtNombre.Text) ||
            string.IsNullOrEmpty(txtApellido.Text) ||
            string.IsNullOrEmpty(txtCorreo.Text) ||
            string.IsNullOrEmpty(txtPassword.Text))
        {
            await DisplayAlert("Error", "Por favor completa todos los campos", "OK");
            return;
        }

        // Validación de correo electrónico
        if (!txtCorreo.Text.Contains("@") || !txtCorreo.Text.Contains("."))
        {
            await DisplayAlert("Error", "Por favor ingresa un correo electrónico válido", "OK");
            return;
        }

        // Validación de contraseña
        if (txtPassword.Text.Length < 6)
        {
            await DisplayAlert("Error", "La contraseña debe tener al menos 6 caracteres", "OK");
            return;
        }

        // Validación de coincidencia de contraseñas
        if (txtPassword.Text != txtRepetirPassword.Text)
        {
            await DisplayAlert("Error", "Las contraseñas no coinciden", "OK");
            return;
        }

        // Validación de edad (mayor de 16 años)
        var edad = DateTime.Today.Year - dtpFechaNacimiento.Date.Year;
        if (dtpFechaNacimiento.Date > DateTime.Today.AddYears(-edad)) edad--;

        if (edad < 16)
        {
            await DisplayAlert("Error", "Debes tener al menos 16 años para registrarte", "OK");
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
                await DisplayAlert("Error", resultado.Contains("ERROR") ?
                    resultado : "Ocurrió un error al registrar el usuario", "OK");
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
