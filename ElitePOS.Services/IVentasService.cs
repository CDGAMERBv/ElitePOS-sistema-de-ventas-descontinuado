using ElitePOS.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace ElitePOS.Services
{
    public interface IVentasService
    {
        Task<IEnumerable<VentaModel>> ObtenerVentas();
        Task<VentaModel?> ObtenerVentaPorId(string id);
        Task<bool> RegistrarVenta(VentaModel venta, bool permitirVentaSinStock = false);
        Task<VentaModel?> GuardarVenta(VentaModel venta) => Task.FromResult<VentaModel?>(null); // Default to avoid breaking implementation
        Task<bool> ActualizarVenta(VentaModel venta);
        Task<bool> AnularVenta(string ventaId);
        Task<IEnumerable<VentaModel>> ObtenerVentasPorRango(DateTime? fechaInicio, DateTime? fechaFin);
        Task<List<UtilidadResumenDto>> ObtenerResumenUtilidadesAsync(DateTime? fechaInicio, DateTime? fechaFin, bool incluirNotas = true);
        Task<bool> ReintentarEnviosSunatPendientes();
        Task<Dictionary<string, int>> ObtenerTopProductosVendidos(int top, DateTime fechaInicio, DateTime fechaFin);
        Task<VentaModel?> ObtenerUltimaVenta();
        Task<VentaModel?> ObtenerPrimeraVenta();
        Task<decimal> ObtenerVentasHoy();
        Task<decimal> ObtenerVentasUltimaSemana();
        Task<int> ObtenerTotalTransaccionesHoy();
        Task<IEnumerable<VentaModel>> ObtenerVentasRecientes(int top);
        Task<IEnumerable<ProductoTopModel>> ObtenerProductosMasVendidos(int top);
        Task<IEnumerable<VentaMensualModel>> ObtenerVentasMensuales();
        Task<IEnumerable<VentaModel>> ObtenerVentasDetalladas(DateTime? inicio, DateTime? fin);
    }
}
