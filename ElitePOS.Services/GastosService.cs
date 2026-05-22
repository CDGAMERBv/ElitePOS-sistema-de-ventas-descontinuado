using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace ElitePOS.Services
{
    public class GastosService : IGastosService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private string FIREBASE_URL => $"https://firestore.googleapis.com/v1/projects/{_config["Firestore:ProjectId"]}/databases/(default)/documents/gastos";

        public GastosService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<IEnumerable<GastoModel>> ObtenerGastos()
        {
            try
            {
                var projectId = _config["Firestore:ProjectId"] ?? "TU_FIREBASE_PROJECT_ID";
                var url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents:runQuery";
                var empresaId = _config["EmpresaId"] ?? "empresa-demo";

                var filters = new List<object>
                {
                    new { fieldFilter = new { field = new { fieldPath = "empresaId" }, op = "EQUAL", value = new { stringValue = empresaId } } }
                };

                var structuredQuery = new
                {
                    from = new[] { new { collectionId = "gastos", allDescendants = false } },
                    where = filters[0],
                    orderBy = new[] { new { field = new { fieldPath = "fechaRegistro" }, direction = "DESCENDING" } }
                };

                var payload = new { structuredQuery };
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                if (!response.IsSuccessStatusCode) return new List<GastoModel>();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var runQueryResponses = JsonSerializer.Deserialize<List<FirestoreRunQueryResponse>>(jsonResponse, options);

                if (runQueryResponses == null) return new List<GastoModel>();

                return runQueryResponses
                    .Where(r => r.Document != null)
                    .Select(r => MapDocumentToGasto(r.Document!))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener gastos: {ex.Message}");
                return new List<GastoModel>();
            }
        }

        public async Task<GastoModel?> ObtenerGastoPorId(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{FIREBASE_URL}/{id}");
                if (!response.IsSuccessStatusCode) return null;

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var doc = JsonSerializer.Deserialize<FirestoreDocument>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return doc != null ? MapDocumentToGasto(doc) : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener gasto: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> RegistrarGasto(GastoModel gasto)
        {
            try
            {
                if (string.IsNullOrEmpty(gasto.id)) gasto.id = Guid.NewGuid().ToString();

                var firestoreDoc = new { fields = MapGastoToFields(gasto) };
                var response = await _httpClient.PatchAsJsonAsync($"{FIREBASE_URL}/{gasto.id}", firestoreDoc);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al registrar gasto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActualizarGasto(GastoModel gasto)
        {
            return await RegistrarGasto(gasto);
        }

        public async Task<bool> AnularGasto(string gastoId)
        {
            try
            {
                var updateData = new
                {
                    fields = new
                    {
                        anulado = new { booleanValue = true }
                    }
                };

                var response = await _httpClient.PatchAsJsonAsync($"{FIREBASE_URL}/{gastoId}?updateMask.fieldPaths=anulado", updateData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al anular gasto: {ex.Message}");
                return false;
            }
        }

        private GastoModel MapDocumentToGasto(FirestoreDocument doc)
        {
            var f = doc.Fields;
            return new GastoModel
            {
                id = doc.Name.Split('/').Last(),
                empresaId = f.empresaId?.StringValue ?? (f.EmpresaId?.StringValue ?? "empresa-demo"),
                fechaRegistro = DateTime.TryParse(f.fechaRegistro?.StringValue ?? f.FechaRegistro?.StringValue, out var fecha) ? fecha : DateTime.Now,
                categoria = f.categoria?.StringValue ?? (f.Categoria?.StringValue ?? "Otros"),
                concepto = f.concepto?.StringValue ?? (f.Concepto?.StringValue ?? ""),
                monto = ParseDecimalValue(f.monto ?? f.Monto),
                usuarioId = f.usuarioId?.StringValue ?? (f.UsuarioId?.StringValue ?? ""),
                nombreUsuario = f.nombreUsuario?.StringValue ?? (f.NombreUsuario?.StringValue ?? ""),
                observaciones = f.observaciones?.StringValue ?? (f.Observaciones?.StringValue ?? ""),
                anulado = f.anulado?.BooleanValue ?? (f.Anulado?.BooleanValue ?? false)
            };
        }

        private object MapGastoToFields(GastoModel g)
        {
            return new
            {
                empresaId = new { stringValue = g.empresaId },
                fechaRegistro = new { stringValue = g.fechaRegistro.ToString("o") },
                categoria = new { stringValue = g.categoria },
                concepto = new { stringValue = g.concepto },
                monto = new { doubleValue = (double)g.monto },
                usuarioId = new { stringValue = g.usuarioId },
                nombreUsuario = new { stringValue = g.nombreUsuario },
                observaciones = new { stringValue = g.observaciones },
                anulado = new { booleanValue = g.anulado }
            };
        }

        private decimal ParseDecimalValue(FirestoreValue? value)
        {
            if (value == null) return 0;
            if (value.DoubleValue.HasValue) return (decimal)value.DoubleValue.Value;
            if (value.IntegerValue != null && decimal.TryParse(value.IntegerValue, out var intValue)) return intValue;
            if (value.StringValue != null && decimal.TryParse(value.StringValue, out var strValue)) return strValue;
            return 0;
        }

        private class FirestoreRunQueryResponse { public FirestoreDocument? Document { get; set; } }
        private class FirestoreDocument { 
            public string Name { get; set; } = string.Empty; 
            public FirestoreFields Fields { get; set; } = new FirestoreFields(); 
        }
        private class FirestoreFields {
            public FirestoreValue? empresaId { get; set; }
            public FirestoreValue? EmpresaId { get; set; }
            public FirestoreValue? fechaRegistro { get; set; }
            public FirestoreValue? FechaRegistro { get; set; }
            public FirestoreValue? categoria { get; set; }
            public FirestoreValue? Categoria { get; set; }
            public FirestoreValue? concepto { get; set; }
            public FirestoreValue? Concepto { get; set; }
            public FirestoreValue? monto { get; set; }
            public FirestoreValue? Monto { get; set; }
            public FirestoreValue? usuarioId { get; set; }
            public FirestoreValue? UsuarioId { get; set; }
            public FirestoreValue? nombreUsuario { get; set; }
            public FirestoreValue? NombreUsuario { get; set; }
            public FirestoreValue? observaciones { get; set; }
            public FirestoreValue? Observaciones { get; set; }
            public FirestoreValue? anulado { get; set; }
            public FirestoreValue? Anulado { get; set; }
        }
        private class FirestoreValue {
            public string? StringValue { get; set; }
            public string? IntegerValue { get; set; }
            public double? DoubleValue { get; set; }
            public bool? BooleanValue { get; set; }
        }
    }
}
