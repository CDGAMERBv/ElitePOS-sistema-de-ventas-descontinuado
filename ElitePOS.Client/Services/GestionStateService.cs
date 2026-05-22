using ElitePOS.Services;
using ElitePOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElitePOS.Client.Services
{
    /// <summary>
    /// Contenedor de estado global con patrón Stale-While-Revalidate (SWR).
    /// Permite navegación instantánea usando caché mientras refresca los datos en segundo plano.
    /// </summary>
    public class GestionStateService
    {
        private readonly IInventarioService _inventarioService;
        private readonly IClientesService _clientesService;
        private readonly IVentasService _ventasService;
        private readonly IComprasService _comprasService;
        private readonly IProformasService _proformasService;
        private readonly IKardexService _kardexService;

        // 🔒 BLOQUEOS DE TAREA y CONTROL DE REFRESCO (SWR)
        private Task? _pendingProductosTask;
        private Task? _pendingVentasTask;
        private DateTime _ultimoRefrescoProductos = DateTime.MinValue;
        private DateTime _ultimoRefrescoClientes = DateTime.MinValue;
        private DateTime _ultimoRefrescoVentas = DateTime.MinValue;
        private DateTime _ultimoRefrescoCompras = DateTime.MinValue;
        private DateTime _ultimoRefrescoProformas = DateTime.MinValue;

        // Cachés de datos
        public List<ProductoModel> ProductosCache { get; private set; } = new();
        public List<ClienteModel> ClientesCache { get; private set; } = new();
        public List<VentaModel> VentasCache { get; private set; } = new();
        public List<CompraModel> ComprasCache { get; private set; } = new();
        public List<VentaModel> ProformasCache { get; private set; } = new();
        
        // 📊 Kardex Cache & Filters
        public List<MovimientoKardexProfesional> kardexCache { get; private set; } = new();
        public string kardexFiltroProductoId { get; set; } = "";
        public string kardexFiltroAlmacenId { get; set; } = "";
        public DateTime kardexFechaInicio { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime kardexFechaFin { get; set; } = DateTime.Now;
        public string kardexSearchTerm { get; set; } = "";

        // 🎯 Product Specific Kardex Cache
        public List<MovimientoKardexProfesional> productoKardexCache { get; private set; } = new();
        public string? productoCargadoId { get; set; }
        public DateTime? productoKardexFechaDesde { get; set; }
        public DateTime? productoKardexFechaHasta { get; set; }

        // Evento para notificar cambios a los componentes suscritos
        public event Action? OnChange;

        // ═══════════════════════════════════════════════════
        // CACHÉ DE ESTADO DE CAJA (evita consulta a Firestore
        // en cada intento de cobro en el POS)
        // null = no se ha consultado aún
        // true = caja abierta
        // false = caja cerrada
        // ═══════════════════════════════════════════════════
        public bool? CajaAbiertaCache { get; set; } = null;

        public GestionStateService(
            IInventarioService inventarioService, 
            IClientesService clientesService, 
            IVentasService ventasService,
            IComprasService comprasService,
            IProformasService proformasService,
            IKardexService kardexService)
        {
            _inventarioService = inventarioService;
            _clientesService = clientesService;
            _ventasService = ventasService;
            _comprasService = comprasService;
            _proformasService = proformasService;
            _kardexService = kardexService;
        }

        // ═════════════════════════════════════════════════════════════════════
        // MÉTODOS DE ACCESO (PATRÓN SWR)
        // ═════════════════════════════════════════════════════════════════════

        public async Task<List<ProductoModel>> GetProductosAsync()
        {
            if (ProductosCache.Count == 0)
            {
                await RefreshProductosAsync();
            }
            else if ((DateTime.Now - _ultimoRefrescoProductos).TotalMinutes > 5)
            {
                // Refresco silencioso en segundo plano
                _ = RefreshProductosAsync();
            }
            return ProductosCache;
        }

        public async Task RefreshProductosAsync()
        {
            // Si ya hay una petición en curso, esperamos esa misma tarea
            if (_pendingProductosTask != null && !_pendingProductosTask.IsCompleted)
            {
                await _pendingProductosTask;
                return;
            }

            _pendingProductosTask = DoRefreshProductosAsync();
            await _pendingProductosTask;
        }

        private async Task DoRefreshProductosAsync()
        {
            try 
            {
                ProductosCache = (await _inventarioService.ObtenerProductos()).ToList();
                _ultimoRefrescoProductos = DateTime.Now;
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando productos: {ex.Message}");
            }
        }

        public async Task<List<ClienteModel>> GetClientesAsync()
        {
            if (ClientesCache.Count == 0)
            {
                await RefreshClientesAsync();
            }
            else if ((DateTime.Now - _ultimoRefrescoClientes).TotalMinutes > 5)
            {
                _ = RefreshClientesAsync();
            }
            return ClientesCache;
        }

        public async Task RefreshClientesAsync()
        {
            try
            {
                ClientesCache = (await _clientesService.ObtenerClientes()).ToList();
                _ultimoRefrescoClientes = DateTime.Now;
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando clientes: {ex.Message}");
            }
        }

        public async Task<List<VentaModel>> GetVentasAsync()
        {
            if (VentasCache.Count == 0)
            {
                await RefreshVentasAsync();
            }
            else if ((DateTime.Now - _ultimoRefrescoVentas).TotalMinutes > 5)
            {
                _ = RefreshVentasAsync();
            }
            return VentasCache;
        }

        public async Task RefreshVentasAsync()
        {
            // Si ya hay una petición en curso, esperamos esa misma tarea
            if (_pendingVentasTask != null && !_pendingVentasTask.IsCompleted)
            {
                await _pendingVentasTask;
                return;
            }

            _pendingVentasTask = DoRefreshVentasAsync();
            await _pendingVentasTask;
        }

        private async Task DoRefreshVentasAsync()
        {
            try
            {
                VentasCache = (await _ventasService.ObtenerVentas()).ToList();
                _ultimoRefrescoVentas = DateTime.Now;
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando ventas: {ex.Message}");
            }
        }

        public async Task<List<CompraModel>> GetComprasAsync()
        {
            if (ComprasCache.Count == 0)
            {
                await RefreshComprasAsync();
            }
            else if ((DateTime.Now - _ultimoRefrescoCompras).TotalMinutes > 5)
            {
                _ = RefreshComprasAsync();
            }
            return ComprasCache;
        }

        public async Task RefreshComprasAsync()
        {
            try
            {
                ComprasCache = (await _comprasService.ObtenerCompras()).ToList();
                _ultimoRefrescoCompras = DateTime.Now;
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando compras: {ex.Message}");
            }
        }

        public async Task<List<VentaModel>> GetProformasAsync()
        {
            if (ProformasCache.Count == 0)
            {
                await RefreshProformasAsync();
            }
            else if ((DateTime.Now - _ultimoRefrescoProformas).TotalMinutes > 5)
            {
                _ = RefreshProformasAsync();
            }
            return ProformasCache;
        }

        public async Task RefreshProformasAsync()
        {
            try
            {
                ProformasCache = (await _proformasService.ObtenerProformas()).ToList();
                _ultimoRefrescoProformas = DateTime.Now;
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando proformas: {ex.Message}");
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // KARDEX STATE MANAGEMENT
        // ═════════════════════════════════════════════════════════════════════

        public async Task RefreshKardexAsync(string productoId, string almacenId, DateTime inicio, DateTime fin)
        {
            try
            {
                List<MovimientoKardexProfesional> movimientos;

                if (!string.IsNullOrEmpty(productoId))
                {
                    movimientos = await _kardexService.ObtenerHistorialProfesional(productoId, inicio, fin);
                }
                else
                {
                    movimientos = await _kardexService.ObtenerKardexGeneral(500, inicio, fin);
                }


                // Filtrado local solo para Almacén
                kardexCache = movimientos
                    .Where(m => string.IsNullOrEmpty(almacenId) || m.almacenId == almacenId)
                    .OrderByDescending(m => m.fecha)
                    .ToList();

                // Actualizar filtros en el estado
                kardexFiltroProductoId = productoId;
                kardexFiltroAlmacenId = almacenId;
                kardexFechaInicio = inicio;
                kardexFechaFin = fin;

                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando Kardex: {ex.Message}");
            }
        }

        public async Task RefreshProductoKardexAsync(string productoId, DateTime? desde, DateTime? hasta)
        {
            try
            {
                var movimientos = await _kardexService.ObtenerHistorialProfesional(productoId, desde, hasta);
                
                productoKardexCache = movimientos
                    .OrderByDescending(m => m.fecha)
                    .ToList();

                productoCargadoId = productoId;
                productoKardexFechaDesde = desde;
                productoKardexFechaHasta = hasta;
                
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando Kardex de producto: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza el caché de estado de caja y notifica a todos los suscriptores.
        /// Llamar con true al abrir caja, false al cerrar.
        /// </summary>
        public void NotificarCambioCaja(bool estadoAbierta)
        {
            CajaAbiertaCache = estadoAbierta;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}


