using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vAgregarMascota : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Usuario _usuario;
    private string _fotoPath;

    public vAgregarMascota(Usuario usuario)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _usuario = usuario;

        // Establecer fecha por defecto (hace 1 año)
        dtpFechaNacimiento.Date = DateTime.Now.AddYears(-1);
    }

    private async void btnAgregarFoto_Clicked(object sender, EventArgs e)
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
            var localPath = Path.Combine(FileSystem.AppDataDirectory, $"mascota_nueva_{Guid.NewGuid()}.jpg");

            using var sourceStream = await photo.OpenReadAsync();
            using var localStream = File.OpenWrite(localPath);
            await sourceStream.CopyToAsync(localStream);

            _fotoPath = localPath;
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
            var nuevaMascota = new Mascota
            {
                Nombre = txtNombre.Text.Trim(),
                Especie = pickerEspecie.Items[pickerEspecie.SelectedIndex],
                Raza = txtRaza.Text?.Trim() ?? "",
                FechaNacimiento = dtpFechaNacimiento.Date,
                Foto = _fotoPath ?? "",
                UsuarioId = _usuario.Id.Value
            };

            var resultado = await _apiService.AgregarMascotaAsync(nuevaMascota);

            if (resultado == "OK")
            {
                await DisplayAlert("Éxito", "Mascota agregada correctamente", "OK");
                await Navigation.PushAsync(new vPerfil(_usuario));
            }
            else
            {
                lblMensaje.Text = $"Error al agregar mascota: {resultado}";
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
            btnGuardar.Text = "Guardar Mascota";
        }
    }

    private async void btnCancelar_Clicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "Confirmar",
            "¿Deseas cancelar sin guardar?",
            "Sí",
            "No");

        if (confirmar)
        {
            await Navigation.PopAsync();
        }
    }
}