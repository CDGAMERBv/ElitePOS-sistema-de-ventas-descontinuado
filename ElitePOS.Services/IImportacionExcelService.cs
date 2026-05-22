using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IImportacionExcelService
    {
        Task<(bool Exito, string Mensaje, int ProductosImportados)> ImportarProductosDesdeExcel(Stream archivoExcel, string empresaId);
        Task<List<ProductoImportacionDTO>> ProcesarArchivoParaPrevisualizacion(Stream stream, string extension);
    }
}


