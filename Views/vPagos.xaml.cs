using KynosPetClub.Models;
using KynosPetClub.Services;
using System.ComponentModel;

namespace KynosPetClub.Views;

public partial class vPagos : ContentPage, INotifyPropertyChanged
{
    private readonly Usuario _usuario;
    private readonly Servicio _servicio;
    private readonly Mascota _mascota;
    private readonly DateTime _fechaServicio;
    private readonly ApiService _apiService;
    private byte[] _comprobanteData;
    private string _comprobanteFileName;
    private int _reservaId;
    private readonly Plan _planSeleccionado;

    public List<string> MetodosPago { get; } = new List<string>
    {
        "Transferencia bancaria",
        "Depósito bancario"
    };

    // Propiedad para binding del Usuario al BottomNavBar
    public Usuario Usuario => _usuario;

    public vPagos(Usuario usuario, Servicio servicio, Mascota mascota, DateTime fechaServicio, int reservaId = 0, Plan planSeleccionado = null)
    {
        InitializeComponent();
        _usuario = usuario;
        _servicio = servicio;
        _mascota = mascota;
        _fechaServicio = fechaServicio;
        _reservaId = reservaId;
        _planSeleccionado = planSeleccionado;
        _apiService = new ApiService();

        BindingContext = this;
        CargarDatosPago();
    }

    private void CargarDatosPago()
    {
        try
        {
            // Cargar información del servicio
            lblNombreServicio.Text = _servicio.Nombre;
            lblDescripcionServicio.Text = !string.IsNullOrEmpty(_servicio.Descripcion)
                ? _servicio.Descripcion
                : $"Servicio para {_mascota.Nombre}";

            // Formatear fecha y hora
            var cultura = new System.Globalization.CultureInfo("es-ES");
            lblFechaHora.Text = $"{_fechaServicio.ToString("dddd, dd 'de' MMMM yyyy", cultura)} a las {_fechaServicio:HH:mm}";

            // Mostrar precio
            lblTotal.Text = _servicio.Precio.ToString("C");

            // Configurar título de la página con información adicional
            Title = $"Pago - {_servicio.Nombre}";
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
        }
    }

    private async void btnSeleccionarComprobante_Clicked(object sender, EventArgs e)
    {
        try
        {
            var opcion = await DisplayActionSheet(
                "Seleccionar comprobante",
                "Cancelar",
                null,
                "📷 Tomar foto",
                "🖼️ Seleccionar de galería");

            if (opcion == "📷 Tomar foto")
            {
                await TomarFoto();
            }
            else if (opcion == "🖼️ Seleccionar de galería")
            {
                await SeleccionarDeGaleria();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al acceder a la cámara/galería: {ex.Message}", "OK");
        }
    }

    private async Task TomarFoto()
    {
        try
        {
            var result = await MediaPicker.Default.CapturePhotoAsync();
            if (result != null)
            {
                await ProcesarImagenSeleccionada(result);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al tomar foto: {ex.Message}", "OK");
        }
    }

    private async Task SeleccionarDeGaleria()
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                await ProcesarImagenSeleccionada(result);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al seleccionar imagen: {ex.Message}", "OK");
        }
    }

