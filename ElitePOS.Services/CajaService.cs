using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace ElitePOS.Services
{
    public class CajaService : ICajaService
    {
        private readonly HttpClient _httpClient;
        private readonly IGastosService _gastosService;
        private readonly IConfiguration _config;
        private string FIREBASE_URL => $"https://firestore.googleapis.com/v1/projects/{_config["Firestore:ProjectId"]}/databases/(default)/documents/movimientos_caja";

        public CajaService(HttpClient httpClient, IGastosService gastosService, IConfiguration config)
        {
            _httpClient = httpClient;
            _gastosService = gastosService;
            _config = config;
        }

        public async Task<bool> AbrirCaja(decimal montoApertura, string usuarioId, string nombreUsuario)
        {
            var movimiento = new MovimientoCajaModel
            {
                id = Guid.NewGuid().ToString(),
                fecha = DateTime.Now,
                tipo = "Apertura",
                monto = montoApertura,
                montoSistema = montoApertura,
                diferencia = 0,
                usuarioId = usuarioId,
                nombreUsuario = nombreUsuario,
                observaciones = "Apertura de caja",
                cajaAbierta = true
            };

            var firestoreDoc = new
            {
                fields = new
                {
                    id = new { stringValue = movimiento.id },
                    empresaId = new { stringValue = movimiento.empresaId },
                    fecha = new { timestampValue = movimiento.fecha.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                    tipo = new { stringValue = movimiento.tipo },
                    monto = new { doubleValue = (double)movimiento.monto },
                    montoSistema = new { doubleValue = (double)movimiento.montoSistema },
                    diferencia = new { doubleValue = (double)movimiento.diferencia },
                    usuarioId = new { stringValue = movimiento.usuarioId },
                    nombreUsuario = new { stringValue = movimiento.nombreUsuario },
                    observaciones = new { stringValue = movimiento.observaciones },
                    cajaAbierta = new { booleanValue = movimiento.cajaAbierta },
                    tipoMovimiento = new { stringValue = "Apertura" } // Etiqueta para la IA
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"{FIREBASE_URL}?documentId={movimiento.id}", firestoreDoc);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CerrarCaja(decimal montoArqueo, string usuarioId, string nombreUsuario, string observaciones)
        {
            var cajaActual = await ObtenerCajaActual();
            if (cajaActual == null) return false;

            var efectivoVentas = await CalcularEfectivoEnCaja();
            var gastos = await CalcularGastosEnCaja();
            var montoSistema = cajaActual.monto + efectivoVentas - gastos;
            var diferencia = montoArqueo - montoSistema;

            var cierre = new MovimientoCajaModel
            {
                id = Guid.NewGuid().ToString(),
                fecha = DateTime.Now,
                tipo = "Cierre",
                monto = montoArqueo,
                montoSistema = montoSistema,
                diferencia = diferencia,
                usuarioId = usuarioId,
                nombreUsuario = nombreUsuario,
                observaciones = observaciones,
                cajaAbierta = false
            };

            var firestoreDoc = new
            {
                fields = new
                {
                    id = new { stringValue = cierre.id },
                    empresaId = new { stringValue = cierre.empresaId },
                    fecha = new { timestampValue = cierre.fecha.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                    tipo = new { stringValue = cierre.tipo },
                    monto = new { doubleValue = (double)cierre.monto },
                    montoSistema = new { doubleValue = (double)cierre.montoSistema },
                    diferencia = new { doubleValue = (double)cierre.diferencia },
                    usuarioId = new { stringValue = cierre.usuarioId },
                    nombreUsuario = new { stringValue = cierre.nombreUsuario },
                    observaciones = new { stringValue = cierre.observaciones },
                    cajaAbierta = new { booleanValue = cierre.cajaAbierta },
                    tipoMovimiento = new { stringValue = "Cierre" } // Etiqueta para la IA
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"{FIREBASE_URL}?documentId={cierre.id}", firestoreDoc);
            return response.IsSuccessStatusCode;
        }

        public async Task<MovimientoCajaModel?> ObtenerCajaActual()
        {
            var response = await _httpClient.GetAsync(FIREBASE_URL);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var firestoreResponse = JsonSerializer.Deserialize<FirestoreDocsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (firestoreResponse?.Documents == null || firestoreResponse.Documents.Count == 0)
                return null;

            var cajas = firestoreResponse.Documents
                .Select(doc => ConvertirAMovimientoCaja(doc))
                .Where(m => m.tipo == "Apertura")
                .OrderByDescending(m => m.fecha)
                .ToList();

            if (!cajas.Any()) return null;

            var ultimaApertura = cajas.First();

            var cierres = firestoreResponse.Documents
                .Select(doc => ConvertirAMovimientoCaja(doc))
                .Where(m => m.tipo == "Cierre" && m.fecha > ultimaApertura.fecha)
                .ToList();

            if (cierres.Any())
                return null;

            return ultimaApertura;
        }

        public async Task<bool> HayCajaAbierta()
        {
            var caja = await ObtenerCajaActual();
            return caja != null;
        }

        public async Task<decimal> CalcularEfectivoEnCaja()
        {
            try
            {
                var cajaActual = await ObtenerCajaActual();
                if (cajaActual == null) return 0;

                var projectId = _config["Firestore:ProjectId"] ?? "TU_FIREBASE_PROJECT_ID";
                var url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents:runQuery";
                var empresaId = _config["EmpresaId"] ?? "empresa-demo";

                var filters = new List<object>
                {
                    new { fieldFilter = new { field = new { fieldPath = "empresaId" }, op = "EQUAL", value = new { stringValue = empresaId } } },
                    new { fieldFilter = new { field = new { fieldPath = "anulada" }, op = "EQUAL", value = new { booleanValue = false } } },
                    new { fieldFilter = new { field = new { fieldPath = "metodoPago" }, op = "EQUAL", value = new { stringValue = "Efectivo" } } },
                    new { fieldFilter = new { field = new { fieldPath = "fechaHora" }, op = "GREATER_THAN_OR_EQUAL", value = new { stringValue = cajaActual.fecha.ToString("o") } } }
                };

                var payload = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "ventas", allDescendants = false } },
                        where = new { compositeFilter = new { op = "AND", filters = filters } }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode) return 0;

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var runQueryResponses = JsonSerializer.Deserialize<List<FirestoreRunQueryResponse>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (runQueryResponses == null) return 0;

                return runQueryResponses
                    .Where(r => r.Document != null)
                    .Select(r => ParseDecimalValue(r.Document!.Fields.ContainsKey("total") ? r.Document!.Fields["total"] : r.Document!.Fields["Total"]))
                    .Sum();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error calculando efectivo en caja: {ex.Message}");
                return 0;
            }
        }

        private decimal ParseDecimalValue(FirestoreValue value)
        {
            if (value.DoubleValue.HasValue) return (decimal)value.DoubleValue.Value;
            if (!string.IsNullOrEmpty(value.StringValue) && decimal.TryParse(value.StringValue, out var val)) return val;
            return 0;
        }

        private class FirestoreRunQueryResponse
        {
            public FirestoreDoc? Document { get; set; }
        }

        public async Task<decimal> CalcularGastosEnCaja()
        {
            try
            {
                var cajaActual = await ObtenerCajaActual();
                if (cajaActual == null) return 0;

                // 🛡️ MIGRACI"N FIRESTORE: Usar IGastosService en lugar de RTDB legacy
                var todosLosGastos = await _gastosService.ObtenerGastos();
                
                // Filtrar solo los gastos no anulados realizados desde la apertura de caja
                var gastosDelTurno = todosLosGastos
                    .Where(g => !g.anulado && g.fechaRegistro >= cajaActual.fecha)
                    .ToList();

                return gastosDelTurno.Sum(g => g.monto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculando gastos en caja: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<MovimientoCajaModel>> ObtenerHistorialCaja(DateTime? desde = null, DateTime? hasta = null)
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

                // REGLA IA: Si se busca ingresos reales, filtrar por TipoMovimiento == "Venta"
                // Nota: Por ahora traemos todo y el filtrado fino se delega al consumidor o se añade aquí si es global.
                
                var structuredQuery = new
                {
                    from = new[] { new { collectionId = "movimientos_caja", allDescendants = false } },
                    where = filters.Count > 1 ? (object)new { compositeFilter = new { op = "AND", filters = filters } } : filters[0],
                    orderBy = new[] { new { field = new { fieldPath = "fecha" }, direction = "DESCENDING" } }
                };

                var payload = new { structuredQuery };
                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode) return new List<MovimientoCajaModel>();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var runQueryResponses = JsonSerializer.Deserialize<List<FirestoreRunQueryResponse>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (runQueryResponses == null) return new List<MovimientoCajaModel>();

                var movimientos = runQueryResponses
                    .Where(r => r.Document != null)
                    .Select(r => ConvertirAMovimientoCaja(r.Document!))
                    .ToList();

                if (desde.HasValue) movimientos = movimientos.Where(m => m.fecha >= desde.Value).ToList();
                if (hasta.HasValue) movimientos = movimientos.Where(m => m.fecha <= hasta.Value).ToList();

                return movimientos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en historial caja runQuery: {ex.Message}");
                return new List<MovimientoCajaModel>();
            }
        }

        private MovimientoCajaModel ConvertirAMovimientoCaja(FirestoreDoc doc)
        {
            return new MovimientoCajaModel
            {
                id = doc.Fields.ContainsKey("id") ? doc.Fields["id"].StringValue : (doc.Fields.ContainsKey("Id") ? doc.Fields["Id"].StringValue : ""),
                empresaId = doc.Fields.ContainsKey("empresaId") ? doc.Fields["empresaId"].StringValue : (doc.Fields.ContainsKey("EmpresaId") ? doc.Fields["EmpresaId"].StringValue : ""),
                fecha = doc.Fields.ContainsKey("fecha") ? DateTime.Parse(doc.Fields["fecha"].TimestampValue) : (doc.Fields.ContainsKey("Fecha") ? DateTime.Parse(doc.Fields["Fecha"].TimestampValue) : DateTime.MinValue),
                tipo = doc.Fields.ContainsKey("tipo") ? doc.Fields["tipo"].StringValue : (doc.Fields.ContainsKey("Tipo") ? doc.Fields["Tipo"].StringValue : ""),
                monto = doc.Fields.ContainsKey("monto") ? (decimal)(doc.Fields["monto"].DoubleValue ?? 0) : (doc.Fields.ContainsKey("Monto") ? (decimal)(doc.Fields["Monto"].DoubleValue ?? 0) : 0),
                montoSistema = doc.Fields.ContainsKey("montoSistema") ? (decimal)(doc.Fields["montoSistema"].DoubleValue ?? 0) : (doc.Fields.ContainsKey("MontoSistema") ? (decimal)(doc.Fields["MontoSistema"].DoubleValue ?? 0) : 0),
                diferencia = doc.Fields.ContainsKey("diferencia") ? (decimal)(doc.Fields["diferencia"].DoubleValue ?? 0) : (doc.Fields.ContainsKey("Diferencia") ? (decimal)(doc.Fields["Diferencia"].DoubleValue ?? 0) : 0),
                usuarioId = doc.Fields.ContainsKey("usuarioId") ? doc.Fields["usuarioId"].StringValue : (doc.Fields.ContainsKey("UsuarioId") ? doc.Fields["UsuarioId"].StringValue : ""),
                nombreUsuario = doc.Fields.ContainsKey("nombreUsuario") ? doc.Fields["nombreUsuario"].StringValue : (doc.Fields.ContainsKey("NombreUsuario") ? doc.Fields["NombreUsuario"].StringValue : ""),
                observaciones = doc.Fields.ContainsKey("observaciones") ? doc.Fields["observaciones"].StringValue : (doc.Fields.ContainsKey("Observaciones") ? doc.Fields["Observaciones"].StringValue : ""),
                cajaAbierta = doc.Fields.ContainsKey("cajaAbierta") ? (doc.Fields["cajaAbierta"].BooleanValue ?? false) : (doc.Fields.ContainsKey("CajaAbierta") ? (doc.Fields["CajaAbierta"].BooleanValue ?? false) : false),
                tipoMovimiento = doc.Fields.ContainsKey("tipoMovimiento") ? doc.Fields["tipoMovimiento"].StringValue : (doc.Fields.ContainsKey("TipoMovimiento") ? doc.Fields["TipoMovimiento"].StringValue : "")
            };
        }

        private VentaModel ConvertirAVenta(FirestoreDoc doc)
        {
            return new VentaModel
            {
                id = doc.Fields.ContainsKey("id") ? doc.Fields["id"].StringValue : (doc.Fields.ContainsKey("Id") ? doc.Fields["Id"].StringValue : ""),
                fechaHora = doc.Fields.ContainsKey("fechaHora") ? DateTime.Parse(doc.Fields["fechaHora"].TimestampValue) : (doc.Fields.ContainsKey("FechaHora") ? DateTime.Parse(doc.Fields["FechaHora"].TimestampValue) : DateTime.MinValue),
                metodoPago = doc.Fields.ContainsKey("metodoPago") ? doc.Fields["metodoPago"].StringValue : (doc.Fields.ContainsKey("MetodoPago") ? doc.Fields["MetodoPago"].StringValue : "Efectivo"),
                total = doc.Fields.ContainsKey("total") ? (decimal)(doc.Fields["total"].DoubleValue ?? 0) : (doc.Fields.ContainsKey("Total") ? (decimal)(doc.Fields["Total"].DoubleValue ?? 0) : 0),
                anulada = doc.Fields.ContainsKey("anulada") ? (doc.Fields["anulada"].BooleanValue ?? false) : (doc.Fields.ContainsKey("Anulada") ? (doc.Fields["Anulada"].BooleanValue ?? false) : false)
            };
        }

        private class FirestoreDocsResponse
        {
            [JsonPropertyName("documents")]
            public List<FirestoreDoc> Documents { get; set; } = new();
        }

        private class FirestoreDoc
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("fields")]
            public Dictionary<string, FirestoreValue> Fields { get; set; } = new();

            [JsonPropertyName("createTime")]
            public string CreateTime { get; set; } = "";

            [JsonPropertyName("updateTime")]
            public string UpdateTime { get; set; } = "";
        }

        private class FirestoreValue
        {
            [JsonPropertyName("stringValue")]
            public string StringValue { get; set; } = "";

            [JsonPropertyName("integerValue")]
            public string IntegerValue { get; set; } = "0";

            [JsonPropertyName("doubleValue")]
            public double? DoubleValue { get; set; }

            [JsonPropertyName("booleanValue")]
            public bool? BooleanValue { get; set; }

            [JsonPropertyName("timestampValue")]
            public string TimestampValue { get; set; } = "";

            [JsonPropertyName("arrayValue")]
            public FirestoreArrayValue? ArrayValue { get; set; }
        }

        private class FirestoreArrayValue
        {
            [JsonPropertyName("values")]
            public List<FirestoreValue> Values { get; set; } = new();
        }
    }
}


