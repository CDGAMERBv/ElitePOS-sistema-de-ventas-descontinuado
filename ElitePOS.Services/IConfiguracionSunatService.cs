using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IConfiguracionSunatService
    {
        Task<ConfiguracionSunatModel> ObtenerConfiguracion();
        Task<bool> GuardarConfiguracion(ConfiguracionSunatModel config);
    }
}


