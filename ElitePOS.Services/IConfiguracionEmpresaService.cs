using ElitePOS.Shared.Models;

namespace ElitePOS.Services;

public interface IConfiguracionEmpresaService
{
    ConfiguracionEmpresaModel? Configuracion { get; }
    Task<ConfiguracionEmpresaModel?> ObtenerConfiguracion(bool forceRefresh = false);
    Task<ConfiguracionEmpresaModel?> ObtenerConfiguracionPorEmpresa(string empresaId);
    Task<bool> GuardarConfiguracion(ConfiguracionEmpresaModel configuracion);
    Task<string> ObtenerSimboloMoneda();
    event Action? OnChange;
}


