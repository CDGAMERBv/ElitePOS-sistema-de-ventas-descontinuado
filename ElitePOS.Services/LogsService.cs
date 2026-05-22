using System.Net.Http.Json;
using System.Text.Json;
using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public class LogsService : ILogsService
    {
        private readonly HttpClient _httpClient;
        private const string PROJECT_ID = "TU_FIREBASE_PROJECT_ID";
        private const string FIREBASE_URL = $"https://firestore.googleapis.com/v1/projects/{PROJECT_ID}/databases/(default)/documents/auditoria";

        public LogsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> RegistrarLog(string empresaId, string usuarioId, string nombreUsuario, string accion, string detalle, string modulo = "", string ip = "")
        {
            try
            {
                var log = new LogModel
                {
                    id = Guid.NewGuid().ToString(),
                    empresaId = empresaId,
                    usuarioId = usuarioId,
                    nombreUsuario = nombreUsuario,
                    accion = accion,
                    detalle = detalle,
                    modulo = modulo,
                    ip = ip,
                    fechaHora = DateTime.Now
                };

                var firestoreDoc = new
                {
                    fields = new
                    {
                        empresaId = new { stringValue = log.empresaId },
                        usuarioId = new { stringValue = log.usuarioId },
                        nombreUsuario = new { stringValue = log.nombreUsuario },
                        accion = new { stringValue = log.accion },
                        detalle = new { stringValue = log.detalle },
                        modulo = new { stringValue = log.modulo },
                        ip = new { stringValue = log.ip },
                        fechaHora = new { timestampValue = log.fechaHora.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
                    }
                };

                var url = $"{FIREBASE_URL}?documentId={log.id}";
                var response = await _httpClient.PostAsJsonAsync(url, firestoreDoc);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RegistrarAccesoRestringido(string empresaId, string usuarioId, string nombreUsuario, string moduloIntentado)
        {
            var detalle = $"Intento de acceso a módulo restringido: {moduloIntentado}";
            return await RegistrarLog(empresaId, usuarioId, nombreUsuario, "Acceso Denegado", detalle, moduloIntentado);
        }

        public async Task<bool> RegistrarEdicionPrecio(string empresaId, string usuarioId, string nombreUsuario, string productoId, decimal precioAnterior, decimal precioNuevo)
        {
            var detalle = $"Precio modificado - Producto: {productoId} - Anterior: ${precioAnterior:N2} → Nuevo: ${precioNuevo:N2}";
            return await RegistrarLog(empresaId, usuarioId, nombreUsuario, "Edición de Precio", detalle, "Inventario");
        }

        public async Task<IEnumerable<LogModel>> ObtenerLogs(string empresaId)
        {
            try
            {
                var response = await _httpClient.GetAsync(FIREBASE_URL);
                if (!response.IsSuccessStatusCode) return new List<LogModel>();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var firestoreResponse = JsonSerializer.Deserialize<FirestoreListResponse>(jsonResponse, options);

                if (firestoreResponse?.Documents == null || !firestoreResponse.Documents.Any()) return new List<LogModel>();

                var logs = firestoreResponse.Documents
                    .Select(doc => new LogModel
                    {
                        id = doc.Name.Split('/').Last(),
                        empresaId = GetVal(doc.Fields, "empresaId"),
                        usuarioId = GetVal(doc.Fields, "usuarioId"),
                        nombreUsuario = GetVal(doc.Fields, "nombreUsuario"),
                        accion = GetVal(doc.Fields, "accion"),
                        detalle = GetVal(doc.Fields, "detalle"),
                        modulo = GetVal(doc.Fields, "modulo"),
                        ip = GetVal(doc.Fields, "ip"),
                        fechaHora = doc.Fields.ContainsKey("fechaHora") && !string.IsNullOrEmpty(doc.Fields["fechaHora"].TimestampValue)
                            ? DateTime.Parse(doc.Fields["fechaHora"].TimestampValue)
                            : (doc.Fields.ContainsKey("FechaHora") && !string.IsNullOrEmpty(doc.Fields["FechaHora"].TimestampValue)
                                ? DateTime.Parse(doc.Fields["FechaHora"].TimestampValue)
                                : DateTime.MinValue)
                    })
                    .Where(l => l.empresaId == empresaId)
                    .OrderByDescending(l => l.fechaHora)
                    .ToList();

                return logs;
            }
            catch
            {
                return new List<LogModel>();
            }
        }

        private string GetVal(Dictionary<string, FirestoreField> fields, string key)
        {
            if (fields.ContainsKey(key)) return fields[key].StringValue;
            var pascalKey = char.ToUpper(key[0]) + key.Substring(1);
            if (fields.ContainsKey(pascalKey)) return fields[pascalKey].StringValue;
            return "";
        }

        private class FirestoreListResponse { public List<FirestoreDocument> Documents { get; set; } = new(); }
        private class FirestoreDocument { 
            public string Name { get; set; } = ""; 
            public Dictionary<string, FirestoreField> Fields { get; set; } = new(); 
        }
        private class FirestoreField { 
            public string StringValue { get; set; } = ""; 
            public string TimestampValue { get; set; } = ""; 
        }
    }
}
