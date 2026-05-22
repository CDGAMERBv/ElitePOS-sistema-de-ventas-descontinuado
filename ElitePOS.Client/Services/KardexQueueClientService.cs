using ElitePOS.Services;
using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;

namespace ElitePOS.Client.Services
{
    public class KardexQueueClientService : IKardexQueueService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<KardexQueueClientService> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public KardexQueueClientService(HttpClient httpClient, ILogger<KardexQueueClientService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<RegistroKardexResult> RegistrarSalidaAsync(string empresaId, string productoId, string productoNombre, int cantidad, string concepto, string? numeroOperacion = null, string? usuarioId = null, string? usuarioNombre = null, bool permitirVentaSinStock = false)
        {
            return await EjecutarSimulado(empresaId, productoId, productoNombre, 0, cantidad, concepto, numeroOperacion, usuarioId, usuarioNombre, permitirVentaSinStock);
        }

        public async Task<RegistroKardexResult> RegistrarEntradaAsync(string empresaId, string productoId, string productoNombre, int cantidad, string concepto, string? numeroOperacion = null, string? usuarioId = null, string? usuarioNombre = null)
        {
            return await EjecutarSimulado(empresaId, productoId, productoNombre, cantidad, 0, concepto, numeroOperacion, usuarioId, usuarioNombre);
        }

        public async Task<RegistroKardexResult> RegistrarAjusteAsync(string empresaId, string productoId, string productoNombre, int cantidadAjuste, string tipoAjuste, string concepto = "Ajuste Manual", string? usuarioId = null, string? usuarioNombre = null)
        {
            return await EjecutarSimulado(empresaId, productoId, productoNombre, tipoAjuste == "Entrada" ? cantidadAjuste : 0, tipoAjuste == "Salida" ? cantidadAjuste : 0, concepto, null, usuarioId, usuarioNombre);
        }

        private async Task<RegistroKardexResult> EjecutarSimulado(string empresaId, string productoId, string productoNombre, int entrada, int salida, string concepto, string? numeroOperacion, string? usuarioId, string? usuarioNombre, bool permitirVentaSinStock = false)
        {
            await _lock.WaitAsync();
            try
            {
                var (stockAnterior, error) = await ObtenerStockActual(empresaId, productoId);
                if (error != null)
                {
                    return new RegistroKardexResult { exitoso = false, mensaje = "Error al leer stock en cliente.", error = error };
                }

                if (salida > 0 && stockAnterior < salida && !permitirVentaSinStock)
                {
                    return new RegistroKardexResult { exitoso = false, mensaje = $"Stock insuficiente ({stockAnterior}).", stockResultante = stockAnterior, error = "STOCK_INSUFICIENTE" };
                }

                int stockResultante = stockAnterior + entrada - salida;
                string numOp = numeroOperacion ?? $"OP-{Guid.NewGuid():N}";

                return new RegistroKardexResult 
                { 
                    exitoso = true, 
                    mensaje = "Movimiento validado en cliente.",
                    stockResultante = stockResultante,
                    numeroOperacion = numOp
                };
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<(int stock, string? error)> ObtenerStockActual(string empresaId, string productoId)
        {
            try
            {
                var url = "https://firestore.googleapis.com/v1/projects/TU_FIREBASE_PROJECT_ID/databases/(default)/documents:runQuery";
                var queryBody = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "kardex_pro" } },
                        where = new
                        {
                            compositeFilter = new { op = "AND", filters = new object[] {
                                new { fieldFilter = new { field = new { fieldPath = "empresaId" }, op = "EQUAL", value = new { stringValue = empresaId } } },
                                new { fieldFilter = new { field = new { fieldPath = "productoId" }, op = "EQUAL", value = new { stringValue = productoId } } }
                            }}
                        },
                        orderBy = new[] { new { field = new { fieldPath = "fechaOperacion" }, direction = "DESCENDING" } },
                        limit = 1
                    }
                };

                var response = await _httpClient.PostAsync(url, JsonContent.Create(queryBody));
                if (!response.IsSuccessStatusCode) return (0, "Error HTTP");

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("document", out var document) &&
                            document.TryGetProperty("fields", out var fields) &&
                            fields.TryGetProperty("stockActual", out var stockField) &&
                            stockField.TryGetProperty("integerValue", out var stockValue))
                        {
                            return (int.Parse(stockValue.GetString()!), null);
                        }
                    }
                }
                return (0, null);
            }
            catch (Exception ex) { return (0, ex.Message); }
        }
    }
}
