using KynosPetClub.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
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

        // Método para obtener los servicios disponibles
        public async Task<List<Servicio>?> ObtenerServiciosAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/servicio";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var servicios = JsonSerializer.Deserialize<List<Servicio>>(json, options);
                return servicios;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Método para crear una reserva
        public async Task<string> CrearReservaAsync(Reserva reserva)
        {
            try
            {
                var url = $"{_supabaseUrl}/reserva";
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(reserva, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Remove("Prefer");
                _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                    return "OK";

                var error = await response.Content.ReadAsStringAsync();
                return $"ERROR: {error}";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuario)
        {
            try
            {
                string url = $"{_supabaseUrl}/usuario?id=eq.{usuario.Id}";
                var usuarioData = new
                {
                    nombre = usuario.nombre,
                    apellido = usuario.apellido
                };

                var json = JsonSerializer.Serialize(usuarioData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync(url, content);
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
    }
}