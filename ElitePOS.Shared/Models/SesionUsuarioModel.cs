using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class SesionUsuarioModel
    {
        [JsonPropertyName("usuarioId")]
        public string usuarioId { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string nombre { get; set; } = string.Empty;

        [JsonPropertyName("correo")]
        public string correo { get; set; } = string.Empty;

        [JsonPropertyName("rol")]
        public string rol { get; set; } = "CAJERO";

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("estaAutenticado")]
        public bool estaAutenticado { get; set; } = false;

        // Propiedades de compatibilidad (Alias)
        [JsonIgnore] public string id { get => usuarioId; set => usuarioId = value; }
        [JsonIgnore] public string nombreCompleto { get => nombre; set => nombre = value; }
        [JsonIgnore] public string Nombre { get => nombre; set => nombre = value; }
        [JsonIgnore] public string UsuarioId { get => usuarioId; set => usuarioId = value; }
        [JsonIgnore] public string Rol { get => rol; set => rol = value; }
        [JsonIgnore] public string EmpresaId { get => empresaId; set => empresaId = value; }
        [JsonIgnore] public bool EstaAutenticado { get => estaAutenticado; set => estaAutenticado = value; }

        public bool EsAdministrador => rol?.Trim()?.Equals("ADMINISTRADOR", StringComparison.OrdinalIgnoreCase) ?? false;
        public bool EsCajero => rol?.Trim()?.Equals("CAJERO", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
