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

                // Crear objeto con todos los campos que se pueden actualizar
                var usuarioData = new
                {
                    nombre = usuario.nombre,
                    apellido = usuario.apellido,
                    fechanac = usuario.fechanac.ToString("yyyy-MM-dd"),
                    foto = usuario.foto,
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
                string url = $"{_supabaseUrl}/reserva?id=eq.{reserva.Id}";

                // Validar que el estado sea uno de los válidos
                var estadosValidos = new[] { "Pendiente", "En curso", "Completado", "Cancelado" };
                if (!estadosValidos.Contains(reserva.Estado))
                {
                    Console.WriteLine($"Estado inválido: {reserva.Estado}");
                    return false;
                }

                var reservaData = new
                {
                    fecha_servicio = reserva.FechaServicio.ToString("yyyy-MM-ddTHH:mm:ss"),
                    estado = reserva.Estado,
                    comentarios = reserva.Comentarios
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(reservaData, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"Actualizando reserva {reserva.Id}: {json}");

                var response = await _httpClient.PatchAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al actualizar reserva: {errorContent}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar reserva: {ex.Message}");
                return false;
            }
        }

        public async Task<string> SubirComprobanteAsync(Comprobante comprobante, Stream fileStream, string fileName)
        {
            try
            {
                // Subir el archivo a Supabase Storage
                var storageUrl = _supabaseUrl.Replace("/rest/v1",
                    $"/storage/v1/object/comprobantes/{comprobante.UsuarioId}/{fileName}");

                using var content = new StreamContent(fileStream);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseApiKey);

                var storageResponse = await _httpClient.PostAsync(storageUrl, content);

                if (!storageResponse.IsSuccessStatusCode)
                {
                    return $"Error al subir archivo: {await storageResponse.Content.ReadAsStringAsync()}";
                }

                // Crear el registro en la tabla comprobante
                var comprobanteUrl = $"{_supabaseUrl}/comprobante";
                var comprobanteData = new
                {
                    descripcion = comprobante.Descripcion,
                    fecha_subida = DateTime.UtcNow,
                    url_archivo = $"{_supabaseUrl.Replace("/rest/v1", "")}/storage/v1/object/public/comprobantes/{comprobante.UsuarioId}/{fileName}",
                    estado = "Pendiente",
                    usuario_id = comprobante.UsuarioId,
                    reserva_id = comprobante.ReservaId
                };

                var json = JsonSerializer.Serialize(comprobanteData);
                var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Remove("Prefer");
                _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

                var response = await _httpClient.PostAsync(comprobanteUrl, stringContent);
                return response.IsSuccessStatusCode ? "OK" : $"Error: {await response.Content.ReadAsStringAsync()}";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
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

        public async Task<List<Comprobante>> ObtenerComprobantesUsuarioAsync(int usuarioId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/comprobantes/usuario/{usuarioId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var comprobantes = JsonSerializer.Deserialize<List<Comprobante>>(json);
                    return comprobantes ?? new List<Comprobante>();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Error al obtener comprobantes: {response.StatusCode}");
                    return new List<Comprobante>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Excepción al obtener comprobantes: {ex.Message}");
                return new List<Comprobante>();
            }
        }

        public async Task<List<Plan>?> ObtenerPlanesAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/plan";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Plan>>(json, options);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}