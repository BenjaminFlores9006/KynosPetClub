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

            // Obtener información de clientes para mostrar
            var todosUsuarios = await _apiService.ObtenerTodosUsuariosAsync();

            Citas.Clear();

            foreach (var cita in citasAsignadas)
            {
                Console.WriteLine($"➕ Agregando cita: {cita.Servicio?.Nombre} - {cita.Estado}");

                // Buscar el cliente
                var cliente = todosUsuarios.FirstOrDefault(u => u.Id == cita.UsuarioId);
                var nombreCliente = cliente != null ? $"{cliente.nombre} {cliente.apellido}" : "Cliente desconocido";

                Citas.Add(new CitaAsignadaViewModel
                {
                    Id = cita.Id,
                    FechaServicio = cita.FechaServicio,
                    Reserva = cita,
                    Servicio = cita.Servicio,
                    Mascota = cita.Mascota,
                    Estado = cita.Estado,
                    EstadoColor = ObtenerColorEstado(cita.Estado),
                    PuedeCompletar = cita.Estado == "En curso", // Solo si está en curso
                    ClienteNombre = nombreCliente
                });
            }

            ActualizarContador();

            if (!Citas.Any())
            {
                Console.WriteLine("📋 No hay citas asignadas");
            }
            else
            {
                Console.WriteLine($"✅ Total citas cargadas en la UI: {Citas.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al cargar citas: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            await DisplayAlert("Error", $"❌ Error al cargar citas: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ActualizarContador()
    {
        if (lblContador != null)
        {
            var totalCitas = Citas.Count;
            lblContador.Text = $"📋 {totalCitas} cita{(totalCitas != 1 ? "s" : "")} asignada{(totalCitas != 1 ? "s" : "")}";
        }
    }

    private Color ObtenerColorEstado(string estado)
    {
        return estado switch
        {
            "En curso" => Color.FromArgb("#2196F3"),    // Azul
            "Completado" => Color.FromArgb("#4CAF50"),   // Verde
            "Cancelado" => Color.FromArgb("#F44336"),    // Rojo
            _ => Color.FromArgb("#FF9800")               // Naranja
        };
    }

    private async void btnCompletar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is CitaAsignadaViewModel vm)
        {
            try
            {
                bool confirmar = await DisplayAlert("Confirmar Completado",
                    $"¿Marcar como completada esta cita?\n\n" +
                    $"🏥 Servicio: {vm.Servicio?.Nombre}\n" +
                    $"🐾 Mascota: {vm.Mascota?.Nombre}\n" +
                    $"👤 Cliente: {vm.ClienteNombre}\n" +
                    $"📅 Fecha: {vm.FechaServicio:dd/MM/yyyy HH:mm}\n\n" +
                    $"Esta acción no se puede deshacer.",
                    "Sí, completar", "Cancelar");

                if (!confirmar) return;

                IsBusy = true;
                Console.WriteLine($"🔄 Completando cita ID: {vm.Id}");

                // Actualizar estado de la reserva
                vm.Reserva.Estado = "Completado";

                // Agregar comentario de completado
                var comentarioCompletar = $"\n\n✅ COMPLETADO por {_funcionario.nombre} {_funcionario.apellido}" +
                                        $"\n📅 Fecha de completado: {DateTime.Now:dd/MM/yyyy HH:mm}" +
                                        $"\n💼 Estado cambiado de 'En curso' a 'Completado'";

                vm.Reserva.Comentarios = (vm.Reserva.Comentarios ?? "") + comentarioCompletar;

                Console.WriteLine($"📝 Actualizando reserva con comentario de completado");

                var resultado = await _apiService.ActualizarReservaAsync(vm.Reserva);

                if (resultado)
                {
                    Console.WriteLine("✅ Cita completada exitosamente");

                    // REMOVER la cita de la lista ya que ahora está completada
                    Citas.Remove(vm);

                    // Actualizar contador
                    ActualizarContador();

                    await DisplayAlert("¡Éxito!",
                        $"✅ Cita completada exitosamente\n\n" +
                        $"🎯 El cliente {vm.ClienteNombre} será notificado de que el servicio fue completado.\n\n" +
                        $"📋 La cita ha sido removida de tu lista de pendientes.", "OK");
                }
                else
                {
                    Console.WriteLine("❌ Error al actualizar la reserva");
                    await DisplayAlert("Error", "❌ No se pudo completar la cita. Intenta de nuevo.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al completar cita: {ex.Message}");
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

// ViewModel simplificado para citas asignadas
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
    public string ClienteNombre { get; set; }

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