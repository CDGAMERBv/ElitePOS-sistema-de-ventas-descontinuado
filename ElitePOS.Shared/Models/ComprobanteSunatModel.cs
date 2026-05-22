using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    /// <summary>
    /// Modelo para comprobante electrónico según estándar UBL 2.1 (SUNAT Perú)
    /// ✅ Cumple con normativa peruana vigente
    /// </summary>
    public class ComprobanteSunatModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("tipoDocumento")]
        public string tipoDocumento { get; set; } = string.Empty; // "01" Factura, "03" Boleta

        [JsonPropertyName("serie")]
        public string serie { get; set; } = string.Empty;

        [JsonPropertyName("numero")]
        public string numero { get; set; } = string.Empty;

        [JsonPropertyName("fechaEmision")]
        public DateTime fechaEmision { get; set; }

        [JsonPropertyName("monedaId")]
        public string monedaId { get; set; } = "PEN"; // Soles peruanos

        // Cliente
        [JsonPropertyName("clienteTipoDocumento")]
        public string clienteTipoDocumento { get; set; } = "1"; // 1=DNI, 6=RUC

        [JsonPropertyName("clienteNumeroDocumento")]
        public string clienteNumeroDocumento { get; set; } = string.Empty;

        [JsonPropertyName("clienteRazonSocial")]
        public string clienteRazonSocial { get; set; } = string.Empty;

        // Totales según SUNAT (UBL 2.1)
        [JsonPropertyName("totalOperacionesGravadas")]
        public decimal totalOperacionesGravadas { get; set; } = 0; // Base imponible (sin IGV)

        [JsonPropertyName("totalOperacionesInafectas")]
        public decimal totalOperacionesInafectas { get; set; } = 0; // Sin impuestos

        [JsonPropertyName("totalOperacionesExoneradas")]
        public decimal totalOperacionesExoneradas { get; set; } = 0; // Exoneradas de IGV

        [JsonPropertyName("totalIgv")]
        public decimal totalIgv { get; set; } = 0; // 18% del gravado

        [JsonPropertyName("totalIcbper")]
        public decimal totalIcbper { get; set; } = 0; // Impuesto bolsas plásticas (S/ 0.50 c/u desde 2024)

        [JsonPropertyName("totalVenta")]
        public decimal totalVenta { get; set; } = 0; // Total a pagar

        // Items del comprobante
        [JsonPropertyName("items")]
        public List<ItemComprobanteSunat> items { get; set; } = new List<ItemComprobanteSunat>();

        // Leyendas SUNAT
        [JsonPropertyName("importeEnLetras")]
        public string importeEnLetras { get; set; } = string.Empty; // "SON: DOSCIENTOS CINCUENTA Y 00/100 SOLES"

        // Hash y firma digital (futuro - para OSE)
        [JsonPropertyName("hashCpe")]
        public string? hashCpe { get; set; }

        [JsonPropertyName("firmaDigital")]
        public string? firmaDigital { get; set; }

        // 🆕 Código QR según normativa SUNAT
        [JsonPropertyName("codigoQr")]
        public string codigoQr { get; set; } = string.Empty; // RUC|Tipo|Serie|Numero|IGV|Total|Fecha|DNI/RUC
    }

    public class ItemComprobanteSunat
    {
        [JsonPropertyName("numero")]
        public int numero { get; set; }

        [JsonPropertyName("unidadMedida")]
        public string unidadMedida { get; set; } = "NIU"; // NIU = Unidad (ver catálogo 03 SUNAT)

        [JsonPropertyName("cantidad")]
        public decimal cantidad { get; set; }

        [JsonPropertyName("descripcion")]
        public string descripcion { get; set; } = string.Empty;

        [JsonPropertyName("valorUnitario")]
        public decimal valorUnitario { get; set; } = 0; // Precio sin IGV

        [JsonPropertyName("precioUnitario")]
        public decimal precioUnitario { get; set; } = 0; // Precio con IGV

        [JsonPropertyName("totalIgv")]
        public decimal totalIgv { get; set; } = 0;

        [JsonPropertyName("totalVenta")]
        public decimal totalVenta { get; set; } = 0;

        // 🆕 Tipo de afectación IGV (catálogo 07 SUNAT)
        [JsonPropertyName("codigoTipoAfectacionIgv")]
        public string codigoTipoAfectacionIgv { get; set; } = "10"; // 10=Gravado, 20=Exonerado, 30=Inafecto
    }

    /// <summary>
    /// 🆕 Resumen diario de boletas (RC - Resumen de Comprobantes)
    /// Requerido por SUNAT para agrupar boletas del día
    /// </summary>
    public class ResumenDiarioModel
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("fechaGeneracion")]
        public DateTime fechaGeneracion { get; set; } = DateTime.Now;

        [JsonPropertyName("fechaResumen")]
        public DateTime fechaResumen { get; set; } // Fecha de las boletas agrupadas

        [JsonPropertyName("identificador")]
        public string identificador { get; set; } = string.Empty; // RC-YYYYMMDD-001

        [JsonPropertyName("boletas")]
        public List<ComprobanteSunatModel> boletas { get; set; } = new List<ComprobanteSunatModel>();

        [JsonPropertyName("totalGravadas")]
        public decimal totalGravadas { get; set; } = 0;

        [JsonPropertyName("totalIgv")]
        public decimal totalIgv { get; set; } = 0;

        [JsonPropertyName("totalVentas")]
        public decimal totalVentas { get; set; } = 0;

        [JsonPropertyName("cantidadComprobantes")]
        public int cantidadComprobantes { get; set; } = 0;
    }
}
