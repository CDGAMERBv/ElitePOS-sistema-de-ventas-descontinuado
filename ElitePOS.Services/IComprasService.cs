using ElitePOS.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace ElitePOS.Services
{
    public interface IComprasService
    {
        Task<IEnumerable<CompraModel>> ObtenerCompras();
        Task<IEnumerable<CompraModel>> ObtenerCompras(DateTime? inicio, DateTime? fin);
        Task<ResultadoOperacion> RegistrarCompra(CompraModel compra);
        Task<bool> EliminarCompra(string id);
        
        Task<bool> RegistrarEntradaProducto(string productoId, int cantidad, decimal costoUnitario, 
            string proveedorId = "", string proveedorNombre = "", string numeroGuia = "", 
            DateTime? fechaVencimiento = null);
    }
}
