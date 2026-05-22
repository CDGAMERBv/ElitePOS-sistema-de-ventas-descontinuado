using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class LimitesPlanModel
    {
        [JsonPropertyName("nombrePlan")]
        public string nombrePlan { get; set; } = "";

        [JsonPropertyName("maxUsuarios")]
        public int maxUsuarios { get; set; }

        [JsonPropertyName("maxAlmacenes")]
        public int maxAlmacenes { get; set; }

        [JsonPropertyName("maxProductos")]
        public int maxProductos { get; set; }

        [JsonPropertyName("maxVentasMes")]
        public int maxVentasMes { get; set; }

        [JsonPropertyName("accesoReportes")]
        public bool accesoReportes { get; set; }

        [JsonPropertyName("accesoModoOffline")]
        public bool accesoModoOffline { get; set; }

        [JsonPropertyName("accesoMultiEmpresa")]
        public bool accesoMultiEmpresa { get; set; }

        [JsonPropertyName("accesoApi")]
        public bool accesoApi { get; set; }
    }
}
