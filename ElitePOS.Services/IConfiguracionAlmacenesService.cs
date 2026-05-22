using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IConfiguracionAlmacenesService
    {
        Task<ConfiguracionAlmacenesModel> ObtenerConfiguracion();
        Task<bool> GuardarConfiguracion(ConfiguracionAlmacenesModel config);
    }
}


