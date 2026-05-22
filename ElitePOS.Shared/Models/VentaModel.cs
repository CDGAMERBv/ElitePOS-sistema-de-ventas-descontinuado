using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    public class VentaModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("empresaId")]
        public string empresaId { get; set; } = "empresa-demo";

        [JsonPropertyName("fechaHora")]
        public DateTime fechaHora { get; set; }

        [JsonPropertyName("numeroComprobante")]
        public string numeroComprobante { get; set; } = string.Empty;

        [JsonPropertyName("cliente")]
        public string cliente { get; set; } = string.Empty;

        [JsonPropertyName("clienteId")]
        public string clienteId { get; set; } = string.Empty;

        [JsonPropertyName("tipoComprobante")]
        public string tipoComprobante { get; set; } = string.Empty;

        [JsonPropertyName("metodoPago")]
        public string metodoPago { get; set; } = "Efectivo";

        [JsonPropertyName("moneda")]
        public string moneda { get; set; } = "PEN";

        [JsonPropertyName("condicionPago")]
        public string condicionPago { get; set; } = "Contado";

        [JsonPropertyName("total")]
        public decimal total { get; set; }

        [JsonPropertyName("subtotalGravada")]
        public decimal subtotalGravada { get; set; }

        [JsonPropertyName("igv")]
        public decimal igv { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal subtotal { get; set; }

        [JsonPropertyName("items")]
        public List<VentaItemModel> items { get; set; } = new List<VentaItemModel>();

        [JsonPropertyName("anulada")]
        public bool anulada { get; set; } = false;

        [JsonPropertyName("usuarioId")]
        public string usuarioId { get; set; } = string.Empty;

        [JsonPropertyName("nombreUsuario")]
        public string nombreUsuario { get; set; } = string.Empty;

        [JsonPropertyName("cajaId")]
        public string cajaId { get; set; } = string.Empty;

        [JsonPropertyName("sucursalId")]
        public string sucursalId { get; set; } = string.Empty;

        [JsonPropertyName("observaciones")]
        public string observaciones { get; set; } = string.Empty;

        [JsonPropertyName("esProforma")]
        public bool esProforma { get; set; } = false;

        [JsonPropertyName("ordenCompra")]
        public string ordenCompra { get; set; } = string.Empty;

        [JsonPropertyName("guiaRemision")]
        public string guiaRemision { get; set; } = string.Empty;

        [JsonPropertyName("placa")]
        public string placa { get; set; } = string.Empty;

        [JsonPropertyName("tipoPago")]
        public string tipoPago { get; set; } = "Contado";

        [JsonPropertyName("estadoPago")]
        public string estadoPago { get; set; } = "pagado";

        [JsonPropertyName("montoPagado")]
        public decimal montoPagado { get; set; } = 0;

        [JsonIgnore]
        public decimal montoPendiente => total - montoPagado;

        [JsonPropertyName("fechaVencimiento")]
        public DateTime? fechaVencimiento { get; set; }

        [JsonPropertyName("estadoSunat")]
        public string estadoSunat { get; set; } = "PENDIENTE";

        [JsonPropertyName("mensajeSunat")]
        public string mensajeSunat { get; set; } = string.Empty;

        [JsonPropertyName("hashSunat")]
        public string hashSunat { get; set; } = string.Empty;

        [JsonPropertyName("intentosEnvio")]
        public int intentosEnvio { get; set; } = 0;

        [JsonPropertyName("fechaUltimoIntento")]
        public DateTime? fechaUltimoIntento { get; set; }

        [JsonPropertyName("tipoDocumentoCliente")]
        public string tipoDocumentoCliente { get; set; } = "DNI";

        [JsonPropertyName("numeroDocumentoCliente")]
        public string numeroDocumentoCliente { get; set; } = string.Empty;

        [JsonPropertyName("direccionCliente")]
        public string direccionCliente { get; set; } = string.Empty;

        [JsonPropertyName("emailCliente")]
        public string emailCliente { get; set; } = string.Empty;

        [JsonPropertyName("tipoOperacion")]
        public string tipoOperacion { get; set; } = "0101";

        [JsonPropertyName("subtotalExonerada")]
        public decimal subtotalExonerada { get; set; } = 0;

        [JsonPropertyName("subtotalInafecta")]
        public decimal subtotalInafecta { get; set; } = 0;

        [JsonPropertyName("mtoCargos")]
        public decimal mtoCargos { get; set; } = 0;

        [JsonPropertyName("mtoTributos")]
        public decimal mtoTributos { get; set; } = 0;

        [JsonPropertyName("mtoDetraccion")]
        public decimal mtoDetraccion { get; set; } = 0;

        [JsonPropertyName("valorVenta")]
        public decimal valorVenta { get; set; } = 0;

        // Propiedades de compatibilidad (Alias)
        [JsonIgnore] public string Id { get => id; set => id = value; }
        [JsonIgnore] public DateTime FechaHora { get => fechaHora; set => fechaHora = value; }
        [JsonIgnore] public string NumeroComprobante { get => numeroComprobante; set => numeroComprobante = value; }
        [JsonIgnore] public string TipoComprobante { get => tipoComprobante; set => tipoComprobante = value; }
        [JsonIgnore] public string MetodoPago { get => metodoPago; set => metodoPago = value; }
        [JsonIgnore] public string Cliente { get => cliente; set => cliente = value; }
        [JsonIgnore] public decimal Total { get => total; set => total = value; }
        [JsonIgnore] public decimal Subtotal { get => subtotal; set => subtotal = value; }
        [JsonIgnore] public decimal IGV { get => igv; set => igv = value; }
        [JsonIgnore] public decimal SubtotalGravada { get => subtotalGravada; set => subtotalGravada = value; }
        [JsonIgnore] public List<VentaItemModel> Items { get => items; set => items = value; }
        [JsonIgnore] public string ClienteId { get => clienteId; set => clienteId = value; }
        [JsonIgnore] public string Observaciones { get => observaciones; set => observaciones = value; }
        [JsonIgnore] public string CondicionPago { get => condicionPago; set => condicionPago = value; }
        [JsonIgnore] public bool EsProforma { get => esProforma; set => esProforma = value; }
        [JsonIgnore] public bool Anulada { get => anulada; set => anulada = value; }
        [JsonIgnore] public string MensajeSunat { get => mensajeSunat; set => mensajeSunat = value; }
        [JsonIgnore] public string HashSunat { get => hashSunat; set => hashSunat = value; }
        [JsonIgnore] public int IntentosEnvio { get => intentosEnvio; set => intentosEnvio = value; }
        [JsonIgnore] public string EstadoSunat { get => estadoSunat; set => estadoSunat = value; }
        [JsonIgnore] public string TipoDocumentoCliente { get => tipoDocumentoCliente; set => tipoDocumentoCliente = value; }
        [JsonIgnore] public string NumeroDocumentoCliente { get => numeroDocumentoCliente; set => numeroDocumentoCliente = value; }
        [JsonIgnore] public string EmailCliente { get => emailCliente; set => emailCliente = value; }
        [JsonIgnore] public string DireccionCliente { get => direccionCliente; set => direccionCliente = value; }
        [JsonIgnore] public string TipoOperacion { get => tipoOperacion; set => tipoOperacion = value; }
        [JsonIgnore] public string Moneda { get => moneda; set => moneda = value; }
        [JsonIgnore] public DateTime? FechaVencimiento { get => fechaVencimiento; set => fechaVencimiento = value; }
        [JsonIgnore] public string? NombreUsuario { get => nombreUsuario; set => nombreUsuario = value; }
        [JsonIgnore] public string? TipoPago { get => tipoPago; set => tipoPago = value; }
        [JsonIgnore] public decimal SubtotalExonerada { get => subtotalExonerada; set => subtotalExonerada = value; }
        [JsonIgnore] public decimal SubtotalInafecta { get => subtotalInafecta; set => subtotalInafecta = value; }
        [JsonIgnore] public decimal MtoCargos { get => mtoCargos; set => mtoCargos = value; }
        [JsonIgnore] public decimal MtoTributos { get => mtoTributos; set => mtoTributos = value; }
        [JsonIgnore] public decimal MtoDetraccion { get => mtoDetraccion; set => mtoDetraccion = value; }
        [JsonIgnore] public decimal ValorVenta { get => valorVenta; set => valorVenta = value; }
        [JsonIgnore] public string OrdenCompra { get => ordenCompra; set => ordenCompra = value; }
        [JsonIgnore] public string GuiaRemision { get => guiaRemision; set => guiaRemision = value; }
        [JsonIgnore] public string Placa { get => placa; set => placa = value; }
    }

    public class VentaItemModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("productoId")]
        public string productoId { get; set; } = string.Empty;

        [JsonPropertyName("codigoInterno")]
        public string codigoInterno { get; set; } = string.Empty;

        [JsonPropertyName("nombreProducto")]
        public string nombreProducto { get; set; } = string.Empty;

        [JsonPropertyName("unidadMedida")]
        public string unidadMedida { get; set; } = "NIU";

        [JsonPropertyName("cantidad")]
        public int cantidad { get; set; }

        [JsonPropertyName("precioUnitario")]
        public decimal precioUnitario { get; set; }

        [JsonPropertyName("precioCosto")]
        public decimal precioCosto { get; set; }

        [JsonPropertyName("precioVenta")]
        public decimal precioVenta { get; set; }

        [JsonPropertyName("subtotal")]
        public decimal subtotal { get; set; }

        // Propiedades de compatibilidad (Alias)
        [JsonIgnore] public string Id { get => id; set => id = value; }
        [JsonIgnore] public string ProductoId { get => productoId; set => productoId = value; }
        [JsonIgnore] public string NombreProducto { get => nombreProducto; set => nombreProducto = value; }
        [JsonIgnore] public int Cantidad { get => (int)cantidad; set => cantidad = value; }
        [JsonIgnore] public decimal PrecioUnitario { get => precioUnitario; set => precioUnitario = value; }
        [JsonIgnore] public decimal PrecioVenta { get => precioVenta; set => precioVenta = value; }
        [JsonIgnore] public decimal Subtotal { get => subtotal; set => subtotal = value; }
        [JsonIgnore] public string? UnidadMedida { get => unidadMedida; set => unidadMedida = value; }
        [JsonIgnore] public string? CodigoInterno { get => codigoInterno; set => codigoInterno = value; }
    }
}
