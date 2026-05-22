using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ElitePOS.Services
{
    public class NumeracionService : INumeracionService
    {
        private readonly HttpClient _httpClient;
        private const string PROJECT_ID = "TU_FIREBASE_PROJECT_ID";
        private const string FIREBASE_URL = $"https://firestore.googleapis.com/v1/projects/{PROJECT_ID}/databases/(default)/documents/contadores";

        public NumeracionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<NumeracionModel> ObtenerSiguienteNumero(string tipo, string empresaId)
        {
            var serie = GetSerieDefault(tipo);
            var numStr = await ObtenerSiguienteCorrelativo(tipo, serie);
            var parts = numStr.Split('-');
            return new NumeracionModel
            {
                serie = parts[0],
                correlativo = int.Parse(parts[1])
            };
        }

        public async Task<string> ObtenerSiguienteNumeroBoleta(string empresaId) => await ObtenerSiguienteCorrelativo("Boleta", "B001");
        public async Task<string> ObtenerSiguienteNumeroFactura(string empresaId) => await ObtenerSiguienteCorrelativo("Factura", "F001");
        public async Task<string> ObtenerSiguienteNumeroProforma(string empresaId) => await ObtenerSiguienteCorrelativo("Proforma", "P001");
        public async Task<string> ObtenerSiguienteNumeroTicket(string empresaId) => await ObtenerSiguienteCorrelativo("Ticket", "T001");
        public async Task<string> ObtenerSiguienteNumeroNotaVenta(string empresaId) => await ObtenerSiguienteCorrelativo("NotaVenta", "NV01");

        public async Task ActualizarContador(string empresaId, string tipoDocumento, int nuevoValor)
        {
            var docId = tipoDocumento.ToLower().Replace(" ", "");
            var url = $"{FIREBASE_URL}/{docId}?updateMask.fieldPaths=valor";
            var body = new { fields = new { valor = new { integerValue = nuevoValor.ToString() } } };
            await _httpClient.PatchAsJsonAsync(url, body);
        }

        private async Task<string> ObtenerSiguienteCorrelativo(string tipo, string serieDefault)
        {
            try
            {
                var docId = tipo.ToLower().Replace(" ", "");
                var response = await _httpClient.GetAsync($"{FIREBASE_URL}/{docId}");
                int ultimo = 0;
                if (response.IsSuccessStatusCode)
                {
                    var doc = await response.Content.ReadFromJsonAsync<FirestoreDocument>();
                    if (doc?.Fields != null && doc.Fields.TryGetValue("valor", out var val))
                        int.TryParse(val.IntegerValue, out ultimo);
                }
                return $"{serieDefault}-{(ultimo + 1):D8}";
            }
            catch { return $"{serieDefault}-00000000"; }
        }

        private string GetSerieDefault(string tipo) => tipo switch
        {
            "Factura" => "F001",
            "Boleta" => "B001",
            "Proforma" => "P001",
            "Ticket" => "T001",
            "Nota de Venta" => "NV01",
            _ => "B001"
        };

        private class FirestoreDocument { [JsonPropertyName("fields")] public Dictionary<string, FirestoreValue> Fields { get; set; } = new(); }
        private class FirestoreValue { [JsonPropertyName("integerValue")] public string? IntegerValue { get; set; } }
    }
}