    private async Task ProcesarImagenSeleccionada(FileResult result)
    {
        try
        {
            btnSeleccionarComprobante.Text = "Procesando imagen...";
            btnSeleccionarComprobante.IsEnabled = false;

            var stream = await result.OpenReadAsync();
            _comprobanteData = new byte[stream.Length];
            await stream.ReadAsync(_comprobanteData, 0, (int)stream.Length);
            _comprobanteFileName = result.FileName;

            // Mostrar imagen
            imgComprobante.Source = ImageSource.FromStream(() => new MemoryStream(_comprobanteData));
            frameComprobanteImagen.IsVisible = true;

            // Habilitar botón de pagar
            btnPagar.IsEnabled = pkMetodoPago.SelectedItem != null;

            await DisplayAlert("✅ Éxito", "Comprobante cargado correctamente", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al procesar imagen: {ex.Message}", "OK");
        }
        finally
        {
            btnSeleccionarComprobante.Text = "📸 Tomar foto del comprobante";
            btnSeleccionarComprobante.IsEnabled = true;
        }
    }

    private void pkMetodoPago_SelectedIndexChanged(object sender, EventArgs e)
    {
        // Habilitar botón de pagar si también hay comprobante
        btnPagar.IsEnabled = pkMetodoPago.SelectedItem != null && _comprobanteData != null;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Suscribirse al evento de cambio del picker
        pkMetodoPago.SelectedIndexChanged += pkMetodoPago_SelectedIndexChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Desuscribirse del evento
        pkMetodoPago.SelectedIndexChanged -= pkMetodoPago_SelectedIndexChanged;
    }

    private async void btnPagar_Clicked(object sender, EventArgs e)
    {
        if (!ValidarDatosPago())
        {
            return;
        }

        try
        {
            btnPagar.IsEnabled = false;
            btnPagar.Text = "⏳ Procesando pago...";

            // Lógica especial para planes
            if (_servicio.Id == 0 && _planSeleccionado != null) // ID especial para planes
            {
                // Actualizar el plan del usuario
                _usuario.PlanId = _planSeleccionado.Id;
                var actualizacionExitosa = await _apiService.ActualizarUsuarioAsync(_usuario);

                if (actualizacionExitosa)
                {
                    await DisplayAlert("✅ Éxito", "Plan adquirido correctamente", "OK");
                    await Navigation.PushAsync(new vPlanes(_usuario));
                    return;
                }
                else
                {
                    await DisplayAlert("❌ Error", "No se pudo adquirir el plan", "OK");
                    return;
                }
            }

            // LÓGICA para reservas normales
            int reservaIdFinal = _reservaId;

            if (_reservaId == 0)
            {
                // Crear la reserva primero (caso "Pagar Ahora")
                var nuevaReserva = new Reserva
                {
                    FechaReserva = DateTime.Now,
                    FechaServicio = _fechaServicio,
                    Estado = "Pendiente",
                    Comentarios = $"Reserva con pago inmediato - {_servicio.Nombre}",
                    UsuarioId = _usuario.Id.Value,
                    MascotaId = _mascota.Id,
                    ServicioId = _servicio.Id
                };

                var resultadoCreacion = await _apiService.CrearReservaAsync(nuevaReserva);

                if (int.TryParse(resultadoCreacion, out int nuevaReservaId))
                {
                    reservaIdFinal = nuevaReservaId;
                }
                else if (resultadoCreacion == "OK")
                {
                    reservaIdFinal = 0;
                }
                else
                {
                    await DisplayAlert("❌ Error",
                        $"No se pudo crear la reserva: {resultadoCreacion}\n\nPor favor, inténtalo nuevamente.",
                        "OK");
                    return;
                }
            }

            // Crear comprobante
            var comprobante = new Comprobante
            {
                Descripcion = $"Pago por {_servicio.Nombre} - {_mascota.Nombre} - {_fechaServicio:dd/MM/yyyy HH:mm}",
                UsuarioId = _usuario.Id.Value,
                ReservaId = reservaIdFinal,
                Estado = "Pendiente",
                FechaSubida = DateTime.Now
            };

            using var stream = new MemoryStream(_comprobanteData);
            var resultadoSubida = await _apiService.SubirComprobanteAsync(comprobante, stream, _comprobanteFileName);

            if (resultadoSubida == "OK" || int.TryParse(resultadoSubida, out _))
            {
                await DisplayAlert("🎉 ¡Pago Enviado!",
                    $"Tu comprobante de pago ha sido enviado correctamente.\n\n" +
                    $"📋 Detalles:\n" +
                    $"• Servicio: {_servicio.Nombre}\n" +
                    $"• Mascota: {_mascota.Nombre}\n" +
                    $"• Fecha: {_fechaServicio:dd/MM/yyyy HH:mm}\n" +
                    $"• Método: {pkMetodoPago.SelectedItem}\n" +
                    $"• Monto: {_servicio.Precio:C}\n\n" +
                    $"⏳ Nuestro equipo verificará el pago y te notificaremos cuando esté confirmado.",
                    "Perfecto");

                await Navigation.PushAsync(new vReserva(_usuario));
            }
            else
            {
                await DisplayAlert("❌ Error",
                    $"No se pudo procesar el pago:\n{resultadoSubida}\n\nPor favor, inténtalo nuevamente.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al procesar pago: {ex.Message}", "OK");
        }
        finally
        {
            btnPagar.IsEnabled = true;
            btnPagar.Text = "💳 Procesar Pago";
        }
    }

    private bool ValidarDatosPago()
    {
        if (pkMetodoPago.SelectedItem == null)
        {
            DisplayAlert("Validación", "Por favor selecciona un método de pago", "OK");
            return false;
        }

        if (_comprobanteData == null)
        {
            DisplayAlert("Validación", "Por favor sube un comprobante de pago", "OK");
            return false;
        }

        // Validar tamaño de archivo (máximo 5MB)
        if (_comprobanteData.Length > 5 * 1024 * 1024)
        {
            DisplayAlert("Validación", "El archivo es demasiado grande. Máximo 5MB permitido.", "OK");
            return false;
        }

        return true;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}