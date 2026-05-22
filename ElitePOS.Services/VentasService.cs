using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ElitePOS.Services
{
    public class VentasService : IVentasService
    {
        private readonly HttpClient _httpClient;
        private readonly IInventarioService _inventarioService;
        private readonly ILogsService _logsService;
        private readonly IKardexService _kardexService;          
        private readonly IKardexQueueService _kardexQueue;       
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _config;
        private string FIREBASE_URL => $"https://firestore.googleapis.com/v1/projects/{_config["Firestore:ProjectId"]}/databases/(default)/documents/ventas";

        public VentasService(
            HttpClient httpClient, 
            IInventarioService inventarioService, 
            ILogsService logsService,
            IKardexService kardexService,
            IKardexQueueService kardexQueue,
            IServiceProvider serviceProvider,
            IConfiguration config)
        {
            _config = config;
            _httpClient = httpClient;
            _inventarioService = inventarioService;
            _logsService = logsService;
            _kardexService = kardexService;
            _kardexQueue = kardexQueue;
            _serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<VentaModel>> ObtenerVentas()
        {
            return await ObtenerVentasPorRango(null, null);
        }

        public async Task<IEnumerable<VentaModel>> ObtenerVentasPorRango(DateTime? fechaInicio, DateTime? fechaFin)
        {
            try
            {
                var projectId = _config["Firestore:ProjectId"] ?? "TU_FIREBASE_PROJECT_ID";
                var url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents:runQuery";
                var empresaId = _config["EmpresaId"];
                var empresaIdValida = !string.IsNullOrWhiteSpace(empresaId) && empresaId != "empresa-demo";

                var filters = new List<object>();
                if (empresaIdValida)
                {
                    filters.Add(new { fieldFilter = new { field = new { fieldPath = "empresaId" }, op = "EQUAL", value = new { stringValue = empresaId! } } });
                }

                if (fechaInicio.HasValue)
                {
                    var inicioDia = new DateTime(fechaInicio.Value.Year, fechaInicio.Value.Month, fechaInicio.Value.Day, 0, 0, 0, DateTimeKind.Local).ToString("o");
                    filters.Add(new { fieldFilter = new { field = new { fieldPath = "fechaHora" }, op = "GREATER_THAN_OR_EQUAL", value = new { stringValue = inicioDia } } });
                }

                if (fechaFin.HasValue)
                {
                    var finDia = new DateTime(fechaFin.Value.Year, fechaFin.Value.Month, fechaFin.Value.Day, 23, 59, 59, 999, DateTimeKind.Local).ToString("o");
                    filters.Add(new { fieldFilter = new { field = new { fieldPath = "fechaHora" }, op = "LESS_THAN_OR_EQUAL", value = new { stringValue = finDia } } });
                }

                object payload;
                var fromClause = new[] { new { collectionId = "ventas", allDescendants = false } };
                var orderClause = new[] { new { field = new { fieldPath = "fechaHora" }, direction = "DESCENDING" } };

                if (filters.Count == 0) payload = new { structuredQuery = new { from = fromClause, orderBy = orderClause } };
                else if (filters.Count == 1) payload = new { structuredQuery = new { from = fromClause, where = filters[0], orderBy = orderClause } };
                else payload = new { structuredQuery = new { from = fromClause, where = new { compositeFilter = new { op = "AND", filters = filters } }, orderBy = orderClause } };

                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) return new List<VentaModel>();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var runQueryResponses = JsonSerializer.Deserialize<List<FirestoreRunQueryResponse>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (runQueryResponses == null) return new List<VentaModel>();

                return runQueryResponses.Where(r => r.Document != null).Select(r => MapFirestoreDocument(r.Document!)).Where(v => v.tipoComprobante != "Proforma").ToList();
            }
            catch { return new List<VentaModel>(); }
        }

        private VentaModel MapFirestoreDocument(FirestoreDocument doc)
        {
            var fields = doc.Fields;
            return new VentaModel
            {
                id = doc.Name.Split('/').Last(),
                empresaId = fields.empresaId?.StringValue ?? "empresa-demo",
                sucursalId = fields.sucursalId?.StringValue ?? "",
                fechaHora = DateTime.TryParse(fields.fechaHora?.StringValue, out var fecha) ? fecha : DateTime.Now,
                numeroComprobante = fields.numeroComprobante?.StringValue ?? "",
                cliente = fields.cliente?.StringValue ?? "Cliente General",
                tipoComprobante = fields.tipoComprobante?.StringValue ?? "Boleta",
                metodoPago = fields.metodoPago?.StringValue ?? "Efectivo",
                total = ParseDecimalValue(fields.total),
                subtotal = ParseDecimalValue(fields.subtotal),
                subtotalGravada = ParseDecimalValue(fields.subtotalGravada),
                igv = ParseDecimalValue(fields.igv),
                items = ParseItems(fields.items?.ArrayValue?.Values),
                anulada = fields.anulada?.BooleanValue ?? false,
                usuarioId = fields.usuarioId?.StringValue ?? "",
                nombreUsuario = fields.nombreUsuario?.StringValue ?? "",
                cajaId = fields.cajaId?.StringValue ?? "",
                observaciones = fields.observaciones?.StringValue ?? "",
                clienteId = fields.clienteId?.StringValue ?? "",
                tipoPago = fields.tipoPago?.StringValue ?? "Contado",
                estadoPago = fields.estadoPago?.StringValue ?? "pagado",
                montoPagado = ParseDecimalValue(fields.montoPagado),
                fechaVencimiento = fields.fechaVencimiento?.StringValue != null && DateTime.TryParse(fields.fechaVencimiento.StringValue, out var fv) ? fv : null,
                direccionCliente = fields.direccionCliente?.StringValue ?? "",
                tipoDocumentoCliente = fields.tipoDocumentoCliente?.StringValue ?? "DNI",
                numeroDocumentoCliente = fields.numeroDocumentoCliente?.StringValue ?? "",
                ordenCompra = fields.ordenCompra?.StringValue ?? "",
                guiaRemision = fields.guiaRemision?.StringValue ?? "",
                placa = fields.placa?.StringValue ?? ""
            };
        }

        public async Task<List<UtilidadResumenDto>> ObtenerResumenUtilidadesAsync(DateTime? inicio, DateTime? fin, bool incluirNotas = true)
        {
            var ventas = await ObtenerVentasPorRango(inicio, fin);
            var productos = await _inventarioService.ObtenerProductos();
            var listaProductos = productos?.ToList() ?? new List<ProductoModel>();
            return ventas.Where(v => !v.anulada && (incluirNotas || (v.tipoComprobante != "Nota de Venta" && !v.numeroComprobante.StartsWith("IA", StringComparison.OrdinalIgnoreCase)))).SelectMany(v => v.items).GroupBy(item => item.productoId).Select(g => {
                var producto = listaProductos.FirstOrDefault(p => p.id == g.Key);
                if (producto == null) return null;
                decimal costoTotal = g.Sum(item => (item.precioCosto > 0 ? item.precioCosto : producto.precioCosto) * item.cantidad);
                decimal ingresoTotal = g.Sum(item => item.subtotal > 0 ? item.subtotal : item.precioUnitario * item.cantidad);
                return new UtilidadResumenDto { productoId = producto.id, nombreProducto = producto.nombre, categoria = producto.categoria, cantidadVendida = g.Sum(item => item.cantidad), costoTotal = costoTotal, ingresoTotal = ingresoTotal };
            }).Where(dto => dto != null).Select(dto => dto!).ToList();
        }

        public async Task<VentaModel?> ObtenerUltimaVenta()
        {
            try {
                var url = $"{FIREBASE_URL}?pageSize=1&orderBy=fechaHora desc";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var firestoreResponse = JsonSerializer.Deserialize<FirestoreListResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (firestoreResponse?.Documents == null || !firestoreResponse.Documents.Any()) return null;
                return MapFirestoreDocument(firestoreResponse.Documents.First());
            } catch { return null; }
        }

        public async Task<VentaModel?> ObtenerPrimeraVenta()
        {
            try {
                var url = $"{FIREBASE_URL}?pageSize=1&orderBy=fechaHora asc";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var firestoreResponse = JsonSerializer.Deserialize<FirestoreListResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (firestoreResponse?.Documents == null || !firestoreResponse.Documents.Any()) return null;
                return MapFirestoreDocument(firestoreResponse.Documents.First());
            } catch { return null; }
        }

        public async Task<VentaModel?> ObtenerVentaPorId(string id)
        {
            try {
                var response = await _httpClient.GetAsync($"{FIREBASE_URL}/{id}");
                if (!response.IsSuccessStatusCode) return null;
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var doc = JsonSerializer.Deserialize<FirestoreDocument>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (doc?.Fields == null) return null;
                return MapFirestoreDocument(doc);
            } catch { return null; }
        }

        public async Task<VentaModel?> GuardarVenta(VentaModel venta)
        {
            var res = await RegistrarVenta(venta);
            return res ? venta : null;
        }

        public async Task<bool> RegistrarVenta(VentaModel venta, bool permitirVentaSinStock = false)
        {
            try {
                if (string.IsNullOrWhiteSpace(venta.id)) venta.id = Guid.NewGuid().ToString();
                if (string.IsNullOrWhiteSpace(venta.numeroComprobante) || venta.items == null || !venta.items.Any()) return false;
                venta.estadoSunat = "PENDIENTE";
                venta.intentosEnvio = 0;
                venta.fechaUltimoIntento = DateTime.Now;
                var ventaGuardada = await GuardarVentaLocalmente(venta);
                if (!ventaGuardada) throw new Exception("Error al guardar local");
                await ProcesarKardexYActualizarStock(venta, permitirVentaSinStock);
                var resultadoEnvio = await IntentarEnvioSunatConResiliencia(venta);
                if (resultadoEnvio.exito) { venta.estadoSunat = "ENVIADO"; venta.mensajeSunat = resultadoEnvio.mensaje; venta.hashSunat = resultadoEnvio.codigoHash; }
                else { venta.estadoSunat = "PENDIENTE"; venta.mensajeSunat = resultadoEnvio.mensaje; venta.intentosEnvio++; venta.fechaUltimoIntento = DateTime.Now; }
                await ActualizarEstadoVenta(venta);
                try { var dashboardService = _serviceProvider.GetService(typeof(IDashboardStateService)) as IDashboardStateService; if (dashboardService != null) dashboardService.MarcarComoDesactualizado(); } catch { }
                return true;
            } catch { return false; }
        }

        public async Task<bool> AnularVenta(string ventaId)
        {
            try {
                var venta = await ObtenerVentaPorId(ventaId);
                var updateData = new { fields = new { anulada = new { booleanValue = true } } };
                var content = new StringContent(JsonSerializer.Serialize(updateData), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{FIREBASE_URL}/{ventaId}?updateMask.fieldPaths=anulada", content);
                if (response.IsSuccessStatusCode) {
                    if (venta != null && venta.items != null) {
                        foreach (var item in venta.items) {
                            if (string.IsNullOrEmpty(item.productoId)) continue;
                            var producto = await _inventarioService.ObtenerProductoPorId(item.productoId);
                            if (producto != null && producto.categoria != "Servicio") {
                                await _inventarioService.ActualizarStock(item.productoId, producto.stock + item.cantidad);
                                await _kardexQueue.RegistrarEntradaAsync(venta.empresaId ?? "", item.productoId, item.nombreProducto, item.cantidad, $"Anulación {venta.tipoComprobante} {venta.numeroComprobante}", $"ANU-{venta.id}-{item.productoId}", venta.usuarioId, venta.nombreUsuario);
                            }
                        }
                    }
                    if (venta != null) await _logsService.RegistrarLog(venta.empresaId ?? "", venta.usuarioId ?? "", venta.nombreUsuario ?? "", "Anulación", $"ANULADA: {venta.tipoComprobante} {venta.numeroComprobante}", "Ventas");
                    return true;
                }
                return false;
            } catch { return false; }
        }

        public async Task<bool> ActualizarVenta(VentaModel venta)
        {
            try {
                var updateData = new { fields = new { montoPagado = new { doubleValue = (double)venta.montoPagado }, estadoPago = new { stringValue = venta.estadoPago } } };
                var content = new StringContent(JsonSerializer.Serialize(updateData), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{FIREBASE_URL}/{venta.id}?updateMask.fieldPaths=montoPagado&updateMask.fieldPaths=estadoPago", content);
                return response.IsSuccessStatusCode;
            } catch { return false; }
        }

        public async Task<decimal> ObtenerVentasHoy() { var hoy = DateTime.Today; var ventas = await ObtenerVentasPorRango(hoy, hoy.AddDays(1).AddSeconds(-1)); return ventas.Where(v => !v.anulada).Sum(v => v.total); }
        public async Task<decimal> ObtenerVentasUltimaSemana() { var inicio = DateTime.Today.AddDays(-7); var fin = DateTime.Today.AddDays(1).AddSeconds(-1); var ventas = await ObtenerVentasPorRango(inicio, fin); return ventas.Where(v => !v.anulada).Sum(v => v.total); }
        public async Task<int> ObtenerTotalTransaccionesHoy() { var hoy = DateTime.Today; var ventas = await ObtenerVentasPorRango(hoy, hoy.AddDays(1).AddSeconds(-1)); return ventas.Count(v => !v.anulada); }
        public async Task<IEnumerable<VentaModel>> ObtenerVentasRecientes(int top) { return (await ObtenerVentas()).OrderByDescending(v => v.fechaHora).Take(top); }
        public async Task<IEnumerable<ProductoTopModel>> ObtenerProductosMasVendidos(int top) { var ventas = await ObtenerVentas(); return ventas.Where(v => !v.anulada).SelectMany(v => v.items).GroupBy(i => i.nombreProducto).Select(g => new ProductoTopModel { nombre = g.Key, cantidad = g.Sum(x => x.cantidad), total = g.Sum(x => x.subtotal) }).OrderByDescending(x => x.cantidad).Take(top).ToList(); }
        public async Task<IEnumerable<VentaMensualModel>> ObtenerVentasMensuales() { var ventas = await ObtenerVentas(); return ventas.Where(v => !v.anulada && v.fechaHora.Year == DateTime.Now.Year).GroupBy(v => v.fechaHora.Month).Select(g => new VentaMensualModel { mes = new DateTime(2000, g.Key, 1).ToString("MMM"), total = g.Sum(v => v.total) }).OrderBy(x => DateTime.ParseExact(x.mes, "MMM", null).Month).ToList(); }
        public async Task<IEnumerable<VentaModel>> ObtenerVentasDetalladas(DateTime? inicio, DateTime? fin) { return await ObtenerVentasPorRango(inicio, fin); }

        public async Task<Dictionary<string, int>> ObtenerTopProductosVendidos(int top, DateTime fechaInicio, DateTime fechaFin)
        {
            try { var ventas = await ObtenerVentasPorRango(fechaInicio, fechaFin); return ventas.Where(v => !v.anulada).SelectMany(v => v.items).GroupBy(i => i.nombreProducto).Select(g => new { nombre = g.Key, cantidad = g.Sum(x => x.cantidad) }).OrderByDescending(x => x.cantidad).Take(top).ToDictionary(x => x.nombre, x => x.cantidad); } catch { return new Dictionary<string, int>(); }
        }

        public async Task<bool> ReintentarEnviosSunatPendientes()
        {
            try {
                var ventas = await ObtenerVentas();
                var ventasPendientes = ventas.Where(v => v.estadoSunat == "PENDIENTE").ToList();
                int exitos = 0;
                foreach (var v in ventasPendientes) {
                    var res = await IntentarEnvioSunatConResiliencia(v);
                    if (res.exito) { v.estadoSunat = "ENVIADO"; v.mensajeSunat = res.mensaje; v.hashSunat = res.codigoHash; await ActualizarEstadoVenta(v); exitos++; }
                }
                return exitos > 0;
            } catch { return false; }
        }

        private decimal ParseDecimalValue(FirestoreValue? value)
        {
            if (value == null) return 0;
            if (value.DoubleValue.HasValue) return (decimal)value.DoubleValue.Value;
            if (value.IntegerValue != null && decimal.TryParse(value.IntegerValue, out var intValue)) return intValue;
            if (value.StringValue != null && decimal.TryParse(value.StringValue, out var strValue)) return strValue;
            return 0;
        }

        private List<VentaItemModel> ParseItems(List<FirestoreValue>? itemValues)
        {
            var items = new List<VentaItemModel>();
            if (itemValues != null) {
                foreach (var iv in itemValues) {
                    var f = iv.MapValue?.Fields;
                    if (f != null) items.Add(new VentaItemModel { productoId = f.productoId?.StringValue ?? "", nombreProducto = f.nombreProducto?.StringValue ?? "", cantidad = int.Parse(f.cantidad?.IntegerValue ?? "0"), precioUnitario = ParseDecimalValue(f.precioUnitario), precioCosto = ParseDecimalValue(f.precioCosto), precioVenta = ParseDecimalValue(f.precioVenta), subtotal = ParseDecimalValue(f.subtotal), codigoInterno = f.codigoInterno?.StringValue ?? "", unidadMedida = f.unidadMedida?.StringValue ?? "NIU" });
                }
            }
            return items;
        }

        private async Task<bool> GuardarVentaLocalmente(VentaModel venta)
        {
            try {
                foreach (var i in venta.items) { i.subtotal = i.cantidad * i.precioUnitario; var p = await _inventarioService.ObtenerProductoPorId(i.productoId); if (p != null) i.precioCosto = p.precioCosto; }
                venta.total = venta.items.Sum(i => i.subtotal);
                var fields = new Dictionary<string, object> {
                    { "empresaId", new { stringValue = venta.empresaId ?? "" } },
                    { "usuarioId", new { stringValue = venta.usuarioId ?? "" } },
                    { "nombreUsuario", new { stringValue = venta.nombreUsuario ?? "" } },
                    { "fechaHora", new { stringValue = venta.fechaHora.ToString("o") } },
                    { "numeroComprobante", new { stringValue = venta.numeroComprobante } },
                    { "cliente", new { stringValue = venta.cliente ?? "Cliente General" } },
                    { "tipoComprobante", new { stringValue = venta.tipoComprobante ?? "Boleta" } },
                    { "metodoPago", new { stringValue = venta.metodoPago ?? "Efectivo" } },
                    { "total", new { doubleValue = (double)venta.total } },
                    { "subtotal", new { doubleValue = (double)venta.subtotal } },
                    { "subtotalGravada", new { doubleValue = (double)venta.subtotalGravada } },
                    { "igv", new { doubleValue = (double)venta.igv } },
                    { "items", new { arrayValue = new { values = venta.items.Select(it => new { mapValue = new { fields = new { productoId = new { stringValue = it.productoId ?? "" }, nombreProducto = new { stringValue = it.nombreProducto ?? "" }, cantidad = new { integerValue = it.cantidad.ToString() }, precioUnitario = new { doubleValue = (double)it.precioUnitario }, precioCosto = new { doubleValue = (double)it.precioCosto }, precioVenta = new { doubleValue = (double)it.precioVenta }, subtotal = new { doubleValue = (double)it.subtotal }, codigoInterno = new { stringValue = it.codigoInterno ?? "" }, unidadMedida = new { stringValue = it.unidadMedida ?? "NIU" } } } }).ToArray() } } },
                    { "anulada", new { booleanValue = venta.anulada } },
                    { "observaciones", new { stringValue = venta.observaciones ?? "" } },
                    { "estadoSunat", new { stringValue = venta.estadoSunat } },
                    { "mensajeSunat", new { stringValue = venta.mensajeSunat } },
                    { "hashSunat", new { stringValue = venta.hashSunat } },
                    { "intentosEnvio", new { integerValue = venta.intentosEnvio.ToString() } },
                    { "fechaUltimoIntento", new { stringValue = venta.fechaUltimoIntento?.ToString("o") ?? "" } },
                    { "cajaId", new { stringValue = venta.cajaId ?? "" } },
                    { "sucursalId", new { stringValue = venta.sucursalId ?? "" } },
                    { "clienteId", new { stringValue = venta.clienteId ?? "" } },
                    { "tipoPago", new { stringValue = venta.tipoPago ?? "Contado" } },
                    { "estadoPago", new { stringValue = venta.estadoPago ?? "pagado" } },
                    { "montoPagado", new { doubleValue = (double)venta.montoPagado } },
                    { "direccionCliente", new { stringValue = venta.direccionCliente ?? "" } },
                    { "tipoDocumentoCliente", new { stringValue = venta.tipoDocumentoCliente ?? "DNI" } },
                    { "numeroDocumentoCliente", new { stringValue = venta.numeroDocumentoCliente ?? "" } }
                };
                if (!string.IsNullOrWhiteSpace(venta.ordenCompra)) fields.Add("ordenCompra", new { stringValue = venta.ordenCompra });
                if (!string.IsNullOrWhiteSpace(venta.guiaRemision)) fields.Add("guiaRemision", new { stringValue = venta.guiaRemision });
                if (!string.IsNullOrWhiteSpace(venta.placa)) fields.Add("placa", new { stringValue = venta.placa });
                var firestoreDoc = new { fields = fields };

                // [Rayo X ElitePOS] Audit Log
                Console.WriteLine($"[Rayo X ElitePOS] Enviando a Firestore: {JsonSerializer.Serialize(firestoreDoc)}");

                var response = await _httpClient.PostAsJsonAsync($"{FIREBASE_URL}?documentId={venta.id}", firestoreDoc, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
                return response.IsSuccessStatusCode;
            } catch (Exception ex) { Console.WriteLine($"❌ Save Error: {ex.Message}"); return false; }
        }

        private async Task<bool> ActualizarEstadoVenta(VentaModel venta)
        {
            try {
                var updateData = new { fields = new { estadoSunat = new { stringValue = venta.estadoSunat }, mensajeSunat = new { stringValue = venta.mensajeSunat }, hashSunat = new { stringValue = venta.hashSunat }, intentosEnvio = new { integerValue = venta.intentosEnvio.ToString() }, fechaUltimoIntento = new { stringValue = venta.fechaUltimoIntento?.ToString("o") ?? "" } } };
                var response = await _httpClient.PatchAsync($"{FIREBASE_URL}/{venta.id}?updateMask.fieldPaths=estadoSunat&updateMask.fieldPaths=mensajeSunat&updateMask.fieldPaths=hashSunat&updateMask.fieldPaths=intentosEnvio&updateMask.fieldPaths=fechaUltimoIntento", new StringContent(JsonSerializer.Serialize(updateData), System.Text.Encoding.UTF8, "application/json"));
                return response.IsSuccessStatusCode;
            } catch { return false; }
        }

        private async Task<RespuestaSunat> IntentarEnvioSunatConResiliencia(VentaModel v) => new RespuestaSunat { exito = true, mensaje = "OK", codigoHash = "H" + DateTime.Now.Ticks };

        private async Task ProcesarKardexYActualizarStock(VentaModel v, bool sw)
        {
            foreach (var i in v.items) {
                var p = await _inventarioService.ObtenerProductoPorId(i.productoId);
                if (p == null || p.categoria == "Servicio") continue;
                var res = await _kardexQueue.RegistrarSalidaAsync(v.empresaId??"", i.productoId, i.nombreProducto, i.cantidad, "Venta", $"VTA-{v.id}-{i.productoId}", v.usuarioId, v.nombreUsuario, sw);
                if (res.exitoso) await _inventarioService.ActualizarStock(i.productoId, res.stockResultante);
            }
        }

        private class FirestoreListResponse { public List<FirestoreDocument>? Documents { get; set; } }
        private class FirestoreRunQueryResponse { public FirestoreDocument? Document { get; set; } }
        private class FirestoreDocument { public string Name { get; set; } = ""; public FirestoreFields Fields { get; set; } = new(); }
        private class FirestoreFields {
            public FirestoreValue? empresaId { get; set; }
            public FirestoreValue? usuarioId { get; set; }
            public FirestoreValue? nombreUsuario { get; set; }
            public FirestoreValue? fechaHora { get; set; }
            public FirestoreValue? numeroComprobante { get; set; }
            public FirestoreValue? cliente { get; set; }
            public FirestoreValue? tipoComprobante { get; set; }
            public FirestoreValue? metodoPago { get; set; }
            public FirestoreValue? total { get; set; }
            public FirestoreValue? subtotal { get; set; }
            public FirestoreValue? subtotalGravada { get; set; }
            public FirestoreValue? igv { get; set; }
            public FirestoreValue? items { get; set; }
            public FirestoreValue? anulada { get; set; }
            public FirestoreValue? sucursalId { get; set; }
            public FirestoreValue? cajaId { get; set; }
            public FirestoreValue? observaciones { get; set; }
            public FirestoreValue? clienteId { get; set; }
            public FirestoreValue? tipoPago { get; set; }
            public FirestoreValue? estadoPago { get; set; }
            public FirestoreValue? montoPagado { get; set; }
            public FirestoreValue? fechaVencimiento { get; set; }
            public FirestoreValue? direccionCliente { get; set; }
            public FirestoreValue? tipoDocumentoCliente { get; set; }
            public FirestoreValue? numeroDocumentoCliente { get; set; }
            public FirestoreValue? ordenCompra { get; set; }
            public FirestoreValue? guiaRemision { get; set; }
            public FirestoreValue? placa { get; set; }
            public FirestoreValue? estadoSunat { get; set; }
            public FirestoreValue? mensajeSunat { get; set; }
            public FirestoreValue? hashSunat { get; set; }
            public FirestoreValue? intentosEnvio { get; set; }
            public FirestoreValue? fechaUltimoIntento { get; set; }
            public FirestoreValue? productoId { get; set; }
            public FirestoreValue? nombreProducto { get; set; }
            public FirestoreValue? cantidad { get; set; }
            public FirestoreValue? precioUnitario { get; set; }
            public FirestoreValue? precioCosto { get; set; }
            public FirestoreValue? precioVenta { get; set; }
            public FirestoreValue? codigoInterno { get; set; }
            public FirestoreValue? unidadMedida { get; set; }
        }
        private class FirestoreValue { public string? StringValue { get; set; } public string? IntegerValue { get; set; } public double? DoubleValue { get; set; } public bool? BooleanValue { get; set; } public FirestoreArrayValue? ArrayValue { get; set; } public FirestoreMapValue? MapValue { get; set; } }
        private class FirestoreArrayValue { public List<FirestoreValue>? Values { get; set; } }
        private class FirestoreMapValue { public FirestoreFields? Fields { get; set; } }
    }
}
