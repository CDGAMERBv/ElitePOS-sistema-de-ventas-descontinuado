using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    /// <summary>
    /// Representa los datos del emisor del comprobante electrónico
    /// </summary>
    public class Emisor
    {
        [Required]
        [StringLength(11, MinimumLength = 11)]
        [JsonPropertyName("ruc")]
        public string ruc { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [JsonPropertyName("razonSocial")]
        public string razonSocial { get; set; } = string.Empty;
        
        [StringLength(300)]
        [JsonPropertyName("nombreComercial")]
        public string nombreComercial { get; set; } = string.Empty;
        
        [StringLength(300)]
        [JsonPropertyName("direccion")]
        public string direccion { get; set; } = string.Empty;
        
        [StringLength(2, MinimumLength = 2)]
        [JsonPropertyName("ubigeo")]
        public string ubigeo { get; set; } = string.Empty;
        
        [StringLength(50)]
        [JsonPropertyName("departamento")]
        public string departamento { get; set; } = string.Empty;
        
        [StringLength(50)]
        [JsonPropertyName("provincia")]
        public string provincia { get; set; } = string.Empty;
        
        [StringLength(50)]
        [JsonPropertyName("distrito")]
        public string distrito { get; set; } = string.Empty;
        
        [StringLength(50)]
        [JsonPropertyName("urbanizacion")]
        public string urbanizacion { get; set; } = string.Empty;
        
        [StringLength(10)]
        [JsonPropertyName("codigoPais")]
        public string codigoPais { get; set; } = "PE";
        
        [StringLength(10)]
        [JsonPropertyName("codigoPaisTelefono")]
        public string codigoPaisTelefono { get; set; } = string.Empty;
        
        [StringLength(20)]
        [JsonPropertyName("telefono")]
        public string telefono { get; set; } = string.Empty;
        
        [StringLength(100)]
        [JsonPropertyName("correoElectronico")]
        public string correoElectronico { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa los datos del receptor del comprobante electrónico
    /// </summary>
    public class Receptor
    {
        [Required]
        [StringLength(11, MinimumLength = 11)]
        [JsonPropertyName("tipoDocumento")]
        public string tipoDocumento { get; set; } = "6"; // 6 = RUC
        
        [Required]
        [StringLength(11, MinimumLength = 8)]
        [JsonPropertyName("numeroDocumento")]
        public string numeroDocumento { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        [JsonPropertyName("razonSocial")]
        public string razonSocial { get; set; } = string.Empty;
        
        [StringLength(300)]
        [JsonPropertyName("direccion")]
        public string direccion { get; set; } = string.Empty;
        
        [StringLength(2, MinimumLength = 2)]
        [JsonPropertyName("ubigeo")]
        public string ubigeo { get; set; } = string.Empty;
        
        [StringLength(50)]
        [JsonPropertyName("departamento")]
        public string departamento { get; set; } = string.Empty;
        
        [StringLength(50)]
        [JsonPropertyName("provincia")]
        public string provincia { get; set; } = string.Empty;
        
        [StringLength(50)]
        [JsonPropertyName("distrito")]
        public string distrito { get; set; } = string.Empty;
        
        [StringLength(100)]
        [JsonPropertyName("correoElectronico")]
        public string correoElectronico { get; set; } = string.Empty;
    }

    /// <summary>
    /// Representa un comprobante electrónico para SUNAT
    /// </summary>
    public class ComprobanteSunat
    {
        [Required]
        [JsonPropertyName("tipoComprobante")]
        public string tipoComprobante { get; set; } = string.Empty;
        
        [Required]
        [StringLength(4)]
        [JsonPropertyName("serie")]
        public string serie { get; set; } = string.Empty;
        
        [Required]
        [StringLength(8)]
        [JsonPropertyName("numero")]
        public string numero { get; set; } = string.Empty;
        
        [Required]
        [JsonPropertyName("fechaEmision")]
        public DateTime fechaEmision { get; set; }
        
        [JsonPropertyName("fechaVencimiento")]
        public DateTime? fechaVencimiento { get; set; }
        
        [Required]
        [JsonPropertyName("tipoMoneda")]
        public string tipoMoneda { get; set; } = "PEN";
        
        [Required]
        [JsonPropertyName("totalGravadas")]
        public decimal totalGravadas { get; set; }
        
        [Required]
        [JsonPropertyName("totalInafectas")]
        public decimal totalInafectas { get; set; }
        
        [Required]
        [JsonPropertyName("totalExoneradas")]
        public decimal totalExoneradas { get; set; }
        
        [Required]
        [JsonPropertyName("totalIgv")]
        public decimal totalIgv { get; set; }
        
        [Required]
        [JsonPropertyName("totalIsc")]
        public decimal totalIsc { get; set; }
        
        [Required]
        [JsonPropertyName("totalOtrosTributos")]
        public decimal totalOtrosTributos { get; set; }
        
        [Required]
        [JsonPropertyName("totalVenta")]
        public decimal totalVenta { get; set; }
        
        [Required]
        [JsonPropertyName("totalDescuentos")]
        public decimal totalDescuentos { get; set; }
        
        [Required]
        [JsonPropertyName("totalAnticipos")]
        public decimal totalAnticipos { get; set; }
        
        [JsonPropertyName("totalPercepciones")]
        public decimal totalPercepciones { get; set; }
        
        [JsonPropertyName("totalRetenciones")]
        public decimal totalRetenciones { get; set; }
        
        [JsonPropertyName("totalDetracciones")]
        public decimal totalDetracciones { get; set; }
        
        [JsonPropertyName("totalBonificaciones")]
        public decimal totalBonificaciones { get; set; }
        
        [JsonPropertyName("totalCargos")]
        public decimal totalCargos { get; set; }
        
        [JsonPropertyName("totalOtrosCargos")]
        public decimal totalOtrosCargos { get; set; }
        
        [Required]
        [JsonPropertyName("emisor")]
        public Emisor emisor { get; set; } = new();
        
        [Required]
        [JsonPropertyName("receptor")]
        public Receptor receptor { get; set; } = new();
        
        [JsonPropertyName("detalles")]
        public List<DetalleComprobanteSunat> detalles { get; set; } = new();
        
        [JsonPropertyName("tributos")]
        public List<TributoComprobante> tributos { get; set; } = new();
        
        [JsonPropertyName("leyendas")]
        public List<LeyendaComprobante> leyendas { get; set; } = new();
    }

    public class DetalleComprobanteSunat
    {
        [Required]
        [JsonPropertyName("codigo")]
        public string codigo { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        [JsonPropertyName("descripcion")]
        public string descripcion { get; set; } = string.Empty;
        
        [Required]
        [JsonPropertyName("cantidad")]
        public decimal cantidad { get; set; }
        
        [Required]
        [JsonPropertyName("precioUnitario")]
        public decimal precioUnitario { get; set; }
        
        [Required]
        [JsonPropertyName("valorUnitario")]
        public decimal valorUnitario { get; set; }
        
        [Required]
        [JsonPropertyName("igv")]
        public decimal igv { get; set; }
        
        [Required]
        [JsonPropertyName("total")]
        public decimal total { get; set; }
        
        [Required]
        [JsonPropertyName("tipoPrecio")]
        public string tipoPrecio { get; set; } = "01";
        
        [Required]
        [JsonPropertyName("unidadMedida")]
        public string unidadMedida { get; set; } = "NIU";
        
        [Required]
        [JsonPropertyName("tipoAfectacionIgv")]
        public string tipoAfectacionIgv { get; set; } = "10";
        
        [JsonPropertyName("porcentajeIgv")]
        public decimal porcentajeIgv { get; set; } = 18m;
        
        [JsonPropertyName("codigoProductoSunat")]
        public string codigoProductoSunat { get; set; } = string.Empty;
        
        [JsonPropertyName("descuento")]
        public decimal descuento { get; set; }
        
        [JsonPropertyName("valorReferencial")]
        public decimal valorReferencial { get; set; }
    }

    public class TributoComprobante
    {
        [Required]
        [JsonPropertyName("id")]
        public string id { get; set; } = "1000";
        
        [Required]
        [JsonPropertyName("nombre")]
        public string nombre { get; set; } = "IGV";
        
        [Required]
        [JsonPropertyName("tipoTributo")]
        public string tipoTributo { get; set; } = "VAT";
        
        [Required]
        [JsonPropertyName("tipoAfectacion")]
        public string tipoAfectacion { get; set; } = "10";
        
        [Required]
        [JsonPropertyName("porcentaje")]
        public decimal porcentaje { get; set; } = 18m;
        
        [Required]
        [JsonPropertyName("baseImponible")]
        public decimal baseImponible { get; set; }
        
        [Required]
        [JsonPropertyName("monto")]
        public decimal monto { get; set; }
    }

    public class LeyendaComprobante
    {
        [Required]
        [JsonPropertyName("codigo")]
        public string codigo { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        [JsonPropertyName("valor")]
        public string valor { get; set; } = string.Empty;
    }

    public class RespuestaSunat
    {
        [JsonPropertyName("exito")]
        public bool exito { get; set; }

        [JsonPropertyName("mensaje")]
        public string mensaje { get; set; } = string.Empty;

        [JsonPropertyName("codigoHash")]
        public string codigoHash { get; set; } = string.Empty;

        [JsonPropertyName("xmlBase64")]
        public string xmlBase64 { get; set; } = string.Empty;

        [JsonPropertyName("cdrBase64")]
        public string cdrBase64 { get; set; } = string.Empty;

        [JsonPropertyName("codigoRespuesta")]
        public string codigoRespuesta { get; set; } = string.Empty;

        [JsonPropertyName("errores")]
        public List<string> errores { get; set; } = new();

        [JsonPropertyName("fechaRespuesta")]
        public DateTime fechaRespuesta { get; set; } = DateTime.Now;

        [JsonPropertyName("ticket")]
        public string ticket { get; set; } = string.Empty;
    }

    public class FirmaDigital
    {
        [JsonPropertyName("certificadoDigital")]
        public string certificadoDigital { get; set; } = string.Empty;

        [JsonPropertyName("claveCertificado")]
        public string claveCertificado { get; set; } = string.Empty;

        [JsonPropertyName("algoritmoFirma")]
        public string algoritmoFirma { get; set; } = "SHA-256";
    }

    public class ConfiguracionFacturacion
    {
        [JsonPropertyName("usuarioSol")]
        public string usuarioSol { get; set; } = string.Empty;

        [JsonPropertyName("claveSol")]
        public string claveSol { get; set; } = string.Empty;

        [JsonPropertyName("rucEmpresa")]
        public string rucEmpresa { get; set; } = string.Empty;

        [JsonPropertyName("urlProduccion")]
        public string urlProduccion { get; set; } = string.Empty;

        [JsonPropertyName("urlBeta")]
        public string urlBeta { get; set; } = string.Empty;

        [JsonPropertyName("modoProduccion")]
        public bool modoProduccion { get; set; } = false;

        [JsonPropertyName("firma")]
        public FirmaDigital firma { get; set; } = new();
    }
}
