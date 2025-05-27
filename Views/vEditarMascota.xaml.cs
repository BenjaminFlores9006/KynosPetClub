using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vEditarMascota : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Usuario _usuario;
    private Mascota _mascota;
    private string _nuevaFotoPath;

    public vEditarMascota(Mascota mascota, Usuario usuario)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _mascota = mascota;
        _usuario = usuario;

        CargarDatosMascota();
    }

    private void CargarDatosMascota()
    {
        txtNombre.Text = _mascota.Nombre;
        txtRaza.Text = _mascota.Raza;
        dtpFechaNacimiento.Date = _mascota.FechaNacimiento;

        // Seleccionar la especie en el picker
        var especies = new[] { "Perro", "Gato", "Conejo", "Hámster", "Ave", "Otro" };
        for (int i = 0; i < especies.Length; i++)
        {
            if (especies[i].Equals(_mascota.Especie, StringComparison.OrdinalIgnoreCase))
            {
                pickerEspecie.SelectedIndex = i;
                break;
            }
        }

        // Cargar foto si existe
        if (!string.IsNullOrEmpty(_mascota.Foto))
        {
            imgMascota.Source = _mascota.Foto;
        }
    }

    private async void btnCambiarFoto_Clicked(object sender, EventArgs e)
    {
        try
        {
            var accion = await DisplayActionSheet(
                "Seleccionar foto",
                "Cancelar",
                null,
                "Tomar foto",
                "Elegir de galería");

            if (accion == "Tomar foto")
            {
                await TomarFotoConCamara();
            }
            else if (accion == "Elegir de galería")
            {
                await ElegirFotoDeGaleria();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al seleccionar foto: {ex.Message}", "OK");
        }
    }

    private async Task TomarFotoConCamara()
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    await ProcesarFotoSeleccionada(photo);
                }
            }
            else
            {
                await DisplayAlert("Error", "La cámara no está disponible", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al tomar foto: {ex.Message}", "OK");
        }
    }

    private async Task ElegirFotoDeGaleria()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo != null)
            {
                await ProcesarFotoSeleccionada(photo);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al elegir foto: {ex.Message}", "OK");
        }
    }

    private async Task ProcesarFotoSeleccionada(FileResult photo)
    {
        try
        {
            // Guardar la foto en el directorio local de la app
            var localPath = Path.Combine(FileSystem.AppDataDirectory, $"mascota_{_mascota.Id}_{Guid.NewGuid()}.jpg");

            using var sourceStream = await photo.OpenReadAsync();
            using var localStream = File.OpenWrite(localPath);
            await sourceStream.CopyToAsync(localStream);

            _nuevaFotoPath = localPath;
            imgMascota.Source = ImageSource.FromFile(localPath);

            lblMensaje.Text = "Foto seleccionada correctamente";
            lblMensaje.TextColor = Colors.Green;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al procesar foto: {ex.Message}", "OK");
        }
    }

    private async void btnGuardar_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNombre.Text))
        {
            lblMensaje.Text = "El nombre es obligatorio";
            lblMensaje.TextColor = Colors.Red;
            return;
        }

        if (pickerEspecie.SelectedIndex == -1)
        {
            lblMensaje.Text = "Selecciona una especie";
            lblMensaje.TextColor = Colors.Red;
            return;
        }

        btnGuardar.IsEnabled = false;
        btnGuardar.Text = "Guardando...";
        lblMensaje.Text = "";

        try
        {
            // Actualizar los datos de la mascota
            _mascota.Nombre = txtNombre.Text.Trim();
            _mascota.Especie = pickerEspecie.Items[pickerEspecie.SelectedIndex];
            _mascota.Raza = txtRaza.Text?.Trim() ?? "";
            _mascota.FechaNacimiento = dtpFechaNacimiento.Date;

            // Si hay una nueva foto, actualizarla
            if (!string.IsNullOrEmpty(_nuevaFotoPath))
            {
                _mascota.Foto = _nuevaFotoPath;
            }

            // Llamar al API para actualizar
            var resultado = await _apiService.ActualizarMascotaAsync(_mascota);

            if (resultado)
            {
                await DisplayAlert("Éxito", "Mascota actualizada correctamente", "OK");
                await Navigation.PushAsync(new vPerfil(_usuario));
            }
            else
            {
                lblMensaje.Text = "Error al actualizar la mascota";
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

    private async void btnCancelar_Clicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "Confirmar",
            "¿Deseas cancelar los cambios?",
            "Sí",
            "No");

        if (confirmar)
        {
            await Navigation.PopAsync();
        }
    }
}