﻿using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KynosPetClub.Views;

public partial class vPagosPendientes : ContentPage, INotifyPropertyChanged
{
    private readonly Usuario _usuario;
    private readonly ApiService _apiService;
    private readonly Plan _planSeleccionado;

    public ObservableCollection<ReservaPendientePago> ReservasPendientesPago { get; set; }

    // Propiedad para binding del Usuario al BottomNavBar
    public Usuario Usuario => _usuario;

    public vPagosPendientes(Usuario usuario, Plan plan = null)
    {
        InitializeComponent();
        _usuario = usuario;
        _planSeleccionado = plan;
        _apiService = new ApiService();
        ReservasPendientesPago = new ObservableCollection<ReservaPendientePago>();

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarReservasPendientes();

        // Si hay un plan seleccionado, mostrar opción para pagarlo
        if (_planSeleccionado != null)
        {
            await MostrarOpcionPagoPlan();
        }
    }

    private async Task MostrarOpcionPagoPlan()
    {
        bool confirmar = await DisplayAlert("Adquirir Plan",
            $"¿Deseas proceder con el pago del plan {_planSeleccionado.Nombre} por {_planSeleccionado.Precio:C}?",
            "Sí, Pagar Ahora", "Más Tarde");

        if (confirmar)
        {
            // Crear un servicio ficticio para el plan
            var servicioPlan = new Servicio
            {
                Id = 0, // ID especial para planes
                Nombre = $"Plan {_planSeleccionado.Nombre}",
                Descripcion = _planSeleccionado.Descripcion,
                Precio = _planSeleccionado.Precio
            };

            // Usar la primera mascota del usuario o crear una ficticia
            var mascotas = await _apiService.ObtenerMascotasUsuarioAsync(_usuario.Id.Value);
            var mascota = mascotas?.FirstOrDefault() ?? new Mascota { Id = 0, Nombre = "Mascota General" };

            await Navigation.PushAsync(new vPagos(
                usuario: _usuario,
                servicio: servicioPlan,
                mascota: mascota,
                fechaServicio: DateTime.Now,
                reservaId: 0,
                planSeleccionado: _planSeleccionado));
        }
    }

    private async Task CargarReservasPendientes()
    {
        try
        {
            // 🔧 AÑADIR DEBUG
            await _apiService.DebugComprobantesAsync(_usuario.Id.Value);

            // Obtener todas las reservas del usuario
            var reservas = await _apiService.ObtenerReservasUsuarioAsync(_usuario.Id.Value);

            if (reservas == null || !reservas.Any())
            {
                ReservasPendientesPago.Clear();
                MostrarEstadoVacio();
                return;
            }

            // Obtener servicios y mascotas para mostrar información completa
            var servicios = await _apiService.ObtenerServiciosAsync();
            var mascotas = await _apiService.ObtenerMascotasUsuarioAsync(_usuario.Id.Value);

            // Obtener comprobantes del usuario
            var comprobantes = await _apiService.ObtenerComprobantesUsuarioAsync(_usuario.Id.Value) ?? new List<Comprobante>();

            // 🔧 LÓGICA MEJORADA: Solo mostrar reservas que NO tienen comprobante asociado
            var reservasSinComprobante = new List<Reserva>();

            foreach (var reserva in reservas)
            {
                // Solo considerar reservas activas (no canceladas)
                if (reserva.Estado == "Cancelado" || reserva.Estado == "Completado")
                    continue;

                var servicio = servicios?.FirstOrDefault(s => s.Id == reserva.ServicioId);

                // 🔧 VERIFICAR COMPROBANTE DE MÚLTIPLES FORMAS
                bool tieneComprobante = false;

                foreach (var comp in comprobantes)
                {
                    // Verificar por ReservaId exacto
                    if (comp.ReservaId == reserva.Id)
                    {
                        tieneComprobante = true;
                        Console.WriteLine($"✅ Comprobante encontrado para reserva {reserva.Id} por ReservaId");
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
                            Console.WriteLine($"✅ Comprobante encontrado para reserva {reserva.Id} por descripción");
                            break;
                        }
                    }
                }

                Console.WriteLine($"Reserva {reserva.Id} - Tiene comprobante: {tieneComprobante}");

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

            // 🔧 MOSTRAR ESTADO CORRECTO
            MostrarEstadoVacio();

            // Debug final
            Console.WriteLine($"📊 RESUMEN:");
            Console.WriteLine($"  - Reservas totales: {reservas.Count()}");
            Console.WriteLine($"  - Comprobantes totales: {comprobantes.Count}");
            Console.WriteLine($"  - Reservas sin comprobante: {reservasParaMostrar.Count}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar reservas pendientes: {ex.Message}", "OK");
        }
    }

    private void MostrarEstadoVacio()
    {
        bool hayPagosPendientes = ReservasPendientesPago.Any();

        collectionViewPagosPendientes.IsVisible = hayPagosPendientes;
        stackNoPayments.IsVisible = !hayPagosPendientes;

        Console.WriteLine($"🎯 Estado UI - Hay pagos pendientes: {hayPagosPendientes}");
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