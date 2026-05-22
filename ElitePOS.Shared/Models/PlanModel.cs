using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class PlanModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = "plan-actual";

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("nombrePlan")]
        public string nombrePlan { get; set; } = "Plan Gratuito";

        [JsonPropertyName("fechaVencimiento")]
        public DateTime fechaVencimiento { get; set; } = DateTime.Now.AddMonths(1);

        [JsonPropertyName("limiteProductos")]
        public int limiteProductos { get; set; } = 100;

        [JsonPropertyName("productosUsados")]
        public int productosUsados { get; set; } = 0;

        [JsonPropertyName("limiteVentas")]
        public int limiteVentas { get; set; } = 500;

        [JsonPropertyName("ventasUsadas")]
        public int ventasUsadas { get; set; } = 0;

        [JsonPropertyName("limiteUsuarios")]
        public int limiteUsuarios { get; set; } = 2;

        [JsonPropertyName("usuariosUsados")]
        public int usuariosUsados { get; set; } = 1;
    }
}
