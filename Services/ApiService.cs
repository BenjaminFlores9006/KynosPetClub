using KynosPetClub.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KynosPetClub.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string SupabaseUrl = "https://cfwybqaykyerljqfqtzk.supabase.co/rest/v1/Usuario";
        private const string SupabaseApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNmd3licWF5a3llcmxqcWZxdHprIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDc2MTU1MTcsImV4cCI6MjA2MzE5MTUxN30.0NvvKf7vF_SLMB4OvpxgatIACDStEWu6MR83LCkn5C0";

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SupabaseApiKey);
            _httpClient.DefaultRequestHeaders.Add("apikey", SupabaseApiKey);
        }

        public async Task<string> RegistrarUsuarioAsync(Usuario usuario)
        {
            var json = JsonSerializer.Serialize(usuario);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(SupabaseUrl, content);

            if (response.IsSuccessStatusCode)
                return "OK";

            // Leer el error detallado
            var error = await response.Content.ReadAsStringAsync();
            return $"ERROR: {error}";
        }

        public async Task<Usuario?> LogInUsuarioAsync(string correo, string contraseña)
        {
            var url = $"{SupabaseUrl}?correo=eq.{correo}&contraseña=eq.{contraseña}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var usuarios = JsonSerializer.Deserialize<List<Usuario>>(json);

            return usuarios?.FirstOrDefault();
        }
    }
}
