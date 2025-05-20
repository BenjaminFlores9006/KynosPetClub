using System.Text.Json.Serialization;

namespace KynosPetClub.Models
{
    public class Rol
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombrerol")]
        public string NombreRol { get; set; }

        [JsonPropertyName("definicionrol")]
        public string DefinicionRol { get; set; }
    }
}
