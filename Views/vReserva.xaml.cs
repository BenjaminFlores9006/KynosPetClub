using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KynosPetClub.Views;

public partial class vReserva : ContentPage, INotifyPropertyChanged
{
    private readonly Usuario _usuario;
    private readonly ApiService _apiService;

    public ObservableCollection<ReservaViewModel> ReservasActivas { get; set; }

    // Propiedad para binding del Usuario al BottomNavBar
    public Usuario Usuario => _usuario;

    public vReserva(Usuario usuario)
    {
        InitializeComponent();
        _usuario = usuario;
        _apiService = new ApiService();
        ReservasActivas = new ObservableCollection<ReservaViewModel>();

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarReservas();
    }

    private async Task CargarReservas()
    {
        try
        {
            // Mostrar indicador de carga
            loadingSection.IsVisible = true;
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;
            emptyStateLayout.IsVisible = false;

            // 🔧 AÑADIR DEBUG
            await _apiService.DebugComprobantesAsync(_usuario.Id.Value);

            // Obtener datos en paralelo
            var reservasTask = _apiService.ObtenerReservasUsuarioAsync(_usuario.Id.Value);
            var serviciosTask = _apiService.ObtenerServiciosAsync();
            var mascotasTask = _apiService.ObtenerMascotasUsuarioAsync(_usuario.Id.Value);
            var comprobantesTask = _apiService.ObtenerComprobantesUsuarioAsync(_usuario.Id.Value);

            await Task.WhenAll(reservasTask, serviciosTask, mascotasTask, comprobantesTask);

            var reservas = await reservasTask ?? new List<Reserva>();
            var servicios = await serviciosTask ?? new List<Servicio>();
            var mascotas = await mascotasTask ?? new List<Mascota>();
            var comprobantes = await comprobantesTask ?? new List<Comprobante>();

            // Filtrar solo reservas activas
            var reservasActivas = reservas
                .Where(r => r.EsReservaActiva() || r.Estado == "Pendiente")
                .OrderBy(r => r.FechaServicio)
                .ToList();

            // Crear ViewModels
            var reservasViewModel = new List<ReservaViewModel>();

            foreach (var reserva in reservasActivas)
            {
                var servicio = servicios.FirstOrDefault(s => s.Id == reserva.ServicioId);
                var mascota = mascotas.FirstOrDefault(m => m.Id == reserva.MascotaId);

                // 🔧 LÓGICA MEJORADA para detectar comprobantes
                bool tieneComprobante = false;

                // Buscar comprobante de múltiples formas
                foreach (var comp in comprobantes)
                {
                    // Verificar por ReservaId exacto
                    if (comp.ReservaId == reserva.Id)
                    {
                        tieneComprobante = true;
                        Console.WriteLine($"✅ Comprobante encontrado por ReservaId: {comp.ReservaId} = {reserva.Id}");
                        break;
                    }

                    // Verificar por descripción (backup)
                    if (!string.IsNullOrEmpty(comp.Descripcion) &&
                        servicio != null &&
                        comp.Descripcion.Contains(servicio.Nombre))
                    {
                        // Verificar que las fechas sean cercanas (dentro de 24 horas)
                        var diferencia = Math.Abs((comp.FechaSubida - reserva.FechaServicio).TotalHours);
                        if (diferencia <= 24)
                        {
                            tieneComprobante = true;
                            Console.WriteLine($"✅ Comprobante encontrado por descripción: {comp.Descripcion}");
                            break;
                        }
                    }
                }

                Console.WriteLine($"Reserva {reserva.Id} - Tiene comprobante: {tieneComprobante}");

                var viewModel = new ReservaViewModel
                {
                    Id = reserva.Id,
                    FechaServicio = reserva.FechaServicio,
                    FechaServicioFormateada = reserva.FechaServicio.ToString("dddd, dd/MM/yyyy 'a las' HH:mm"),
                    Estado = reserva.Estado,
                    Comentarios = reserva.Comentarios,
                    Servicio = servicio,
                    MascotaInfo = mascota != null ? $"{mascota.Nombre} ({mascota.Especie})" : "Mascota desconocida",
                    ColorEstado = ObtenerColorEstado(reserva.Estado),
                    PuedeCancelarse = reserva.Estado != "Cancelado" && reserva.Estado != "Completado",
                    TieneComentarios = !string.IsNullOrWhiteSpace(reserva.Comentarios),

                    // 🔧 CORREGIDO: Solo mostrar "Pendiente de pago" si NO tiene comprobante
                    MostrarPendientePago = !tieneComprobante,
                    ReservaOriginal = reserva
                };

                reservasViewModel.Add(viewModel);
            }

            // Actualizar la colección
            ReservasActivas.Clear();
            foreach (var reserva in reservasViewModel)
            {
                ReservasActivas.Add(reserva);
            }

            // Mostrar estado vacío si no hay reservas
            emptyStateLayout.IsVisible = !ReservasActivas.Any();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar reservas: {ex.Message}", "OK");
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
            "Pendiente" => Color.FromArgb("#FF9800"), // Naranja
            "En curso" => Color.FromArgb("#2196F3"),  // Azul
            "Completado" => Color.FromArgb("#4CAF50"), // Verde
            "Cancelado" => Color.FromArgb("#F44336"),  // Rojo
            _ => Color.FromArgb("#9E9E9E") // Gris por defecto
        };
    }

