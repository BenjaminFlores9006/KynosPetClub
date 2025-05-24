using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;

namespace KynosPetClub.Views;

public partial class vPerfil : ContentPage
{
    private readonly ApiService _apiService;
    private Usuario _usuario;
    public ObservableCollection<Mascota> Mascotas { get; set; } = new();

    // Propiedad para binding del Usuario al BottomNavBar
    public Usuario Usuario => _usuario;

    public vPerfil(Usuario usuario)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _usuario = usuario;
        BindingContext = this;

        CargarDatosUsuario();
        CargarMascotas();
        MostrarBotonesSegunRol();
    }

    private void MostrarBotonesSegunRol()
    {
        // Ocultar todos primero
        btnAdminOpciones.IsVisible = false;
        btnVerCitas.IsVisible = false;

        if (_usuario.RolId == 1) // Administrador
        {
            btnAdminOpciones.IsVisible = true;
            btnAdminOpciones.Text = "⚙️ Opciones de Administrador";
            btnAdminOpciones.BackgroundColor = Color.FromArgb("#6A1B9A"); // Color morado para admin
        }
        else if (_usuario.RolId == 3) // Funcionario
        {
            btnVerCitas.IsVisible = true;
            btnVerCitas.Text = "📅 Ver Citas Asignadas";
            btnVerCitas.BackgroundColor = Color.FromArgb("#0288D1"); // Color azul para funcionario
        }
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

    private async void btnAdminOpciones_Clicked(object sender, EventArgs e)
    {
        try
        {
            var opcion = await DisplayActionSheet(
                "Opciones de Administrador",
                "Cancelar",
                null,
                "✅ Aprobar Pagos",
                "👥 Administrar Usuarios",
                "👨‍⚕️ Asignar Citas a Funcionarios");

            switch (opcion)
            {
                case "✅ Aprobar Pagos":
                    await Navigation.PushAsync(new vAprobarPagos(_usuario));
                    break;
                case "👥 Administrar Usuarios":
                    await Navigation.PushAsync(new vAdministrarUsuarios(_usuario));
                    break;
                case "👨‍⚕️ Asignar Citas a Funcionarios":
                    await Navigation.PushAsync(new vAsignarCitas(_usuario));
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al acceder a opciones: {ex.Message}", "OK");
        }
    }

    private async void btnVerCitas_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Verificar que el usuario sea realmente funcionario
            if (_usuario.RolId != 3)
            {
                await DisplayAlert("Acceso denegado", "No tienes permisos para ver citas asignadas", "OK");
                return;
            }

            await Navigation.PushAsync(new vCitasAsignadas(_usuario));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al acceder a citas: {ex.Message}", "OK");
        }
    }
}