using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;

namespace KynosPetClub.Views;

public partial class vCitasAsignadas : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Usuario _funcionario;
    public ObservableCollection<CitaAsignadaViewModel> Citas { get; set; } = new();
    public vCitasAsignadas(Usuario funcionario)
	{
		InitializeComponent();
        if (funcionario.RolId != 3)
        {
            DisplayAlert("Error", "Acceso no autorizado", "OK");
            Navigation.PopAsync();
            return;
        }

        _apiService = new ApiService();
        _funcionario = funcionario;
        BindingContext = this;

        Title = $"Citas de {funcionario.nombre}";
        CargarCitas();
    }

    private async void CargarCitas()
    {
        try
        {
            IsBusy = true;
            var citas = await _apiService.ObtenerCitasAsignadasAsync(_funcionario.Id.Value);

            Citas.Clear();
            foreach (var cita in citas)
            {
                Citas.Add(new CitaAsignadaViewModel
                {
                    Reserva = cita,
                    Servicio = cita.Servicio,
                    Mascota = cita.Mascota,
                    Estado = cita.Estado,
                    EstadoColor = cita.Estado == "En curso" ? Colors.Blue :
                                cita.Estado == "Completado" ? Colors.Green :
                                Colors.Orange,
                    PuedeCompletar = cita.Estado == "En curso"
                });
            }

            if (!Citas.Any())
            {
                await DisplayAlert("Información", "No tienes citas asignadas actualmente", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar citas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }

    }

    private async void btnCompletar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is CitaAsignadaViewModel vm)
        {
            bool confirmar = await DisplayAlert("Confirmar",
                "¿Marcar esta cita como completada?",
                "Sí", "No");

            if (confirmar)
            {
                vm.Reserva.Estado = "Completado";
                var resultado = await _apiService.ActualizarReservaAsync(vm.Reserva);

                if (resultado)
                {
                    await DisplayAlert("Éxito", "Cita completada", "OK");
                    vm.Estado = "Completado";
                    vm.EstadoColor = Colors.Green;
                    vm.PuedeCompletar = false;
                    // Forzar actualización de la UI
                    cvCitas.ItemsSource = null;
                    cvCitas.ItemsSource = Citas;
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo completar la cita", "OK");
                }
            }
        }
    }

    private async void btnVolver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    public class CitaAsignadaViewModel
    {
        public Reserva Reserva { get; set; }
        public Servicio Servicio { get; set; }
        public Mascota Mascota { get; set; }
        public string Estado { get; set; }
        public Color EstadoColor { get; set; }
        public bool PuedeCompletar { get; set; }
    }
}