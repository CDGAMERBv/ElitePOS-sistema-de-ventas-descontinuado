using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IGastosService
    {
        Task<IEnumerable<GastoModel>> ObtenerGastos();
        Task<GastoModel?> ObtenerGastoPorId(string id);
        Task<bool> RegistrarGasto(GastoModel gasto);
        Task<bool> ActualizarGasto(GastoModel gasto);
        Task<bool> AnularGasto(string gastoId);
    }
}


