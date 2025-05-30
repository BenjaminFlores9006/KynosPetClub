﻿using System.Text.Json.Serialization;

public class Servicio
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; }

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; }

    [JsonPropertyName("precio")]
    public decimal Precio { get; set; }
}