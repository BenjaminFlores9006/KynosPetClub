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

    // Propiedades de navegación para datos relacionados de Supabase
    [JsonPropertyName("servicio")]
    public Servicio Servicio { get; set; }

    [JsonPropertyName("mascota")]
    public Mascota Mascota { get; set; }

    // Enum para estados válidos
    public static class EstadosValidos
    {
        public const string Pendiente = "Pendiente";
        public const string EnCurso = "En curso";
        public const string Completado = "Completado";
        public const string Cancelado = "Cancelado";
    }

    // Método helper para validar estado
    public bool TieneEstadoValido()
    {
        return Estado == EstadosValidos.Pendiente ||
               Estado == EstadosValidos.EnCurso ||
               Estado == EstadosValidos.Completado ||
               Estado == EstadosValidos.Cancelado;
    }

    // Método helper para verificar si es una reserva activa
    public bool EsReservaActiva()
    {
        return Estado == EstadosValidos.Pendiente || Estado == EstadosValidos.EnCurso;
    }
}