using ElitePOS.Shared.Models;
using System.Net.Http.Json;

namespace ElitePOS.Services
{
    public class ConfiguracionImpresionService : IConfiguracionImpresionService
    {
        private readonly HttpClient _http;
        private const string FIREBASE_URL = "https://firestore.googleapis.com/v1/projects/TU_FIREBASE_PROJECT_ID/databases/(default)/documents/ConfiguracionImpresion";
        private const string DOCUMENTO_ID = "config-impresion";

        public ConfiguracionImpresionService(HttpClient http)
        {
            _http = http;
        }

        public async Task<ConfiguracionImpresionModel> ObtenerConfiguracion()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<FirebaseDocumentResponse>($"{FIREBASE_URL}/{DOCUMENTO_ID}");
                
                if (response?.Fields != null)
                {
                    return new ConfiguracionImpresionModel
                    {
                        Id = DOCUMENTO_ID,
                        FormatoTicket = response.Fields.FormatoTicket?.StringValue ?? "80mm",
                        Encabezado = response.Fields.Encabezado?.StringValue ?? "",
                        PieDePagina = response.Fields.PieDePagina?.StringValue ?? "",
                        ImprimirAutomaticamente = response.Fields.ImprimirAutomaticamente?.BooleanValue ?? false,
                        MostrarLogo = response.Fields.MostrarLogo?.BooleanValue ?? true
                    };
                }

                // Documento no existe aún: crear con valores por defecto (primera ejecución)
                var configPorDefecto = new ConfiguracionImpresionModel
                {
                    Id = DOCUMENTO_ID,
                    FormatoTicket = "80mm",
                    Encabezado = "",
                    PieDePagina = "",
                    ImprimirAutomaticamente = false,
                    MostrarLogo = true
                };

                await GuardarConfiguracion(configPorDefecto);
                return configPorDefecto;
            }
            catch (Exception ex)
            {
                // 🛡️ FIX CRÍTICO: Error de RED → NO sobreescribir Firestore.
                // Retornamos una config por defecto EN MEMORIA SOLAMENTE.
                // Los datos reales en Firestore permanecen intactos.
                Console.WriteLine($"⚠️ Error al leer configuración de impresión (se usará fallback local): {ex.Message}");

                return new ConfiguracionImpresionModel
                {
                    Id = DOCUMENTO_ID,
                    FormatoTicket = "80mm",
                    Encabezado = "",
                    PieDePagina = "",
                    ImprimirAutomaticamente = false,
                    MostrarLogo = true
                };
                // ⚠️ NOTA: Este fallback es temporal. El usuario verá los valores por defecto
                // pero si guarda ahora, SÍ sobreescribirá. Se recomienda en futuro
                // deshabilitar el botón "Guardar" si _cargando falló por red.
            }
        }

        public async Task<bool> GuardarConfiguracion(ConfiguracionImpresionModel config)
        {
            try
            {
                var firebaseData = new
                {
                    fields = new
                    {
                        FormatoTicket = new { stringValue = config.FormatoTicket },
                        Encabezado = new { stringValue = config.Encabezado },
                        PieDePagina = new { stringValue = config.PieDePagina },
                        ImprimirAutomaticamente = new { booleanValue = config.ImprimirAutomaticamente },
                        MostrarLogo = new { booleanValue = config.MostrarLogo }
                    }
                };

                var response = await _http.PatchAsJsonAsync($"{FIREBASE_URL}/{DOCUMENTO_ID}", firebaseData);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private class FirebaseDocumentResponse
        {
            public ConfigFields Fields { get; set; } = new();
        }

        private class ConfigFields
        {
            public FirebaseStringValue? FormatoTicket { get; set; }
            public FirebaseStringValue? Encabezado { get; set; }
            public FirebaseStringValue? PieDePagina { get; set; }
            public FirebaseBooleanValue? ImprimirAutomaticamente { get; set; }
            public FirebaseBooleanValue? MostrarLogo { get; set; }
        }

        private class FirebaseStringValue
        {
            public string StringValue { get; set; } = "";
        }

        private class FirebaseBooleanValue
        {
            public bool BooleanValue { get; set; }
        }
    }
}


