using ElitePOS.Services;
using ElitePOS.Shared.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace ElitePOS.Client.Services
{
    public class BackupService : IBackupService
    {
        private readonly IInventarioService _inventarioService;
        private readonly IVentasService _ventasService;
        private readonly IClientesService _clientesService;
        private readonly IComprasService _comprasService;
        private readonly IProformasService _proformasService;
        private readonly IConfiguracionEmpresaService _configuracionService;
        private readonly IJSRuntime _jsRuntime;

        public BackupService(
            IInventarioService inventarioService,
            IVentasService ventasService,
            IClientesService clientesService,
            IComprasService comprasService,
            IProformasService proformasService,
            IConfiguracionEmpresaService configuracionService,
            IJSRuntime jsRuntime)
        {
            _inventarioService = inventarioService;
            _ventasService = ventasService;
            _clientesService = clientesService;
            _comprasService = comprasService;
            _proformasService = proformasService;
            _configuracionService = configuracionService;
            _jsRuntime = jsRuntime;
        }

        public async Task<string> GenerarBackupCompleto(string empresaId)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("    💾 GENERANDO BACKUP COMPLETO DE LA EMPRESA");
            Console.WriteLine("═══════════════════════════════════════════════════════");

            var backup = new
            {
                FechaGeneracion = DateTime.Now,
                EmpresaId = empresaId,
                Version = "1.0",
                
                // Configuración de la empresa
                Configuracion = await _configuracionService.ObtenerConfiguracion(),
                
                // Inventario
                Productos = await _inventarioService.ObtenerProductos(),
                
                // Ventas
                Ventas = await _ventasService.ObtenerVentas(),
                
                // Clientes
                Clientes = await _clientesService.ObtenerClientes(),
                
                // Compras
                Compras = await _comprasService.ObtenerCompras(),
                
                // Proformas
                Proformas = await _proformasService.ObtenerProformas(),
                
                // Estadísticas
                Estadisticas = new
                {
                    TotalProductos = (await _inventarioService.ObtenerProductos()).Count(),
                    TotalVentas = (await _ventasService.ObtenerVentas()).Count(),
                    TotalClientes = (await _clientesService.ObtenerClientes()).Count(),
                    TotalCompras = (await _comprasService.ObtenerCompras()).Count()
                }
            };

            var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            Console.WriteLine($"✅ Backup generado: {json.Length} caracteres");
            Console.WriteLine($"   Productos: {backup.Estadisticas.TotalProductos}");
            Console.WriteLine($"   Ventas: {backup.Estadisticas.TotalVentas}");
            Console.WriteLine($"   Clientes: {backup.Estadisticas.TotalClientes}");
            Console.WriteLine($"   Compras: {backup.Estadisticas.TotalCompras}");
            Console.WriteLine("═══════════════════════════════════════════════════════");

            return json;
        }

        public async Task DescargarBackup(string empresaId, string nombreEmpresa)
        {
            try
            {
                var json = await GenerarBackupCompleto(empresaId);
                
                var nombreArchivo = $"ElitePOS_Backup_{nombreEmpresa.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}";

                await _jsRuntime.InvokeVoidAsync("descargarBackup", json, nombreArchivo);

                Console.WriteLine($"📥 Backup descargado: {nombreArchivo}.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al descargar backup: {ex.Message}");
                throw;
            }
        }
    }
}


