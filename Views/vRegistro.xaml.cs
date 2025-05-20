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
        // Validaci�n de campos
        if (string.IsNullOrEmpty(txtNombre.Text) ||
            string.IsNullOrEmpty(txtApellido.Text) ||
            string.IsNullOrEmpty(txtCorreo.Text) ||
            string.IsNullOrEmpty(txtPassword.Text))
        {
            await DisplayAlert("Error", "Por favor completa todos los campos", "OK");
            return;
        }

        // Validaci�n de correo electr�nico
        if (!txtCorreo.Text.Contains("@") || !txtCorreo.Text.Contains("."))
        {
            await DisplayAlert("Error", "Por favor ingresa un correo electr�nico v�lido", "OK");
            return;
        }

        // Validaci�n de contrase�a
        if (txtPassword.Text.Length < 6)
        {
            await DisplayAlert("Error", "La contrase�a debe tener al menos 6 caracteres", "OK");
            return;
        }

        // Validaci�n de coincidencia de contrase�as
        if (txtPassword.Text != txtRepetirPassword.Text)
        {
            await DisplayAlert("Error", "Las contrase�as no coinciden", "OK");
            return;
        }

        // Validaci�n de edad (mayor de 16 a�os)
        var edad = DateTime.Today.Year - dtpFechaNacimiento.Date.Year;
        if (dtpFechaNacimiento.Date > DateTime.Today.AddYears(-edad)) edad--;

        if (edad < 16)
        {
            await DisplayAlert("Error", "Debes tener al menos 16 a�os para registrarte", "OK");
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
                await DisplayAlert("Error", resultado.Contains("ERROR") ?
                    resultado : "Ocurri� un error al registrar el usuario", "OK");
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
