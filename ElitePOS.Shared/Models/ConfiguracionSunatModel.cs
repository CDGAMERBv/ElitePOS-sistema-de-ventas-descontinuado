using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class ConfiguracionSunatModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = "config-sunat";

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo"; // Multi-empresa

        [JsonPropertyName("modo")]
        public string modo { get; set; } = "BETA";
        
        [Required(ErrorMessage = "El RUC Emisor es requerido")]
        [RegularExpression(@"^[0-9]{11}$", ErrorMessage = "El RUC debe tener exactamente 11 dígitos numéricos")]
        [JsonPropertyName("ruc")]
        public string ruc { get; set; } = string.Empty;

        [Required(ErrorMessage = "La URL de la API es requerida")]
        [Url(ErrorMessage = "Formato de URL inválido. Ejemplo: https://api.pse.pe/v1")]
        [JsonPropertyName("apiUrl")]
        public string apiUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "El Token de Autorización es requerido")]
        [JsonPropertyName("apiToken")]
        public string apiToken { get; set; } = string.Empty;

        [Required(ErrorMessage = "La serie de la boleta por defecto es requerida")]
        [RegularExpression(@"^B[A-Z0-9]{3}$", ErrorMessage = "La serie de Boleta debe empezar con 'B' seguido de 3 caracteres alfanuméricos (Ej: B001)")]
        [JsonPropertyName("serieBoleta")]
        public string serieBoleta { get; set; } = "B001";

        [Required(ErrorMessage = "La serie de la factura por defecto es requerida")]
        [RegularExpression(@"^F[A-Z0-9]{3}$", ErrorMessage = "La serie de Factura debe empezar con 'F' seguido de 3 caracteres alfanuméricos (Ej: F001)")]
        [JsonPropertyName("serieFactura")]
        public string serieFactura { get; set; } = "F001";
    }
}
