using System;
using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class ResultadoPrueba
    {
        [JsonPropertyName("nombrePrueba")]
        public string nombrePrueba { get; set; } = string.Empty;

        [JsonPropertyName("fechaEjecucion")]
        public DateTime fechaEjecucion { get; set; } = DateTime.Now;

        [JsonPropertyName("estado")]
        public string estado { get; set; } = string.Empty; // OK, ERROR

        [JsonPropertyName("mensaje")]
        public string mensaje { get; set; } = string.Empty;

        [JsonPropertyName("costoCalculado")]
        public decimal costoCalculado { get; set; }

        [JsonPropertyName("costoEsperado")]
        public decimal costoEsperado { get; set; }

        [JsonPropertyName("discrepancia")]
        public decimal discrepancia { get; set; }
    }
}
