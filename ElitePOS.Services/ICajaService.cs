using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface ICajaService
    {
        Task<bool> AbrirCaja(decimal montoApertura, string usuarioId, string nombreUsuario);
        Task<bool> CerrarCaja(decimal montoArqueo, string usuarioId, string nombreUsuario, string observaciones);
        Task<MovimientoCajaModel?> ObtenerCajaActual();
        Task<bool> HayCajaAbierta();
        Task<decimal> CalcularEfectivoEnCaja();
        Task<decimal> CalcularGastosEnCaja();
        Task<List<MovimientoCajaModel>> ObtenerHistorialCaja(DateTime? desde = null, DateTime? hasta = null);
    }
}


