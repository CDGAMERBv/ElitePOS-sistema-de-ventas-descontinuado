using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public class ConfiguracionEmpresaService : BaseFirestoreService<ConfiguracionEmpresaModel>, IConfiguracionEmpresaService
    {
        private readonly ISesionService _sesionService;
        private ConfiguracionEmpresaModel? _configuracion;

        protected override string CollectionName => "Empresas";

        public ConfiguracionEmpresaModel? Configuracion => _configuracion;

        public event Action? OnChange;

        public ConfiguracionEmpresaService(HttpClient http, ISesionService sesionService) : base(http)
        {
            _sesionService = sesionService;
        }

        public async Task<ConfiguracionEmpresaModel?> ObtenerConfiguracion(bool forceRefresh = false)
        {
            try
            {
                if (_configuracion != null && !forceRefresh)
                    return _configuracion;

                string empresaId = _sesionService.UsuarioActual?.empresaId ?? "empresa-demo";
                
                _configuracion = await GetDocumentAsync(empresaId);

                if (_configuracion == null)
                {
                    _configuracion = new ConfiguracionEmpresaModel { empresaId = empresaId };
                }

                OnChange?.Invoke();
                return _configuracion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Error al obtener configuraci?n: {ex.Message}");
                return null;
            }
        }

        public async Task<ConfiguracionEmpresaModel?> ObtenerConfiguracionPorEmpresa(string empresaId)
        {
            return await GetDocumentAsync(empresaId);
        }

        public async Task<bool> GuardarConfiguracion(ConfiguracionEmpresaModel configuracion)
        {
            try
            {
                string empresaId = configuracion.empresaId;
                if (string.IsNullOrEmpty(empresaId))
                    empresaId = _sesionService.UsuarioActual?.empresaId ?? "empresa-demo";

                bool exito = await SaveDocumentAsync(empresaId, configuracion, isUpdate: true);

                if (exito)
                {
                    _configuracion = configuracion;
                    OnChange?.Invoke();
                }

                return exito;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Error al guardar configuraci?n: {ex.Message}");
                return false;
            }
        }

        public async Task<string> ObtenerSimboloMoneda()
        {
            var config = await ObtenerConfiguracion();
            return config?.simboloMoneda ?? "S/";
        }
    }
}


