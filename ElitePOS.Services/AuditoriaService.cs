using ElitePOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElitePOS.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly HttpClient _httpClient;
        private const string PROJECT_ID = "TU_FIREBASE_PROJECT_ID";
        private const string COLLECTION = "auditoria";
        private readonly string _baseUrl;

        public AuditoriaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = $"https://firestore.googleapis.com/v1/projects/{PROJECT_ID}/databases/(default)/documents/{COLLECTION}";
        }

        public async Task RegistrarAccionAsync(AuditoriaModel accion)
        {
            try
            {
                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["fechaHora"] = new { timestampValue = accion.fechaHora.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        ["usuario"] = new { stringValue = accion.usuario ?? "" },
                        ["modulo"] = new { stringValue = accion.modulo ?? "" },
                        ["accion"] = new { stringValue = accion.accion ?? "" },
                        ["detalle"] = new { stringValue = accion.detalle ?? "" },
                        ["ip"] = new { stringValue = accion.ip ?? "" },
                        ["empresaId"] = new { stringValue = accion.empresaId ?? "" }
                    }
                };

                var url = $"{_baseUrl}?documentId={accion.id}";
                var response = await _httpClient.PostAsJsonAsync(url, firestoreDoc);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al registrar auditoria: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exceción al registrar auditoria: {ex.Message}");
            }
        }

        public async Task<List<AuditoriaModel>> ObtenerAuditoriaAsync(DateTime desde, DateTime hasta, string? usuario = null, string? modulo = null)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{PROJECT_ID}/databases/(default)/documents:runQuery";
                
                var queryBody = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = COLLECTION } },
                        where = new
                        {
                            compositeFilter = new
                            {
                                op = "AND",
                                filters = new List<object>
                                {
                                    new { fieldFilter = new { field = new { fieldPath = "fechaHora" }, op = "GREATER_THAN_OR_EQUAL", value = new { timestampValue = desde.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } } },
                                    new { fieldFilter = new { field = new { fieldPath = "fechaHora" }, op = "LESS_THAN_OR_EQUAL", value = new { timestampValue = hasta.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } } }
                                }
                            }
                        },
                        orderBy = new[] { new { field = new { fieldPath = "fechaHora" }, direction = "DESCENDING" } }
                    }
                };

                // Add optional filters
                if (!string.IsNullOrEmpty(usuario))
                {
                    ((List<object>)queryBody.structuredQuery.where.compositeFilter.filters).Add(new { fieldFilter = new { field = new { fieldPath = "usuario" }, op = "EQUAL", value = new { stringValue = usuario } } });
                }
                if (!string.IsNullOrEmpty(modulo))
                {
                    ((List<object>)queryBody.structuredQuery.where.compositeFilter.filters).Add(new { fieldFilter = new { field = new { fieldPath = "modulo" }, op = "EQUAL", value = new { stringValue = modulo } } });
                }

                var response = await _httpClient.PostAsJsonAsync(url, queryBody);
                if (!response.IsSuccessStatusCode) return new List<AuditoriaModel>();

                var json = await response.Content.ReadAsStringAsync();
                var results = new List<AuditoriaModel>();
                
                using var doc = JsonDocument.Parse(json);
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("document", out var document))
                    {
                        if (document.TryGetProperty("fields", out var fields))
                        {
                            results.Add(MapToAuditoriaModel(document.GetProperty("name").GetString() ?? "", fields));
                        }
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener auditoria: {ex.Message}");
                return new List<AuditoriaModel>();
            }
        }

        private AuditoriaModel MapToAuditoriaModel(string name, JsonElement fields)
        {
            return new AuditoriaModel
            {
                id = name.Split('/').Last(),
                fechaHora = fields.TryGetProperty("fechaHora", out var fh) ? DateTime.Parse(fh.GetProperty("timestampValue").GetString() ?? DateTime.Now.ToString()) : 
                           (fields.TryGetProperty("FechaHora", out var fhP) ? DateTime.Parse(fhP.GetProperty("timestampValue").GetString() ?? DateTime.Now.ToString()) : DateTime.Now),
                usuario = fields.TryGetProperty("usuario", out var u) ? u.GetProperty("stringValue").GetString() ?? "" : 
                         (fields.TryGetProperty("Usuario", out var uP) ? uP.GetProperty("stringValue").GetString() ?? "" : ""),
                modulo = fields.TryGetProperty("modulo", out var m) ? m.GetProperty("stringValue").GetString() ?? "" : 
                        (fields.TryGetProperty("Modulo", out var mP) ? mP.GetProperty("stringValue").GetString() ?? "" : ""),
                accion = fields.TryGetProperty("accion", out var a) ? a.GetProperty("stringValue").GetString() ?? "" : 
                        (fields.TryGetProperty("Accion", out var aP) ? aP.GetProperty("stringValue").GetString() ?? "" : ""),
                detalle = fields.TryGetProperty("detalle", out var d) ? d.GetProperty("stringValue").GetString() ?? "" : 
                         (fields.TryGetProperty("Detalle", out var dP) ? dP.GetProperty("stringValue").GetString() ?? "" : ""),
                ip = fields.TryGetProperty("ip", out var i) ? i.GetProperty("stringValue").GetString() ?? "" : 
                    (fields.TryGetProperty("IP", out var iP) ? iP.GetProperty("stringValue").GetString() ?? "" : ""),
                empresaId = fields.TryGetProperty("empresaId", out var e) ? e.GetProperty("stringValue").GetString() ?? "" : 
                           (fields.TryGetProperty("EmpresaId", out var eP) ? eP.GetProperty("stringValue").GetString() ?? "" : "")
            };
        }
    }
}
