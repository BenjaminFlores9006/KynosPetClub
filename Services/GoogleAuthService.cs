using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Authentication; // Para WebAuthenticator
using Microsoft.Maui.ApplicationModel; // Para MainThread

namespace KynosPetClub.Services
{
    // Asegúrate de que estas clases estén definidas en tu proyecto.
    // Lo ideal es tenerlas en una carpeta 'Models'.
    public class AuthResult // Clase para el resultado de la autenticación
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public UserInfo UserInfo { get; set; }
        public string AccessToken { get; set; } // Token de sesión de Supabase
    }

    public class UserInfo // Clase para la información del usuario obtenida de Supabase Auth
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
        // Otros campos que puedas necesitar de Supabase o Google
    }

    public class GoogleAuthService
    {
        // NO necesitas using Supabase.Gotrue; si no estás usando la librería oficial.

        // Asegúrate de que estas URLs y claves sean correctas para tu proyecto Supabase.
        // REEMPLAZA ESTOS VALORES CON LOS REALES DE TU PROYECTO SUPABASE
        private const string SUPABASE_URL = "https://cfwybqaykyerljqfqtzk.supabase.co"; // Ejemplo: TU_URL_DE_SUPABASE
        private const string SUPABASE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNmd3licWF5a3llcmxqcWZxdHprIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDc2MTU1MTcsImV4cCI6MjA2MzE5MTUxN30.0NvvKf7vF_SLMB4OvpxgatIACDStEWu6MR83LCkn5C0"; // Ejemplo: TU_SUPABASE_ANON_KEY

        // El esquema de redirección y el host/path deben coincidir con lo configurado en tu plataforma Android/iOS
        // (WebAuthenticatorCallbackActivity.cs) y en la configuración de OAuth de Supabase.
        private const string REDIRECT_URI_SCHEME = "com.companyname.kynospetclub"; // Reemplaza con el ID de tu app
        private const string REDIRECT_URI_HOST = "auth";
        private const string REDIRECT_URI_PATH = "callback";


        public async Task<AuthResult> SignInWithGoogleAsync()
        {
            try
            {
                // Construye la URL de autenticación de Supabase para Google
                // Supabase redirigirá a Google para la autenticación, y luego a tu app.
                // Es crucial que 'redirect_to' sea una URI válida.
                var redirectUri = $"{REDIRECT_URI_SCHEME}://{REDIRECT_URI_HOST}/{REDIRECT_URI_PATH}";
                var encodedRedirectUri = Uri.EscapeDataString(redirectUri);

                var authUrl = $"{SUPABASE_URL}/auth/v1/authorize?provider=google&redirect_to={encodedRedirectUri}";

                Console.WriteLine($"🔗 Auth URL: {authUrl}");
                Console.WriteLine($"✅ Redirect URI: {redirectUri}");

                Uri startUri = new Uri(authUrl);
                Uri callbackUri = new Uri(redirectUri);

                // Abrir el navegador para la autenticación
                var authResultFromWeb = await WebAuthenticator.Default.AuthenticateAsync(
                    startUri,
                    callbackUri);

                if (authResultFromWeb != null && authResultFromWeb.AccessToken != null)
                {
                    // El AccessToken es el token de sesión de Supabase (JWT)
                    string supabaseAccessToken = authResultFromWeb.AccessToken;
                    Console.WriteLine($"Token de acceso de Supabase obtenido: {supabaseAccessToken}");

                    // Guardar el token en SecureStorage
                    await SecureStorage.SetAsync("supabase_jwt", supabaseAccessToken);
                    Console.WriteLine("Supabase JWT guardado.");

                    // Obtener información del usuario usando el AccessToken de Supabase
                    var userInfo = await GetUserInfoFromSupabaseAuthAsync(supabaseAccessToken);

                    if (userInfo != null)
                    {
                        Console.WriteLine($"✅ UserInfo obtenido: Email: {userInfo.Email}");
                        return new AuthResult { Success = true, UserInfo = userInfo, AccessToken = supabaseAccessToken };
                    }
                    else
                    {
                        Console.WriteLine("❌ No se pudo obtener la información del usuario de Supabase Auth.");
                        return new AuthResult { Success = false, ErrorMessage = "No se pudo obtener la información del usuario después del login con Google." };
                    }
                }
                else
                {
                    Console.WriteLine("❌ No se obtuvo un token de acceso de WebAuthenticator.");
                    return new AuthResult { Success = false, ErrorMessage = "Login con Google cancelado o fallido." };
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Autenticación con Google cancelada por el usuario.");
                return new AuthResult { Success = false, ErrorMessage = "El login con Google fue cancelado." };
            }
            catch (UriFormatException ex) // Captura específica para errores de formato de URI
            {
                Console.WriteLine($"❌ Error de formato de URI en SignInWithGoogleAsync: {ex.Message}");
                return new AuthResult { Success = false, ErrorMessage = $"Error de configuración de URL: {ex.Message}. Asegúrate de que las URIs en GoogleAuthService y WebAuthenticatorCallbackActivity sean válidas y coincidan." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error general en SignInWithGoogleAsync: {ex.Message}");
                return new AuthResult { Success = false, ErrorMessage = $"Ocurrió un error inesperado durante el login con Google: {ex.Message}" };
            }
        }

        // Método para obtener información del usuario desde Supabase Auth (a través de la API)
        private async Task<UserInfo> GetUserInfoFromSupabaseAuthAsync(string accessToken)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                    httpClient.DefaultRequestHeaders.Add("apikey", SUPABASE_KEY);

                    // Endpoint para obtener información del usuario autenticado en Supabase
                    var response = await httpClient.GetAsync($"{SUPABASE_URL}/auth/v1/user");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Respuesta de Supabase /user: {json}");

                        // Deserializar el JSON directamente a una clase o usar JsonDocument para un acceso más robusto.
                        // El error "The JSON value could not be converted to System.Boolean. Path: $.email_confirmed_at" (image_ebd72f.png)
                        // sugiere que 'email_confirmed_at' no se está interpretando correctamente como booleano.
                        // La clase UserInfo no debe tener un campo booleano para 'email_confirmed_at'.
                        // O bien, deserealiza a un diccionario o usa JsonDocument para extraer solo lo que necesitas.
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            JsonElement root = doc.RootElement;

                            string id = root.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() : null;
                            string email = root.TryGetProperty("email", out JsonElement emailElement) ? emailElement.GetString() : null;
                            string name = null;
                            string picture = null;

                            // Supabase almacena metadatos de usuario en 'user_metadata' o 'raw_user_meta_data'
                            if (root.TryGetProperty("user_metadata", out JsonElement userMetadata))
                            {
                                if (userMetadata.TryGetProperty("full_name", out JsonElement fullNameElement))
                                {
                                    name = fullNameElement.GetString();
                                }
                                else if (userMetadata.TryGetProperty("name", out JsonElement nameElement))
                                {
                                    name = nameElement.GetString();
                                }
                                if (userMetadata.TryGetProperty("avatar_url", out JsonElement avatarUrlElement))
                                {
                                    picture = avatarUrlElement.GetString();
                                }
                            }

                            // A veces, para Google, los datos están en raw_user_meta_data
                            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(picture))
                            {
                                if (root.TryGetProperty("raw_user_meta_data", out JsonElement rawUserMetaData))
                                {
                                    if (rawUserMetaData.TryGetProperty("full_name", out JsonElement rawFullNameElement))
                                    {
                                        name = rawFullNameElement.GetString();
                                    }
                                    else if (rawUserMetaData.TryGetProperty("name", out JsonElement rawNameElement))
                                    {
                                        name = rawNameElement.GetString();
                                    }
                                    if (rawUserMetaData.TryGetProperty("picture", out JsonElement rawPictureElement))
                                    {
                                        picture = rawPictureElement.GetString();
                                    }
                                }
                            }

                            return new UserInfo
                            {
                                Id = id,
                                Email = email,
                                Name = name ?? email?.Split('@')[0], // Fallback al nombre del email si no se encuentra
                                Picture = picture
                            };
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error al obtener UserInfo de Supabase Auth: {response.StatusCode} - {errorContent}");
                        return null;
                    }
                }
            }
            catch (JsonException ex) // Captura específica para errores de JSON
            {
                Console.WriteLine($"❌ Error de JSON al obtener UserInfo: {ex.Message}");
                // Puedes imprimir el contenido JSON aquí para depurar
                // Console.WriteLine($"JSON recibido: {jsonContent}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción al obtener UserInfo de Supabase Auth: {ex.Message}");
                return null;
            }
        }

        // Si vas a tener registro por email/contraseña, este método debería estar en ApiService,
        // no en GoogleAuthService. Lo dejo aquí solo para la corrección del error CS1061
        // que mostraste, pero conceptualmente, ¡muevelo a ApiService!
        public async Task<AuthResult> SignUpWithEmailAndPasswordAsync(string email, string password)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var requestBody = new
                    {
                        email = email,
                        password = password
                    };
                    var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

                    httpClient.DefaultRequestHeaders.Add("apikey", SUPABASE_KEY);

                    var response = await httpClient.PostAsync($"{SUPABASE_URL}/auth/v1/signup", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Respuesta de registro con Email/Contraseña: {json}");
                        // Opcional: Deserializar el usuario de Supabase Auth si se devuelve en el registro
                        // var supabaseUser = JsonSerializer.Deserialize<JsonElement>(json);
                        return new AuthResult { Success = true, ErrorMessage = "Registro exitoso." };
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error en SignUpWithEmailAndPasswordAsync: {response.StatusCode} - {errorContent}");
                        return new AuthResult { Success = false, ErrorMessage = $"Error al registrar usuario: {errorContent}" };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción en SignUpWithEmailAndPasswordAsync: {ex.Message}");
                return new AuthResult { Success = false, ErrorMessage = $"Ocurrió un error inesperado durante el registro: {ex.Message}" };
            }
        }
    }
}