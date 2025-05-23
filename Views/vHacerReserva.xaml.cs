using KynosPetClub.Models;
using KynosPetClub.Services;
using System.ComponentModel;

namespace KynosPetClub.Views;

public partial class vHacerReserva : ContentPage, INotifyPropertyChanged
{
    private readonly Usuario _usuario;
    private readonly ApiService _apiService;
    private List<Servicio> _servicios;
    private List<Mascota> _mascotas;
    private Servicio _servicioSeleccionado;

    public vHacerReserva(Usuario usuario, Servicio servicioSeleccionado = null)
    {
        InitializeComponent();
        _usuario = usuario;
        _apiService = new ApiService();
        _servicioSeleccionado = servicioSeleccionado;

        BindingContext = this;

        // Configurar fecha mínima (hoy) y máxima (30 días adelante)
        datePicker.MinimumDate = DateTime.Today;
        datePicker.MaximumDate = DateTime.Today.AddDays(30);
        datePicker.Date = DateTime.Today.AddDays(1); // Por defecto mañana
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatos();
    }

    private async Task CargarDatos()
    {
        try
        {
            // Cargar servicios y mascotas en paralelo
            var serviciosTask = _apiService.ObtenerServiciosAsync();
            var mascotasTask = _apiService.ObtenerMascotasUsuarioAsync(_usuario.Id.Value);

            await Task.WhenAll(serviciosTask, mascotasTask);

            _servicios = await serviciosTask ?? new List<Servicio>();
            _mascotas = await mascotasTask ?? new List<Mascota>();

            // Configurar picker de servicios
            pickerServicio.ItemsSource = _servicios.Select(s => s.Nombre).ToList();
            pickerServicio.ItemDisplayBinding = new Binding(".");

            // Agregar evento para cuando cambie la selección de servicio
            pickerServicio.SelectedIndexChanged += OnServicioSeleccionChanged;

            // Configurar picker de mascotas
            if (_mascotas.Any())
            {
                pickerMascota.ItemsSource = _mascotas.Select(m => $"{m.Nombre} ({m.Especie})").ToList();
                pickerMascota.ItemDisplayBinding = new Binding(".");
            }
            else
            {
                await DisplayAlert("Información",
                    "Necesitas registrar al menos una mascota antes de hacer una reserva.", "OK");
                await Navigation.PopAsync();
                return;
            }

            // Preseleccionar servicio si viene de la página principal
            if (_servicioSeleccionado != null)
            {
                var index = _servicios.FindIndex(s => s.Id == _servicioSeleccionado.Id);
                if (index >= 0)
                    pickerServicio.SelectedIndex = index;
            }

            // Configurar horas disponibles
            ConfigurarHorasDisponibles();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
        }
    }

    private void ConfigurarHorasDisponibles()
    {
        ActualizarHorasSegunServicio();
    }

    private void ActualizarHorasSegunServicio()
    {
        var horas = new List<string>();
        var servicioSeleccionado = pickerServicio.SelectedIndex >= 0 ? _servicios[pickerServicio.SelectedIndex] : null;

        if (servicioSeleccionado == null)
        {
            // Si no hay servicio seleccionado, mostrar horario general
            GenerarHorasGenerales(horas);
        }
        else
        {
            var nombreServicio = servicioSeleccionado.Nombre.ToLower();

            if (nombreServicio.Contains("peluquer"))
            {
                // Peluquería: Martes a Sábado, 09:00 - 16:30
                GenerarHorasPeluqueria(horas);
            }
            else
            {
                // Hospedaje, Guardería, Veterinaria: Lunes a Sábado, 09:00 - 17:00
                GenerarHorasOtrosServicios(horas);
            }
        }

        pickerHora.ItemsSource = horas;
        pickerHora.ItemDisplayBinding = new Binding(".");
        pickerHora.SelectedIndex = -1; // Resetear selección
    }

    private void GenerarHorasGenerales(List<string> horas)
    {
        // Horario general de 09:00 a 17:00
        for (int hora = 9; hora <= 17; hora++)
        {
            horas.Add($"{hora:00}:00");
            if (hora < 17)
                horas.Add($"{hora:00}:30");
        }
    }

    private void GenerarHorasPeluqueria(List<string> horas)
    {
        // Peluquería: 09:00 - 16:30
        for (int hora = 9; hora <= 16; hora++)
        {
            horas.Add($"{hora:00}:00");
            if (hora == 16)
            {
                horas.Add("16:30"); // Última hora para peluquería
            }
            else
            {
                horas.Add($"{hora:00}:30");
            }
        }
    }

