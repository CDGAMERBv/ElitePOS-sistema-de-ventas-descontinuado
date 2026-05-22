using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IAbonosService
    {
        Task<bool> RegistrarAbono(AbonoModel abono);
        Task<List<AbonoModel>> ObtenerAbonosPorVenta(string ventaId);
        Task<List<AbonoModel>> ObtenerAbonosPorCliente(string clienteId);
        Task<decimal> CalcularTotalAbonos(string ventaId);
    }
}


