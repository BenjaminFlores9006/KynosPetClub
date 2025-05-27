using KynosPetClub.Models;
using KynosPetClub.Services;
using Microsoft.Maui.ApplicationModel; // Necesario para SecureStorage
using Supabase.Gotrue; // Necesario para la autenticación de Supabase Gotrue (si la usas directamente)

namespace KynosPetClub.Views;

public partial class vRegistro : ContentPage
{
    private ApiService _apiService; // Instancia de ApiService
    private GoogleAuthService _authService; // Instancia de GoogleAuthService (para Supabase Auth)

    public vRegistro()
    {
        InitializeComponent();
        dtpFechaNacimiento.Date = DateTime.Today.AddYears(-18);
        dtpFechaNacimiento.MaximumDate = DateTime.Today;

        _apiService = new ApiService(); // Inicializa ApiService
        _authService = new GoogleAuthService(); // Reutiliza GoogleAuthService para Supabase Auth (es el mismo cliente)
    }

    private async void btnRegistrar_Clicked(object sender, EventArgs e)
    {
        // 1. Validación de campos (mantener la lógica actual)
        if (string.IsNullOrEmpty(txtNombre.Text) ||
            string.IsNullOrEmpty(txtApellido.Text) ||
            string.IsNullOrEmpty(txtCorreo.Text) ||
            string.IsNullOrEmpty(txtPassword.Text))
        {
            await DisplayAlert("Error", "Por favor completa todos los campos", "OK");
            return;
        }

        if (!txtCorreo.Text.Contains("@") || !txtCorreo.Text.Contains("."))
        {
            await DisplayAlert("Error", "Por favor ingresa un correo electrónico válido", "OK");
            return;
        }

        if (txtPassword.Text.Length < 6)
        {
            await DisplayAlert("Error", "La contraseña debe tener al menos 6 caracteres", "OK");
            return;
        }

        if (txtPassword.Text != txtRepetirPassword.Text)
        {
            await DisplayAlert("Error", "Las contraseñas no coinciden", "OK");
            return;
        }

        var edad = DateTime.Today.Year - dtpFechaNacimiento.Date.Year;
        if (dtpFechaNacimiento.Date > DateTime.Today.AddYears(-edad)) edad--;

        if (edad < 16)
        {
            await DisplayAlert("Error", "Debes tener al menos 16 años para registrarte", "OK");
            return;
        }

        // Mostrar indicador de carga
        btnRegistrar.IsEnabled = false;
        btnRegistrar.Text = "Registrando...";

        try
        {
            // PASO 1: REGISTRAR EN SUPABASE AUTH (sistema de autenticación principal)
            Console.WriteLine("Intentando registrar en Supabase Auth...");
            var authResponse = await _authService.SignUpWithEmailAndPasswordAsync(txtCorreo.Text, txtPassword.Text);

            if (authResponse.Success && authResponse.UserInfo != null)
            {
                Console.WriteLine($"✅ Registro en Supabase Auth exitoso. Email: {authResponse.UserInfo.Email}, AuthId: {authResponse.UserInfo.Id}");

                // PASO 2: GUARDAR DATOS ADICIONALES EN TU TABLA 'usuario'
                // Crear el objeto usuario con los datos del formulario y el AuthId de Supabase
                var nuevoUsuario = new Usuario
                {
                    nombre = txtNombre.Text,
                    apellido = txtApellido.Text,
                    fechanac = dtpFechaNacimiento.Date,
                    correo = txtCorreo.Text,
                    contraseña = txtPassword.Text, // Aquí sí se envía la contraseña para tu tabla si la necesitas
                    AuthId = authResponse.UserInfo.Id, // ¡Muy importante! Guardar el ID de Supabase Auth
                    RolId = 2 // Por defecto rol de cliente
                    // No hay PlanId por defecto aquí, a menos que quieras asignarlo
                };

                Console.WriteLine("Intentando registrar en mi tabla 'usuario'...");
                var resultadoRegistroDB = await _apiService.RegistrarUsuarioAsync(nuevoUsuario);

                if (resultadoRegistroDB == "OK")
                {
                    await DisplayAlert("Éxito", "Usuario registrado correctamente y autenticado.", "OK");

                    // Opcional: Iniciar sesión automáticamente después del registro si el usuario no necesita verificar correo
                    // Si Supabase requiere verificación de correo, deberías redirigir a una página de "verifique su correo"
                    // Por ahora, asumimos que no hay verificación o que ya pasó.
                    await Navigation.PopAsync(); // Volver a la página de login (vLogIn)
                    // También podrías navegar a MainPage directamente si ya consideras el usuario logueado
                    // await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    // Si falla el registro en tu tabla 'usuario', considera qué hacer.
                    // Podrías intentar revertir el registro en Supabase Auth (más complejo)
                    // o simplemente informar al usuario que hubo un error y que intente de nuevo.
                    Console.WriteLine($"❌ Error al registrar en la tabla 'usuario': {resultadoRegistroDB}");
                    await DisplayAlert("Error", $"Usuario creado en Supabase Auth, pero no se pudo guardar en tu base de datos: {resultadoRegistroDB}. Intenta de nuevo.", "OK");

                    // Opcional: Eliminar usuario de Supabase Auth si la DB falla
                    // await _authService.DeleteUser(authResponse.UserInfo.Id); // Esto es más avanzado y requiere credenciales de admin/servicio
                }
            }
            else
            {
                // Manejar el error de registro en Supabase Auth
                Console.WriteLine($"❌ Error al registrar en Supabase Auth: {authResponse.ErrorMessage}");
                string errorMessage = authResponse.ErrorMessage;

                // Supabase a veces da errores genéricos, puedes hacer un mapeo aquí
                if (errorMessage.Contains("User already registered"))
                {
                    errorMessage = "Este correo electrónico ya está registrado.";
                }
                else if (errorMessage.Contains("password is too short"))
                {
                    errorMessage = "La contraseña es demasiado corta (mínimo 6 caracteres).";
                }

                await DisplayAlert("Error de Registro", errorMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error general en registro: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            await DisplayAlert("Error", $"Ocurrió un error inesperado: {ex.Message}", "OK");
        }
        finally
        {
            // Restaurar el botón
            btnRegistrar.IsEnabled = true;
            btnRegistrar.Text = "Registrarse";
        }
    }
}