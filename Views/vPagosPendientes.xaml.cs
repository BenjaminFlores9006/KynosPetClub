using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KynosPetClub.Views;

public partial class vPagosPendientes : ContentPage, INotifyPropertyChanged
{
    private readonly Usuario _usuario;
    private readonly ApiService _apiService;

    public ObservableCollection<ReservaPendientePago> ReservasPendientesPago { get; set; }

    // Propiedad para binding del Usuario al BottomNavBar
    public Usuario Usuario => _usuario;

    public vPagosPendientes(Usuario usuario)
    {
        InitializeComponent();
        _usuario = usuario;
        _apiService = new ApiService();
        ReservasPendientesPago = new ObservableCollection<ReservaPendientePago>();

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarReservasPendientes();
    }

    private async Task CargarReservasPendientes()
    {
        try
        {
            // Obtener todas las reservas del usuario
            var reservas = await _apiService.ObtenerReservasUsuarioAsync(_usuario.Id.Value);

            if (reservas == null || !reservas.Any())
            {
                ReservasPendientesPago.Clear();
                return;
            }

            // Obtener servicios y mascotas para mostrar información completa
            var servicios = await _apiService.ObtenerServiciosAsync();
            var mascotas = await _apiService.ObtenerMascotasUsuarioAsync(_usuario.Id.Value);

            // Obtener comprobantes del usuario
            var comprobantes = await _apiService.ObtenerComprobantesUsuarioAsync(_usuario.Id.Value) ?? new List<Comprobante>();

            // LÓGICA NUEVA: Solo mostrar reservas que NO tienen comprobante asociado
            var reservasSinComprobante = new List<Reserva>();

            foreach (var reserva in reservas)
            {
                // Solo considerar reservas activas (no canceladas)
                if (reserva.Estado == "Cancelado" || reserva.Estado == "Completado")
                    continue;

                // Verificar si esta reserva tiene algún comprobante asociado
                var tieneComprobante = comprobantes.Any(c => c.ReservaId == reserva.Id);

                // Si NO tiene comprobante, necesita pago
                if (!tieneComprobante)
                {
                    reservasSinComprobante.Add(reserva);
                }
            }

            // Crear objetos para mostrar en la UI
            var reservasParaMostrar = reservasSinComprobante
                .OrderBy(r => r.FechaServicio)
                .Select(r =>
                {
                    var servicio = servicios?.FirstOrDefault(s => s.Id == r.ServicioId);
                    var mascota = mascotas?.FirstOrDefault(m => m.Id == r.MascotaId);

                    return new ReservaPendientePago
                    {
                        ReservaId = r.Id,
                        ServicioNombre = servicio?.Nombre ?? "Servicio desconocido",
                        MascotaNombre = mascota?.Nombre ?? "Mascota desconocida",
                        FechaHoraFormateada = r.FechaServicio.ToString("dddd, dd/MM/yyyy 'a las' HH:mm"),
                        Precio = servicio?.Precio.ToString("C") ?? "$0.00",
                        ServicioOriginal = servicio,
                        MascotaOriginal = mascota,
                        FechaServicio = r.FechaServicio,
                        ReservaOriginal = r
                    };
                })
                .ToList();

            // Actualizar la colección
            ReservasPendientesPago.Clear();
            foreach (var reserva in reservasParaMostrar)
            {
                ReservasPendientesPago.Add(reserva);
            }

            // Debug
            System.Diagnostics.Debug.WriteLine($"Reservas totales: {reservas.Count()}");
            System.Diagnostics.Debug.WriteLine($"Comprobantes totales: {comprobantes.Count}");
            System.Diagnostics.Debug.WriteLine($"Reservas sin comprobante: {reservasParaMostrar.Count}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar reservas pendientes: {ex.Message}", "OK");
        }
    }

    private async void btnPagar_Clicked(object sender, EventArgs e)
    {
        try
        {
            var button = sender as Button;
            var reservaPendiente = button?.BindingContext as ReservaPendientePago;

            if (reservaPendiente?.ServicioOriginal != null &&
                reservaPendiente?.MascotaOriginal != null)
            {
                // Navegar a vPagos con los datos de la reserva
                await Navigation.PushAsync(new vPagos(
                    _usuario,
                    reservaPendiente.ServicioOriginal,
                    reservaPendiente.MascotaOriginal,
                    reservaPendiente.FechaServicio,
                    reservaPendiente.ReservaId
                ));
            }
            else
            {
                await DisplayAlert("Error", "No se pudieron cargar los datos de la reserva", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al abrir página de pago: {ex.Message}", "OK");
        }
    }

    private async void btnIrInicio_Clicked(object sender, EventArgs e)
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

// Clase helper para mostrar datos en la UI
public class ReservaPendientePago
{
    public int ReservaId { get; set; }
    public string ServicioNombre { get; set; }
    public string MascotaNombre { get; set; }
    public string FechaHoraFormateada { get; set; }
    public string Precio { get; set; }
    public Servicio ServicioOriginal { get; set; }
    public Mascota MascotaOriginal { get; set; }
    public DateTime FechaServicio { get; set; }
    public Reserva ReservaOriginal { get; set; }
}