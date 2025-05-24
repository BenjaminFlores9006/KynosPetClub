using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;

namespace KynosPetClub.Views;

public partial class vPlanes : ContentPage
{
    private readonly Usuario _usuario;
    private readonly ApiService _apiService;
    public ObservableCollection<Plan> PlanesDisponibles { get; set; } = new();

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

            PlanesDisponibles.Clear();
            foreach (var plan in planes)
            {
                PlanesDisponibles.Add(new PlanViewModel
                {
                    Id = plan.Id,
                    Nombre = plan.Nombre,
                    Descripcion = plan.Descripcion,
                    Precio = plan.Precio,
                    DuracionDias = plan.DuracionDias,
                    ColorPlan = ObtenerColorPlan(plan.Id)
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar planes: {ex.Message}", "OK");
        }
    }

    private Color ObtenerColorPlan(int planId)
    {
        return planId switch
        {
            1 => Color.FromArgb("#4CAF50"), // Verde para plan básico
            2 => Color.FromArgb("#2196F3"), // Azul para plan premium
            3 => Color.FromArgb("#FFC107"), // Amarillo para plan gold
            _ => Color.FromArgb("#9E9E9E")  // Gris por defecto
        };
    }

    private void ActualizarPreciosEnUI()
    {
        // Si tienes precios dinámicos desde la API, puedes actualizar los Labels aquí
        // Por ejemplo, encontrar los Labels de precio y actualizarlos
        // Esto es opcional dependiendo de si quieres precios dinámicos o estáticos
    }
    private async void btnSeleccionarPlan_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is PlanViewModel plan)
        {
            bool confirmar = await DisplayAlert("Confirmar",
                $"¿Deseas adquirir el plan {plan.Nombre} por {plan.Precio:C}?\n\n{plan.Descripcion}",
                "Sí", "No");

            if (confirmar)
            {
                // Crear un servicio ficticio para representar el plan
                var servicioPlan = new Servicio
                {
                    Id = 0, // ID especial para planes
                    Nombre = $"Plan {plan.Nombre}",
                    Descripcion = plan.Descripcion,
                    Precio = plan.Precio
                };

                // Obtener la primera mascota del usuario o crear una ficticia
                var mascotas = await _apiService.ObtenerMascotasUsuarioAsync(_usuario.Id.Value);
                var mascota = mascotas?.FirstOrDefault() ?? new Mascota
                {
                    Id = 0,
                    Nombre = "Mascota General"
                };

                // Convertir el PlanViewModel a Plan para pasarlo como parámetro
                var planSeleccionado = new Plan
                {
                    Id = plan.Id,
                    Nombre = plan.Nombre,
                    Descripcion = plan.Descripcion,
                    Precio = plan.Precio,
                    DuracionDias = plan.DuracionDias
                };

                // Navegar a vPagos con todos los parámetros necesarios
                await Navigation.PushAsync(new vPagos(
                    usuario: _usuario,
                    servicio: servicioPlan,
                    mascota: mascota,
                    fechaServicio: DateTime.Now,
                    reservaId: 0,
                    planSeleccionado: planSeleccionado));
            }
        }
    }

    public class PlanViewModel : Plan
    {
        public Color ColorPlan { get; set; }
    }
}