    private void GenerarHorasOtrosServicios(List<string> horas)
    {
        // Otros servicios: 09:00 - 17:00
        for (int hora = 9; hora <= 17; hora++)
        {
            horas.Add($"{hora:00}:00");
            if (hora < 17)
                horas.Add($"{hora:00}:30");
        }
    }

    private void OnServicioSeleccionChanged(object sender, EventArgs e)
    {
        // Actualizar horarios y validación de fecha cuando cambie el servicio
        ActualizarHorasSegunServicio();
        ValidarFechaSegunServicio();
    }

    private void DatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        ValidarFechaSegunServicio();
        ActualizarHorasSegunServicio();
    }

    private void ValidarFechaSegunServicio()
    {
        var fechaSeleccionada = datePicker.Date;
        var servicioSeleccionado = pickerServicio.SelectedIndex >= 0 ? _servicios[pickerServicio.SelectedIndex] : null;

        if (servicioSeleccionado == null)
        {
            // Sin servicio seleccionado
            lblFechaInfo.Text = "Selecciona un servicio para ver los días disponibles";
            lblFechaInfo.TextColor = Colors.Gray;
            pickerHora.IsEnabled = false;
            return;
        }

        var nombreServicio = servicioSeleccionado.Nombre.ToLower();
        var diaSemana = fechaSeleccionada.DayOfWeek;

        if (nombreServicio.Contains("peluquer"))
        {
            // Peluquería: Martes a Sábado
            if (diaSemana == DayOfWeek.Sunday || diaSemana == DayOfWeek.Monday)
            {
                lblFechaInfo.Text = "❌ Peluquería: Solo de martes a sábado (09:00-16:30)";
                lblFechaInfo.TextColor = Colors.Red;
                pickerHora.IsEnabled = false;
            }
            else
            {
                lblFechaInfo.Text = "✅ Peluquería disponible: Martes a Sábado (09:00-16:30)";
                lblFechaInfo.TextColor = Colors.Green;
                pickerHora.IsEnabled = true;
            }
        }
        else
        {
            // Hospedaje, Guardería, Veterinaria: Lunes a Sábado
            if (diaSemana == DayOfWeek.Sunday)
            {
                lblFechaInfo.Text = "❌ Servicio disponible de lunes a sábado (09:00-17:00)";
                lblFechaInfo.TextColor = Colors.Red;
                pickerHora.IsEnabled = false;
            }
            else
            {
                lblFechaInfo.Text = "✅ Servicio disponible: Lunes a Sábado (09:00-17:00)";
                lblFechaInfo.TextColor = Colors.Green;
                pickerHora.IsEnabled = true;
            }
        }
    }

    private async void btnGuardarReserva_Clicked(object sender, EventArgs e)
    {
        if (!ValidarFormulario())
            return;

        try
        {
            btnGuardarReserva.IsEnabled = false;
            btnGuardarReserva.Text = "Guardando...";

            var servicioSeleccionado = _servicios[pickerServicio.SelectedIndex];
            var mascotaSeleccionada = _mascotas[pickerMascota.SelectedIndex];

            // Crear fecha y hora combinadas
            var fechaSeleccionada = datePicker.Date;
            var horaSeleccionada = pickerHora.SelectedItem.ToString();
            var partesHora = horaSeleccionada.Split(':');
            var fechaServicio = fechaSeleccionada.AddHours(int.Parse(partesHora[0])).AddMinutes(int.Parse(partesHora[1]));

            // LÓGICA NUEVA: Preguntar método de pago ANTES de guardar
            var confirmacion = await DisplayAlert("💳 Método de Pago",
                $"📋 Resumen de tu reserva:\n\n" +
                $"• Servicio: {servicioSeleccionado.Nombre}\n" +
                $"• Mascota: {mascotaSeleccionada.Nombre}\n" +
                $"• Fecha: {fechaServicio:dddd, dd/MM/yyyy}\n" +
                $"• Hora: {fechaServicio:HH:mm}\n" +
                $"• Precio: {servicioSeleccionado.Precio:C}\n\n" +
                $"¿Cómo deseas realizar el pago?",
                "💳 Pagar Ahora", "⏰ Pagar Después");

            if (confirmacion)
            {
                // PAGAR AHORA: Ir directo a vPagos SIN guardar reserva
                await Navigation.PushAsync(new vPagos(_usuario, servicioSeleccionado, mascotaSeleccionada, fechaServicio, 0));

                // Limpiar formulario ya que la reserva se creará cuando suba el comprobante
                LimpiarFormulario();
            }
            else
            {
                // PAGAR DESPUÉS: Guardar reserva primero
                var nuevaReserva = new Reserva
                {
                    FechaReserva = DateTime.Now,
                    FechaServicio = fechaServicio,
                    Estado = "Pendiente",
                    Comentarios = editorObservaciones.Text?.Trim() ?? "",
                    UsuarioId = _usuario.Id.Value,
                    MascotaId = mascotaSeleccionada.Id,
                    ServicioId = servicioSeleccionado.Id
                };

                var resultado = await _apiService.CrearReservaAsync(nuevaReserva);

                if (resultado == "OK" || int.TryParse(resultado, out int reservaId))
                {
                    await DisplayAlert("🎉 ¡Reserva Creada!",
                        $"Tu reserva ha sido agendada exitosamente.\n\n" +
                        $"ℹ️ Recuerda realizar el pago desde la sección 'Pagos' en la barra inferior.\n\n" +
                        $"La reserva aparecerá como 'Pendiente de pago' hasta que subas el comprobante.",
                        "Entendido");

                    LimpiarFormulario();
                }
                else
                {
                    await DisplayAlert("Error", $"No se pudo crear la reserva: {resultado}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al procesar la reserva: {ex.Message}", "OK");
        }
        finally
        {
            btnGuardarReserva.IsEnabled = true;
            btnGuardarReserva.Text = "Agendar Reserva";
        }
    }

    private bool ValidarFormulario()
    {
        if (pickerServicio.SelectedIndex == -1)
        {
            DisplayAlert("Validación", "Selecciona un servicio", "OK");
            return false;
        }

        if (pickerMascota.SelectedIndex == -1)
        {
            DisplayAlert("Validación", "Selecciona una mascota", "OK");
            return false;
        }

        if (pickerHora.SelectedIndex == -1)
        {
            DisplayAlert("Validación", "Selecciona una hora", "OK");
            return false;
        }

        var servicioSeleccionado = _servicios[pickerServicio.SelectedIndex];
        var fechaSeleccionada = datePicker.Date;
        var nombreServicio = servicioSeleccionado.Nombre.ToLower();
        var diaSemana = fechaSeleccionada.DayOfWeek;

        // Validar días según el servicio
        if (nombreServicio.Contains("peluquer"))
        {
            if (diaSemana == DayOfWeek.Sunday || diaSemana == DayOfWeek.Monday)
            {
                DisplayAlert("Validación", "Peluquería solo está disponible de martes a sábado", "OK");
                return false;
            }
        }
        else
        {
            if (diaSemana == DayOfWeek.Sunday)
            {
                DisplayAlert("Validación", "Este servicio solo está disponible de lunes a sábado", "OK");
                return false;
            }
        }

        // Validar que no sea una fecha pasada
        var horaSeleccionada = pickerHora.SelectedItem.ToString();
        var partesHora = horaSeleccionada.Split(':');
        var fechaServicio = fechaSeleccionada.AddHours(int.Parse(partesHora[0])).AddMinutes(int.Parse(partesHora[1]));

        if (fechaServicio <= DateTime.Now)
        {
            DisplayAlert("Validación", "No puedes agendar una cita en el pasado", "OK");
            return false;
        }

        // Validar horarios según el servicio
        var hora = int.Parse(partesHora[0]);
        var minutos = int.Parse(partesHora[1]);

        if (nombreServicio.Contains("peluquer"))
        {
            // Peluquería: 09:00 - 16:30
            if (hora < 9 || (hora == 16 && minutos > 30) || hora > 16)
            {
                DisplayAlert("Validación", "Peluquería atiende de 09:00 a 16:30", "OK");
                return false;
            }
        }
        else
        {
            // Otros servicios: 09:00 - 17:00
            if (hora < 9 || hora > 17)
            {
                DisplayAlert("Validación", "Este servicio atiende de 09:00 a 17:00", "OK");
                return false;
            }
        }

        return true;
    }

    private void btnLimpiarFormulario_Clicked(object sender, EventArgs e)
    {
        LimpiarFormulario();
    }

    private void LimpiarFormulario()
    {
        pickerServicio.SelectedIndex = -1;
        pickerMascota.SelectedIndex = -1;
        pickerHora.SelectedIndex = -1;
        datePicker.Date = DateTime.Today.AddDays(1);
        editorObservaciones.Text = "";
        lblFechaInfo.Text = "Selecciona un servicio para ver días disponibles";
        lblFechaInfo.TextColor = Colors.Gray;
        pickerHora.IsEnabled = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}