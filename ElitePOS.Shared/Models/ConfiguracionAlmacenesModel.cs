using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class ConfiguracionAlmacenesModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = "config-almacenes";

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo"; // Multi-empresa

        [JsonPropertyName("notificarStockMinimo")]
        public bool notificarStockMinimo { get; set; } = true;

        [JsonPropertyName("almacenes")]
        public List<AlmacenModel> almacenes { get; set; } = new List<AlmacenModel>();
    }
}
