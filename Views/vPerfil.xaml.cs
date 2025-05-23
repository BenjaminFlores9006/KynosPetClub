using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Recargar datos cuando la página aparece (por si se editó algo)
        await CargarMascotas();
        CargarDatosUsuario();
    }

    private void CargarDatosUsuario()
    {
        lblSaludo.Text = $"¡Hola, {_usuario.nombre}!";
        lblNombre.Text = $"{_usuario.nombre} {_usuario.apellido}";
        lblCorreo.Text = _usuario.correo;
        lblFechaNac.Text = $"Nacimiento: {_usuario.fechanac:dd/MM/yyyy}";
    }

    private async Task CargarMascotas()
    {
        try
        {
            if (_usuario.Id.HasValue)
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
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar mascotas: {ex.Message}", "OK");
        }
    }

    private async void btnEditarPerfil_Clicked(object sender, EventArgs e)
    {
        // Verificar que solo el usuario logueado pueda editar
        if (_usuario?.Id != null)
        {
            await Navigation.PushAsync(new vEditarUsuario(_usuario));
        }
        else
        {
            await DisplayAlert("Error", "No se puede editar el perfil en este momento", "OK");
        }
    }

    private async void btnEditarMascota_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Mascota mascota)
        {
            // Verificar que la mascota pertenece al usuario logueado
            if (mascota.UsuarioId == _usuario.Id)
            {
                await Navigation.PushAsync(new vEditarMascota(mascota, _usuario));
            }
            else
            {
                await DisplayAlert("Error", "No tienes permisos para editar esta mascota", "OK");
            }
        }
    }

    private async void btnEliminarMascota_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Mascota mascota)
        {
            // Verificar que la mascota pertenece al usuario logueado
            if (mascota.UsuarioId != _usuario.Id)
            {
                await DisplayAlert("Error", "No tienes permisos para eliminar esta mascota", "OK");
                return;
            }

            bool confirmar = await DisplayAlert(
                "Confirmar eliminación",
                $"¿Estás seguro de que deseas eliminar a {mascota.Nombre}?",
                "Sí, eliminar",
                "Cancelar");

            if (confirmar)
            {
                try
                {
                    var resultado = await _apiService.EliminarMascotaAsync(mascota.Id);
                    if (resultado)
                    {
                        Mascotas.Remove(mascota);
                        await DisplayAlert("Éxito", $"{mascota.Nombre} ha sido eliminada", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo eliminar la mascota", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error al eliminar: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void btnAgregarMascota_Clicked(object sender, EventArgs e)
    {
        if (_usuario?.Id != null)
        {
            await Navigation.PushAsync(new vAgregarMascota(_usuario));
        }
        else
        {
            await DisplayAlert("Error", "No se puede agregar mascota en este momento", "OK");
        }
    }

    private async void btnCerrarSesion_Clicked(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "Cerrar Sesión",
            "¿Estás seguro de que deseas cerrar sesión?",
            "Sí",
            "No");

        if (confirmar)
        {
            try
            {
                // Limpiar datos almacenados
                SecureStorage.RemoveAll();

                // Navegar a la página de login y limpiar la pila de navegación
                Application.Current.MainPage = new NavigationPage(new vLogIn());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cerrar sesión: {ex.Message}", "OK");
            }
        }
    }
}