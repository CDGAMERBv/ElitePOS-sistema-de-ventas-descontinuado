using ElitePOS.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace ElitePOS.Services
{
    public interface IKardexService
    {
        Task<bool> RegistrarMovimiento(MovimientoKardexModel movimiento);
        Task<List<MovimientoKardexModel>> ObtenerHistorial(string productoId);
        Task<List<MovimientoKardexProfesional>> ObtenerKardexGeneral(int limit, DateTime? desde = null, DateTime? hasta = null);
        Task<List<MovimientoKardexModel>> ObtenerKardexProducto(string productoId, DateTime? desde = null, DateTime? hasta = null);
        
        // 🆕 MÉTODOS PARA KARDEX PROFESIONAL
        Task RegistrarMovimientoProfesional(MovimientoKardexProfesional movimiento);
        Task<List<MovimientoKardexProfesional>> ObtenerHistorialProfesional(string productoId, DateTime? desde = null, DateTime? hasta = null);
        Task<bool> RegistrarEntradaCompra(string productoId, int cantidad, decimal costoUnitario, 
            string proveedorId = "", string proveedorNombre = "", string numeroLote = "", 
            DateTime? fechaVencimiento = null, string documentoReferencia = "");
    }
}
