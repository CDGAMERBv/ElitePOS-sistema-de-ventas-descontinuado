using ElitePOS.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Timers;

namespace ElitePOS.Services
{
    public class ActualizacionTiempoRealService : IActualizacionTiempoRealService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private System.Timers.Timer? _timer;
        private ConfiguracionEmpresaModel? _configuracionAnterior;
        private bool _monitoreoActivo = false;

        public event Action? OnConfiguracionCambiada;

        public ActualizacionTiempoRealService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task IniciarMonitoreo()
        {
            if (_monitoreoActivo)
                return;

            _monitoreoActivo = true;

            // Obtener configuración inicial usando un scope
            using (var scope = _serviceProvider.CreateScope())
            {
                var configuracionService = scope.ServiceProvider.GetRequiredService<IConfiguracionEmpresaService>();
                _configuracionAnterior = await configuracionService.ObtenerConfiguracion();
            }

            // Polling cada 30 segundos para detectar cambios
            _timer = new System.Timers.Timer(30000); // 30 segundos
            _timer.Elapsed += async (sender, e) => await VerificarCambios();
            _timer.AutoReset = true;
            _timer.Start();

            Console.WriteLine("🔄 Monitoreo de configuración en tiempo real iniciado (polling cada 30s)");
        }

        public void DetenerMonitoreo()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            _monitoreoActivo = false;
            Console.WriteLine("⏹️ Monitoreo de configuración detenido");
        }

        private async Task VerificarCambios()
        {
            try
            {
                ConfiguracionEmpresaModel? configuracionActual;

                // Crear un nuevo scope para cada verificación
                using (var scope = _serviceProvider.CreateScope())
                {
                    var configuracionService = scope.ServiceProvider.GetRequiredService<IConfiguracionEmpresaService>();
                    configuracionActual = await configuracionService.ObtenerConfiguracion();
                }

                if (_configuracionAnterior == null)
                {
                    _configuracionAnterior = configuracionActual;
                    return;
                }

                // Detectar cambios en campos importantes
                bool hubioCambio = 
                    configuracionActual?.nombreComercial != _configuracionAnterior?.nombreComercial ||
                    configuracionActual?.razonSocial != _configuracionAnterior?.razonSocial ||
                    configuracionActual?.moneda != _configuracionAnterior?.moneda ||
                    configuracionActual?.simboloMoneda != _configuracionAnterior?.simboloMoneda ||
                    configuracionActual?.logoUrl != _configuracionAnterior?.logoUrl;

                if (hubioCambio)
                {
                    Console.WriteLine("🔔 Configuración actualizada detectada - notificando componentes");
                    _configuracionAnterior = configuracionActual;
                    OnConfiguracionCambiada?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al verificar cambios en configuración: {ex.Message}");
            }
        }

        public async Task<ConfiguracionEmpresaModel?> ObtenerConfiguracionActual()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var configuracionService = scope.ServiceProvider.GetRequiredService<IConfiguracionEmpresaService>();
                return await configuracionService.ObtenerConfiguracion();
            }
        }

        public void NotificarCambioInmediato()
        {
            Console.WriteLine("⚡ Notificación inmediata de cambio forzada - actualizando componentes sin esperar polling");
            OnConfiguracionCambiada?.Invoke();
        }

        public void Dispose()
        {
            DetenerMonitoreo();
        }
    }
}


