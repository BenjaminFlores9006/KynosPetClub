using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vPlanes : ContentPage
{
    private readonly Usuario _usuario;
    private readonly ApiService _apiService;
    public List<Plan> PlanesDisponibles { get; set; }
    public vPlanes(Usuario usuario)
	{
		InitializeComponent();
        _usuario = usuario;
        _apiService = new ApiService();
        BindingContext = this;

        CargarPlanes();
    }

    private async void CargarPlanes()
    {
        try
        {
            var planes = await _apiService.ObtenerPlanesAsync();
            if (planes != null && planes.Any())
            {
                PlanesDisponibles = planes;
                // Actualizar UI con los planes
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar planes: {ex.Message}", "OK");
        }
    }

    private async void btnPlan1_Clicked(object sender, EventArgs e)
    {
        if (PlanesDisponibles != null && PlanesDisponibles.Count > 0)
        {
            await Navigation.PushAsync(new vPagos(_usuario,
                new Servicio { Nombre = "Plan Básico", Precio = PlanesDisponibles[0].Precio },
                new Mascota(),
                DateTime.Now));
        }
    }

    private async void btnPlan2_Clicked(object sender, EventArgs e)
    {
        if (PlanesDisponibles != null && PlanesDisponibles.Count > 1)
        {
            await Navigation.PushAsync(new vPagos(_usuario,
                new Servicio { Nombre = "Plan Completo", Precio = PlanesDisponibles[1].Precio },
                new Mascota(),
                DateTime.Now));
        }
    }
}