using System.Text.Json.Serialization;

public class Reserva
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("fecha_reserva")]
    public DateTime FechaReserva { get; set; }

    [JsonPropertyName("fecha_servicio")]
    public DateTime FechaServicio { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; }

    [JsonPropertyName("comentarios")]
    public string Comentarios { get; set; }

    [JsonPropertyName("usuario_id")]
    public int UsuarioId { get; set; }

    [JsonPropertyName("mascota_id")]
    public int MascotaId { get; set; }

    [JsonPropertyName("servicio_id")]
    public int ServicioId { get; set; }

    // Propiedades de navegación
    [JsonIgnore]
    public Servicio Servicio { get; set; }

    [JsonIgnore]
    public Mascota Mascota { get; set; }
}