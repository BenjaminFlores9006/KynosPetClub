using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KynosPetClub.Views;

public partial class vAprobarPagos : ContentPage, INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly Usuario _admin;

    public ObservableCollection<ComprobanteViewModel> Comprobantes { get; set; } = new();

    private List<ComprobanteViewModel> _todosComprobantes = new();
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

    public vAprobarPagos(Usuario admin)
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

        CargarComprobantes();
    }

    private async void CargarComprobantes()
    {
        try
        {
            IsLoading = true;

            // Obtener comprobantes pendientes
            var comprobantes = await _apiService.ObtenerComprobantesPendientesAsync();
            var usuarios = await _apiService.ObtenerTodosUsuariosAsync();

            // Obtener todas las reservas para poder mapear los servicios
            var todasReservas = new List<Reserva>();
            foreach (var usuario in usuarios)
            {
                if (usuario.Id.HasValue)
                {
                    var reservasUsuario = await _apiService.ObtenerReservasUsuarioAsync(usuario.Id.Value);
                    if (reservasUsuario != null)
                    {
                        todasReservas.AddRange(reservasUsuario);
                    }
                }
            }

            Comprobantes.Clear();
            _todosComprobantes.Clear();

            foreach (var comprobante in comprobantes)
            {
                var usuario = usuarios.FirstOrDefault(u => u.Id == comprobante.UsuarioId);
                var reserva = todasReservas.FirstOrDefault(r => r.Id == comprobante.ReservaId);

                var comprobanteVM = new ComprobanteViewModel
                {
                    Id = comprobante.Id,
                    Descripcion = comprobante.Descripcion,
                    FechaSubida = comprobante.FechaSubida,
                    UrlArchivo = comprobante.UrlArchivo,
                    Estado = comprobante.Estado,
                    ComentarioAdmin = comprobante.ComentarioAdmin,
                    UsuarioId = comprobante.UsuarioId,
                    ReservaId = comprobante.ReservaId,
                    UsuarioNombre = usuario != null ? $"{usuario.nombre} {usuario.apellido}" : "Usuario desconocido",
                    ReservaNombre = reserva?.Servicio?.Nombre ?? "Servicio desconocido",
                    ComprobanteOriginal = comprobante
                };

                Comprobantes.Add(comprobanteVM);
                _todosComprobantes.Add(comprobanteVM);
            }

            ActualizarContador();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"❌ Error al cargar comprobantes: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ActualizarContador()
    {
        if (lblContador != null)
        {
            lblContador.Text = $"💳 {Comprobantes.Count} comprobante(s) pendiente(s)";
        }
    }

    private async void btnAprobar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ComprobanteViewModel comprobante)
        {
            try
            {
                bool confirmar = await DisplayAlert(
                    "Aprobar Comprobante",
                    $"¿Aprobar el comprobante de {comprobante.UsuarioNombre}?\n\nEsto cambiará la reserva a 'En curso'.",
                    "Sí, aprobar",
                    "Cancelar");

                if (!confirmar) return;

                IsLoading = true;

                // 1. Actualizar estado del comprobante
                comprobante.ComprobanteOriginal.Estado = "Aprobado";
                comprobante.ComprobanteOriginal.ComentarioAdmin = "Pago aprobado por administrador";

                var resultadoComprobante = await _apiService.ActualizarComprobanteAsync(comprobante.ComprobanteOriginal);

                if (!resultadoComprobante)
                {
                    await DisplayAlert("Error", "❌ No se pudo actualizar el comprobante", "OK");
                    return;
                }

                // 2. Cambiar estado de la reserva a "En curso"
                var reservas = await _apiService.ObtenerReservasUsuarioAsync(comprobante.UsuarioId);
                var reserva = reservas?.FirstOrDefault(r => r.Id == comprobante.ReservaId);

                if (reserva != null)
                {
                    reserva.Estado = "En curso";
                    var resultadoReserva = await _apiService.ActualizarReservaAsync(reserva);

                    if (!resultadoReserva)
                    {
                        await DisplayAlert("Advertencia", "⚠️ Comprobante aprobado pero no se pudo actualizar la reserva", "OK");
                    }
                }

                // 3. Remover de la lista (ya no es pendiente)
                Comprobantes.Remove(comprobante);
                _todosComprobantes.Remove(comprobante);
                ActualizarContador();

                await DisplayAlert("Éxito", "✅ Comprobante aprobado y reserva actualizada a 'En curso'", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"❌ Error al aprobar: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async void btnRechazar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ComprobanteViewModel comprobante)
        {
            try
            {
                string motivo = await DisplayPromptAsync(
                    "Rechazar Comprobante",
                    "Ingresa el motivo del rechazo:",
                    "OK",
                    "Cancelar",
                    placeholder: "Ej: Imagen no clara, datos incorrectos...");

                if (string.IsNullOrEmpty(motivo)) return;

                IsLoading = true;

                // Actualizar estado del comprobante
                comprobante.ComprobanteOriginal.Estado = "Rechazado";
                comprobante.ComprobanteOriginal.ComentarioAdmin = motivo;

                var resultado = await _apiService.ActualizarComprobanteAsync(comprobante.ComprobanteOriginal);

                if (resultado)
                {
                    // Remover de la lista (ya no es pendiente)
                    Comprobantes.Remove(comprobante);
                    _todosComprobantes.Remove(comprobante);
                    ActualizarContador();

                    await DisplayAlert("Éxito", "✅ Comprobante rechazado", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "❌ No se pudo rechazar el comprobante", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"❌ Error al rechazar: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async void btnComentar_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ComprobanteViewModel comprobante)
        {
            try
            {
                string comentario = await DisplayPromptAsync(
                    "Agregar Comentario",
                    "Ingresa un comentario para el usuario:",
                    "Guardar",
                    "Cancelar",
                    placeholder: "Ej: Favor enviar imagen más clara...");

                if (string.IsNullOrEmpty(comentario)) return;

                IsLoading = true;

                // Actualizar comentario (mantener estado pendiente)
                comprobante.ComprobanteOriginal.ComentarioAdmin = comentario;

                var resultado = await _apiService.ActualizarComprobanteAsync(comprobante.ComprobanteOriginal);

                if (resultado)
                {
                    // Actualizar el ViewModel
                    comprobante.ComentarioAdmin = comentario;
                    comprobante.OnPropertyChanged(nameof(comprobante.TieneComentarioAdmin));

                    await DisplayAlert("Éxito", "✅ Comentario agregado", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "❌ No se pudo agregar el comentario", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"❌ Error al comentar: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async void OnImageTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is ComprobanteViewModel comprobante)
        {
            // Mostrar imagen en pantalla completa
            await Navigation.PushAsync(new ContentPage
            {
                Title = "Comprobante",
                Content = new ScrollView
                {
                    Content = new StackLayout
                    {
                        Children =
                        {
                            new Image
                            {
                                Source = comprobante.UrlArchivo,
                                Aspect = Aspect.AspectFit,
                                BackgroundColor = Colors.White
                            }
                        }
                    }
                }
            });
        }
    }

    private async void btnVolver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchTerm = e.NewTextValue?.ToLower().Trim() ?? string.Empty;

        Comprobantes.Clear();

        if (string.IsNullOrEmpty(searchTerm))
        {
            foreach (var comprobante in _todosComprobantes)
            {
                Comprobantes.Add(comprobante);
            }
        }
        else
        {
            var comprobantesFiltrados = _todosComprobantes.Where(c =>
                c.Descripcion.ToLower().Contains(searchTerm) ||
                c.UsuarioNombre.ToLower().Contains(searchTerm) ||
                c.ReservaNombre.ToLower().Contains(searchTerm));

            foreach (var comprobante in comprobantesFiltrados)
            {
                Comprobantes.Add(comprobante);
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

// ViewModel para comprobantes
public class ComprobanteViewModel : INotifyPropertyChanged
{
    private string _comentarioAdmin;

    public int Id { get; set; }
    public string Descripcion { get; set; }
    public DateTime FechaSubida { get; set; }
    public string UrlArchivo { get; set; }
    public string Estado { get; set; }
    public int UsuarioId { get; set; }
    public int ReservaId { get; set; }
    public string UsuarioNombre { get; set; }
    public string ReservaNombre { get; set; }
    public Comprobante ComprobanteOriginal { get; set; }

    public string ComentarioAdmin
    {
        get => _comentarioAdmin;
        set
        {
            _comentarioAdmin = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TieneComentarioAdmin));
        }
    }

    public string FechaSubidaTexto => $"📅 Subido: {FechaSubida:dd/MM/yyyy HH:mm}";
    public string UsuarioInfo => $"👤 Usuario: {UsuarioNombre}";
    public string ReservaInfo => $"🐾 Servicio: {ReservaNombre}";
    public bool EsPendiente => Estado == "Pendiente";
    public bool TieneComentarioAdmin => !string.IsNullOrEmpty(ComentarioAdmin);

    public string EstadoColor
    {
        get
        {
            return Estado switch
            {
                "Pendiente" => "#FF9800", // Naranja
                "Aprobado" => "#4CAF50",   // Verde
                "Rechazado" => "#F44336",  // Rojo
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