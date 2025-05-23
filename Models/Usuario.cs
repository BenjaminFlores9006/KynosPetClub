using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KynosPetClub.Models
{
    public class Usuario
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("nombre")]
        public string nombre { get; set; }

        [JsonPropertyName("apellido")]
        public string apellido { get; set; }

        [JsonPropertyName("fechanac")]
        public DateTime fechanac { get; set; }

        [JsonPropertyName("correo")]
        public string correo { get; set; }

        [JsonPropertyName("contraseña")]
        public string contraseña { get; set; }

        [JsonPropertyName("foto")]
        public string foto { get; set; }

        [JsonPropertyName("auth_id")]
        public string? AuthId { get; set; }

        [JsonPropertyName("rol_id")]
        public int? RolId { get; set; }

        [JsonPropertyName("plan_id")]
        public int? PlanId { get; set; }

        // Constructor vacío necesario para la deserialización
        public Usuario() { }

        // Constructor para crear un nuevo usuario desde el formulario de registro
        public Usuario(string nombre, string apellido, DateTime fechanac, string correo, string contraseña)
        {
            this.nombre = nombre;
            this.apellido = apellido;
            this.fechanac = fechanac;
            this.correo = correo;
            this.contraseña = contraseña;
            // Los campos Id, AuthId, RolId y PlanId serán asignados por la base de datos o posteriormente
        }
    }
}