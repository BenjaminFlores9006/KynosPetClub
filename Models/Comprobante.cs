using System.Text.Json.Serialization;

namespace KynosPetClub.Models
{
    public class Comprobante
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; }

        [JsonPropertyName("fecha_subida")]
        public DateTime FechaSubida { get; set; }

        [JsonPropertyName("url_archivo")]
        public string UrlArchivo { get; set; }

        [JsonPropertyName("estado")]
        public string Estado { get; set; }

        [JsonPropertyName("comentario_admin")]
        public string ComentarioAdmin { get; set; }

        [JsonPropertyName("usuario_id")]
        public int UsuarioId { get; set; }

        [JsonPropertyName("reserva_id")]
        public int ReservaId { get; set; }
    }
}
