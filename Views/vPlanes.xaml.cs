using KynosPetClub.Models;
using KynosPetClub.Services;

namespace KynosPetClub.Views;

public partial class vPlanes : ContentPage
{
    private readonly Usuario _usuario;
    private readonly ApiService _apiService;

    public List<Plan> PlanesDisponibles { get; set; }

    // Propiedad para binding del Usuario al BottomNavBar
    public Usuario Usuario => _usuario;

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
                // Actualizar UI con los planes si es necesario
                ActualizarPreciosEnUI();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar planes: {ex.Message}", "OK");
        }
    }

    private void ActualizarPreciosEnUI()
    {
        // Si tienes precios dinámicos desde la API, puedes actualizar los Labels aquí
        // Por ejemplo, encontrar los Labels de precio y actualizarlos
        // Esto es opcional dependiendo de si quieres precios dinámicos o estáticos
    }

    private async void btnPlan1_Clicked(object sender, EventArgs e)
    {
        try
        {
            decimal precio = 100; // Precio por defecto

            // Si tienes planes cargados desde la API, usar ese precio
            if (PlanesDisponibles != null && PlanesDisponibles.Count > 0)
            {
                precio = PlanesDisponibles[0].Precio;
            }

            await Navigation.PushAsync(new vPagos(_usuario,
                new Servicio
                {
                    Nombre = "Plan Básico",
                    Precio = precio,
                    Descripcion = "Plan básico mensual con servicios esenciales"
                },
                new Mascota { Nombre = "Plan de membresía" },
                DateTime.Now));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al procesar el plan: {ex.Message}", "OK");
        }
    }

    private async void btnPlan2_Clicked(object sender, EventArgs e)
    {
        try
        {
            decimal precio = 200; // Precio por defecto

            // Si tienes planes cargados desde la API, usar ese precio
            if (PlanesDisponibles != null && PlanesDisponibles.Count > 1)
            {
                precio = PlanesDisponibles[1].Precio;
            }

            await Navigation.PushAsync(new vPagos(_usuario,
                new Servicio
                {
                    Nombre = "Plan Completo",
                    Precio = precio,
                    Descripcion = "Plan completo mensual con servicios premium"
                },
                new Mascota { Nombre = "Plan de membresía" },
                DateTime.Now));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al procesar el plan: {ex.Message}", "OK");
        }
    }
}