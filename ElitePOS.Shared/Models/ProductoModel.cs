using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class ProductoModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("nombre")]
        public string nombre { get; set; } = string.Empty;

        [JsonPropertyName("precioVenta")]
        public decimal precioVenta { get; set; }

        [JsonPropertyName("precioCompra")]
        public decimal precioCompra { get; set; }

        [JsonPropertyName("precioCosto")]
        public decimal precioCosto { get; set; }

        [JsonPropertyName("stock")]
        public int stock { get; set; }

        [JsonPropertyName("codigoBarras")]
        public string codigoBarras { get; set; } = string.Empty;

        [JsonPropertyName("categoria")]
        public string categoria { get; set; } = string.Empty;

        [JsonPropertyName("tipo")]
        public string tipo { get; set; } = "Producto";

        [JsonPropertyName("imagenUrl")]
        public string imagenUrl { get; set; } = string.Empty;

        [JsonPropertyName("unidadMedida")]
        public string unidadMedida { get; set; } = "Unidad";

        [JsonPropertyName("tipoAfectacionIgv")]
        public string tipoAfectacionIgv { get; set; } = "10";

        [JsonPropertyName("stockMinimo")]
        public int stockMinimo { get; set; } = 5;
        
        [JsonPropertyName("costoPromedio")]
        public decimal costoPromedio { get; set; } = 0;

        [JsonPropertyName("costoTotalAcumulado")]
        public decimal costoTotalAcumulado { get; set; } = 0;

        [JsonPropertyName("unidadesCompradasHistoricas")]
        public int unidadesCompradasHistoricas { get; set; } = 0;

        [JsonPropertyName("ultimoCambioCosto")]
        public DateTime ultimoCambioCosto { get; set; } = DateTime.Now;
        
        [JsonPropertyName("tieneVariantes")]
        public bool tieneVariantes { get; set; } = false;

        [JsonPropertyName("variantes")]
        public List<ProductoVariante> variantes { get; set; } = new List<ProductoVariante>();

        [JsonPropertyName("lotes")]
        public List<ProductoLote> lotes { get; set; } = new List<ProductoLote>();

        // Propiedades de compatibilidad (Alias)
        [JsonIgnore] public string Id { get => id; set => id = value; }
        [JsonIgnore] public string Nombre { get => nombre; set => nombre = value; }
        [JsonIgnore] public string CodigoBarras { get => codigoBarras; set => codigoBarras = value; }
        [JsonIgnore] public int Stock { get => stock; set => stock = value; }
        [JsonIgnore] public int stockActual { get => stock; set => stock = value; }
        [JsonIgnore] public decimal PrecioVenta { get => precioVenta; set => precioVenta = value; }
        [JsonIgnore] public decimal PrecioCosto { get => precioCosto; set => precioCosto = value; }
        [JsonIgnore] public decimal PrecioCompra { get => precioCompra; set => precioCompra = value; }
        [JsonIgnore] public string Categoria { get => categoria; set => categoria = value; }
        [JsonIgnore] public string Tipo { get => tipo; set => tipo = value; }
        [JsonIgnore] public string EmpresaId { get => empresaId; set => empresaId = value; }
    }

    public class ProductoVariante
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("productoId")]
        public string productoId { get; set; } = string.Empty;

        [JsonPropertyName("tipoVariante")]
        public string tipoVariante { get; set; } = string.Empty;

        [JsonPropertyName("valorVariante")]
        public string valorVariante { get; set; } = string.Empty;

        [JsonPropertyName("precioVenta")]
        public decimal precioVenta { get; set; }

        [JsonPropertyName("stock")]
        public int stock { get; set; }

        [JsonPropertyName("codigoBarras")]
        public string codigoBarras { get; set; } = string.Empty;

        [JsonPropertyName("sku")]
        public string sku { get; set; } = string.Empty;
    }

    public class ProductoLote
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("productoId")]
        public string productoId { get; set; } = string.Empty;

        [JsonPropertyName("numeroLote")]
        public string numeroLote { get; set; } = string.Empty;

        [JsonPropertyName("cantidad")]
        public int cantidad { get; set; }

        [JsonPropertyName("fechaVencimiento")]
        public DateTime fechaVencimiento { get; set; }

        [JsonPropertyName("fechaIngreso")]
        public DateTime fechaIngreso { get; set; }

        [JsonPropertyName("precioCosto")]
        public decimal precioCosto { get; set; }

        [JsonPropertyName("proveedor")]
        public string proveedor { get; set; } = string.Empty;

        [JsonPropertyName("activo")]
        public bool activo { get; set; } = true;
    }
}
