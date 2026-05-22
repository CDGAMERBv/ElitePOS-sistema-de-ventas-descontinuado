using ElitePOS.Services;
using ElitePOS.Shared.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace ElitePOS.Client.Services
{
    public class SincronizacionOfflineService : ISincronizacionOfflineService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IVentasService _ventasService;
        private const string STORAGE_KEY = "elitepos_ventas_pendientes";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SincronizacionOfflineService(IJSRuntime jsRuntime, IVentasService ventasService)
        {
            _jsRuntime = jsRuntime;
            _ventasService = ventasService;
        }

        /// <summary>
        /// Detecta si hay conexión a internet
        /// </summary>
        public async Task<bool> HayConexion()
        {
            try
            {
                // ✅ CORREGIDO: usar función helper JavaScript window.isOnline()
                var resultado = await _jsRuntime.InvokeAsync<bool>("isOnline");
                return resultado;
            }
            catch
            {
                // Si falla la verificación, asumimos que SÍ hay conexión
                return true;
            }
        }

        /// <summary>
        /// Guarda una venta en LocalStorage cuando no hay conexión
        /// </summary>
        public async Task GuardarVentaOffline(VentaModel venta)
        {
            try
            {
                Console.WriteLine("💾 Guardando venta en modo OFFLINE...");
                
                // Obtener ventas pendientes actuales
                var ventasPendientes = await ObtenerVentasPendientes();

                // Agregar la nueva venta
                venta.id = $"offline_{Guid.NewGuid()}"; // ID temporal para modo offline
                ventasPendientes.Add(venta);

                // Guardar en localStorage
                var json = JsonSerializer.Serialize(ventasPendientes);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, json);

                Console.WriteLine($"✅ Venta guardada offline: {venta.numeroComprobante}");
                Console.WriteLine($"📊 Total ventas pendientes: {ventasPendientes.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al guardar venta offline: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todas las ventas pendientes de sincronización
        /// </summary>
        public async Task<List<VentaModel>> ObtenerVentasPendientes()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY);

                if (string.IsNullOrEmpty(json))
                    return new List<VentaModel>();

                var ventas = JsonSerializer.Deserialize<List<VentaModel>>(json, JsonOptions);
                return ventas ?? new List<VentaModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener ventas pendientes: {ex.Message}");
                return new List<VentaModel>();
            }
        }

        /// <summary>
        /// Cuenta las ventas pendientes de sincronización
        /// </summary>
        public async Task<int> ContarVentasPendientes()
        {
            var ventas = await ObtenerVentasPendientes();
            return ventas.Count;
        }

        /// <summary>
        /// Sincroniza todas las ventas pendientes con Firebase
        /// </summary>
        public async Task<int> SincronizarVentasPendientes()
        {
            try
            {
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("    🔄 SINCRONIZANDO VENTAS OFFLINE → FIREBASE");
                Console.WriteLine("═══════════════════════════════════════════════════════════");

                var ventasPendientes = await ObtenerVentasPendientes();

                if (!ventasPendientes.Any())
                {
                    Console.WriteLine("✅ No hay ventas pendientes de sincronización");
                    return 0;
                }

                Console.WriteLine($"📊 {ventasPendientes.Count} ventas encontradas para sincronizar");

                int exitosas = 0;
                int fallidas = 0;

                foreach (var venta in ventasPendientes)
                {
                    try
                    {
                        var idOfflineOriginal = venta.id;
                        // Generar nuevo ID para Firebase
                        venta.id = Guid.NewGuid().ToString();

                        // Intentar registrar en Firebase
                        var resultado = await _ventasService.RegistrarVenta(venta);

                        if (resultado)
                        {
                            Console.WriteLine($"   ✅ Sincronizada: {venta.numeroComprobante}");
                            exitosas++;
                            
                            // Eliminar del storage offline
                            await EliminarVentaOffline(idOfflineOriginal);
                        }
                        else
                        {
                            Console.WriteLine($"   ❌ Error al sincronizar: {venta.numeroComprobante}");
                            fallidas++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ Excepción al sincronizar {venta.numeroComprobante}: {ex.Message}");
                        fallidas++;
                    }

                    // Pequeño delay para no saturar Firebase
                    await Task.Delay(200);
                }

                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine($"📈 RESULTADO:");
                Console.WriteLine($"   ✅ Exitosas: {exitosas}");
                Console.WriteLine($"   ❌ Fallidas: {fallidas}");
                Console.WriteLine("═══════════════════════════════════════════════════════════");

                return exitosas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error crítico en sincronización: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Elimina una venta del almacenamiento offline
        /// </summary>
        public async Task EliminarVentaOffline(string ventaId)
        {
            try
            {
                var ventasPendientes = await ObtenerVentasPendientes();
                ventasPendientes.RemoveAll(v => v.id == ventaId);

                if (ventasPendientes.Any())
                {
                    var json = JsonSerializer.Serialize(ventasPendientes);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, json);
                }
                else
                {
                    // Si no quedan ventas, eliminar la clave completa
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", STORAGE_KEY);
                }

                Console.WriteLine($"🗑️ Venta offline eliminada: {ventaId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al eliminar venta offline: {ex.Message}");
            }
        }
    }
}


