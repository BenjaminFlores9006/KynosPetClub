using KynosPetClub.Models;
using KynosPetClub.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KynosPetClub.Views;

public partial class vAdministrarUsuarios : ContentPage, INotifyPropertyChanged
{
    private readonly ApiService _apiService;
    private readonly Usuario _admin;

    public ObservableCollection<UsuarioViewModel> Usuarios { get; set; } = new();
    public ObservableCollection<Models.Rol> RolesDisponibles { get; set; } = new();
    public ObservableCollection<Models.Plan> PlanesDisponibles { get; set; } = new();

    private List<UsuarioViewModel> _todosUsuarios = new();
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();

            // Verificar si los elementos existen antes de usarlos
            if (loadingIndicator != null)
            {
                loadingIndicator.IsVisible = value;
                loadingIndicator.IsRunning = value;
            }
        }
    }

    public vAdministrarUsuarios(Usuario admin)
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            // Si InitializeComponent falla, mostrar error
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

            // Cargar datos en paralelo para mejor rendimiento
            var usuariosTask = _apiService.ObtenerTodosUsuariosAsync();
            var rolesTask = _apiService.ObtenerRolesAsync();
            var planesTask = _apiService.ObtenerPlanesAsync();

            await Task.WhenAll(usuariosTask, rolesTask, planesTask);

            var usuarios = await usuariosTask;
            var roles = await rolesTask;
            var planes = await planesTask;

            // Cargar roles y planes disponibles
            RolesDisponibles.Clear();
            foreach (var rol in roles)
            {
                RolesDisponibles.Add(rol);
            }

            PlanesDisponibles.Clear();
            foreach (var plan in planes)
            {
                PlanesDisponibles.Add(plan);
            }

            // Crear ViewModels de usuarios
            Usuarios.Clear();
            _todosUsuarios.Clear();

            foreach (var usuario in usuarios)
            {
                var rol = roles.FirstOrDefault(r => r.Id == usuario.RolId);
                var plan = planes.FirstOrDefault(p => p.Id == usuario.PlanId);

                var usuarioVM = new UsuarioViewModel
                {
                    Id = usuario.Id,
                    nombre = usuario.nombre,
                    apellido = usuario.apellido,
                    correo = usuario.correo,
                    fechanac = usuario.fechanac,
                    RolId = usuario.RolId,
                    PlanId = usuario.PlanId,
                    RolNombre = rol?.NombreRol?.ToUpper() ?? "SIN ROL",
                    PlanNombre = plan?.Nombre ?? "Sin plan",
                    RolSeleccionado = rol,
                    PlanSeleccionado = plan,
                    UsuarioOriginal = usuario
                };

                Usuarios.Add(usuarioVM);
                _todosUsuarios.Add(usuarioVM);
            }

            ActualizarContador();

            // Asegurar que el CollectionView esté vinculado
            if (cvUsuarios != null)
            {
                cvUsuarios.ItemsSource = Usuarios;
            }

            // Debug: Verificar datos
            Console.WriteLine($"Total usuarios cargados: {Usuarios.Count}");
            if (Usuarios.Any())
            {
                Console.WriteLine($"Primer usuario: {Usuarios.First().NombreCompleto}");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"❌ Error al cargar usuarios: {ex.Message}", "OK");
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
            lblContador.Text = $"👥 {Usuarios.Count} usuario(s) encontrado(s)";
        }
    }

    private async void btnEditarUsuario_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is UsuarioViewModel usuario)
        {
            // Cerrar cualquier panel de edición abierto
            foreach (var u in Usuarios)
            {
                u.EstaEditando = false;
            }

            // Abrir panel de edición para este usuario
            usuario.EstaEditando = true;
        }
    }

    private async void btnGuardarCambios_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is UsuarioViewModel usuario)
        {
            try
            {
                IsLoading = true;

                // Crear objeto usuario actualizado
                var usuarioActualizado = new Usuario
                {
                    Id = usuario.Id,
                    nombre = usuario.nombre,
                    apellido = usuario.apellido,
                    correo = usuario.correo,
                    fechanac = usuario.fechanac,
                    RolId = usuario.RolSeleccionado?.Id,
                    PlanId = usuario.PlanSeleccionado?.Id,
                    contraseña = usuario.UsuarioOriginal.contraseña, // Mantener contraseña original
                    foto = usuario.UsuarioOriginal.foto,
                    AuthId = usuario.UsuarioOriginal.AuthId
                };

                // Llamar al API para actualizar
                var resultado = await _apiService.ActualizarUsuarioAsync(usuarioActualizado);

                if (resultado)
                {
                    // Actualizar los datos locales
                    usuario.RolId = usuario.RolSeleccionado?.Id;
                    usuario.PlanId = usuario.PlanSeleccionado?.Id;
                    usuario.RolNombre = usuario.RolSeleccionado?.NombreRol?.ToUpper() ?? "SIN ROL";
                    usuario.PlanNombre = usuario.PlanSeleccionado?.Nombre ?? "Sin plan";
                    usuario.UsuarioOriginal = usuarioActualizado;

                    // Cerrar panel de edición
                    usuario.EstaEditando = false;

                    // Forzar actualización visual
                    OnPropertyChanged(nameof(Usuarios));

                    // También actualizar en la lista completa para búsquedas
                    var usuarioEnLista = _todosUsuarios.FirstOrDefault(u => u.Id == usuario.Id);
                    if (usuarioEnLista != null)
                    {
                        usuarioEnLista.RolId = usuario.RolId;
                        usuarioEnLista.PlanId = usuario.PlanId;
                        usuarioEnLista.RolNombre = usuario.RolNombre;
                        usuarioEnLista.PlanNombre = usuario.PlanNombre;
                        usuarioEnLista.RolSeleccionado = usuario.RolSeleccionado;
                        usuarioEnLista.PlanSeleccionado = usuario.PlanSeleccionado;
                        usuarioEnLista.UsuarioOriginal = usuarioActualizado;
                    }

                    await DisplayAlert("Éxito", "✅ Usuario actualizado correctamente", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "❌ No se pudo actualizar el usuario", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"❌ Error al guardar cambios: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void btnCancelarEdicion_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is UsuarioViewModel usuario)
        {
            // Restaurar valores originales
            var rolOriginal = RolesDisponibles.FirstOrDefault(r => r.Id == usuario.RolId);
            var planOriginal = PlanesDisponibles.FirstOrDefault(p => p.Id == usuario.PlanId);

            usuario.RolSeleccionado = rolOriginal;
            usuario.PlanSeleccionado = planOriginal;

            // Cerrar panel de edición
            usuario.EstaEditando = false;
        }
    }

    private async void btnVolver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void searchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchTerm = e.NewTextValue?.ToLower().Trim() ?? string.Empty;

        Usuarios.Clear();

        if (string.IsNullOrEmpty(searchTerm))
        {
            // Mostrar todos los usuarios
            foreach (var usuario in _todosUsuarios)
            {
                Usuarios.Add(usuario);
            }
        }
        else
        {
            // Filtrar usuarios
            var usuariosFiltrados = _todosUsuarios.Where(u =>
                u.nombre.ToLower().Contains(searchTerm) ||
                u.apellido.ToLower().Contains(searchTerm) ||
                u.correo.ToLower().Contains(searchTerm) ||
                u.RolNombre.ToLower().Contains(searchTerm) ||
                u.PlanNombre.ToLower().Contains(searchTerm));

            foreach (var usuario in usuariosFiltrados)
            {
                Usuarios.Add(usuario);
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

// ViewModel para representar usuarios en la vista
public class UsuarioViewModel : INotifyPropertyChanged
{
    private bool _estaEditando;
    private Models.Rol _rolSeleccionado;
    private Models.Plan _planSeleccionado;
    private string _rolNombre;
    private string _planNombre;

    public int? Id { get; set; }
    public string nombre { get; set; }
    public string apellido { get; set; }
    public string correo { get; set; }
    public DateTime fechanac { get; set; }
    public int? RolId { get; set; }
    public int? PlanId { get; set; }

    public string RolNombre
    {
        get => _rolNombre;
        set
        {
            _rolNombre = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RolColor)); // También actualizar el color
        }
    }

    public string PlanNombre
    {
        get => _planNombre;
        set
        {
            _planNombre = value;
            OnPropertyChanged();
        }
    }

    public Usuario UsuarioOriginal { get; set; }

    public string NombreCompleto => $"{nombre} {apellido}";
    public string FechaNacimiento => fechanac.ToString("dd/MM/yyyy");

    public string RolColor
    {
        get
        {
            return RolId switch
            {
                1 => "#E74C3C", // Administrador - Rojo
                2 => "#3498DB", // Usuario - Azul
                3 => "#F39C12", // Funcionario - Naranja
                _ => "#95A5A6"   // Sin rol - Gris
            };
        }
    }

    public bool EstaEditando
    {
        get => _estaEditando;
        set
        {
            _estaEditando = value;
            OnPropertyChanged();
        }
    }

    public Models.Rol RolSeleccionado
    {
        get => _rolSeleccionado;
        set
        {
            _rolSeleccionado = value;
            OnPropertyChanged();
        }
    }

    public Models.Plan PlanSeleccionado
    {
        get => _planSeleccionado;
        set
        {
            _planSeleccionado = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}