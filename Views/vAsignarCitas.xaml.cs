using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;

namespace KynosPetClub.Views;

public partial class vAsignarCitas : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Usuario _admin;
    public ObservableCollection<ReservaAsignacionViewModel> Reservas { get; set; } = new();
    private List<Usuario> _funcionarios = new();
    public vAsignarCitas(Usuario admin)
	{
		InitializeComponent();

        if (admin.RolId != 1)
        {
            DisplayAlert("Error", "Acceso no autorizado", "OK");
            Navigation.PopAsync();
            return;
        }
        _apiService = new ApiService();
        _admin = admin;
        BindingContext = this;
        CargarDatos();
    }

    private async void CargarDatos()
    {
        try
        {
            IsBusy = true;

            // Obtener datos en paralelo
            var reservasTask = _apiService.ObtenerReservasPendientesAsignacionAsync();
            var funcionariosTask = _apiService.ObtenerFuncionariosAsync();

            await Task.WhenAll(reservasTask, funcionariosTask);

            var reservas = await reservasTask;
            _funcionarios = await funcionariosTask;

            Reservas.Clear();
            foreach (var reserva in reservas)
            {
                Reservas.Add(new ReservaAsignacionViewModel
                {
                    Reserva = reserva,
                    Servicio = reserva.Servicio,
                    Mascota = reserva.Mascota,
                    FuncionariosDisponibles = _funcionarios,
                    FechaFormateada = reserva.FechaServicio.ToString("dd/MM/yyyy HH:mm")
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void btnAsignar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is ReservaAsignacionViewModel vm)
        {
            if (vm.FuncionarioAsignado == null)
            {
                await DisplayAlert("Error", "Selecciona un funcionario", "OK");
                return;
            }

            bool confirmar = await DisplayAlert("Confirmar",
                $"¿Asignar esta cita a {vm.FuncionarioAsignado.nombre}?",
                "Sí", "No");

            if (confirmar)
            {
                try
                {
                    var resultado = await _apiService.AsignarFuncionarioAReservaAsync(
                        vm.Reserva.Id,
                        vm.FuncionarioAsignado.Id.Value);

                    if (resultado)
                    {
                        await DisplayAlert("Éxito", "Cita asignada correctamente", "OK");
                        Reservas.Remove(vm);
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo asignar la cita", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void btnVolver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    public class ReservaAsignacionViewModel
    {
        public Reserva Reserva { get; set; }
        public Servicio Servicio { get; set; }
        public Mascota Mascota { get; set; }
        public List<Usuario> FuncionariosDisponibles { get; set; }
        public Usuario FuncionarioAsignado { get; set; }
        public string FechaFormateada { get; set; }
    }
}