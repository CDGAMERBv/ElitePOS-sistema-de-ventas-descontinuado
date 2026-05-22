using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IProformasService
    {
        Task<IEnumerable<VentaModel>> ObtenerProformas();
        Task<IEnumerable<VentaModel>> ObtenerProformasPorRango(DateTime? fechaInicio, DateTime? fechaFin);
        Task<VentaModel?> ObtenerProformaPorId(string id);
        Task<bool> RegistrarProforma(VentaModel proforma);
        Task<bool> EliminarProforma(string id);
        Task<bool> ConvertirAVenta(string proformaId);
    }
}


