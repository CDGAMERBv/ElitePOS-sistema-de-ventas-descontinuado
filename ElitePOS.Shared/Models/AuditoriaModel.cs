using System;
using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class AuditoriaModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("fechaHora")]
        public DateTime fechaHora { get; set; } = DateTime.Now;

        [JsonPropertyName("usuario")]
        public string usuario { get; set; } = string.Empty;

        [JsonPropertyName("modulo")]
        public string modulo { get; set; } = string.Empty;

        [JsonPropertyName("accion")]
        public string accion { get; set; } = string.Empty;

        [JsonPropertyName("detalle")]
        public string detalle { get; set; } = string.Empty;

        [JsonPropertyName("ip")]
        public string ip { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = string.Empty;
    }
}
