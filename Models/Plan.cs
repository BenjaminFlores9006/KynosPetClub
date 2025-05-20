using System.Text.Json.Serialization;

namespace KynosPetClub.Models
{
    public class Plan
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }

        [JsonPropertyName("precio")]
        public decimal Precio { get; set; }
        [JsonPropertyName("duracion_dias")]
        public int DuracionDias { get; set; }
    }
}
