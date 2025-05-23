using System.Text.Json.Serialization;

public class Mascota
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; }

    [JsonPropertyName("especie")]
    public string Especie { get; set; }

    [JsonPropertyName("raza")]
    public string Raza { get; set; }

    [JsonPropertyName("fecha_nacimiento")]
    public DateTime FechaNacimiento { get; set; }

    [JsonPropertyName("foto")]
    public string Foto { get; set; }

    [JsonPropertyName("usuario_id")]
    public int UsuarioId { get; set; }
}