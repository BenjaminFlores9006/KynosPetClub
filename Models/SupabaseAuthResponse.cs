// Agregar este modelo a tu carpeta Models

using System.Text.Json.Serialization;

namespace KynosPetClub.Models
{
    public class SupabaseAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string access_token { get; set; }

        [JsonPropertyName("token_type")]
        public string token_type { get; set; }

        [JsonPropertyName("expires_in")]
        public int expires_in { get; set; }

        [JsonPropertyName("refresh_token")]
        public string refresh_token { get; set; }

        [JsonPropertyName("user")]
        public SupabaseUser user { get; set; }
    }

    public class SupabaseUser
    {
        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("email")]
        public string email { get; set; }

        [JsonPropertyName("email_confirmed_at")]
        public string email_confirmed_at { get; set; }
    }
}