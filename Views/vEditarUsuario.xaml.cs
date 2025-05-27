using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vEditarUsuario : ContentPage
{
    private readonly ApiService _apiService;
    private Usuario _usuario;

    public vEditarUsuario(Usuario usuario)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _usuario = usuario;

        CargarDatosUsuario();
    }

    private void CargarDatosUsuario()
    {
        txtNombre.Text = _usuario.nombre;
        txtApellido.Text = _usuario.apellido;
        txtCorreo.Text = _usuario.correo;
        dtpFechaNacimiento.Date = _usuario.fechanac;
    }

    private async void btnGuardar_Clicked(object sender, EventArgs e)
    {
        // Validar campos obligatorios
        if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
            string.IsNullOrWhiteSpace(txtApellido.Text))
        {
            lblMensaje.Text = "Nombre y apellido son obligatorios";
            lblMensaje.TextColor = Colors.Red;
            return;
        }

        // Validar contrase�as si se est�n cambiando
        if (!string.IsNullOrWhiteSpace(txtPasswordActual.Text) ||
            !string.IsNullOrWhiteSpace(txtPasswordNueva.Text) ||
            !string.IsNullOrWhiteSpace(txtPasswordConfirmar.Text))
        {
            if (!ValidarCambioPassword())
                return;
        }

        btnGuardar.IsEnabled = false;
        btnGuardar.Text = "Guardando...";
        lblMensaje.Text = "";

        try
        {
            // Actualizar datos b�sicos
            _usuario.nombre = txtNombre.Text.Trim();
            _usuario.apellido = txtApellido.Text.Trim();
            _usuario.fechanac = dtpFechaNacimiento.Date;

            // Si se est� cambiando la contrase�a
            if (!string.IsNullOrWhiteSpace(txtPasswordNueva.Text))
            {
                _usuario.contrase�a = txtPasswordNueva.Text;
            }

            // Llamar al API para actualizar
            var resultado = await _apiService.ActualizarUsuarioAsync(_usuario);

            if (resultado)
            {
                await DisplayAlert("�xito", "Perfil actualizado correctamente", "OK");
                await Navigation.PushAsync(new vPerfil(_usuario));
            }
            else
            {
                lblMensaje.Text = "Error al actualizar el perfil";
                lblMensaje.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            lblMensaje.Text = $"Error: {ex.Message}";
            lblMensaje.TextColor = Colors.Red;
        }
        finally
        {
            btnGuardar.IsEnabled = true;
            btnGuardar.Text = "Guardar Cambios";
        }
    }

    private bool ValidarCambioPassword()
    {
        // Verificar contrase�a actual
        if (txtPasswordActual.Text != _usuario.contrase�a)
        {
            lblMensaje.Text = "La contrase�a actual es incorrecta";
            lblMensaje.TextColor = Colors.Red;
            return false;
        }

        // Validar nueva contrase�a
        if (string.IsNullOrWhiteSpace(txtPasswordNueva.Text))
        {
            lblMensaje.Text = "Ingresa la nueva contrase�a";
            lblMensaje.TextColor = Colors.Red;
            return false;
        }

        if (txtPasswordNueva.Text.Length < 6)
        {
            lblMensaje.Text = "La nueva contrase�a debe tener al menos 6 caracteres";
            lblMensaje.TextColor = Colors.Red;
            return false;
        }

        if (txtPasswordNueva.Text != txtPasswordConfirmar.Text)
        {
            lblMensaje.Text = "Las contrase�as no coinciden";
            lblMensaje.TextColor = Colors.Red;
            return false;
        }

        return true;
    }

    private async void btnCancelar_Clicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "Confirmar",
            "�Deseas cancelar los cambios?",
            "S�",
            "No");

        if (confirmar)
        {
            await Navigation.PopAsync();
        }
    }
}