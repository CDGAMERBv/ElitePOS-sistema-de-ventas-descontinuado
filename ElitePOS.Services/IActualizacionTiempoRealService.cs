using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IActualizacionTiempoRealService
    {
        event Action? OnConfiguracionCambiada;
        Task IniciarMonitoreo();
        void DetenerMonitoreo();
        Task<ConfiguracionEmpresaModel?> ObtenerConfiguracionActual();
        void NotificarCambioInmediato();
    }
}


