using ElitePOS.Shared.Models;

namespace ElitePOS.Services;

public class PlanLimitesService : IPlanLimitesService
{
    private readonly IConfiguracionEmpresaService _configuracionService;
    private readonly IInventarioService _inventarioService;
    private readonly IUsuariosService _usuariosService;
    private readonly IVentasService _ventasService;
    private readonly IConfiguracionAlmacenesService _almacenesService;

    public PlanLimitesService(
        IConfiguracionEmpresaService configuracionService,
        IInventarioService inventarioService,
        IUsuariosService usuariosService,
        IVentasService ventasService,
        IConfiguracionAlmacenesService almacenesService)
    {
        _configuracionService = configuracionService;
        _inventarioService = inventarioService;
        _usuariosService = usuariosService;
        _ventasService = ventasService;
        _almacenesService = almacenesService;
    }

    public async Task<LimitesPlanModel> ObtenerLimitesPlan()
    {
        var config = await _configuracionService.ObtenerConfiguracion();
        var planActual = config?.planActual?.ToLower() ?? "bronce";

        return planActual switch
        {
            "bronce" or "basico" => new LimitesPlanModel
            {
                nombrePlan = "Bronce",
                maxUsuarios = 1,
                maxAlmacenes = 1,
                maxProductos = 500,
                maxVentasMes = 100,
                accesoReportes = true,
                accesoModoOffline = false,
                accesoMultiEmpresa = false,
                accesoApi = false
            },
            "pro" or "profesional" => new LimitesPlanModel
            {
                nombrePlan = "Pro",
                maxUsuarios = 3,
                maxAlmacenes = 3,
                maxProductos = int.MaxValue,
                maxVentasMes = int.MaxValue,
                accesoReportes = true,
                accesoModoOffline = true,
                accesoMultiEmpresa = false,
                accesoApi = false
            },
            "infinity" or "premium" => new LimitesPlanModel
            {
                nombrePlan = "Infinity",
                maxUsuarios = int.MaxValue,
                maxAlmacenes = int.MaxValue,
                maxProductos = int.MaxValue,
                maxVentasMes = int.MaxValue,
                accesoReportes = true,
                accesoModoOffline = true,
                accesoMultiEmpresa = true,
                accesoApi = true
            },
            _ => new LimitesPlanModel
            {
                nombrePlan = "Bronce",
                maxUsuarios = 1,
                maxAlmacenes = 1,
                maxProductos = 500,
                maxVentasMes = 100,
                accesoReportes = true,
                accesoModoOffline = false,
                accesoMultiEmpresa = false,
                accesoApi = false
            }
        };
    }

    public async Task<(bool Permitido, string Mensaje)> PuedeAgregarProducto()
    {
        try
        {
            var limites = await ObtenerLimitesPlan();
            var productos = await _inventarioService.ObtenerProductos();
            int cantidadActual = productos?.Count() ?? 0;

            if (cantidadActual >= limites.maxProductos)
            {
                return (false, $"Has alcanzado el límite de {limites.maxProductos} productos del plan {limites.nombrePlan}. Actualiza a un plan superior para agregar más productos.");
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar límite de productos: {ex.Message}");
            return (true, string.Empty);
        }
    }

    public async Task<(bool Permitido, string Mensaje)> PuedeAgregarUsuario()
    {
        try
        {
            var limites = await ObtenerLimitesPlan();
            var usuarios = await _usuariosService.ObtenerUsuarios();
            int cantidadActual = usuarios?.Count ?? 0;

            if (cantidadActual >= limites.maxUsuarios)
            {
                return (false, $"Has alcanzado el límite de {limites.maxUsuarios} usuario(s) del plan {limites.nombrePlan}. Actualiza a Pro o Infinity para agregar más usuarios.");
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar límite de usuarios: {ex.Message}");
            return (true, string.Empty);
        }
    }

    public async Task<(bool Permitido, string Mensaje)> PuedeRegistrarVenta()
    {
        try
        {
            var limites = await ObtenerLimitesPlan();
            
            if (limites.maxVentasMes == int.MaxValue)
            {
                return (true, string.Empty);
            }

            var ventas = await _ventasService.ObtenerVentas();
            if (ventas == null)
            {
                return (true, string.Empty);
            }

            var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            int ventasEsteMes = ventas.Count(v => v.fechaHora >= inicioMes);

            if (ventasEsteMes >= limites.maxVentasMes)
            {
                return (false, $"Has alcanzado el límite de {limites.maxVentasMes} ventas mensuales del plan {limites.nombrePlan}. Actualiza a Pro o Infinity para ventas ilimitadas.");
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar límite de ventas: {ex.Message}");
            return (true, string.Empty);
        }
    }

    public async Task<(bool Permitido, string Mensaje)> PuedeAgregarAlmacen()
    {
        try
        {
            var limites = await ObtenerLimitesPlan();
            var config = await _almacenesService.ObtenerConfiguracion();
            var cantidadActual = config?.almacenes?.Count ?? 0;

            if (cantidadActual >= limites.maxAlmacenes)
            {
                return (false, $"Has alcanzado el límite de {limites.maxAlmacenes} almacén(es) del plan {limites.nombrePlan}. Actualiza a Pro o Infinity para más almacenes.");
            }

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar límite de almacenes: {ex.Message}");
            return (true, string.Empty);
        }
    }

    public async Task<(bool Vencido, DateTime? FechaVencimiento)> VerificarVencimiento()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracion();
            
            if (config == null)
            {
                return (false, null);
            }

            var vencido = DateTime.Now > config.fechaVencimiento;
            return (vencido, config.fechaVencimiento);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar vencimiento: {ex.Message}");
            return (false, null);
        }
    }

    public async Task<bool> EstaEnPrueba()
    {
        try
        {
            var config = await _configuracionService.ObtenerConfiguracion();
            return config?.esPrueba ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar período de prueba: {ex.Message}");
            return false;
        }
    }
}
