using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KynosPetClub.Views;

public partial class vCitasAsignadas : ContentPage, INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly Usuario _funcionario;
    private bool _isBusy;

    public ObservableCollection<CitaAsignadaViewModel> Citas { get; set; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public vCitasAsignadas(Usuario funcionario)
    {
        InitializeComponent();

        // Verificar que sea funcionario
        if (funcionario.RolId != 3)
        {
            DisplayAlert("Error", "❌ Acceso no autorizado", "OK");
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
            Console.WriteLine($"🔍 Cargando citas para funcionario: {_funcionario.nombre} (ID: {_funcionario.Id})");

            // Obtener citas asignadas a este funcionario
            var citasAsignadas = await _apiService.ObtenerCitasAsignadasAsync(_funcionario.Id.Value);

            Console.WriteLine($"📋 Citas encontradas: {citasAsignadas.Count}");

            Citas.Clear();

            foreach (var cita in citasAsignadas)
            {
                Console.WriteLine($"➕ Agregando cita: {cita.Servicio?.Nombre} - {cita.Estado}");

                Citas.Add(new CitaAsignadaViewModel
                {
                    Id = cita.Id,
                    FechaServicio = cita.FechaServicio,
                    Reserva = cita,
                    Servicio = cita.Servicio,
                    Mascota = cita.Mascota,
                    Estado = cita.Estado,
                    EstadoColor = ObtenerColorEstado(cita.Estado),
                    PuedeCompletar = cita.Estado == "En curso"
                });
            }

            if (!Citas.Any())
            {
                await DisplayAlert("Información",
                    "📋 No tienes citas asignadas actualmente.\n\n" +
                    "Las citas aparecerán aquí cuando un administrador te las asigne.", "OK");
            }
            else
            {
                Console.WriteLine($"✅ Total citas cargadas en la UI: {Citas.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al cargar citas: {ex.Message}");
            await DisplayAlert("Error", $"❌ Error al cargar citas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Color ObtenerColorEstado(string estado)
    {
        return estado switch
        {
            "En curso" => Colors.Blue,
            "Completado" => Colors.Green,
            "Cancelado" => Colors.Red,
            _ => Colors.Orange
        };
    }

    private async void btnCompletar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is CitaAsignadaViewModel vm)
        {
            try
            {
                bool confirmar = await DisplayAlert("Confirmar",
                    $"¿Marcar como completada la cita?\n\n" +
                    $"🏥 Servicio: {vm.Servicio?.Nombre}\n" +
                    $"🐾 Mascota: {vm.Mascota?.Nombre}\n" +
                    $"📅 Fecha: {vm.FechaServicio:dd/MM/yyyy HH:mm}",
                    "Sí, completar", "Cancelar");

                if (!confirmar) return;

                IsBusy = true;

                // Actualizar estado de la reserva
                vm.Reserva.Estado = "Completado";

                // Agregar comentario de completado
                var comentarioCompletar = $"\n\n✅ COMPLETADO por {_funcionario.nombre} {_funcionario.apellido} el {DateTime.Now:dd/MM/yyyy HH:mm}";
                vm.Reserva.Comentarios = (vm.Reserva.Comentarios ?? "") + comentarioCompletar;

                var resultado = await _apiService.ActualizarReservaAsync(vm.Reserva);

                if (resultado)
                {
                    // Actualizar ViewModel
                    vm.Estado = "Completado";
                    vm.EstadoColor = Colors.Green;
                    vm.PuedeCompletar = false;

                    // Forzar actualización de la UI
                    OnPropertyChanged(nameof(Citas));

                    await DisplayAlert("Éxito",
                        "✅ Cita completada exitosamente\n\n" +
                        "El cliente podrá ver que el servicio fue completado.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "❌ No se pudo completar la cita. Intenta de nuevo.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"❌ Error al completar cita: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private async void btnVolver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ViewModel mejorado para citas asignadas
public class CitaAsignadaViewModel : INotifyPropertyChanged
{
    private string _estado;
    private Color _estadoColor;
    private bool _puedeCompletar;

    public int Id { get; set; }
    public DateTime FechaServicio { get; set; }
    public Reserva Reserva { get; set; }
    public Servicio Servicio { get; set; }
    public Mascota Mascota { get; set; }

    public string Estado
    {
        get => _estado;
        set
        {
            _estado = value;
            OnPropertyChanged();
        }
    }

    public Color EstadoColor
    {
        get => _estadoColor;
        set
        {
            _estadoColor = value;
            OnPropertyChanged();
        }
    }

    public bool PuedeCompletar
    {
        get => _puedeCompletar;
        set
        {
            _puedeCompletar = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}