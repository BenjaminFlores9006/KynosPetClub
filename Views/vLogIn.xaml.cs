using KynosPetClub.Models;
using KynosPetClub.Services;
using Microsoft.Maui.ApplicationModel; // Para MainThread

namespace KynosPetClub.Views
{
    public partial class vLogIn : ContentPage
    {
        private readonly ApiService _apiService;
        private readonly GoogleAuthService _googleAuthService;

        public vLogIn()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _googleAuthService = new GoogleAuthService();
        }

        private async void OnGoogleLoginButtonClicked(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("Iniciando SignInWithGoogleAsync...");
                var authResult = await _googleAuthService.SignInWithGoogleAsync();

                if (authResult.Success && authResult.UserInfo != null)
                {
                    Console.WriteLine($"✅ Login con Google exitoso en Supabase Auth. Email: {authResult.UserInfo.Email}, AuthId: {authResult.UserInfo.Id}");

                    // Variable para almacenar el objeto Usuario que se pasará a vInicio
                    Usuario usuarioParaNavegacion = null;

                    Console.WriteLine($"Buscando usuario {authResult.UserInfo.Email} en mi base de datos...");
                    var usuarioEnMiDB = await _apiService.ObtenerUsuarioPorEmailAsync(authResult.UserInfo.Email);

                    if (usuarioEnMiDB == null)
                    {
                        // 3. El usuario es nuevo para MI BASE DE DATOS. Registrarlo.
                        Console.WriteLine($"❗ Usuario {authResult.UserInfo.Email} no encontrado en mi DB. Procediendo a registrarlo.");

                        var nuevoUsuario = new Models.Usuario
                        {
                            correo = authResult.UserInfo.Email,
                            AuthId = authResult.UserInfo.Id,
                            RolId = 2,
                            nombre = authResult.UserInfo.Name ?? authResult.UserInfo.Email.Split('@')[0],
                            apellido = "",
                            fechanac = DateTime.UtcNow,
                            foto = authResult.UserInfo.Picture,
                            contraseña = "" // Si la columna no es NULLABLE, esto es crucial.
                        };

                        Console.WriteLine($"Registrando nuevo usuario en mi DB: {nuevoUsuario.correo}");
                        var registroResultado = await _apiService.RegistrarUsuarioAsync(nuevoUsuario);

                        if (registroResultado == "OK")
                        {
                            Console.WriteLine($"✅ Usuario {nuevoUsuario.correo} registrado exitosamente en mi DB.");
                            usuarioParaNavegacion = nuevoUsuario; // El usuario recién creado
                        }
                        else
                        {
                            Console.WriteLine($"❌ Error al registrar usuario en mi DB: {registroResultado}");
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await DisplayAlert("Error", $"No se pudo registrar el usuario en la base de datos: {registroResultado}", "OK");
                            });
                            return; // Salir si el registro falló
                        }
                    }
                    else
                    {
                        // 4. El usuario ya existe en tu tabla 'usuario'.
                        Console.WriteLine($"ℹ️ Usuario {authResult.UserInfo.Email} ya existe en mi DB. Preparando para iniciar sesión.");
                        usuarioParaNavegacion = usuarioEnMiDB; // El usuario existente
                    }

