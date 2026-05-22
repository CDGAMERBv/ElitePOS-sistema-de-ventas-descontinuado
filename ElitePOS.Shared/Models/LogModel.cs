using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class LogModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = string.Empty;

        [JsonPropertyName("usuarioId")]
        public string usuarioId { get; set; } = string.Empty;

        [JsonPropertyName("nombreUsuario")]
        public string nombreUsuario { get; set; } = string.Empty;

        [JsonPropertyName("accion")]
        public string accion { get; set; } = string.Empty;

        [JsonPropertyName("detalle")]
        public string detalle { get; set; } = string.Empty;

        [JsonPropertyName("modulo")]
        public string modulo { get; set; } = string.Empty;

        [JsonPropertyName("ip")]
        public string ip { get; set; } = string.Empty;

        [JsonPropertyName("fechaHora")]
        public DateTime fechaHora { get; set; }
    }
}
