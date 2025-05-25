using KynosPetClub.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace KynosPetClub.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseApiKey;

        public ApiService()
        {
            _httpClient = new HttpClient();

            // Obtener la URL base y la API key desde la configuración
            _supabaseUrl = "https://cfwybqaykyerljqfqtzk.supabase.co/rest/v1";
            _supabaseApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNmd3licWF5a3llcmxqcWZxdHprIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDc2MTU1MTcsImV4cCI6MjA2MzE5MTUxN30.0NvvKf7vF_SLMB4OvpxgatIACDStEWu6MR83LCkn5C0";

            // Configurar los headers globales para todas las peticiones
            ConfigureHttpClientHeaders();
        }

        private void ConfigureHttpClientHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseApiKey);
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseApiKey);
        }

        public async Task<string> RegistrarUsuarioAsync(Usuario usuario)
        {
            try
            {
                Console.WriteLine($"Registrando usuario: {usuario.nombre} {usuario.apellido}");

                string url = $"{_supabaseUrl}/usuario";

                var usuarioData = new
                {
                    nombre = usuario.nombre,
                    apellido = usuario.apellido,
                    fechanac = usuario.fechanac,
                    correo = usuario.correo,
                    contraseña = usuario.contraseña,
                    foto = usuario.foto,
                    auth_id = usuario.AuthId,
                    rol_id = usuario.RolId ?? 2, // Por defecto rol de cliente
                    plan_id = usuario.PlanId
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(usuarioData, options);
                Console.WriteLine($"JSON a enviar: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Remove("Prefer");
                _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var resultado = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Respuesta exitosa: {resultado}");
                    return "OK";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error HTTP {response.StatusCode}: {error}");
                    return $"ERROR: [{response.StatusCode}] {error}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
                return $"ERROR: {ex.Message}";
            }
        }

        public async Task<Usuario?> LoginUsuarioAsync(string correo, string contraseña)
        {
            try
            {
                Console.WriteLine($"Intentando login con: {correo}");

                var url = $"{_supabaseUrl}/usuario?correo=eq.{Uri.EscapeDataString(correo)}&contraseña=eq.{Uri.EscapeDataString(contraseña)}";

                Console.WriteLine($"URL de búsqueda: {_supabaseUrl}/usuario?correo=eq.{Uri.EscapeDataString(correo)}&contraseña=***");

                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Código de respuesta: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error en la respuesta: {errorContent}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta recibida (longitud): {json.Length} caracteres");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var usuarios = JsonSerializer.Deserialize<List<Usuario>>(json, options);

                if (usuarios == null || !usuarios.Any())
                {
                    Console.WriteLine("No se encontraron usuarios con esas credenciales");
                    return null;
                }

                Console.WriteLine($"Usuario encontrado: {usuarios[0].nombre} {usuarios[0].apellido}");
                return usuarios.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception en login: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        // Método para obtener las mascotas de un usuario
        public async Task<List<Mascota>?> ObtenerMascotasUsuarioAsync(int usuarioId)
        {
            try
            {
                var url = $"{_supabaseUrl}/mascota?usuario_id=eq.{usuarioId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var mascotas = JsonSerializer.Deserialize<List<Mascota>>(json, options);
                return mascotas;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuario)
        {
            try
            {
                string url = $"{_supabaseUrl}/usuario?id=eq.{usuario.Id}";

                // Para administradores, permitir actualización completa incluyendo rol y plan
                var usuarioData = new
                {
                    nombre = usuario.nombre,
                    apellido = usuario.apellido,
                    fechanac = usuario.fechanac.ToString("yyyy-MM-dd"),
                    correo = usuario.correo,
                    foto = usuario.foto,
                    rol_id = usuario.RolId,
                    plan_id = usuario.PlanId,
                    contraseña = usuario.contraseña
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(usuarioData, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"Actualizando usuario {usuario.Id}: {json}");

                var response = await _httpClient.PatchAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al actualizar usuario: {errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar usuario: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActualizarUsuarioAdminAsync(Usuario usuario)
        {
            try
            {
                string url = $"{_supabaseUrl}/usuario?id=eq.{usuario.Id}";
                var usuarioData = new
                {
                    nombre = usuario.nombre,
                    apellido = usuario.apellido,
                    correo = usuario.correo,
                    fechanac = usuario.fechanac.ToString("yyyy-MM-dd"),
                    rol_id = usuario.RolId,
                    plan_id = usuario.PlanId
                };

                var json = JsonSerializer.Serialize(usuarioData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync(url, content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> AgregarMascotaAsync(Mascota mascota)
        {
            try
            {
                string url = $"{_supabaseUrl}/mascota";
                var mascotaData = new
                {
                    nombre = mascota.Nombre,
                    especie = mascota.Especie,
                    raza = mascota.Raza,
                    fecha_nacimiento = mascota.FechaNacimiento.ToString("yyyy-MM-dd"),
                    foto = mascota.Foto,
                    usuario_id = mascota.UsuarioId
                };

                var json = JsonSerializer.Serialize(mascotaData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return $"Error {response.StatusCode}: {errorContent}";
                }

                return "OK";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        public async Task<bool> ActualizarMascotaAsync(Mascota mascota)
        {
            try
            {
                string url = $"{_supabaseUrl}/mascota?id=eq.{mascota.Id}";
                var mascotaData = new
                {
                    nombre = mascota.Nombre,
                    especie = mascota.Especie,
                    raza = mascota.Raza,
                    foto = mascota.Foto,
                    fecha_nacimiento = mascota.FechaNacimiento.ToString("yyyy-MM-dd")
                };

                var json = JsonSerializer.Serialize(mascotaData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al actualizar mascota: {errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar mascota: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarMascotaAsync(int mascotaId)
        {
            try
            {
                string url = $"{_supabaseUrl}/mascota?id=eq.{mascotaId}";
                var response = await _httpClient.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar mascota: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Servicio>> ObtenerServiciosAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/servicio?select=id,nombre,descripcion,precio";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<Servicio>>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener servicios: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Reserva>> ObtenerReservasUsuarioAsync(int usuarioId)
        {
            try
            {
                var url = $"{_supabaseUrl}/reserva?usuario_id=eq.{usuarioId}&select=*,servicio:servicio_id(*),mascota:mascota_id(*)";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Reserva>>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener reservas: {ex.Message}");
                return null;
            }
        }

        public async Task<string> CrearReservaAsync(Reserva reserva)
        {
            try
            {
                var url = $"{_supabaseUrl}/reserva";
                var reservaData = new
                {
                    fecha_reserva = reserva.FechaReserva,
                    fecha_servicio = reserva.FechaServicio,
                    estado = reserva.Estado,
                    comentarios = reserva.Comentarios,
                    usuario_id = reserva.UsuarioId,
                    mascota_id = reserva.MascotaId,
                    servicio_id = reserva.ServicioId
                };

                var json = JsonSerializer.Serialize(reservaData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var createdReserva = JsonSerializer.Deserialize<List<Reserva>>(responseJson);
                    return createdReserva?.FirstOrDefault()?.Id.ToString() ?? "OK";
                }

                return $"Error: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        // Agregar este método a tu clase ApiService existente

        public async Task<bool> ActualizarReservaAsync(Reserva reserva)
        {
            try
            {
                Console.WriteLine($"🔄 INICIANDO: Actualizar reserva ID: {reserva.Id} a estado: {reserva.Estado}");

                var url = $"{_supabaseUrl}/reserva?id=eq.{reserva.Id}";
                Console.WriteLine($"🔍 URL: {url}");

                // Crear objeto con todos los campos necesarios
                var updateData = new
                {
                    estado = reserva.Estado,
                    comentarios = reserva.Comentarios,
                    fecha_reserva = reserva.FechaReserva.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    fecha_servicio = reserva.FechaServicio.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    usuario_id = reserva.UsuarioId,
                    mascota_id = reserva.MascotaId,
                    servicio_id = reserva.ServicioId
                };

                var json = JsonSerializer.Serialize(updateData);
                Console.WriteLine($"📤 JSON enviado: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Agregar headers necesarios para Supabase
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "application/json");

                var response = await _httpClient.PatchAsync(url, content);

                Console.WriteLine($"📡 Response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Error response: {error}");
                    Console.WriteLine($"❌ Headers de la respuesta: {response.Headers}");

                    // Intentar con PUT si PATCH falla
                    Console.WriteLine("🔄 Intentando con PUT...");
                    var putResponse = await _httpClient.PutAsync(url, content);
                    Console.WriteLine($"📡 PUT Response status: {putResponse.StatusCode}");

                    if (!putResponse.IsSuccessStatusCode)
                    {
                        var putError = await putResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ PUT Error response: {putError}");
                        return false;
                    }

                    return true;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Response exitoso: {responseContent}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EXCEPCIÓN al actualizar reserva: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // Reemplaza tu método SubirComprobanteAsync con esta versión súper simple

        // Si la versión anterior sigue fallando, usa esta versión ULTRA simple

        // REEMPLAZA tu método SubirComprobanteAsync con este que SÍ va a funcionar

        public async Task<string> SubirComprobanteAsync(Comprobante comprobante, Stream fileStream, string fileName)
        {
            try
            {
                Console.WriteLine("🚀 === INICIANDO SUBIDA BULLETPROOF ===");

                // 1. Preparar datos del archivo
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var extension = Path.GetExtension(fileName)?.ToLower() ?? ".jpg";
                var fileName_safe = $"user{comprobante.UsuarioId}_{timestamp}{extension}";

                Console.WriteLine($"📁 Archivo: {fileName_safe}");

                // 2. URL para el storage público
                var storageUrl = $"https://cfwybqaykyerljqfqtzk.supabase.co/storage/v1/object/comprobantes/{fileName_safe}";
                Console.WriteLine($"🌐 URL: {storageUrl}");

                // 3. Preparar el contenido
                fileStream.Position = 0; // Asegurar que esté al inicio
                using var content = new StreamContent(fileStream);

                // Configurar headers del archivo
                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                        break;
                    case ".png":
                        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                        break;
                    default:
                        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                        break;
                }

                // 4. Configurar HttpClient para storage
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Clear();

                // Headers mínimos necesarios
                httpClient.DefaultRequestHeaders.Add("apikey", _supabaseApiKey);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseApiKey);

                Console.WriteLine("📤 Subiendo archivo...");

                // 5. Subir archivo
                var response = await httpClient.PostAsync(storageUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📊 Status: {response.StatusCode}");
                Console.WriteLine($"📋 Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error al subir archivo: [{response.StatusCode}] {responseContent}";
                }

                // 6. URL pública del archivo
                var publicUrl = $"https://cfwybqaykyerljqfqtzk.supabase.co/storage/v1/object/public/comprobantes/{fileName_safe}";
                Console.WriteLine($"🔗 URL pública: {publicUrl}");

                // 7. Guardar en la base de datos
                var comprobanteData = new
                {
                    descripcion = comprobante.Descripcion ?? "Comprobante de pago",
                    fecha_subida = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    url_archivo = publicUrl,
                    estado = "Pendiente",
                    usuario_id = comprobante.UsuarioId,
                    reserva_id = comprobante.ReservaId > 0 ? (int?)comprobante.ReservaId : null
                };

                var json = JsonSerializer.Serialize(comprobanteData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                Console.WriteLine($"💾 Guardando en DB: {json}");

                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                // Usar el HttpClient principal para la base de datos
                _httpClient.DefaultRequestHeaders.Remove("Prefer");
                _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

                var dbUrl = $"{_supabaseUrl}/comprobante";
                var dbResponse = await _httpClient.PostAsync(dbUrl, stringContent);
                var dbResponseContent = await dbResponse.Content.ReadAsStringAsync();

                Console.WriteLine($"🗄️ DB Status: {dbResponse.StatusCode}");
                Console.WriteLine($"🗄️ DB Response: {dbResponseContent}");

                if (!dbResponse.IsSuccessStatusCode)
                {
                    return $"Archivo subido pero error en DB: [{dbResponse.StatusCode}] {dbResponseContent}";
                }

                Console.WriteLine("✅ === SUBIDA COMPLETADA EXITOSAMENTE ===");
                return "OK";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
                return $"Exception: {ex.Message}";
            }
        }

        // AGREGA este método a tu ApiService para debug
        public async Task DebugComprobantesAsync(int usuarioId)
        {
            try
            {
                var comprobantes = await ObtenerComprobantesUsuarioAsync(usuarioId);
                var reservas = await ObtenerReservasUsuarioAsync(usuarioId);

                Console.WriteLine("=== DEBUG COMPROBANTES ===");
                Console.WriteLine($"Total comprobantes: {comprobantes?.Count ?? 0}");
                Console.WriteLine($"Total reservas: {reservas?.Count ?? 0}");

                if (comprobantes != null)
                {
                    foreach (var comp in comprobantes)
                    {
                        Console.WriteLine($"Comprobante ID: {comp.Id}");
                        Console.WriteLine($"  - ReservaId: {comp.ReservaId}");
                        Console.WriteLine($"  - UsuarioId: {comp.UsuarioId}");
                        Console.WriteLine($"  - Estado: {comp.Estado}");
                        Console.WriteLine($"  - Descripción: {comp.Descripcion}");
                        Console.WriteLine("---");
                    }
                }

                if (reservas != null)
                {
                    foreach (var res in reservas)
                    {
                        Console.WriteLine($"Reserva ID: {res.Id}");
                        Console.WriteLine($"  - Estado: {res.Estado}");
                        Console.WriteLine($"  - ServicioId: {res.ServicioId}");
                        Console.WriteLine($"  - Fecha: {res.FechaServicio}");
                        Console.WriteLine("---");
                    }
                }

                Console.WriteLine("=== FIN DEBUG ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en debug: {ex.Message}");
            }
        }

        public async Task<string> AutenticarConSupabaseAsync(string email, string password)
        {
            try
            {
                var authUrl = "https://cfwybqaykyerljqfqtzk.supabase.co/auth/v1/token?grant_type=password";

                var authData = new
                {
                    email = email,
                    password = password
                };

                var json = JsonSerializer.Serialize(authData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Crear cliente específico para autenticación
                var authClient = new HttpClient();
                authClient.DefaultRequestHeaders.Clear();
                authClient.DefaultRequestHeaders.Add("apikey", _supabaseApiKey);

                var response = await authClient.PostAsync(authUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Respuesta de auth: {responseJson}");

                    // Extraer el access_token del JSON de respuesta
                    var authResponse = JsonSerializer.Deserialize<JsonDocument>(responseJson);
                    if (authResponse.RootElement.TryGetProperty("access_token", out var tokenElement))
                    {
                        var accessToken = tokenElement.GetString();
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            // Guardar el token
                            await SecureStorage.SetAsync("supabase_jwt", accessToken);
                            Console.WriteLine("JWT token guardado exitosamente");
                            return "OK";
                        }
                    }
                }

                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error en autenticación Supabase: {error}");
                return $"Error: {error}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception en autenticación: {ex.Message}");
                return $"Exception: {ex.Message}";
            }
        }

        public async Task<Usuario?> LoginUsuarioCompletoAsync(string correo, string contraseña)
        {
            try
            {
                Console.WriteLine($"Intentando login completo con: {correo}");

                // 1. Primero autenticar con Supabase Auth
                var resultadoAuth = await AutenticarConSupabaseAsync(correo, contraseña);

                if (resultadoAuth != "OK")
                {
                    Console.WriteLine($"Falló la autenticación: {resultadoAuth}");
                    return null;
                }

                // 2. Luego obtener datos del usuario desde tu tabla
                var url = $"{_supabaseUrl}/usuario?correo=eq.{Uri.EscapeDataString(correo)}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al obtener usuario: {errorContent}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var usuarios = JsonSerializer.Deserialize<List<Usuario>>(json, options);

                if (usuarios == null || !usuarios.Any())
                {
                    Console.WriteLine("No se encontró el usuario en la tabla");
                    return null;
                }

                var usuario = usuarios.FirstOrDefault();
                Console.WriteLine($"Login exitoso para: {usuario.nombre} {usuario.apellido}");
                return usuario;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception en login completo: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> VerificarTokenValidoAsync()
        {
            try
            {
                var token = await SecureStorage.GetAsync("supabase_jwt");

                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                // Verificar si el token es válido haciendo una petición simple
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.DefaultRequestHeaders.Add("apikey", _supabaseApiKey);

                var response = await httpClient.GetAsync($"{_supabaseUrl}/usuario?select=id&limit=1");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task CerrarSesionAsync()
        {
            try
            {
                SecureStorage.Remove("supabase_jwt");
                SecureStorage.Remove("supabase_refresh_token");
                Console.WriteLine("Tokens eliminados - sesión cerrada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cerrar sesión: {ex.Message}");
            }
        }
        public async Task<Servicio?> ObtenerServicioPorIdAsync(int servicioId)
        {
            try
            {
                var url = $"{_supabaseUrl}/servicio?id=eq.{servicioId}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var servicios = JsonSerializer.Deserialize<List<Servicio>>(json, options);
                return servicios?.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Mascota?> ObtenerMascotaPorIdAsync(int mascotaId)
        {
            try
            {
                var url = $"{_supabaseUrl}/mascota?id=eq.{mascotaId}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var mascotas = JsonSerializer.Deserialize<List<Mascota>>(json, options);
                return mascotas?.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }


        // Versión simplificada sin opciones JSON:

        // ELIMINA el método ObtenerComprobantesUsuarioAsync incorrecto y reemplázalo con este:
        public async Task<List<Comprobante>> ObtenerComprobantesUsuarioAsync(int usuarioId)
        {
            try
            {
                Console.WriteLine($"🔍 Obteniendo comprobantes para usuario {usuarioId}");

                // 🔧 URL CORRECTA para Supabase
                var url = $"{_supabaseUrl}/comprobante?usuario_id=eq.{usuarioId}";
                Console.WriteLine($"📋 URL: {url}");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📊 Status: {response.StatusCode}");
                Console.WriteLine($"📋 Response: {content}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error HTTP: {response.StatusCode} - {content}");
                    return new List<Comprobante>();
                }

                if (string.IsNullOrEmpty(content) || content == "[]")
                {
                    Console.WriteLine("ℹ️ No hay comprobantes para este usuario");
                    return new List<Comprobante>();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var comprobantes = JsonSerializer.Deserialize<List<Comprobante>>(content, options);
                Console.WriteLine($"✅ Comprobantes deserializados: {comprobantes?.Count ?? 0}");

                return comprobantes ?? new List<Comprobante>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción al obtener comprobantes: {ex.Message}");
                Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");
                return new List<Comprobante>();
            }
        }

        // AGREGA este método a tu ApiService
        public async Task<bool> VerificarComprobanteParaReservaAsync(int reservaId, int usuarioId)
        {
            try
            {
                Console.WriteLine($"🔍 Verificando comprobante para reserva {reservaId}");

                var comprobantes = await ObtenerComprobantesUsuarioAsync(usuarioId);

                if (comprobantes == null || !comprobantes.Any())
                {
                    Console.WriteLine($"❌ No hay comprobantes para usuario {usuarioId}");
                    return false;
                }

                // Verificar si existe comprobante para esta reserva
                bool tieneComprobante = comprobantes.Any(c => c.ReservaId == reservaId);

                Console.WriteLine($"✅ ¿Reserva {reservaId} tiene comprobante? {tieneComprobante}");

                // Debug: mostrar todos los ReservaId de los comprobantes
                foreach (var comp in comprobantes)
                {
                    Console.WriteLine($"  📄 Comprobante {comp.Id}: ReservaId={comp.ReservaId}");
                }

                return tieneComprobante;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error verificando comprobante: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Plan>> ObtenerPlanesAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/plan";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error al obtener planes: {response.StatusCode}");
                    return new List<Plan>();
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta planes JSON: {json}");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var planes = JsonSerializer.Deserialize<List<Plan>>(json, options);
                return planes ?? new List<Plan>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener planes: {ex.Message}");
                return new List<Plan>();
            }
        }

        public async Task<List<Comprobante>> ObtenerComprobantesPendientesAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/comprobante?estado=eq.Pendiente";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new List<Comprobante>();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Comprobante>>(json, options) ?? new List<Comprobante>();
            }
            catch (Exception)
            {
                return new List<Comprobante>();
            }
        }

        public async Task<bool> ActualizarComprobanteAsync(Comprobante comprobante)
        {
            try
            {
                string url = $"{_supabaseUrl}/comprobante?id=eq.{comprobante.Id}";
                var comprobanteData = new
                {
                    estado = comprobante.Estado,
                    comentario_admin = comprobante.ComentarioAdmin
                };

                var json = JsonSerializer.Serialize(comprobanteData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Usuario>> ObtenerTodosUsuariosAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/usuario";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new List<Usuario>();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Usuario>>(json, options) ?? new List<Usuario>();
            }
            catch (Exception)
            {
                return new List<Usuario>();
            }
        }

        public async Task<List<Rol>> ObtenerRolesAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/rol";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error al obtener roles: {response.StatusCode}");
                    return new List<Rol>();
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta roles JSON: {json}");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var roles = JsonSerializer.Deserialize<List<Rol>>(json, options);
                return roles ?? new List<Rol>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener roles: {ex.Message}");
                return new List<Rol>();
            }
        }

        // Método mejorado para obtener reservas pendientes de asignación
        public async Task<List<Reserva>> ObtenerReservasPendientesAsignacionAsync()
        {
            try
            {
                Console.WriteLine("🔍 Obteniendo reservas En curso para asignación...");

                var url = $"{_supabaseUrl}/reserva?estado=eq.En%20curso&order=fecha_servicio.asc";
                Console.WriteLine($"🔗 URL: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error HTTP: {response.StatusCode}");
                    return new List<Reserva>();
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📄 JSON recibido: {json}");

                if (string.IsNullOrEmpty(json) || json == "[]")
                {
                    Console.WriteLine("📄 No hay reservas En curso");
                    return new List<Reserva>();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var reservas = JsonSerializer.Deserialize<List<Reserva>>(json, options);

                // Cargar servicios y mascotas para cada reserva
                foreach (var reserva in reservas ?? new List<Reserva>())
                {
                    if (reserva.ServicioId > 0)
                    {
                        reserva.Servicio = await ObtenerServicioPorIdAsync(reserva.ServicioId);
                    }

                    if (reserva.MascotaId > 0)
                    {
                        reserva.Mascota = await ObtenerMascotaPorIdAsync(reserva.MascotaId);
                    }
                }

                Console.WriteLine($"✅ Reservas cargadas: {reservas?.Count ?? 0}");
                return reservas ?? new List<Reserva>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener reservas para asignación: {ex.Message}");
                return new List<Reserva>();
            }
        }

        // 🔧 REEMPLAZA el método ObtenerCitasAsignadasAsync en tu ApiService.cs

        public async Task<List<Reserva>> ObtenerCitasAsignadasAsync(int funcionarioId)
        {
            try
            {
                Console.WriteLine($"🔍 Obteniendo citas asignadas al funcionario ID: {funcionarioId}");

                // Primero obtener el nombre del funcionario
                var funcionario = await ObtenerUsuarioPorIdAsync(funcionarioId);
                if (funcionario == null)
                {
                    Console.WriteLine("❌ Funcionario no encontrado");
                    return new List<Reserva>();
                }

                var nombreFuncionario = $"{funcionario.nombre} {funcionario.apellido}";
                Console.WriteLine($"👤 Buscando citas asignadas a: {nombreFuncionario}");

                // Obtener SOLO reservas "En curso"
                var url = $"{_supabaseUrl}/reserva?estado=eq.En%20curso&order=fecha_servicio.asc";
                Console.WriteLine($"🔗 URL: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error HTTP: {response.StatusCode}");
                    return new List<Reserva>();
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📄 JSON reservas En curso: {json}");

                if (string.IsNullOrEmpty(json) || json == "[]")
                {
                    Console.WriteLine("📄 No hay reservas En curso");
                    return new List<Reserva>();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var todasReservas = JsonSerializer.Deserialize<List<Reserva>>(json, options) ?? new List<Reserva>();

                Console.WriteLine($"📊 Total reservas En curso encontradas: {todasReservas.Count}");

                // Filtrar las que tienen asignado este funcionario en los comentarios
                var reservasAsignadas = new List<Reserva>();

                foreach (var reserva in todasReservas)
                {
                    Console.WriteLine($"🔍 Revisando reserva ID {reserva.Id} - Comentarios: {reserva.Comentarios?.Substring(0, Math.Min(50, reserva.Comentarios?.Length ?? 0))}...");

                    if (!string.IsNullOrEmpty(reserva.Comentarios) &&
                        reserva.Comentarios.Contains($"FUNCIONARIO ASIGNADO: {nombreFuncionario}"))
                    {
                        Console.WriteLine($"✅ ¡Encontrada cita asignada! Reserva ID {reserva.Id}");

                        // Cargar servicio y mascota
                        if (reserva.ServicioId > 0)
                        {
                            reserva.Servicio = await ObtenerServicioPorIdAsync(reserva.ServicioId);
                            Console.WriteLine($"   🏥 Servicio: {reserva.Servicio?.Nombre}");
                        }

                        if (reserva.MascotaId > 0)
                        {
                            reserva.Mascota = await ObtenerMascotaPorIdAsync(reserva.MascotaId);
                            Console.WriteLine($"   🐾 Mascota: {reserva.Mascota?.Nombre}");
                        }

                        reservasAsignadas.Add(reserva);
                    }
                    else
                    {
                        Console.WriteLine($"❌ Reserva ID {reserva.Id} NO asignada a {nombreFuncionario}");
                    }
                }

                Console.WriteLine($"✅ Total citas asignadas al funcionario: {reservasAsignadas.Count}");

                // Ordenar por fecha (más próximas primero)
                return reservasAsignadas.OrderBy(r => r.FechaServicio).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener citas asignadas: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                return new List<Reserva>();
            }
        }

        public async Task<Usuario> ObtenerUsuarioPorIdAsync(int usuarioId)
        {
            try
            {
                var url = $"{_supabaseUrl}/usuario?id=eq.{usuarioId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(json) || json == "[]")
                    return null;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var usuarios = JsonSerializer.Deserialize<List<Usuario>>(json, options);

                return usuarios?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener usuario por ID: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AsignarFuncionarioAReservaAsync(int reservaId, int funcionarioId)
        {
            try
            {
                var url = $"{_supabaseUrl}/reserva?id=eq.{reservaId}";
                var data = new
                {
                    funcionario_id = funcionarioId,
                    estado = "En curso",
                    fecha_asignacion = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Usuario>> ObtenerFuncionariosAsync()
        {
            try
            {
                Console.WriteLine("🔍 Obteniendo funcionarios (rol 3)...");

                var url = $"{_supabaseUrl}/usuario?rol_id=eq.3&order=nombre.asc";
                Console.WriteLine($"🔗 URL: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error HTTP: {response.StatusCode}");
                    return new List<Usuario>();
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📄 JSON funcionarios: {json}");

                if (string.IsNullOrEmpty(json) || json == "[]")
                {
                    Console.WriteLine("📄 No hay funcionarios");
                    return new List<Usuario>();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var funcionarios = JsonSerializer.Deserialize<List<Usuario>>(json, options);

                Console.WriteLine($"✅ Funcionarios cargados: {funcionarios?.Count ?? 0}");
                return funcionarios ?? new List<Usuario>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener funcionarios: {ex.Message}");
                return new List<Usuario>();
            }
        }


        public void DebugHeaders()
        {
            Console.WriteLine("🔍 DEBUGGING HEADERS DEL HTTPCLIENT:");
            Console.WriteLine($"📍 Base URL: {_httpClient.BaseAddress}");

            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                Console.WriteLine($"📋 Header: {header.Key} = {string.Join(", ", header.Value)}");
            }
        }

        // TAMBIÉN agrega este método para probar la conexión:
        public async Task<bool> TestConexionAsync()
        {
            try
            {
                Console.WriteLine("🧪 PROBANDO CONEXIÓN A SUPABASE...");

                var url = $"{_supabaseUrl}/reserva?id=eq.48"; // Probar con la reserva específica
                Console.WriteLine($"🔍 URL de prueba: {url}");

                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"📡 Test response status: {response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📄 Test response content: {content}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en test de conexión: {ex.Message}");
                return false;
            }
        }

    }
}