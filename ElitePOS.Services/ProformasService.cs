using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ElitePOS.Services
{
    public class ProformasService : IProformasService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogsService _logsService;
        private readonly IConfiguration _config;
        private string FIREBASE_URL => $"https://firestore.googleapis.com/v1/projects/{_config["Firestore:ProjectId"]}/databases/(default)/documents/ventas";

        public ProformasService(HttpClient httpClient, ILogsService logsService, IConfiguration config)
        {
            _httpClient = httpClient;
            _logsService = logsService;
            _config = config;
        }

        public async Task<IEnumerable<VentaModel>> ObtenerProformas()
        {
            try
            {
                var response = await _httpClient.GetAsync(FIREBASE_URL);
                if (!response.IsSuccessStatusCode) return new List<VentaModel>();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var firestoreResponse = JsonSerializer.Deserialize<FirestoreListResponse>(jsonResponse, options);

                if (firestoreResponse?.Documents == null) return new List<VentaModel>();

                return firestoreResponse.Documents
                    .Select(doc => MapFirestoreDocument(doc))
                    .Where(v => v.tipoComprobante == "Proforma")
                    .OrderByDescending(p => p.fechaHora)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener proformas: {ex.Message}");
                return new List<VentaModel>();
            }
        }

        public async Task<IEnumerable<VentaModel>> ObtenerProformasPorRango(DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{_config["Firestore:ProjectId"]}/databases/(default)/documents:runQuery";
                var filtroFechas = ConstruirFiltroFechasProformas(fechaInicio, fechaFin);

                object payload = filtroFechas != null 
                    ? new { structuredQuery = new { from = new[] { new { collectionId = "ventas", allDescendants = false } }, where = filtroFechas, orderBy = new[] { new { field = new { fieldPath = "fechaHora" }, direction = "DESCENDING" } } } }
                    : new { structuredQuery = new { from = new[] { new { collectionId = "ventas", allDescendants = false } }, orderBy = new[] { new { field = new { fieldPath = "fechaHora" }, direction = "DESCENDING" } } } };

                var response = await _httpClient.PostAsJsonAsync(url, payload);
                if (!response.IsSuccessStatusCode) return new List<VentaModel>();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var runQueryResponses = JsonSerializer.Deserialize<List<FirestoreRunQueryResponse>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (runQueryResponses == null) return new List<VentaModel>();

                return runQueryResponses
                    .Where(r => r.Document != null)
                    .Select(r => MapFirestoreDocument(r.Document!))
                    .Where(v => v.tipoComprobante != null && v.tipoComprobante.Equals("Proforma", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al filtrar proformas: {ex.Message}");
                return new List<VentaModel>();
            }
        }

        private object? ConstruirFiltroFechasProformas(DateTime? desde, DateTime? hasta)
        {
            var filters = new List<object>();
            if (desde.HasValue)
            {
                var inicioDia = new DateTime(desde.Value.Year, desde.Value.Month, desde.Value.Day, 0, 0, 0, DateTimeKind.Local).ToString("o");
                filters.Add(new { fieldFilter = new { field = new { fieldPath = "fechaHora" }, op = "GREATER_THAN_OR_EQUAL", value = new { stringValue = inicioDia } } });
            }
            if (hasta.HasValue)
            {
                var finDia = new DateTime(hasta.Value.Year, hasta.Value.Month, hasta.Value.Day, 23, 59, 59, 999, DateTimeKind.Local).ToString("o");
                filters.Add(new { fieldFilter = new { field = new { fieldPath = "fechaHora" }, op = "LESS_THAN_OR_EQUAL", value = new { stringValue = finDia } } });
            }
            if (filters.Count == 1) return filters[0];
            if (filters.Count > 1) return new { compositeFilter = new { op = "AND", filters = filters } };
            return null;
        }

        private VentaModel MapFirestoreDocument(FirestoreDocument doc)
        {
            var fields = doc.Fields;
            var v = new VentaModel();
            v.id = doc.Name.Split('/').Last();
            v.empresaId = (fields.empresaId?.StringValue ?? fields.EmpresaId?.StringValue) ?? "";
            v.fechaHora = DateTime.TryParse(fields.fechaHora?.StringValue ?? fields.FechaHora?.StringValue, out var fecha) ? fecha : DateTime.Now;
            v.numeroComprobante = (fields.numeroComprobante?.StringValue ?? fields.NumeroComprobante?.StringValue) ?? "";
            v.cliente = (fields.cliente?.StringValue ?? fields.Cliente?.StringValue) ?? "Cliente General";
            v.tipoComprobante = (fields.tipoComprobante?.StringValue ?? fields.TipoComprobante?.StringValue) ?? "Proforma";
            v.metodoPago = (fields.metodoPago?.StringValue ?? fields.MetodoPago?.StringValue) ?? "Efectivo";
            v.total = ParseDecimalValue(fields.total ?? fields.Total);
            v.items = ParseItems(fields.items?.ArrayValue?.Values ?? fields.Items?.ArrayValue?.Values);
            v.anulada = (fields.anulada?.BooleanValue ?? fields.Anulada?.BooleanValue) ?? false;
            v.usuarioId = (fields.usuarioId?.StringValue ?? fields.UsuarioId?.StringValue) ?? "";
            v.nombreUsuario = (fields.nombreUsuario?.StringValue ?? fields.NombreUsuario?.StringValue) ?? "";
            return v;
        }

        public async Task<VentaModel?> ObtenerProformaPorId(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{FIREBASE_URL}/{id}");
                if (!response.IsSuccessStatusCode) return null;
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var doc = JsonSerializer.Deserialize<FirestoreDocument>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return doc != null ? MapFirestoreDocument(doc) : null;
            }
            catch { return null; }
        }

        public async Task<bool> RegistrarProforma(VentaModel proforma)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(proforma.id)) proforma.id = Guid.NewGuid().ToString();
                var firestoreDoc = new
                {
                    fields = new
                    {
                        empresaId = new { stringValue = proforma.empresaId ?? "" },
                        fechaHora = new { stringValue = proforma.fechaHora.ToString("o") },
                        numeroComprobante = new { stringValue = proforma.numeroComprobante },
                        cliente = new { stringValue = proforma.cliente },
                        tipoComprobante = new { stringValue = proforma.tipoComprobante ?? "Proforma" },
                        metodoPago = new { stringValue = proforma.metodoPago ?? "Efectivo" },
                        total = new { doubleValue = (double)proforma.total },
                        items = new
                        {
                            arrayValue = new
                            {
                                values = proforma.items.Select(item => new
                                {
                                    mapValue = new
                                    {
                                        fields = new
                                        {
                                            productoId = new { stringValue = item.productoId },
                                            nombreProducto = new { stringValue = item.nombreProducto },
                                            cantidad = new { integerValue = item.cantidad.ToString() },
                                            precioUnitario = new { doubleValue = (double)item.precioUnitario },
                                            subtotal = new { doubleValue = (double)item.subtotal }
                                        }
                                    }
                                }).ToArray()
                            }
                        },
                        anulada = new { booleanValue = proforma.anulada },
                        usuarioId = new { stringValue = proforma.usuarioId ?? "" },
                        nombreUsuario = new { stringValue = proforma.nombreUsuario ?? "" }
                    }
                };

                var url = $"{FIREBASE_URL}?documentId={proforma.id}";
                var response = await _httpClient.PostAsJsonAsync(url, firestoreDoc);
                if (response.IsSuccessStatusCode)
                {
                    await _logsService.RegistrarLog(proforma.empresaId ?? "", proforma.usuarioId ?? "", proforma.nombreUsuario ?? "", "Proforma", $"Proforma {proforma.numeroComprobante} - {proforma.cliente} - Total: ${proforma.total:N2}", "Ventas");
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public async Task<bool> EliminarProforma(string id)
        {
            try { return (await _httpClient.DeleteAsync($"{FIREBASE_URL}/{id}")).IsSuccessStatusCode; }
            catch { return false; }
        }

        public async Task<bool> ConvertirAVenta(string proformaId) => await EliminarProforma(proformaId);

        private decimal ParseDecimalValue(FirestoreValue? value)
        {
            if (value == null) return 0;
            if (value.DoubleValue.HasValue) return (decimal)value.DoubleValue.Value;
            if (value.IntegerValue != null && decimal.TryParse(value.IntegerValue, out var intValue)) return intValue;
            if (value.StringValue != null && decimal.TryParse(value.StringValue, out var strValue)) return strValue;
            return 0;
        }

        private List<VentaItemModel> ParseItems(FirestoreValue[]? values)
        {
            if (values == null) return new List<VentaItemModel>();
            return values.Select(v =>
            {
                var fields = v.MapValue?.Fields;
                if (fields == null) return null;
                return new VentaItemModel
                {
                    productoId = (fields.productoId?.StringValue ?? fields.ProductoId?.StringValue) ?? "",
                    nombreProducto = (fields.nombreProducto?.StringValue ?? fields.NombreProducto?.StringValue) ?? "",
                    cantidad = int.TryParse(fields.cantidad?.IntegerValue ?? fields.Cantidad?.IntegerValue, out var c) ? c : 0,
                    precioUnitario = ParseDecimalValue(fields.precioUnitario ?? fields.PrecioUnitario),
                    subtotal = ParseDecimalValue(fields.subtotal ?? fields.Subtotal)
                };
            }).Where(i => i != null).Cast<VentaItemModel>().ToList();
        }

        private class FirestoreListResponse { public FirestoreDocument[]? Documents { get; set; } }
        private class FirestoreRunQueryResponse { public FirestoreDocument? Document { get; set; } }
        private class FirestoreDocument { public string Name { get; set; } = ""; public FirestoreFields Fields { get; set; } = new(); }
        private class FirestoreFields
        {
            public FirestoreValue? empresaId { get; set; }
            public FirestoreValue? EmpresaId { get; set; }
            public FirestoreValue? fechaHora { get; set; }
            public FirestoreValue? FechaHora { get; set; }
            public FirestoreValue? numeroComprobante { get; set; }
            public FirestoreValue? NumeroComprobante { get; set; }
            public FirestoreValue? cliente { get; set; }
            public FirestoreValue? Cliente { get; set; }
            public FirestoreValue? tipoComprobante { get; set; }
            public FirestoreValue? TipoComprobante { get; set; }
            public FirestoreValue? metodoPago { get; set; }
            public FirestoreValue? MetodoPago { get; set; }
            public FirestoreValue? total { get; set; }
            public FirestoreValue? Total { get; set; }
            public FirestoreValue? items { get; set; }
            public FirestoreValue? Items { get; set; }
            public FirestoreValue? anulada { get; set; }
            public FirestoreValue? Anulada { get; set; }
            public FirestoreValue? usuarioId { get; set; }
            public FirestoreValue? UsuarioId { get; set; }
            public FirestoreValue? nombreUsuario { get; set; }
            public FirestoreValue? NombreUsuario { get; set; }

            // Fields for items
            public FirestoreValue? productoId { get; set; }
            public FirestoreValue? ProductoId { get; set; }
            public FirestoreValue? nombreProducto { get; set; }
            public FirestoreValue? NombreProducto { get; set; }
            public FirestoreValue? cantidad { get; set; }
            public FirestoreValue? Cantidad { get; set; }
            public FirestoreValue? precioUnitario { get; set; }
            public FirestoreValue? PrecioUnitario { get; set; }
            public FirestoreValue? subtotal { get; set; }
            public FirestoreValue? Subtotal { get; set; }
        }
        private class FirestoreValue
        {
            public string? StringValue { get; set; }
            public string? IntegerValue { get; set; }
            public double? DoubleValue { get; set; }
            public bool? BooleanValue { get; set; }
            public FirestoreArrayValue? ArrayValue { get; set; }
            public FirestoreMapValue? MapValue { get; set; }
        }
        private class FirestoreArrayValue { public FirestoreValue[]? Values { get; set; } }
        private class FirestoreMapValue { public FirestoreFields? Fields { get; set; } }
    }
}
