using ElitePOS.Shared.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ElitePOS.Services
{
    /// <summary>
    /// Implementación del servicio de cola serializada para Kardex.
    /// 
    /// ARQUITECTURA:
    ///   - Un SemaphoreSlim(1,1) por EmpresaId → garantiza serialización POR empresa.
    ///   - Las dependencias (HttpClient) se inyectan directamente ya que el servicio
    ///     ha sido cambiado a Scoped para respetar la inyección de dependencias en Blazor WASM.
    ///   - Como el Diccionario de locks es un campo de instancia, en Blazor WASM (donde Scoped
    ///     vive durante toda la vida de la app) funciona perfectamente como un Singleton local.
    /// </summary>
    public class KardexQueueService : IKardexQueueService
    {
        // NOTA IMPORTANTE PARA EL SERVIDOR:
        // Si este servicio corre en un backend real con múltiples usuarios concurrentes (API)
        // en Scoped y se instancia por petición, este diccionario se crearía nuevo por petición,
        // perdiendo la sincronización. Lo hacemos estático para que el bloqueo
        // perdure entre scopes de peticiones si se usa en un backend real.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<KardexQueueService> _logger;

        private string ProjectId => _config["Firestore:ProjectId"] ?? "TU_FIREBASE_PROJECT_ID";
        
        private string KardexUrl => 
            $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents/kardex_pro";

        public KardexQueueService(
            HttpClient httpClient, 
            IConfiguration config,
            ILogger<KardexQueueService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<RegistroKardexResult> RegistrarSalidaAsync(
            string empresaId, string productoId, string productoNombre,
            int cantidad, string concepto,
            string? numeroOperacion = null, string? usuarioId = null, string? usuarioNombre = null,
            bool permitirVentaSinStock = false)
        {
            return await EjecutarMovimientoSerializado(
                empresaId, productoId, productoNombre,
                entrada: 0,
                salida: cantidad,
                concepto: concepto,
                numeroOperacion: numeroOperacion,
                usuarioId: usuarioId,
                usuarioNombre: usuarioNombre,
                permitirVentaSinStock: permitirVentaSinStock);
        }

        public async Task<RegistroKardexResult> RegistrarEntradaAsync(
            string empresaId, string productoId, string productoNombre,
            int cantidad, string concepto,
            string? numeroOperacion = null, string? usuarioId = null, string? usuarioNombre = null)
        {
            return await EjecutarMovimientoSerializado(
                empresaId, productoId, productoNombre,
                entrada: cantidad,
                salida: 0,
                concepto: concepto,
                numeroOperacion: numeroOperacion,
                usuarioId: usuarioId,
                usuarioNombre: usuarioNombre);
        }

        public async Task<RegistroKardexResult> RegistrarAjusteAsync(
            string empresaId, string productoId, string productoNombre,
            int cantidadAjuste, string tipoAjuste, string concepto = "Ajuste Manual",
            string? usuarioId = null, string? usuarioNombre = null)
        {
            return await EjecutarMovimientoSerializado(
                empresaId, productoId, productoNombre,
                entrada: tipoAjuste == "Entrada" ? cantidadAjuste : 0,
                salida: tipoAjuste == "Salida" ? cantidadAjuste : 0,
                concepto: concepto,
                numeroOperacion: null,
                usuarioId: usuarioId,
                usuarioNombre: usuarioNombre);
        }

        private async Task<RegistroKardexResult> EjecutarMovimientoSerializado(
            string empresaId, string productoId, string productoNombre,
            int entrada, int salida, string concepto,
            string? numeroOperacion, string? usuarioId, string? usuarioNombre,
            bool permitirVentaSinStock = false)
        {
            if (string.IsNullOrWhiteSpace(empresaId) || string.IsNullOrWhiteSpace(productoId))
            {
                return new RegistroKardexResult 
                { 
                    exitoso = false, 
                    mensaje = "EmpresaId y ProductoId son requeridos.", 
                    error = "INVALID_ARGS" 
                };
            }

            // 1. Obtener (o crear) el semáforo exclusivo de esta empresa
            var semaforo = _locks.GetOrAdd(empresaId, _ => new SemaphoreSlim(1, 1));

            // 2. Intentar adquirir el semáforo (máx 10 segundos de espera)
            bool adquirido = await semaforo.WaitAsync(TimeSpan.FromSeconds(10));
            if (!adquirido)
            {
                _logger.LogWarning("⚠️ Timeout al procesar Kardex para empresa {EmpresaId}, producto {ProductoId}", 
                    empresaId, productoId);
                return new RegistroKardexResult 
                { 
                    exitoso = false, 
                    mensaje = "El sistema está ocupado procesando otras transacciones. Intente de nuevo.", 
                    error = "TIMEOUT" 
                };
            }

            try
            {
                _logger.LogInformation("🔒 Semáforo adquirido para empresa {EmpresaId}", empresaId);

                // IDEMPOTENCIA
                if (!string.IsNullOrEmpty(numeroOperacion))
                {
                    var yaExiste = await VerificarOperacionExistente(empresaId, productoId, numeroOperacion);
                    if (yaExiste)
                    {
                        return new RegistroKardexResult 
                        { 
                            exitoso = true, 
                            mensaje = "Operación ya registrada anteriormente.", 
                            numeroOperacion = numeroOperacion 
                        };
                    }
                }

                // 3. Leer stock actual
                var (stockAnterior, errorLectura) = await ObtenerStockActual(empresaId, productoId);
                if (errorLectura != null)
                {
                    return new RegistroKardexResult { exitoso = false, mensaje = "Error al leer stock actual.", error = errorLectura };
                }

                // 4. Validar stock
                if (salida > 0 && stockAnterior < salida && !permitirVentaSinStock)
                {
                    return new RegistroKardexResult 
                    { 
                        exitoso = false, 
                        mensaje = $"Stock insuficiente. Stock actual: {stockAnterior}, requerido: {salida}.", 
                        stockResultante = stockAnterior,
                        error = "STOCK_INSUFICIENTE" 
                    };
                }

                int stockResultante = stockAnterior + entrada - salida;
                var numOp = numeroOperacion ?? $"OP-{Guid.NewGuid():N}";
                var idDocumento = $"{empresaId}_{productoId}_{numOp}_{DateTime.UtcNow:yyyyMMddHHmmssff}";

                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["NumeroOperacion"]   = new { stringValue = numOp },
                        ["FechaOperacion"]    = new { timestampValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        ["UsuarioId"]         = new { stringValue = string.IsNullOrWhiteSpace(usuarioId) ? "SISTEMA" : usuarioId },
                        ["UsuarioNombre"]     = new { stringValue = string.IsNullOrWhiteSpace(usuarioNombre) ? "Sistema Automático" : usuarioNombre },
                        ["ProductoId"]        = new { stringValue = productoId },
                        ["ProductoNombre"]    = new { stringValue = productoNombre },
                        ["Concepto"]          = new { stringValue = concepto },
                        ["StockAnterior"]     = new { integerValue = stockAnterior.ToString() },
                        ["Entrada"]           = new { integerValue = entrada.ToString() },
                        ["Salida"]            = new { integerValue = salida.ToString() },
                        ["StockActual"]       = new { integerValue = stockResultante.ToString() },
                        ["EmpresaId"]         = new { stringValue = empresaId },
                        ["FechaRegistro"]     = new { timestampValue = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                        ["Inmutable"]         = new { booleanValue = true },
                    }
                };

                var url = $"{KardexUrl}?documentId={idDocumento}";
                var response = await _httpClient.PostAsync(url, JsonContent.Create(firestoreDoc));

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return new RegistroKardexResult { exitoso = false, mensaje = "Error en base de datos al guardar Kardex.", error = errorBody };
                }

                return new RegistroKardexResult 
                { 
                    exitoso = true, 
                    mensaje = "Movimiento registrado correctamente.", 
                    stockResultante = stockResultante, 
                    numeroOperacion = numOp 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Excepción en KardexQueueService para empresa {EmpresaId}", empresaId);
                return new RegistroKardexResult { exitoso = false, mensaje = "Error interno.", error = ex.Message };
            }
            finally
            {
                semaforo.Release();
                _logger.LogInformation("🔓 Semáforo liberado para empresa {EmpresaId}", empresaId);
            }
        }

        private async Task<(int stock, string? error)> ObtenerStockActual(string empresaId, string productoId)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
                var queryBody = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "kardex_pro", allDescendants = false } },
                        where = new
                        {
                            compositeFilter = new
                            {
                                op = "AND",
                                filters = new object[]
                                {
                                    new { fieldFilter = new { field = new { fieldPath = "EmpresaId" }, op = "EQUAL", value = new { stringValue = empresaId } } },
                                    new { fieldFilter = new { field = new { fieldPath = "ProductoId" }, op = "EQUAL", value = new { stringValue = productoId } } }
                                }
                            }
                        },
                        orderBy = new[] { new { field = new { fieldPath = "FechaOperacion" }, direction = "DESCENDING" } },
                        limit = 1
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(queryBody), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                    return (0, "Error HTTP al consultar stock actual.");

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("document", out var document) &&
                            document.TryGetProperty("fields", out var fields) &&
                            fields.TryGetProperty("StockActual", out var stockField) &&
                            stockField.TryGetProperty("integerValue", out var stockValue))
                        {
                            if (int.TryParse(stockValue.GetString(), out var stock))
                                return (stock, null);
                        }
                    }
                }

                return (0, null);
            }
            catch (Exception ex)
            {
                return (0, ex.Message);
            }
        }

        private async Task<bool> VerificarOperacionExistente(string empresaId, string productoId, string numeroOperacion)
        {
            try
            {
                var url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
                var queryBody = new
                {
                    structuredQuery = new
                    {
                        from = new[] { new { collectionId = "kardex_pro" } },
                        where = new
                        {
                            compositeFilter = new
                            {
                                op = "AND",
                                filters = new object[]
                                {
                                    new { fieldFilter = new { field = new { fieldPath = "EmpresaId" }, op = "EQUAL", value = new { stringValue = empresaId } } },
                                    new { fieldFilter = new { field = new { fieldPath = "NumeroOperacion" }, op = "EQUAL", value = new { stringValue = numeroOperacion } } }
                                }
                            }
                        },
                        limit = 1
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(queryBody), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) return false;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("document", out _))
                            return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}


