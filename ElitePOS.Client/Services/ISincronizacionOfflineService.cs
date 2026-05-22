using ElitePOS.Services;
using ElitePOS.Shared.Models;

namespace ElitePOS.Client.Services
{
    public interface ISincronizacionOfflineService
    {
        /// <summary>
        /// Guarda una venta en LocalStorage cuando no hay conexión
        /// </summary>
        Task GuardarVentaOffline(VentaModel venta);

        /// <summary>
        /// Obtiene todas las ventas pendientes de sincronización
        /// </summary>
        Task<List<VentaModel>> ObtenerVentasPendientes();

        /// <summary>
        /// Sincroniza todas las ventas pendientes con Firebase
        /// </summary>
        Task<int> SincronizarVentasPendientes();

        /// <summary>
        /// Elimina una venta del almacenamiento offline
        /// </summary>
        Task EliminarVentaOffline(string ventaId);

        /// <summary>
        /// Detecta si hay conexión a internet
        /// </summary>
        Task<bool> HayConexion();

        /// <summary>
        /// Cuenta las ventas pendientes de sincronización
        /// </summary>
        Task<int> ContarVentasPendientes();
    }
}


