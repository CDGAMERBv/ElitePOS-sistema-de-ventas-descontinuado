using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IComprobanteSunatService
    {
        /// <summary>
        /// Genera el JSON del comprobante según estándar UBL 2.1 de SUNAT
        /// ✅ Incluye cálculo de IGV, ICBPER y código QR
        /// </summary>
        Task<ComprobanteSunatModel> GenerarComprobanteSunat(VentaModel venta, ClienteModel cliente);

        /// <summary>
        /// Convierte número a letras (para campo ImporteEnLetras)
        /// </summary>
        string ConvertirNumeroALetras(decimal monto);

        /// <summary>
        /// Calcula IGV (18%) y totales según SUNAT
        /// </summary>
        (decimal baseImponible, decimal igv, decimal total) CalcularTotalesSunat(decimal montoTotal);

        /// <summary>
        /// 🆕 Genera código QR según formato SUNAT
        /// Formato: RUC|TipoDoc|Serie|Numero|IGV|Total|Fecha|TipoDocCliente|NumDocCliente|
        /// </summary>
        string GenerarCodigoQR(ComprobanteSunatModel comprobante);

        /// <summary>
        /// 🆕 Genera resumen diario de boletas (RC - Resumen de Comprobantes)
        /// Requerido por SUNAT para envío al siguiente día hábil
        /// </summary>
        Task<ResumenDiarioModel> GenerarResumenDiario(DateTime fecha, List<VentaModel> ventas);

        Task<List<VentaModel>> ObtenerEstadoEnvios(DateTime? fecha = null) => Task.FromResult(new List<VentaModel>());
    }
}