    private async void OnVerDetallesClicked(object sender, EventArgs e)
    {
        try
        {
            var button = sender as Button;
            var reserva = button?.CommandParameter as ReservaViewModel;

            if (reserva != null)
            {
                string detalles = $"📋 Detalles de la Reserva\n\n" +
                    $"🏥 Servicio: {reserva.Servicio?.Nombre ?? "N/A"}\n" +
                    $"💰 Precio: {(reserva.Servicio?.Precio ?? 0):C}\n" +
                    $"🐕 Mascota: {reserva.MascotaInfo}\n" +
                    $"📅 Fecha: {reserva.FechaServicioFormateada}\n" +
                    $"📊 Estado: {reserva.Estado}\n";

                if (reserva.MostrarPendientePago)
                {
                    detalles += $"⚠️ Estado de Pago: Pendiente de pago\n";
                }
                else
                {
                    detalles += $"✅ Estado de Pago: Pago registrado\n";
                }

                if (reserva.TieneComentarios)
                {
                    detalles += $"📝 Comentarios: {reserva.Comentarios}\n";
                }

                await DisplayAlert("Detalles", detalles, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al mostrar detalles: {ex.Message}", "OK");
        }
    }

    private async void OnCancelarReservaClicked(object sender, EventArgs e)
    {
        try
        {
            var button = sender as Button;
            var reserva = button?.CommandParameter as ReservaViewModel;

            if (reserva != null)
            {
                bool confirmar = await DisplayAlert("⚠️ Cancelar Reserva",
                    $"¿Estás seguro de que deseas cancelar esta reserva?\n\n" +
                    $"🏥 Servicio: {reserva.Servicio?.Nombre}\n" +
                    $"📅 Fecha: {reserva.FechaServicioFormateada}\n\n" +
                    $"Esta acción no se puede deshacer.",
                    "Sí, cancelar", "No");

                if (confirmar)
                {
                    var reservaParaActualizar = reserva.ReservaOriginal;
                    reservaParaActualizar.Estado = "Cancelado";

                    var resultado = await _apiService.ActualizarReservaAsync(reservaParaActualizar);

                    if (resultado)
                    {
                        await DisplayAlert("✅ Cancelada", "La reserva ha sido cancelada exitosamente.", "OK");
                        await CargarReservas(); // Recargar lista
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo cancelar la reserva. Inténtalo nuevamente.", "OK");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cancelar reserva: {ex.Message}", "OK");
        }
    }

    private async void OnVerServiciosClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new vInicio(_usuario));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al navegar: {ex.Message}", "OK");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ViewModel para mostrar reservas en la UI
public class ReservaViewModel
{
    public int Id { get; set; }
    public DateTime FechaServicio { get; set; }
    public string FechaServicioFormateada { get; set; }
    public string Estado { get; set; }
    public string Comentarios { get; set; }
    public Servicio Servicio { get; set; }
    public string MascotaInfo { get; set; }
    public Color ColorEstado { get; set; }
    public bool PuedeCancelarse { get; set; }
    public bool TieneComentarios { get; set; }
    public bool MostrarPendientePago { get; set; }
    public Reserva ReservaOriginal { get; set; }
}