                    // --- NAVEGACIÓN A vInicio ---
                    if (usuarioParaNavegacion != null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            // AQUI ES DONDE CAMBIAMOS A Navigation.PushAsync
                            // y pasamos el usuarioParaNavegacion al constructor de vInicio
                            await Navigation.PushAsync(new vInicio(usuarioParaNavegacion));
                            // Opcional: Eliminar la página de login de la pila
                            Navigation.RemovePage(this);
                            Console.WriteLine("Navegación a vInicio completada con usuario (Google).");
                        });
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await DisplayAlert("Error", "No se pudo obtener la información del usuario para navegar a la página de inicio.", "OK");
                        });
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Login Fallido: {authResult.ErrorMessage}");
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlert("Login Fallido", authResult.ErrorMessage, "OK");
                    });
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Autenticación con Google cancelada por el usuario.");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Acceso Cancelado", "El login con Google fue cancelado.", "OK");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error general en OnGoogleLoginButtonClicked: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Error", $"Ocurrió un error al iniciar sesión con Google: {ex.Message}", "OK");
                });
            }
            finally
            {
                // ... (restablecer UI si aplica)
            }
        }

        private async void btnIniciarSesion_Clicked(object sender, EventArgs e)
        {
            var correo = txtCorreo.Text?.Trim();
            var contraseña = txtPassword.Text;

            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contraseña))
            {
                await DisplayAlert("Error", "Por favor ingresa tu correo y contraseña", "OK");
                return;
            }

            if (!correo.Contains("@") || !correo.Contains("."))
            {
                await DisplayAlert("Error", "Por favor ingresa un correo electrónico válido", "Ok");
                return;
            }

            btnIniciarSesion.IsEnabled = false;
            btnIniciarSesion.Text = "Iniciando sesión...";

            try
            {
                // Autenticar con Supabase para obtener JWT si es necesario para el ApiService.
                // Si tu ApiService ya maneja el JWT internamente después de LoginUsuarioAsync,
                // esta llamada separada podría no ser estrictamente necesaria aquí si solo quieres pasar el Usuario.
                // Sin embargo, mantenerla para asegurar la autenticación en Supabase Auth.
                var resultadoAuthSupabase = await _apiService.AutenticarConSupabaseAsync(correo, contraseña);
                if (resultadoAuthSupabase != "OK")
                {
                    Console.WriteLine($"Autenticación JWT con Supabase Auth falló, pero se intentará el login normal a la DB: {resultadoAuthSupabase}");
                    // Puedes decidir mostrar un alert aquí o solo continuar con el login si esperas que LoginUsuarioAsync
                    // funcione independientemente de un JWT inicial (menos común).
                }

                // Tu lógica original de login para obtener el objeto Usuario de tu DB.
                var usuario = await _apiService.LoginUsuarioAsync(correo, contraseña);

                if (usuario != null)
                {
                    // Guardar datos de sesión (tu código original)
                    await SecureStorage.SetAsync("user_id", usuario.Id.ToString());
                    await SecureStorage.SetAsync("user_name", $"{usuario.nombre} {usuario.apellido}");
                    await SecureStorage.SetAsync("user_email", usuario.correo);

                    // Revisa si el JWT se guardó correctamente por _apiService.AutenticarConSupabaseAsync
                    // Si no, o si quieres un placeholder para desarrollo, puedes mantener esto.
                    var existeJWT = await SecureStorage.GetAsync("supabase_jwt");
                    if (string.IsNullOrEmpty(existeJWT))
                    {
                        // ADVERTENCIA: Esta clave es una clave ANON, no debe usarse para operaciones sensibles.
                        // Solo para asegurar que el JWT no esté vacío durante el desarrollo.
                        await SecureStorage.SetAsync("supabase_jwt", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNmd3licWF5a3llcmxqcWZxdHprIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDc2MTU1MTcsImV4cCI6MjA2MzE5MTUxN30.0NvvKf7vF_SLMB4OvpxgatIACDStEWu6MR83LCkn5C0");
                        Console.WriteLine("JWT temporal configurado para desarrollo.");
                    }

                    // --- NAVEGACIÓN A vInicio ---
                    await Navigation.PushAsync(new vInicio(usuario));
                    Navigation.RemovePage(this); // Eliminar la página de login de la pila
                }
                else
                {
                    await DisplayAlert("Error", "Credenciales inválidas o usuario no encontrado en la base de datos de la aplicación.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
            finally
            {
                btnIniciarSesion.IsEnabled = true;
                btnIniciarSesion.Text = "Iniciar sesión";
            }
        }

        private async void btnRegistrar_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new vRegistro());
        }
    }
}