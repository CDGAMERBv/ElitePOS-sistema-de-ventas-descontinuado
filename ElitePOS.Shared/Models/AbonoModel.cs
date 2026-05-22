using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class AbonoModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("ventaId")]
        public string ventaId { get; set; } = string.Empty;

        [JsonPropertyName("numeroComprobante")]
        public string numeroComprobante { get; set; } = string.Empty;

        [JsonPropertyName("clienteId")]
        public string clienteId { get; set; } = string.Empty;

        [JsonPropertyName("nombreCliente")]
        public string nombreCliente { get; set; } = string.Empty;

        [JsonPropertyName("montoAbono")]
        public decimal montoAbono { get; set; }

        [JsonPropertyName("fechaAbono")]
        public DateTime fechaAbono { get; set; } = DateTime.Now;

        [JsonPropertyName("usuarioId")]
        public string usuarioId { get; set; } = string.Empty;

        [JsonPropertyName("nombreUsuario")]
        public string nombreUsuario { get; set; } = string.Empty;

        [JsonPropertyName("observaciones")]
        public string observaciones { get; set; } = string.Empty;

        [JsonPropertyName("metodoPago")]
        public string metodoPago { get; set; } = "Efectivo";

        // Propiedades de compatibilidad (Alias)
        [JsonIgnore] public decimal monto { get => montoAbono; set => montoAbono = value; }
        [JsonIgnore] public DateTime fecha { get => fechaAbono; set => fechaAbono = value; }
    }
}
