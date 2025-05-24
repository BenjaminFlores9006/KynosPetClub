using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KynosPetClub.Views;

public partial class vAdministrarUsuarios : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Usuario _admin;
    public ObservableCollection<UsuarioViewModel> Usuarios { get; set; } = new();
    private List<UsuarioViewModel> _todosUsuarios = new();
    public vAdministrarUsuarios(Usuario admin)
	{
		InitializeComponent();
        _apiService = new ApiService();
        _admin = admin;
        BindingContext = this;

        if (_admin.RolId != 1)
        {
            DisplayAlert("Error", "Acceso no autorizado", "OK");
            Navigation.PopAsync();
            return;
        }
        CargarUsuarios();
    }

    private async void CargarUsuarios()
    {
        try
        {
            IsBusy = true;

            // Obtener datos en paralelo
            var usuariosTask = _apiService.ObtenerTodosUsuariosAsync();
            var rolesTask = _apiService.ObtenerRolesAsync();
            var planesTask = _apiService.ObtenerPlanesAsync();

            await Task.WhenAll(usuariosTask, rolesTask, planesTask);

            var usuarios = await usuariosTask;
            var roles = await rolesTask;
            var planes = await planesTask;

            Usuarios.Clear();
            foreach (var usuario in usuarios)
            {
                var rol = roles.FirstOrDefault(r => r.Id == usuario.RolId);
                var plan = planes.FirstOrDefault(p => p.Id == usuario.PlanId);

                Usuarios.Add(new UsuarioViewModel
                {
                    Id = usuario.Id,
                    nombre = usuario.nombre,
                    apellido = usuario.apellido,
                    correo = usuario.correo,
                    RolNombre = rol?.NombreRol ?? "Sin rol",
                    PlanNombre = plan?.Nombre ?? "Sin plan",
                    UsuarioOriginal = usuario
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar usuarios: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void btnEditarUsuario_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is UsuarioViewModel usuario)
        {
            await Navigation.PushAsync(new vEditarUsuarioAdmin(usuario.UsuarioOriginal, _admin));
        }
    }

    private async void btnVolver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchTerm = e.NewTextValue?.ToLower() ?? string.Empty;

        Usuarios.Clear();

        foreach (var user in _todosUsuarios.Where(u =>
            u.nombre.ToLower().Contains(searchTerm) ||
            u.correo.ToLower().Contains(searchTerm) ||
            u.RolNombre.ToLower().Contains(searchTerm)))
        {
            Usuarios.Add(user);
        }
    }

    public class UsuarioViewModel
    {
        public int? Id { get; set; }
        public string nombre { get; set; }
        public string apellido { get; set; }
        public string correo { get; set; }
        public int? RolId { get; set; }
        public string RolNombre { get; set; }
        public string PlanNombre { get; set; }
        public Usuario UsuarioOriginal { get; set; }
    }
}