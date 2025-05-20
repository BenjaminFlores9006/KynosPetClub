using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace KynosPetClub.Views;

public partial class vPerfil : ContentPage
{
    private readonly ApiService _apiService;
    private Usuario _usuario;
    public ObservableCollection<Mascota> Mascotas { get; set; } = new();
    public vPerfil(Usuario usuario)
	{
		InitializeComponent();
        _apiService = new ApiService();
        _usuario = usuario;
        BindingContext = this;

        CargarDatosUsuario();
        CargarMascotas();
    }

    private void CargarDatosUsuario()
    {
        lblNombre.Text = $"{_usuario.nombre} {_usuario.apellido}";
        lblCorreo.Text = _usuario.correo;
        lblFechaNac.Text = _usuario.fechanac.ToString("dd/MM/yyyy");
    }

    private async void CargarMascotas()
    {
        try
        {
            var mascotas = await _apiService.ObtenerMascotasUsuarioAsync(_usuario.Id.Value);
            if (mascotas != null)
            {
                Mascotas.Clear();
                foreach (var mascota in mascotas)
                {
                    Mascotas.Add(mascota);
                }

                // Actualizar UI
                cvMascotas.ItemsSource = Mascotas;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar mascotas: {ex.Message}", "OK");
        }
    }

    private async void btnEditarPerfil_Clicked(object sender, EventArgs e)
    {
        var nuevoNombre = await DisplayPromptAsync("Editar", "Nuevo nombre:", initialValue: _usuario.nombre);
        var nuevoApellido = await DisplayPromptAsync("Editar", "Nuevo apellido:", initialValue: _usuario.apellido);

        if (!string.IsNullOrEmpty(nuevoNombre) && !string.IsNullOrEmpty(nuevoApellido))
        {
            _usuario.nombre = nuevoNombre;
            _usuario.apellido = nuevoApellido;

            // Llamada REAL al API para actualizar
            var resultado = await _apiService.ActualizarUsuarioAsync(_usuario);

            if (resultado)
            {
                CargarDatosUsuario();
                await DisplayAlert("Éxito", "Perfil actualizado en la base de datos", "OK");
            }
            else
            {
                await DisplayAlert("Error", "No se pudo actualizar el perfil", "OK");
            }
        }
    }

    private async void btnEliminarMascota_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Mascota mascota)
        {
            bool confirmar = await DisplayAlert("Confirmar", $"¿Eliminar a {mascota.Nombre}?", "Sí", "No");
            if (confirmar)
            {
                var resultado = await _apiService.EliminarMascotaAsync(mascota.Id);
                if (resultado)
                {
                    Mascotas.Remove(mascota);
                    await DisplayAlert("Éxito", "Mascota eliminada", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo eliminar la mascota", "OK");
                }
            }
        }
    }

    private async void btnAgregarMascota_Clicked(object sender, EventArgs e)
    {
        var nombre = await DisplayPromptAsync("Nueva Mascota", "Nombre:");
        var especie = await DisplayPromptAsync("Nueva Mascota", "Especie:");
        var raza = await DisplayPromptAsync("Nueva Mascota", "Raza:");

        // Selector de fecha mejorado
        var fechaNac = await SeleccionarFechaNacimiento(DateTime.Now);

        if (!string.IsNullOrEmpty(nombre) && !string.IsNullOrEmpty(especie) && fechaNac != null)
        {
            var nuevaMascota = new Mascota
            {
                Nombre = nombre,
                Especie = especie,
                Raza = raza,
                FechaNacimiento = fechaNac.Value,
                UsuarioId = _usuario.Id.Value
            };

            var resultado = await _apiService.AgregarMascotaAsync(nuevaMascota);
            if (resultado == "OK")
            {
                Mascotas.Add(nuevaMascota);
                await DisplayAlert("Éxito", "Mascota agregada", "OK");
            }
            else
            {
                await DisplayAlert("Error", $"No se pudo agregar: {resultado}", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Debes completar todos los campos", "OK");
        }
    }

    private async Task<DateTime?> SeleccionarFechaNacimiento(DateTime fechaDefault)
    {
        try
        {
            // Primero intentamos con un DatePicker
            var fecha = await DisplayPromptAsync(
                "Fecha de Nacimiento",
                "Ingrese la fecha (dd/MM/yyyy):",
                initialValue: fechaDefault.ToString("dd/MM/yyyy"),
                keyboard: Keyboard.Numeric);

            if (DateTime.TryParseExact(fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaResult))
            {
                return fechaResult;
            }

            await DisplayAlert("Error", "Formato de fecha incorrecto. Use dd/MM/yyyy", "OK");
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async void btnCerrarSesion_Clicked(object sender, EventArgs e)
    {
        SecureStorage.RemoveAll();
        await Navigation.PushAsync(new vLogIn());
    }

    private async void btnEditarMascota_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Mascota mascota)
        {
            var nuevoNombre = await DisplayPromptAsync("Editar Mascota", "Nuevo nombre:", initialValue: mascota.Nombre);
            var nuevaEspecie = await DisplayPromptAsync("Editar Mascota", "Nueva especie:", initialValue: mascota.Especie);
            var nuevaRaza = await DisplayPromptAsync("Editar Mascota", "Nueva raza:", initialValue: mascota.Raza);

            var nuevaFecha = await SeleccionarFechaNacimiento(mascota.FechaNacimiento);

            if (!string.IsNullOrEmpty(nuevoNombre) && !string.IsNullOrEmpty(nuevaEspecie) && nuevaFecha != null)
            {
                mascota.Nombre = nuevoNombre;
                mascota.Especie = nuevaEspecie;
                mascota.Raza = nuevaRaza;
                mascota.FechaNacimiento = nuevaFecha.Value;

                var resultado = await _apiService.ActualizarMascotaAsync(mascota);
                if (resultado)
                {
                    // Actualizar la lista
                    var index = Mascotas.IndexOf(mascota);
                    Mascotas[index] = mascota;
                    await DisplayAlert("Éxito", "Mascota actualizada", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo actualizar la mascota", "OK");
                }
            }
        }
    }
}