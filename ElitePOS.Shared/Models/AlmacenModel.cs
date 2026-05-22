using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class AlmacenModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("codigo")]
        public string codigo { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string nombre { get; set; } = string.Empty;

        [JsonPropertyName("ubicacion")]
        public string ubicacion { get; set; } = string.Empty;

        [JsonPropertyName("activo")]
        public bool activo { get; set; } = true;

        [JsonIgnore]
        public bool IsLocked { get; set; } = false;

        // Propiedades de compatibilidad (Alias)
        [JsonIgnore] public string Id { get => id; set => id = value; }
        [JsonIgnore] public string Codigo { get => codigo; set => codigo = value; }
        [JsonIgnore] public string Nombre { get => nombre; set => nombre = value; }
        [JsonIgnore] public string Ubicacion { get => ubicacion; set => ubicacion = value; }
    }
}
