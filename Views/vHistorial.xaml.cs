using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KynosPetClub.Views;

public partial class vHistorial : ContentPage, INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly Usuario _usuario;
    private string _filtroActual = "Todos";

    // 🔧 DOS COLECCIONES: Una para todas las reservas y otra para mostrar
    public ObservableCollection<HistorialViewModel> ReservasHistorial { get; set; } = new();
    private List<HistorialViewModel> _todasLasReservas = new List<HistorialViewModel>(); // Lista completa

    // Propiedad para binding del Usuario al BottomNavBar
    public Usuario Usuario => _usuario;

    public vHistorial(Usuario usuario)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _usuario = usuario;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarHistorial();
    }

    private async Task CargarHistorial()
    {
        try
        {
            Console.WriteLine("🔄 Iniciando carga de historial...");

            // Mostrar indicador de carga
            loadingSection.IsVisible = true;
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;
            emptyStateLayout.IsVisible = false;

            // Obtener datos en paralelo
            var reservasTask = _apiService.ObtenerReservasUsuarioAsync(_usuario.Id.Value);
            var serviciosTask = _apiService.ObtenerServiciosAsync();
            var mascotasTask = _apiService.ObtenerMascotasUsuarioAsync(_usuario.Id.Value);

            await Task.WhenAll(reservasTask, serviciosTask, mascotasTask);

            var todasReservas = await reservasTask ?? new List<Reserva>();
            var servicios = await serviciosTask ?? new List<Servicio>();
            var mascotas = await mascotasTask ?? new List<Mascota>();

            // 🔧 FILTRAR SOLO RESERVAS DE HISTORIAL (Completado o Cancelado)
            var reservasHistorial = todasReservas
                .Where(r => r.Estado == "Completado" || r.Estado == "Cancelado")
                .OrderByDescending(r => r.FechaServicio)
                .ToList();

            Console.WriteLine($"📊 Total reservas: {todasReservas.Count}");
            Console.WriteLine($"📖 Reservas de historial: {reservasHistorial.Count}");

            // Crear ViewModels para mostrar
            var historialViewModels = new List<HistorialViewModel>();

            foreach (var reserva in reservasHistorial)
            {
                var servicio = servicios.FirstOrDefault(s => s.Id == reserva.ServicioId);
                var mascota = mascotas.FirstOrDefault(m => m.Id == reserva.MascotaId);

                var viewModel = new HistorialViewModel
                {
                    Id = reserva.Id,
                    Estado = reserva.Estado,
                    FechaServicio = reserva.FechaServicio,
                    FechaServicioFormateada = reserva.FechaServicio.ToString("dddd, dd/MM/yyyy 'a las' HH:mm"),
                    Comentarios = reserva.Comentarios,
                    ServicioNombre = servicio?.Nombre ?? "Servicio desconocido",
                    MascotaInfo = mascota != null ? $"{mascota.Nombre} ({mascota.Especie})" : "Mascota desconocida",
                    PrecioFormateado = servicio?.Precio.ToString("C") ?? "$0.00",
                    ColorEstado = ObtenerColorEstado(reserva.Estado),
                    TieneComentarios = !string.IsNullOrWhiteSpace(reserva.Comentarios),
                    ReservaOriginal = reserva
                };

                historialViewModels.Add(viewModel);
                Console.WriteLine($"➕ Agregando: {viewModel.ServicioNombre} - {viewModel.Estado}");
            }

            // 🔧 GUARDAR TODAS LAS RESERVAS EN LA LISTA COMPLETA
            _todasLasReservas = historialViewModels;

            // Aplicar filtro actual (esto actualizará ReservasHistorial)
            AplicarFiltro(_filtroActual);

            Console.WriteLine($"✅ Carga completada. Total en historial: {_todasLasReservas.Count}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al cargar historial: {ex.Message}");
            await DisplayAlert("Error", $"Error al cargar historial: {ex.Message}", "OK");
        }
        finally
        {
            // Ocultar indicador de carga
            loadingSection.IsVisible = false;
            loadingIndicator.IsVisible = false;
            loadingIndicator.IsRunning = false;
        }
    }

    private Color ObtenerColorEstado(string estado)
    {
        return estado switch
        {
            "Completado" => Color.FromArgb("#4CAF50"), // Verde
            "Cancelado" => Color.FromArgb("#F44336"),  // Rojo
            _ => Color.FromArgb("#9E9E9E") // Gris por defecto
        };
    }

    private async void btnFiltroSeleccionado_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button)
            {
                Console.WriteLine($"🔍 Botón presionado: {button.Text}");

                // Obtener filtro del texto del botón
                string filtro = button.Text switch
                {
                    "📋 Todos" => "Todos",
                    "✅ Completados" => "Completado",
                    "❌ Cancelados" => "Cancelado",
                    _ => "Todos"
                };

                Console.WriteLine($"🎯 Filtro seleccionado: {filtro}");

                _filtroActual = filtro;

                // Actualizar estilos de botones
                ActualizarEstilosBotones(button);

                // Aplicar filtro
                AplicarFiltro(filtro);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al aplicar filtro: {ex.Message}");
            await DisplayAlert("Error", $"Error al aplicar filtro: {ex.Message}", "OK");
        }
    }

    private void ActualizarEstilosBotones(Button botonSeleccionado)
    {
        // Resetear todos los botones
        btnTodos.BackgroundColor = Color.FromArgb("#E0E0E0");
        btnTodos.TextColor = Color.FromArgb("#666");

        btnCompletados.BackgroundColor = Color.FromArgb("#E8F5E9");
        btnCompletados.TextColor = Color.FromArgb("#2E7D32");

        btnCancelados.BackgroundColor = Color.FromArgb("#FFEBEE");
        btnCancelados.TextColor = Color.FromArgb("#C62828");

        // Resaltar botón seleccionado
        botonSeleccionado.BackgroundColor = Color.FromArgb("#4B8A8B");
        botonSeleccionado.TextColor = Colors.White;

        Console.WriteLine($"🎨 Estilos actualizados. Botón seleccionado: {botonSeleccionado.Text}");
    }

    private void AplicarFiltro(string filtro)
    {
        try
        {
            Console.WriteLine($"🔍 Aplicando filtro: {filtro}");
            Console.WriteLine($"📊 Total reservas disponibles: {_todasLasReservas.Count}");

            // 🔧 FILTRAR DESDE LA LISTA COMPLETA
            var reservasFiltradas = filtro switch
            {
                "Completado" => _todasLasReservas.Where(r => r.Estado == "Completado").ToList(),
                "Cancelado" => _todasLasReservas.Where(r => r.Estado == "Cancelado").ToList(),
                _ => _todasLasReservas.ToList() // "Todos"
            };

            Console.WriteLine($"📋 Reservas después del filtro: {reservasFiltradas.Count}");

            // 🔧 ACTUALIZAR LA COLECCIÓN OBSERVABLE
            ReservasHistorial.Clear();
            foreach (var reserva in reservasFiltradas)
            {
                ReservasHistorial.Add(reserva);
                Console.WriteLine($"➕ Mostrando: {reserva.ServicioNombre} - {reserva.Estado}");
            }

            // Mostrar/ocultar elementos según resultados
            bool hayResultados = reservasFiltradas.Any();
            emptyStateLayout.IsVisible = !hayResultados;
            cvHistorial.IsVisible = hayResultados;

            Console.WriteLine($"✅ Filtro aplicado exitosamente");
            Console.WriteLine($"📊 Resultados mostrados: {ReservasHistorial.Count}");
            Console.WriteLine($"👁️ Vista vacía visible: {!hayResultados}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error aplicando filtro: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ViewModel para mostrar historial en la UI
public class HistorialViewModel
{
    public int Id { get; set; }
    public string Estado { get; set; }
    public DateTime FechaServicio { get; set; }
    public string FechaServicioFormateada { get; set; }
    public string Comentarios { get; set; }
    public string ServicioNombre { get; set; }
    public string MascotaInfo { get; set; }
    public string PrecioFormateado { get; set; }
    public Color ColorEstado { get; set; }
    public bool TieneComentarios { get; set; }
    public Reserva ReservaOriginal { get; set; }
}