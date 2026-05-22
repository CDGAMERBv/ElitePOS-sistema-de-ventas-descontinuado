using ElitePOS.Shared.Models;
using ElitePOS.Services;

namespace ElitePOS.Client.Services
{
    public class DashboardStateService : IDashboardStateService
    {
        private readonly IVentasService _ventasService;
        private readonly IInventarioService _inventarioService;
        private readonly IClientesService _clientesService;
        private readonly IConfiguracionAlmacenesService _almacenesService;

        public decimal VentasHoy { get; private set; }
        public decimal VentasSemana { get; private set; }
        public int TransaccionesHoy { get; private set; }
        public int StockBajoCount { get; private set; }
        public int TotalClientes { get; private set; }
        public List<VentaModel> VentasRecientes { get; private set; } = new();
        public List<ProductoTopModel> ProductosMasVendidos { get; private set; } = new();
        public List<ProductoModel> ProductosBajoStock { get; private set; } = new();
        public List<VentaMensualModel> VentasMensuales { get; private set; } = new();

        public bool DatosCargados { get; private set; } = false;

        public event Action? OnChange;

        public DashboardStateService(
            IVentasService ventasService,
            IInventarioService inventarioService,
            IClientesService clientesService,
            IConfiguracionAlmacenesService almacenesService)
        {
            _ventasService = ventasService;
            _inventarioService = inventarioService;
            _clientesService = clientesService;
            _almacenesService = almacenesService;
        }

        public void MarcarComoDesactualizado()
        {
            DatosCargados = false;
        }

        public async Task CargarDatosAsync(bool forzar = false)
        {
            if (DatosCargados && !forzar) return;

            try
            {
                // 🛡️ OPTIMIZACIÓN NUCLEAR: Solo cargar ventas del año actual (o los últimos 30 días si está iniciando el año)
                var hoy = DateTime.Today;
                var inicioAnio = new DateTime(hoy.Year, 1, 1);
                var fechaFiltro = (hoy - inicioAnio).TotalDays < 30 ? hoy.AddDays(-30) : inicioAnio;
                var ventasRef = (await _ventasService.ObtenerVentasPorRango(fechaFiltro, null)).ToList();
                var semana = DateTime.Today.AddDays(-7);

                // 1. Calcular métricas de ventas LOCALMENTE (LINQ sobre la lista en memoria)
                VentasHoy = ventasRef.Where(v => !v.anulada && v.fechaHora.Date == hoy).Sum(v => v.total);
                VentasSemana = ventasRef.Where(v => !v.anulada && v.fechaHora.Date >= semana).Sum(v => v.total);
                TransaccionesHoy = ventasRef.Count(v => !v.anulada && v.fechaHora.Date == hoy);
                VentasRecientes = ventasRef.OrderByDescending(v => v.fechaHora).Take(10).ToList();
                
                // Productos más vendidos (Top 10)
                ProductosMasVendidos = ventasRef
                    .Where(v => !v.anulada)
                    .SelectMany(v => v.items)
                    .GroupBy(i => i.nombreProducto)
                    .Select(g => new ProductoTopModel
                    {
                        Nombre = g.Key,
                        Cantidad = g.Sum(x => x.cantidad),
                        Total = g.Sum(x => x.subtotal)
                    })
                    .OrderByDescending(x => x.cantidad)
                    .Take(10)
                    .ToList();

                // Ventas Mensuales (Año actual)
                VentasMensuales = ventasRef
                    .Where(v => !v.anulada && v.fechaHora.Year == hoy.Year)
                    .GroupBy(v => v.fechaHora.Month)
                    .Select(g => new VentaMensualModel
                    {
                        Mes = new DateTime(2000, g.Key, 1).ToString("MMM"),
                        Total = g.Sum(v => v.total)
                    })
                    .OrderBy(x => DateTime.ParseExact(x.Mes, "MMM", null).Month)
                    .ToList();

                // 2. Cargar otras colecciones independientes en paralelo
                var tasks = new Task[]
                {
                    Task.Run(async () => TotalClientes = (await _clientesService.ObtenerClientes()).Count()),
                    Task.Run(async () => StockBajoCount = (await _inventarioService.ObtenerProductosConStockBajo(100)).Count()),
                    Task.Run(async () => ProductosBajoStock = (await _inventarioService.ObtenerProductosConStockBajo(10)).ToList())
                };

                await Task.WhenAll(tasks);
                DatosCargados = true;
                OnChange?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar dashboard: {ex.Message}");
            }
        }
    }
}


