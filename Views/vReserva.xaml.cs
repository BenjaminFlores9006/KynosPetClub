using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace KynosPetClub.Views;

public partial class vReserva : ContentPage, INotifyPropertyChanged
{
    private readonly Usuario _usuario;
    private readonly ApiService _apiService;
    private ObservableCollection<ReservaViewModel> _reservasActivas;

    // Propiedades para binding
    public Usuario Usuario => _usuario;
    public ObservableCollection<ReservaViewModel> ReservasActivas
    {
        get => _reservasActivas;
        set
        {
            _reservasActivas = value;
            OnPropertyChanged();
        }
    }

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
        await CargarReservasActivas();
    }

    private async Task CargarReservasActivas()
    {
        try
        {
            loadingIndicator.IsVisible = true;
            loadingIndicator.IsRunning = true;
            emptyStateLayout.IsVisible = false;

            var reservas = await _apiService.ObtenerReservasUsuarioAsync(_usuario.Id.Value);

            if (reservas != null)
            {
                // Filtrar solo reservas activas (Pendiente y En curso)
                var reservasActivas = reservas
                    .Where(r => r.Estado == "Pendiente" || r.Estado == "En curso")
                    .OrderBy(r => r.FechaServicio)
                    .ToList();

                ReservasActivas.Clear();

                foreach (var reserva in reservasActivas)
                {
                    // Cargar datos relacionados si no vienen en la consulta
                    if (reserva.Servicio == null)
                        reserva.Servicio = await _apiService.ObtenerServicioPorIdAsync(reserva.ServicioId);

                    if (reserva.Mascota == null)
                        reserva.Mascota = await _apiService.ObtenerMascotaPorIdAsync(reserva.MascotaId);

                    ReservasActivas.Add(new ReservaViewModel(reserva));
                }

                emptyStateLayout.IsVisible = !ReservasActivas.Any();
            }
            else
            {
                emptyStateLayout.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar reservas: {ex.Message}", "OK");
            emptyStateLayout.IsVisible = true;
        }
        finally
        {
            loadingIndicator.IsVisible = false;
            loadingIndicator.IsRunning = false;
        }
    }

    private async void OnVerDetallesClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ReservaViewModel reservaVM)
        {
            var detalles = $"Servicio: {reservaVM.Servicio.Nombre}\n" +
                          $"Mascota: {reservaVM.MascotaInfo}\n" +
                          $"Fecha: {reservaVM.FechaServicioFormateada}\n" +
                          $"Estado: {reservaVM.Estado}\n" +
                          $"Precio: {reservaVM.Servicio.Precio:C}";

            if (!string.IsNullOrEmpty(reservaVM.Comentarios))
                detalles += $"\nComentarios: {reservaVM.Comentarios}";

            await DisplayAlert("Detalles de la Reserva", detalles, "OK");
        }
    }

    private async void OnCancelarReservaClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ReservaViewModel reservaVM)
        {
            var confirmar = await DisplayAlert("Confirmar Cancelación",
                $"¿Estás seguro de que deseas cancelar la reserva de {reservaVM.Servicio.Nombre}?",
                "Sí", "No");

            if (confirmar)
            {
                try
                {
                    // Actualizar el estado de la reserva a "Cancelado"
                    var reservaActualizada = reservaVM.ReservaOriginal;
                    reservaActualizada.Estado = "Cancelado";

                    var resultado = await _apiService.ActualizarReservaAsync(reservaActualizada);

                    if (resultado)
                    {
                        await DisplayAlert("Éxito", "Reserva cancelada correctamente", "OK");
                        await CargarReservasActivas(); // Recargar la lista
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo cancelar la reserva", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error al cancelar reserva: {ex.Message}", "OK");
                }
            }
        }
    }

    private async void OnVerServiciosClicked(object sender, EventArgs e)
    {
        // Navegar de vuelta a la pantalla principal de servicios
        await Navigation.PopToRootAsync();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// ViewModel para las reservas con propiedades calculadas
public class ReservaViewModel
{
    public Reserva ReservaOriginal { get; set; }

    public ReservaViewModel(Reserva reserva)
    {
        ReservaOriginal = reserva;
    }

    public int Id => ReservaOriginal.Id;
    public string Estado => ReservaOriginal.Estado;
    public DateTime FechaServicio => ReservaOriginal.FechaServicio;
    public Servicio Servicio => ReservaOriginal.Servicio;
    public Mascota Mascota => ReservaOriginal.Mascota;
    public string Comentarios => ReservaOriginal.Comentarios;

    public string MascotaInfo => $"{Mascota?.Nombre} ({Mascota?.Especie})";

    public string FechaServicioFormateada =>
        $"{FechaServicio:dddd, dd MMMM yyyy} a las {FechaServicio:HH:mm}";

    public string ImagenServicio => Servicio?.Nombre switch
    {
        "Veterinaria" => "veterinaria.jpg",
        "Peluqueria" => "peluqueria.png",
        "Guarderia" => "guarderia.png",
        "Hospedaje" => "hospedaje.png",
        _ => "servicio_generico.png"
    };

    public Color ColorEstado => Estado switch
    {
        "Pendiente" => Colors.Orange,
        "En curso" => Colors.Blue,
        "Completado" => Colors.Green,
        "Cancelado" => Colors.Red,
        _ => Colors.Gray
    };

    public bool PuedeCancelarse => Estado == "Pendiente";

    public bool TieneComentarios => !string.IsNullOrEmpty(Comentarios);
}