using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IConfiguracionImpresionService
    {
        Task<ConfiguracionImpresionModel> ObtenerConfiguracion();
        Task<bool> GuardarConfiguracion(ConfiguracionImpresionModel config);
    }
}


