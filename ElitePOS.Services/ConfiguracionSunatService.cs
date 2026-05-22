using ElitePOS.Shared.Models;
using System.Net.Http.Json;

namespace ElitePOS.Services
{
    public class ConfiguracionSunatService : IConfiguracionSunatService
    {
        private readonly HttpClient _http;
        private const string FIREBASE_URL = "https://firestore.googleapis.com/v1/projects/TU_FIREBASE_PROJECT_ID/databases/(default)/documents/configuracion_sunat";
        private const string DOCUMENTO_ID = "config-sunat";

        public ConfiguracionSunatService(HttpClient http)
        {
            _http = http;
        }

        public async Task<ConfiguracionSunatModel> ObtenerConfiguracion()
        {
            try
            {
                var response = await _http.GetFromJsonAsync<FirebaseDocumentResponse>($"{FIREBASE_URL}/{DOCUMENTO_ID}");
                
                if (response?.Fields != null)
                {
                    var f = response.Fields;
                    return new ConfiguracionSunatModel
                    {
                        id = DOCUMENTO_ID,
                        modo = f.modo?.StringValue ?? (f.Modo?.StringValue ?? "BETA"),
                        ruc = f.ruc?.StringValue ?? (f.Ruc?.StringValue ?? ""),
                        apiUrl = f.apiUrl?.StringValue ?? (f.ApiUrl?.StringValue ?? "https://api.pse.pe/v1"),
                        apiToken = f.apiToken?.StringValue ?? (f.ApiToken?.StringValue ?? ""),
                        serieBoleta = f.serieBoleta?.StringValue ?? (f.SerieBoleta?.StringValue ?? "B001"),
                        serieFactura = f.serieFactura?.StringValue ?? (f.SerieFactura?.StringValue ?? "F001")
                    };
                }

                return new ConfiguracionSunatModel { id = DOCUMENTO_ID };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener configuración SUNAT: {ex.Message}");
                return new ConfiguracionSunatModel { id = DOCUMENTO_ID };
            }
        }

        public async Task<bool> GuardarConfiguracion(ConfiguracionSunatModel config)
        {
            try
            {
                var firebaseData = new
                {
                    fields = new
                    {
                        modo = new { stringValue = config.modo },
                        ruc = new { stringValue = config.ruc },
                        apiUrl = new { stringValue = config.apiUrl },
                        apiToken = new { stringValue = config.apiToken },
                        serieBoleta = new { stringValue = config.serieBoleta },
                        serieFactura = new { stringValue = config.serieFactura }
                    }
                };

                var response = await _http.PatchAsJsonAsync($"{FIREBASE_URL}/{DOCUMENTO_ID}", firebaseData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al guardar configuración: {ex.Message}");
                return false;
            }
        }

        private class FirebaseDocumentResponse { public ConfigFields Fields { get; set; } = new(); }
        private class ConfigFields {
            public FirebaseStringValue? modo { get; set; }
            public FirebaseStringValue? Modo { get; set; }
            public FirebaseStringValue? ruc { get; set; }
            public FirebaseStringValue? Ruc { get; set; }
            public FirebaseStringValue? apiUrl { get; set; }
            public FirebaseStringValue? ApiUrl { get; set; }
            public FirebaseStringValue? apiToken { get; set; }
            public FirebaseStringValue? ApiToken { get; set; }
            public FirebaseStringValue? serieBoleta { get; set; }
            public FirebaseStringValue? SerieBoleta { get; set; }
            public FirebaseStringValue? serieFactura { get; set; }
            public FirebaseStringValue? SerieFactura { get; set; }
        }
        private class FirebaseStringValue { public string StringValue { get; set; } = ""; }
    }
}
