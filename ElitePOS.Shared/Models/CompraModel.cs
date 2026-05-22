using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class CompraModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo"; // Multi-empresa

        [JsonPropertyName("fechaCompra")]
        public DateTime fechaCompra { get; set; }

        [JsonPropertyName("proveedor")]
        public string proveedor { get; set; } = string.Empty;

        [JsonPropertyName("numeroDocumento")]
        public string numeroDocumento { get; set; } = string.Empty;

        [JsonPropertyName("productoId")]
        public string productoId { get; set; } = string.Empty;

        [JsonPropertyName("nombreProducto")]
        public string nombreProducto { get; set; } = string.Empty;

        [JsonPropertyName("cantidad")]
        public int cantidad { get; set; }

        [JsonPropertyName("costoUnitario")]
        public decimal costoUnitario { get; set; }

        [JsonPropertyName("total")]
        public decimal total { get; set; }

        [JsonPropertyName("usuarioId")]
        public string usuarioId { get; set; } = string.Empty; // Quién registró la compra

        [JsonPropertyName("nombreUsuario")]
        public string nombreUsuario { get; set; } = string.Empty;

        // Propiedades de compatibilidad (Alias)
        [JsonIgnore] public string Id { get => id; set => id = value; }
        [JsonIgnore] public string EmpresaId { get => empresaId; set => empresaId = value; }
        [JsonIgnore] public DateTime Fecha { get => fechaCompra; set => fechaCompra = value; }
        [JsonIgnore] public DateTime FechaCompra { get => fechaCompra; set => fechaCompra = value; }
        [JsonIgnore] public string ProveedorNombre { get => proveedor; set => proveedor = value; }
        [JsonIgnore] public string Proveedor { get => proveedor; set => proveedor = value; }
        [JsonIgnore] public string ProductoNombre { get => nombreProducto; set => nombreProducto = value; }
        [JsonIgnore] public decimal Total { get => total; set => total = value; }
    }
}
