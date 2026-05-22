using ElitePOS.Shared.Models;
using System.Threading.Tasks;

namespace ElitePOS.Services
{
    public interface INumeracionService
    {
        Task<NumeracionModel> ObtenerSiguienteNumero(string tipo, string empresaId);
        Task<string> ObtenerSiguienteNumeroBoleta(string empresaId);
        Task<string> ObtenerSiguienteNumeroFactura(string empresaId);
        Task<string> ObtenerSiguienteNumeroProforma(string empresaId);
        Task<string> ObtenerSiguienteNumeroTicket(string empresaId);
        Task<string> ObtenerSiguienteNumeroNotaVenta(string empresaId);
        Task ActualizarContador(string empresaId, string tipoDocumento, int nuevoValor);
    }
}
