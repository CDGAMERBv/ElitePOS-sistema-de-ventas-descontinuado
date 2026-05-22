using ElitePOS.Shared.Models;

namespace ElitePOS.Services;

public interface IInventarioService
{
    Task<IEnumerable<ProductoModel>> ObtenerProductos();
    Task<ProductoModel?> ObtenerProductoPorId(string id);
    Task<ProductoModel?> BuscarProductoPorCodigo(string codigoBarras);
    Task<bool> AgregarProducto(ProductoModel producto);
    Task<bool> EditarProducto(ProductoModel producto);
    Task<bool> ActualizarProducto(ProductoModel producto);
    Task<bool> EliminarProducto(string id);
    Task<bool> ActualizarStock(string productoId, int nuevaCantidad);
    
    // 🆕 MÉTODOS PARA COSTEO PROFESIONAL - PROMEDIO PONDERADO
    /// <summary>
    /// Calcula y actualiza el costo promedio ponderado de un producto
    /// </summary>
    /// <param name="productoId">ID del producto a actualizar</param>
    /// <param name="cantidadEntrante">Cantidad de la nueva entrada</param>
    /// <param name="costoEntrante">Costo unitario de la nueva entrada</param>
    /// <returns>Producto actualizado con nuevo costo promedio</returns>
    Task<ProductoModel?> CalcularYActualizarCostoPromedio(string productoId, int cantidadEntrante, decimal costoEntrante);
    
    /// <summary>
    /// Obtiene el costo promedio actual de un producto
    /// </summary>
    /// <param name="productoId">ID del producto</param>
    /// <returns>Costo promedio actual</returns>
    Task<decimal> ObtenerCostoPromedio(string productoId);
    Task<IEnumerable<ProductoModel>> ObtenerProductosConStockBajo(int limite);
}


