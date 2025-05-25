using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KynosPetClub.Views;

public partial class vAsignarCitas : ContentPage, INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly Usuario _admin;

    public ObservableCollection<ReservaAsignacionViewModel> Reservas { get; set; } = new();
    public ObservableCollection<FuncionarioViewModel> FuncionariosDisponibles { get; set; } = new();

    private List<ReservaAsignacionViewModel> _todasReservas = new();
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            if (loadingIndicator != null)
            {
                loadingIndicator.IsVisible = value;
                loadingIndicator.IsRunning = value;
            }
        }
    }

    public vAsignarCitas(Usuario admin)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en InitializeComponent: {ex.Message}");
        }

        _apiService = new ApiService();
        _admin = admin;
        BindingContext = this;

        // Verificar permisos de administrador
        if (_admin.RolId != 1)
        {
            DisplayAlert("Error", "❌ Acceso no autorizado", "OK");
            Navigation.PopAsync();
            return;
        }

        CargarDatos();
    }

    private async void CargarDatos()
    {
        try
        {
            IsLoading = true;
            Console.WriteLine("🔄 Iniciando carga de datos para asignación...");

            // Cargar datos en paralelo
            var reservasTask = _apiService.ObtenerReservasPendientesAsignacionAsync();
            var funcionariosTask = _apiService.ObtenerFuncionariosAsync();
            var usuariosTask = _apiService.ObtenerTodosUsuariosAsync();

            await Task.WhenAll(reservasTask, funcionariosTask, usuariosTask);

            var reservas = await reservasTask;
            var funcionarios = await funcionariosTask;
            var usuarios = await usuariosTask;

            Console.WriteLine($"📊 Datos cargados - Reservas: {reservas.Count}, Funcionarios: {funcionarios.Count}, Usuarios: {usuarios.Count}");

            // Cargar funcionarios disponibles
            FuncionariosDisponibles.Clear();
            foreach (var funcionario in funcionarios)
            {
                if (funcionario.Id.HasValue)
                {
                    var funcionarioVM = new FuncionarioViewModel
                    {
                        Id = funcionario.Id.Value,
                        NombreCompleto = $"{funcionario.nombre} {funcionario.apellido}",
                        Correo = funcionario.correo
                    };
                    FuncionariosDisponibles.Add(funcionarioVM);
                    Console.WriteLine($"👨‍⚕️ Funcionario disponible: {funcionarioVM.NombreCompleto}");
                }
            }

            // Crear ViewModels de reservas
            Reservas.Clear();
            _todasReservas.Clear();

            foreach (var reserva in reservas)
            {
                var cliente = usuarios.FirstOrDefault(u => u.Id == reserva.UsuarioId);

                var reservaVM = new ReservaAsignacionViewModel
                {
                    Id = reserva.Id,
                    FechaServicio = reserva.FechaServicio,
                    Estado = reserva.Estado,
                    Comentarios = reserva.Comentarios,
                    UsuarioId = reserva.UsuarioId,
                    MascotaId = reserva.MascotaId,
                    ServicioId = reserva.ServicioId,

                    // Información para mostrar
                    ServicioNombre = reserva.Servicio?.Nombre ?? "Servicio desconocido",
                    ClienteNombre = cliente != null ? $"{cliente.nombre} {cliente.apellido}" : "Cliente desconocido",
                    MascotaNombre = reserva.Mascota?.Nombre ?? "Mascota desconocida",
                    PrecioServicio = reserva.Servicio?.Precio ?? 0,

                    // Detectar si ya tiene funcionario asignado en comentarios
                    FuncionarioAsignadoNombre = ExtraerFuncionarioDeComentarios(reserva.Comentarios),

                    ReservaOriginal = reserva
                };

                Console.WriteLine($"🔍 Reserva ID {reserva.Id}: Estado={reserva.Estado}, PuedeAsignar={reservaVM.PuedeAsignar}, TieneFuncionario={reservaVM.TieneFuncionarioAsignado}");

                Reservas.Add(reservaVM);
                _todasReservas.Add(reservaVM);
            }

            ActualizarContador();
            Console.WriteLine("✅ Carga de datos completada");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al cargar datos: {ex.Message}");
            await DisplayAlert("Error", $"❌ Error al cargar datos: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string ExtraerFuncionarioDeComentarios(string comentarios)
    {
        if (string.IsNullOrEmpty(comentarios))
            return null;

        try
        {
            // Buscar patrón "FUNCIONARIO ASIGNADO: NombreCompleto (ID: 123)"
            var patron = "FUNCIONARIO ASIGNADO: ";
            var indice = comentarios.IndexOf(patron);
            if (indice >= 0)
            {
                var inicio = indice + patron.Length;
                var fin = comentarios.IndexOf(" (ID:", inicio);
                if (fin > inicio)
                {
                    var nombreFuncionario = comentarios.Substring(inicio, fin - inicio);
                    Console.WriteLine($"✅ Funcionario encontrado en comentarios: {nombreFuncionario}");
                    return nombreFuncionario;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al extraer funcionario de comentarios: {ex.Message}");
        }

        return null;
    }

    private void ActualizarContador()
    {
        if (lblContador != null)
        {
            var sinAsignar = Reservas.Count(r => !r.TieneFuncionarioAsignado);
            var conFuncionario = Reservas.Count(r => r.TieneFuncionarioAsignado);
            lblContador.Text = $"📋 {sinAsignar} sin asignar • {conFuncionario} con funcionario • Total: {Reservas.Count}";
            Console.WriteLine($"📊 Contador actualizado: {sinAsignar} sin asignar, {conFuncionario} con funcionario");
        }
    }

    private async void btnAsignarFuncionario_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ReservaAsignacionViewModel reserva)
        {
            try
            {
                Console.WriteLine($"🎯 Iniciando asignación para reserva ID: {reserva.Id}");

                if (reserva.FuncionarioSeleccionado == null)
                {
                    await DisplayAlert("Error", "⚠️ Selecciona un funcionario primero", "OK");
                    return;
                }

                bool confirmar = await DisplayAlert(
                    "Asignar Funcionario",
                    $"¿Asignar funcionario a esta cita?\n\n" +
                    $"🏥 Servicio: {reserva.ServicioNombre}\n" +
                    $"👤 Cliente: {reserva.ClienteNombre}\n" +
                    $"🐾 Mascota: {reserva.MascotaNombre}\n" +
                    $"📅 Fecha: {reserva.FechaServicioTexto} a las {reserva.HoraServicioTexto}\n" +
                    $"👨‍⚕️ Funcionario: {reserva.FuncionarioSeleccionado.NombreCompleto}\n\n" +
                    $"El funcionario podrá ver esta cita en su lista de citas asignadas.",
                    "Sí, asignar",
                    "Cancelar");

                if (!confirmar) return;

                IsLoading = true;
                Console.WriteLine($"🔄 Asignando funcionario: {reserva.FuncionarioSeleccionado.NombreCompleto}");

                // Preparar el comentario de asignación
                var comentarioActual = string.IsNullOrEmpty(reserva.Comentarios) ? "" : reserva.Comentarios.Trim();

                // Separar con doble salto de línea si ya hay comentarios
                var separador = string.IsNullOrEmpty(comentarioActual) ? "" : "\n\n";

                var comentarioAsignacion = $"👨‍⚕️ FUNCIONARIO ASIGNADO: {reserva.FuncionarioSeleccionado.NombreCompleto} (ID: {reserva.FuncionarioSeleccionado.Id})" +
                                         $"\n📅 Asignado el: {DateTime.Now:dd/MM/yyyy HH:mm}" +
                                         $"\n👤 Asignado por: {_admin.nombre} {_admin.apellido}";

                var nuevoComentario = comentarioActual + separador + comentarioAsignacion;

                Console.WriteLine($"📝 Comentario a guardar: {nuevoComentario}");

                // Actualizar la reserva
                reserva.ReservaOriginal.Comentarios = nuevoComentario;
                // Mantener el estado "En curso" (ya está así)

                var resultado = await _apiService.ActualizarReservaAsync(reserva.ReservaOriginal);

                if (resultado)
                {
                    Console.WriteLine("✅ Reserva actualizada exitosamente");

                    // Actualizar el ViewModel
                    reserva.Comentarios = nuevoComentario;
                    reserva.FuncionarioAsignadoNombre = reserva.FuncionarioSeleccionado.NombreCompleto;

                    // Notificar cambios de propiedades
                    reserva.OnPropertyChanged(nameof(reserva.TieneFuncionarioAsignado));
                    reserva.OnPropertyChanged(nameof(reserva.PuedeAsignar));
                    reserva.OnPropertyChanged(nameof(reserva.FuncionarioAsignadoTexto));
                    reserva.OnPropertyChanged(nameof(reserva.TieneComentarios));

                    ActualizarContador();

                    await DisplayAlert("Éxito",
                        $"✅ Funcionario asignado correctamente\n\n" +
                        $"🎯 {reserva.FuncionarioSeleccionado.NombreCompleto} ahora puede ver esta cita en su lista de citas asignadas.\n\n" +
                        $"📱 La cita aparecerá automáticamente en la app del funcionario.", "OK");
                }
                else
                {
                    Console.WriteLine("❌ Error al actualizar la reserva");
                    await DisplayAlert("Error", "❌ No se pudo asignar el funcionario. Intenta de nuevo.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en asignación: {ex.Message}");
                await DisplayAlert("Error", $"❌ Error al asignar: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async void btnVolver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchTerm = e.NewTextValue?.ToLower().Trim() ?? string.Empty;

        Reservas.Clear();

        if (string.IsNullOrEmpty(searchTerm))
        {
            foreach (var reserva in _todasReservas)
            {
                Reservas.Add(reserva);
            }
        }
        else
        {
            var reservasFiltradas = _todasReservas.Where(r =>
                r.ServicioNombre.ToLower().Contains(searchTerm) ||
                r.ClienteNombre.ToLower().Contains(searchTerm) ||
                r.MascotaNombre.ToLower().Contains(searchTerm) ||
                (r.FuncionarioAsignadoNombre?.ToLower().Contains(searchTerm) ?? false));

            foreach (var reserva in reservasFiltradas)
            {
                Reservas.Add(reserva);
            }
        }

        ActualizarContador();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ViewModel para funcionarios
public class FuncionarioViewModel
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; }
    public string Correo { get; set; }

    public override string ToString()
    {
        return NombreCompleto;
    }
}

// ViewModel para reservas en asignación
public class ReservaAsignacionViewModel : INotifyPropertyChanged
{
    private FuncionarioViewModel _funcionarioSeleccionado;
    private string _funcionarioAsignadoNombre;

    public int Id { get; set; }
    public DateTime FechaServicio { get; set; }
    public string Estado { get; set; }
    public string Comentarios { get; set; }
    public int UsuarioId { get; set; }
    public int MascotaId { get; set; }
    public int ServicioId { get; set; }

    // Información para mostrar
    public string ServicioNombre { get; set; }
    public string ClienteNombre { get; set; }
    public string MascotaNombre { get; set; }
    public decimal PrecioServicio { get; set; }

    public string FuncionarioAsignadoNombre
    {
        get => _funcionarioAsignadoNombre;
        set
        {
            _funcionarioAsignadoNombre = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TieneFuncionarioAsignado));
            OnPropertyChanged(nameof(PuedeAsignar));
            OnPropertyChanged(nameof(FuncionarioAsignadoTexto));
        }
    }

    public Reserva ReservaOriginal { get; set; }

    public FuncionarioViewModel FuncionarioSeleccionado
    {
        get => _funcionarioSeleccionado;
        set
        {
            _funcionarioSeleccionado = value;
            OnPropertyChanged();
        }
    }

    // Propiedades calculadas
    public string FechaServicioTexto => FechaServicio.ToString("dd/MM/yyyy");
    public string HoraServicioTexto => FechaServicio.ToString("HH:mm");
    public string PrecioTexto => PrecioServicio.ToString("C");
    public bool TieneComentarios => !string.IsNullOrEmpty(Comentarios);
    public bool TieneFuncionarioAsignado => !string.IsNullOrEmpty(FuncionarioAsignadoNombre);
    public bool PuedeAsignar => Estado == "En curso" && !TieneFuncionarioAsignado;

    public string FuncionarioAsignadoTexto =>
        TieneFuncionarioAsignado ? $"✅ Asignado a: {FuncionarioAsignadoNombre}" : "❌ Sin asignar";

    public string EstadoColor
    {
        get
        {
            return Estado switch
            {
                "Pendiente" => "#FF9800", // Naranja
                "En curso" => "#2196F3",  // Azul
                "Completado" => "#4CAF50", // Verde
                "Cancelado" => "#F44336",  // Rojo
                _ => "#95A5A6"             // Gris
            };
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}