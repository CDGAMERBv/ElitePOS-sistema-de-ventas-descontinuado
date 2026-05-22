using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class UsuarioModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("nombreCompleto")]
        public string nombreCompleto { get; set; } = string.Empty;

        [JsonPropertyName("nombreUsuario")]
        public string nombreUsuario { get; set; } = string.Empty;

        [JsonPropertyName("correo")]
        public string correo { get; set; } = string.Empty;

        [JsonPropertyName("contrasena")]
        public string contrasena { get; set; } = string.Empty;

        [JsonPropertyName("rol")]
        public string rol { get; set; } = "Cajero";

        [JsonPropertyName("activo")]
        public bool activo { get; set; } = true;

        [Obsolete("Usar nombreCompleto en su lugar")]
        [JsonIgnore]
        public string Nombre 
        { 
            get => nombreCompleto; 
            set => nombreCompleto = value; 
        }

        [JsonIgnore] public string nombre { get => nombreCompleto; set => nombreCompleto = value; }
        [JsonIgnore] public string NombreCompleto { get => nombreCompleto; set => nombreCompleto = value; }
        [JsonIgnore] public string NombreUsuario { get => nombreUsuario; set => nombreUsuario = value; }
        [JsonIgnore] public string Rol { get => rol; set => rol = value; }
        [JsonIgnore] public string Correo { get => correo; set => correo = value; }
        [JsonIgnore] public string EmpresaId { get => empresaId; set => empresaId = value; }
        [JsonIgnore] public string Id { get => id; set => id = value; }
    }
}
