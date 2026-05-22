using ElitePOS.Shared.Models;
using System.Text;
using System.Globalization;
using ExcelDataReader;
using System.Data;

namespace ElitePOS.Services
{
    public class ImportacionExcelService : IImportacionExcelService
    {
        private readonly IInventarioService _inventarioService;

        public ImportacionExcelService(IInventarioService inventarioService)
        {
            _inventarioService = inventarioService;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public async Task<List<ProductoImportacionDTO>> ProcesarArchivoParaPrevisualizacion(Stream stream, string extension)
        {
            var productos = new List<ProductoImportacionDTO>();
            try
            {
                using var reader = extension.ToLower() == ".csv" 
                    ? ExcelReaderFactory.CreateCsvReader(stream) 
                    : ExcelReaderFactory.CreateReader(stream);

                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                });

                if (result.Tables.Count > 0)
                {
                    var table = result.Tables[0];
                    foreach (DataRow row in table.Rows)
                    {
                        var dto = new ProductoImportacionDTO();
                        dto.codigoBarras = GetValue(row, "Codigo", "CodigoBarras", "Cod");
                        dto.nombre = GetValue(row, "Producto", "Nombre", "Descripcion");
                        
                        var precioC = GetValue(row, "Precio_Compra", "PrecioCompra", "Costo");
                        if (decimal.TryParse(precioC.Replace("$", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pc))
                            dto.precioCompra = pc;

                        var precioV = GetValue(row, "Precio_Venta", "PrecioVenta", "Precio");
                        if (decimal.TryParse(precioV.Replace("$", "").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pv))
                            dto.precioVenta = pv;

                        var stockVal = GetValue(row, "Stock_Actual", "Stock", "Cantidad");
                        if (int.TryParse(stockVal, out int s))
                            dto.stock = s;

                        dto.categoria = GetValue(row, "Categoria", "Grupo");
                        if (string.IsNullOrEmpty(dto.categoria)) dto.categoria = "General";

                        if (string.IsNullOrWhiteSpace(dto.nombre))
                        {
                            dto.mensajeError = "El nombre es obligatorio.";
                            dto.seleccionado = false;
                        }
                        else if (dto.precioVenta <= 0)
                        {
                            dto.mensajeError = "El precio de venta debe ser mayor a 0.";
                            dto.seleccionado = false;
                        }
                        productos.Add(dto);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando archivo: {ex.Message}");
            }
            return productos;
        }

        private string GetValue(DataRow row, params string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                if (row.Table.Columns.Contains(name))
                    return row[name]?.ToString()?.Trim() ?? "";
            }
            return "";
        }

        public async Task<(bool Exito, string Mensaje, int ProductosImportados)> ImportarProductosDesdeExcel(Stream archivoExcel, string empresaId)
        {
            try
            {
                var dtos = await ProcesarArchivoParaPrevisualizacion(archivoExcel, ".csv");
                int importados = 0;
                foreach (var dto in dtos.Where(d => d.seleccionado))
                {
                    dto.empresaId = empresaId;
                    var existente = await _inventarioService.BuscarProductoPorCodigo(dto.codigoBarras);
                    if (existente != null)
                    {
                        existente.nombre = dto.nombre;
                        existente.precioCompra = dto.precioCompra;
                        existente.precioVenta = dto.precioVenta;
                        existente.stock += dto.stock;
                        await _inventarioService.EditarProducto(existente);
                    }
                    else
                    {
                        await _inventarioService.AgregarProducto(dto);
                    }
                    importados++;
                }
                return (true, $"Importación exitosa: {importados} productos.", importados);
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}", 0);
            }
        }
    }
}
