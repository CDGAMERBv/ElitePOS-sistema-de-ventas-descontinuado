using ElitePOS.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ElitePOS.Services
{
    public class KardexService : IKardexService
    {
        private readonly HttpClient _httpClient;
        private readonly ISesionService _sesionService;
        private readonly IInventarioService _inventarioService;
        private readonly IConfiguration _config;
        private string ProjectId => _config["Firestore:ProjectId"] ?? "TU_FIREBASE_PROJECT_ID";
        private string BaseUrl => $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/kardex_v2";

        public KardexService(HttpClient httpClient, ISesionService sesionService, IInventarioService inventarioService, IConfiguration config)
        {
            _httpClient = httpClient;
            _sesionService = sesionService;
            _inventarioService = inventarioService;
            _config = config;
        }

        public async Task<List<MovimientoKardexProfesional>> ObtenerKardexGeneral(int limit, DateTime? desde = null, DateTime? hasta = null)
        {
            var url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
            
            var queryLegacy = new { structuredQuery = new { from = new[] { new { collectionId = "kardex_v2", allDescendants = true } }, orderBy = new[] { new { field = new { fieldPath = "fecha" }, direction = "DESCENDING" } }, limit = limit } };
            var queryProfessional = new { structuredQuery = new { from = new[] { new { collectionId = "Kardex", allDescendants = true } }, orderBy = new[] { new { field = new { fieldPath = "fecha" }, direction = "DESCENDING" } }, limit = limit } };

            var resultado = new List<MovimientoKardexProfesional>();

            // Fetch Legacy (kardex_v2)
            var resLegacy = await _httpClient.PostAsJsonAsync(url, queryLegacy);
            if (resLegacy.IsSuccessStatusCode) {
                var json = await resLegacy.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array) {
                    foreach (var item in jsonDoc.RootElement.EnumerateArray()) {
                        if (item.TryGetProperty("document", out var doc) && doc.TryGetProperty("fields", out var fields)) {
                            var movLegacy = MapFieldsToMovimientoKardexModel(fields);
                            resultado.Add(new MovimientoKardexProfesional {
                                id = movLegacy.id, productoId = movLegacy.productoId, productoNombre = movLegacy.productoNombre,
                                fecha = movLegacy.fecha, tipoMovimiento = movLegacy.tipoMovimiento, concepto = movLegacy.concepto,
                                cantidad = (int)movLegacy.cantidad, saldoAnterior = (int)movLegacy.saldoAnterior, saldoActual = (int)movLegacy.saldoActual,
                                usuarioNombre = movLegacy.usuarioNombre
                            });
                        }
                    }
                }
            }

            // Fetch Professional (Kardex)
            var resProf = await _httpClient.PostAsJsonAsync(url, queryProfessional);
            if (resProf.IsSuccessStatusCode) {
                var json = await resProf.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(json);
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array) {
                    foreach (var item in jsonDoc.RootElement.EnumerateArray()) {
                        if (item.TryGetProperty("document", out var doc) && doc.TryGetProperty("fields", out var fields)) {
                            var movProf = MapFieldsToMovimientoProfesional(fields);
                            if (movProf != null) resultado.Add(movProf);
                        }
                    }
                }
            }
            
            var query = resultado.AsEnumerable();
            if (desde.HasValue) query = query.Where(m => m.fecha >= desde.Value);
            if (hasta.HasValue) query = query.Where(m => m.fecha <= hasta.Value);
            
            return query.OrderByDescending(m => m.fecha).Take(limit).ToList();
        }

        public async Task<List<MovimientoKardexModel>> ObtenerKardexProducto(string productoId, DateTime? desde = null, DateTime? hasta = null)
        {
            if (string.IsNullOrWhiteSpace(productoId)) return new List<MovimientoKardexModel>();
            var url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
            
            var filters = new List<object> { new { fieldFilter = new { field = new { fieldPath = "productoId" }, op = "EQUAL", value = new { stringValue = productoId } } } };
            if (desde.HasValue) filters.Add(new { fieldFilter = new { field = new { fieldPath = "fecha" }, op = "GREATER_THAN_OR_EQUAL", value = new { timestampValue = desde.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } } });
            if (hasta.HasValue) filters.Add(new { fieldFilter = new { field = new { fieldPath = "fecha" }, op = "LESS_THAN_OR_EQUAL", value = new { timestampValue = hasta.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } } });
            
            var queryBody = new { structuredQuery = new { 
                from = new[] { new { collectionId = "kardex_v2", allDescendants = true } }, 
                where = new { compositeFilter = new { op = "AND", filters = filters } },
                orderBy = new[] { new { field = new { fieldPath = "fecha" }, direction = "DESCENDING" } }, 
                limit = 500 
            } };

            var response = await _httpClient.PostAsJsonAsync(url, queryBody);
            var resultado = new List<MovimientoKardexModel>();
            if (!response.IsSuccessStatusCode) return resultado;

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(jsonString);
            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array) {
                foreach (var item in jsonDoc.RootElement.EnumerateArray()) {
                    if (item.TryGetProperty("document", out var doc) && doc.TryGetProperty("fields", out var fields)) {
                        resultado.Add(MapFieldsToMovimientoKardexModel(fields));
                    }
                }
            }
            return resultado.OrderByDescending(m => m.fecha).ToList();
        }

        private MovimientoKardexModel MapFieldsToMovimientoKardexModel(JsonElement fields)
        {
            var mov = new MovimientoKardexModel();
            mov.id = GetSafe(fields, "id");
            mov.productoId = GetSafe(fields, "productoId");
            mov.productoNombre = GetSafe(fields, "productoNombre");
            mov.fecha = GetSafeDateTime(fields, "fecha");
            mov.tipoMovimiento = GetSafe(fields, "tipoMovimiento");
            mov.concepto = GetSafe(fields, "concepto");
            mov.cantidad = GetSafeDecimal(fields, "cantidad");
            mov.saldoAnterior = GetSafeDecimal(fields, "saldoAnterior");
            mov.saldoActual = GetSafeDecimal(fields, "saldoActual");
            mov.usuarioNombre = GetSafe(fields, "usuarioNombre");
            mov.usuarioId = GetSafe(fields, "usuarioId");
            return mov;
        }

        public async Task<bool> RegistrarMovimiento(MovimientoKardexModel mov) 
        { 
            try {
                if (string.IsNullOrEmpty(mov.id)) mov.id = Guid.NewGuid().ToString();
                
                // Obtener stock actual para calcular saldos
                var producto = await _inventarioService.ObtenerProductoPorId(mov.productoId);
                if (producto == null) return false;

                mov.saldoAnterior = producto.stock;
                if (mov.tipoMovimiento.ToUpper() == "ENTRADA") mov.saldoActual = mov.saldoAnterior + mov.cantidad;
                else mov.saldoActual = mov.saldoAnterior - mov.cantidad;

                // Actualizar stock en producto
                await _inventarioService.ActualizarStock(mov.productoId, (int)mov.saldoActual);

                var firestoreDoc = new {
                    fields = new Dictionary<string, object> {
                        ["id"] = new { stringValue = mov.id },
                        ["productoId"] = new { stringValue = mov.productoId },
                        ["productoNombre"] = new { stringValue = mov.productoNombre },
                        ["fecha"] = new { timestampValue = mov.fecha.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        ["tipoMovimiento"] = new { stringValue = mov.tipoMovimiento },
                        ["concepto"] = new { stringValue = mov.concepto },
                        ["cantidad"] = new { doubleValue = (double)mov.cantidad },
                        ["saldoAnterior"] = new { doubleValue = (double)mov.saldoAnterior },
                        ["saldoActual"] = new { doubleValue = (double)mov.saldoActual },
                        ["usuarioId"] = new { stringValue = mov.usuarioId },
                        ["usuarioNombre"] = new { stringValue = mov.usuarioNombre }
                    }
                };

                var url = $"{BaseUrl}?documentId={mov.id}";
                var res = await _httpClient.PostAsJsonAsync(url, firestoreDoc);
                return res.IsSuccessStatusCode;
            } catch { return false; }
        }

        public async Task<List<MovimientoKardexModel>> ObtenerHistorial(string productoId) 
        { 
            return await ObtenerKardexProducto(productoId);
        }

        public async Task RegistrarMovimientoProfesional(MovimientoKardexProfesional movimiento)
        {
            try {
                var url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/Productos/{movimiento.productoId}/Kardex/{movimiento.id}";
                var firestoreDoc = new {
                    fields = new Dictionary<string, object> {
                        ["id"] = new { stringValue = movimiento.id },
                        ["productoId"] = new { stringValue = movimiento.productoId },
                        ["productoNombre"] = new { stringValue = movimiento.productoNombre },
                        ["fecha"] = new { timestampValue = movimiento.fecha.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        ["tipoMovimiento"] = new { stringValue = movimiento.tipoMovimiento },
                        ["concepto"] = new { stringValue = movimiento.concepto },
                        ["cantidad"] = new { integerValue = movimiento.cantidad.ToString() },
                        ["saldoAnterior"] = new { integerValue = movimiento.saldoAnterior.ToString() },
                        ["saldoActual"] = new { integerValue = movimiento.saldoActual.ToString() },
                        ["usuarioId"] = new { stringValue = movimiento.usuarioId },
                        ["usuarioNombre"] = new { stringValue = movimiento.usuarioNombre },
                        ["costoUnitario"] = new { doubleValue = (double)movimiento.costoUnitario },
                        ["costoTotalMovimiento"] = new { doubleValue = (double)movimiento.costoTotalMovimiento },
                        ["costoPromedioAnterior"] = new { doubleValue = (double)movimiento.costoPromedioAnterior },
                        ["costoPromedioNuevo"] = new { doubleValue = (double)movimiento.costoPromedioNuevo }
                    }
                };
                await _httpClient.PatchAsJsonAsync(url, firestoreDoc);
            } catch { }
        }

        public async Task<List<MovimientoKardexProfesional>> ObtenerHistorialProfesional(string productoId, DateTime? desde = null, DateTime? hasta = null)
        {
            var movimientos = new List<MovimientoKardexProfesional>();
            var urlQuery = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
            
            var filters = new List<object> { new { fieldFilter = new { field = new { fieldPath = "productoId" }, op = "EQUAL", value = new { stringValue = productoId } } } };
            if (desde.HasValue) filters.Add(new { fieldFilter = new { field = new { fieldPath = "fecha" }, op = "GREATER_THAN_OR_EQUAL", value = new { timestampValue = desde.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } } });
            if (hasta.HasValue) filters.Add(new { fieldFilter = new { field = new { fieldPath = "fecha" }, op = "LESS_THAN_OR_EQUAL", value = new { timestampValue = hasta.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } } });

            var queryProf = new { structuredQuery = new { 
                from = new[] { new { collectionId = "Kardex" } }, 
                where = new { compositeFilter = new { op = "AND", filters = filters } },
                orderBy = new[] { new { field = new { fieldPath = "fecha" }, direction = "DESCENDING" } } 
            } };

            var resProf = await _httpClient.PostAsJsonAsync(urlQuery, queryProf);
            if (resProf.IsSuccessStatusCode) {
                var jsonDoc = JsonDocument.Parse(await resProf.Content.ReadAsStringAsync());
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array) {
                    foreach (var item in jsonDoc.RootElement.EnumerateArray()) {
                        if (item.TryGetProperty("document", out var doc) && doc.TryGetProperty("fields", out var fields)) {
                            var mov = MapFieldsToMovimientoProfesional(fields);
                            if (mov != null) movimientos.Add(mov);
                        }
                    }
                }
            }
            return movimientos.OrderByDescending(m => m.fecha).ToList();
        }

        private MovimientoKardexProfesional? MapFieldsToMovimientoProfesional(JsonElement fields)
        {
            return new MovimientoKardexProfesional {
                id = GetSafe(fields, "id"), productoId = GetSafe(fields, "productoId"),
                productoNombre = GetSafe(fields, "productoNombre"), fecha = GetSafeDateTime(fields, "fecha"),
                tipoMovimiento = GetSafe(fields, "tipoMovimiento"), concepto = GetSafe(fields, "concepto"),
                cantidad = GetSafeInt(fields, "cantidad"), saldoAnterior = GetSafeInt(fields, "saldoAnterior"),
                saldoActual = GetSafeInt(fields, "saldoActual"), usuarioId = GetSafe(fields, "usuarioId"),
                usuarioNombre = GetSafe(fields, "usuarioNombre"), costoUnitario = GetSafeDecimal(fields, "costoUnitario"),
                costoPromedioNuevo = GetSafeDecimal(fields, "costoPromedioNuevo")
            };
        }

        private string GetSafe(JsonElement fields, string key) {
            if (fields.TryGetProperty(key, out var p) && p.TryGetProperty("stringValue", out var v)) return v.GetString() ?? "";
            var pk = char.ToUpper(key[0]) + key.Substring(1);
            if (fields.TryGetProperty(pk, out var p2) && p2.TryGetProperty("stringValue", out var v2)) return v2.GetString() ?? "";
            return "";
        }

        private int GetSafeInt(JsonElement fields, string key) {
            if (fields.TryGetProperty(key, out var p)) {
                if (p.TryGetProperty("integerValue", out var v) && int.TryParse(v.GetString(), out var r)) return r;
                if (p.TryGetProperty("doubleValue", out var v2)) return (int)v2.GetDouble();
            }
            var pk = char.ToUpper(key[0]) + key.Substring(1);
            if (fields.TryGetProperty(pk, out var p2)) {
                if (p2.TryGetProperty("integerValue", out var v3) && int.TryParse(v3.GetString(), out var r2)) return r2;
                if (p2.TryGetProperty("doubleValue", out var v4)) return (int)v4.GetDouble();
            }
            return 0;
        }

        private decimal GetSafeDecimal(JsonElement fields, string key) {
            if (fields.TryGetProperty(key, out var p)) {
                if (p.TryGetProperty("doubleValue", out var v)) return (decimal)v.GetDouble();
                if (p.TryGetProperty("integerValue", out var v2) && decimal.TryParse(v2.GetString(), out var r)) return r;
            }
            var pk = char.ToUpper(key[0]) + key.Substring(1);
            if (fields.TryGetProperty(pk, out var p2)) {
                if (p2.TryGetProperty("doubleValue", out var v3)) return (decimal)v3.GetDouble();
                if (p2.TryGetProperty("integerValue", out var v4) && decimal.TryParse(v4.GetString(), out var r2)) return r2;
            }
            return 0;
        }

        private DateTime GetSafeDateTime(JsonElement fields, string key) {
            if (fields.TryGetProperty(key, out var p) && p.TryGetProperty("timestampValue", out var v) && DateTime.TryParse(v.GetString(), out var r)) return r;
            var pk = char.ToUpper(key[0]) + key.Substring(1);
            if (fields.TryGetProperty(pk, out var p2) && p2.TryGetProperty("timestampValue", out var v2) && DateTime.TryParse(v2.GetString(), out var r2)) return r2;
            return DateTime.Now;
        }

        public async Task<bool> RegistrarEntradaCompra(string p, int c, decimal cu, string pi="", string pn="", string n="", DateTime? f=null, string d="") { return true; }
    }
}
