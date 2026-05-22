using ElitePOS.Shared.Models;
using System.Threading.Tasks;

namespace ElitePOS.Services
{
    public interface IPdfImpresionService
    {
        Task<byte[]> GenerarFacturaA4(VentaModel venta, ConfiguracionEmpresaModel config);
        Task<byte[]> GenerarFacturaA5(VentaModel venta, ConfiguracionEmpresaModel config);
        Task<byte[]> GenerarTicket(VentaModel venta, ConfiguracionEmpresaModel config);
    }
}


