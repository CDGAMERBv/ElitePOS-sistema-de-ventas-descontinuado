using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class ProductoTopModel
    {
        [JsonPropertyName("nombre")]
        public string nombre { get; set; } = string.Empty;

        [JsonPropertyName("cantidad")]
        public int cantidad { get; set; }

        [JsonPropertyName("total")]
        public decimal total { get; set; }

        [JsonIgnore] public string Nombre { get => nombre; set => nombre = value; }
        [JsonIgnore] public int Cantidad { get => cantidad; set => cantidad = value; }
        [JsonIgnore] public decimal Total { get => total; set => total = value; }
    }

    public class VentaMensualModel
    {
        [JsonPropertyName("mes")]
        public string mes { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public decimal total { get; set; }

        [JsonIgnore] public string Mes { get => mes; set => mes = value; }
        [JsonIgnore] public decimal Total { get => total; set => total = value; }
    }
}
