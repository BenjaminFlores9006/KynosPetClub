using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vEditarUsuarioAdmin : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Usuario _admin;
    public Usuario Usuario { get; set; }
    public List<Rol> Roles { get; set; }
    public Rol RolSeleccionado { get; set; }
    public List<Plan> Planes { get; set; }
    public Plan PlanSeleccionado { get; set; }
    public string MensajeError { get; set; }
    public vEditarUsuarioAdmin(Usuario usuario, Usuario admin)
	{
		InitializeComponent();
        _apiService = new ApiService();
        _admin = admin;
        Usuario = usuario;
        BindingContext = this;
        CargarDatos();
    }

    private async void CargarDatos()
    {
        try
        {
            var rolesTask = _apiService.ObtenerRolesAsync();
            var planesTask = _apiService.ObtenerPlanesAsync();

            await Task.WhenAll(rolesTask, planesTask);

            Roles = await rolesTask;
            Planes = await planesTask;

            RolSeleccionado = Roles.FirstOrDefault(r => r.Id == Usuario.RolId);
            PlanSeleccionado = Planes.FirstOrDefault(p => p.Id == Usuario.PlanId);

            OnPropertyChanged(nameof(Roles));
            OnPropertyChanged(nameof(RolSeleccionado));
            OnPropertyChanged(nameof(Planes));
            OnPropertyChanged(nameof(PlanSeleccionado));
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar datos: {ex.Message}";
            OnPropertyChanged(nameof(MensajeError));
        }
    }

    private async void btnGuardar_Clicked(object sender, EventArgs e)
    {
        try
        {
            Usuario.RolId = RolSeleccionado?.Id;
            Usuario.PlanId = PlanSeleccionado?.Id;

            var resultado = await _apiService.ActualizarUsuarioAdminAsync(Usuario);

            if (resultado)
            {
                await DisplayAlert("Éxito", "Usuario actualizado correctamente", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                MensajeError = "No se pudo actualizar el usuario";
                OnPropertyChanged(nameof(MensajeError));
            }
        }
        catch (Exception ex)
        {
            MensajeError = $"Error: {ex.Message}";
            OnPropertyChanged(nameof(MensajeError));
        }
    }

    private async void btnCancelar_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}