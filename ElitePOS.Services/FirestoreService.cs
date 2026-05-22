using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElitePOS.Services
{
    public class FirestoreService : IInventarioService
    {
        private readonly HttpClient _httpClient;
        private readonly string _projectId;
        private readonly string _baseUrl;
        private const string CollectionName = "productos";

        public FirestoreService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _projectId = "TU_FIREBASE_PROJECT_ID";
            _baseUrl = $"https://firestore.googleapis.com/v1/projects/{_projectId}/databases/(default)/documents/{CollectionName}";
        }

        public async Task<IEnumerable<ProductoModel>> ObtenerProductos()
        {
            try
            {
                var response = await _httpClient.GetAsync(_baseUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return Enumerable.Empty<ProductoModel>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var firestoreResponse = JsonSerializer.Deserialize<FirestoreListResponse>(json, options);

                if (firestoreResponse?.Documents == null || !firestoreResponse.Documents.Any())
                {
                    return Enumerable.Empty<ProductoModel>();
                }

                return firestoreResponse.Documents.Select(doc => ConvertToProducto(doc)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepci?n al obtener productos: {ex.Message}");
                return Enumerable.Empty<ProductoModel>();
            }
        }

        public async Task<ProductoModel?> ObtenerProductoPorId(string id)
        {
            try
            {
                var url = $"{_baseUrl}/{id}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var firestoreDoc = JsonSerializer.Deserialize<FirestoreDocument>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return firestoreDoc != null ? ConvertToProducto(firestoreDoc) : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> AgregarProducto(ProductoModel producto)
        {
            try
            {
                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["nombre"] = new { stringValue = producto.nombre ?? "" },
                        ["precioVenta"] = new { doubleValue = (double)producto.precioVenta },
                        ["precioCompra"] = new { doubleValue = (double)producto.precioCompra },
                        ["precioCosto"] = new { doubleValue = (double)producto.precioCosto },
                        ["stock"] = new { integerValue = producto.stock.ToString() },
                        ["codigoBarras"] = new { stringValue = producto.codigoBarras ?? "" },
                        ["categoria"] = new { stringValue = producto.categoria ?? "" },
                        ["imagenUrl"] = new { stringValue = producto.imagenUrl ?? "" },
                        ["unidadMedida"] = new { stringValue = producto.unidadMedida ?? "NIU" },
                        ["empresaId"] = new { stringValue = producto.empresaId ?? "" }
                    }
                };

                var url = $"{_baseUrl}?documentId={producto.id}";
                var response = await _httpClient.PostAsJsonAsync(url, firestoreDoc);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepci?n al agregar producto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EditarProducto(ProductoModel producto)
        {
            try
            {
                var firestoreDoc = new
                {
                    fields = new Dictionary<string, object>
                    {
                        ["nombre"] = new { stringValue = producto.nombre ?? "" },
                        ["precioVenta"] = new { doubleValue = (double)producto.precioVenta },
                        ["precioCompra"] = new { doubleValue = (double)producto.precioCompra },
                        ["precioCosto"] = new { doubleValue = (double)producto.precioCosto },
                        ["stock"] = new { integerValue = producto.stock.ToString() },
                        ["codigoBarras"] = new { stringValue = producto.codigoBarras ?? "" },
                        ["categoria"] = new { stringValue = producto.categoria ?? "" },
                        ["imagenUrl"] = new { stringValue = producto.imagenUrl ?? "" },
                        ["unidadMedida"] = new { stringValue = producto.unidadMedida ?? "NIU" },
                        ["empresaId"] = new { stringValue = producto.empresaId ?? "" }
                    }
                };

                var url = $"{_baseUrl}/{producto.id}";
                var response = await _httpClient.PatchAsJsonAsync(url, firestoreDoc);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepci?n al editar producto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActualizarProducto(ProductoModel producto)
        {
            return await EditarProducto(producto);
        }

        public async Task<bool> EliminarProducto(string id)
        {
            try
            {
                var url = $"{_baseUrl}/{id}";
                var response = await _httpClient.DeleteAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ProductoModel?> BuscarProductoPorCodigo(string codigoBarras)
        {
            try
            {
                var productos = await ObtenerProductos();
                return productos.FirstOrDefault(p => p.codigoBarras == codigoBarras);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ActualizarStock(string productoId, int nuevaCantidad)
        {
            try
            {
                var url = $"{_baseUrl}/{productoId}?updateMask.fieldPaths=stock";
                var firestoreDoc = new
                {
                    fields = new
                    {
                        stock = new { integerValue = nuevaCantidad.ToString() }
                    }
                };

                var response = await _httpClient.PatchAsJsonAsync(url, firestoreDoc);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private ProductoModel ConvertToProducto(FirestoreDocument doc)
        {
            var fields = doc.Fields;

            return new ProductoModel
            {
                id = doc.Name?.Split('/').Last() ?? string.Empty,
                nombre = fields?.nombre?.StringValue ?? string.Empty,
                precioVenta = ParseDecimalValue(fields?.precioVenta),
                precioCompra = ParseDecimalValue(fields?.precioCompra),
                precioCosto = ParseDecimalValue(fields?.precioCosto),
                stock = ParseIntValue(fields?.stock),
                codigoBarras = fields?.codigoBarras?.StringValue ?? string.Empty,
                categoria = fields?.categoria?.StringValue ?? string.Empty,
                imagenUrl = fields?.imagenUrl?.StringValue ?? string.Empty,
                unidadMedida = fields?.unidadMedida?.StringValue ?? "NIU",
                empresaId = fields?.empresaId?.StringValue ?? "",
                costoPromedio = ParseDecimalValue(fields?.costoPromedio),
                costoTotalAcumulado = ParseDecimalValue(fields?.costoTotalAcumulado),
                unidadesCompradasHistoricas = ParseIntValue(fields?.unidadesCompradasHistoricas)
            };
        }

        private decimal ParseDecimalValue(FirestoreValue? value)
        {
            if (value == null) return 0m;
            if (value.DoubleValue != null)
            {
                if (value.DoubleValue is JsonElement element && element.ValueKind == JsonValueKind.Number)
                    return element.GetDecimal();
                
                var doubleStr = value.DoubleValue.ToString();
                if (decimal.TryParse(doubleStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
                    return result;
            }
            if (!string.IsNullOrEmpty(value.IntegerValue))
            {
                if (decimal.TryParse(value.IntegerValue, out decimal result))
                    return result;
            }
            return 0m;
        }

        private int ParseIntValue(FirestoreValue? value)
        {
            if (value == null) return 0;
            if (!string.IsNullOrEmpty(value.IntegerValue))
            {
                if (int.TryParse(value.IntegerValue, out int result))
                    return result;
            }
            if (value.DoubleValue != null)
            {
                var doubleStr = value.DoubleValue.ToString();
                if (int.TryParse(doubleStr, out int result))
                    return result;
            }
            return 0;
        }

        private class FirestoreListResponse
        {
            [JsonPropertyName("documents")]
            public List<FirestoreDocument>? Documents { get; set; }
        }

        private class FirestoreDocument
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("fields")]
            public FirestoreFields? Fields { get; set; }
        }

        private class FirestoreFields
        {
            public FirestoreValue? nombre { get; set; }
            public FirestoreValue? precioVenta { get; set; }
            public FirestoreValue? precioCompra { get; set; }
            public FirestoreValue? precioCosto { get; set; }
            public FirestoreValue? stock { get; set; }
            public FirestoreValue? codigoBarras { get; set; }
            public FirestoreValue? categoria { get; set; }
            public FirestoreValue? imagenUrl { get; set; }
            public FirestoreValue? unidadMedida { get; set; }
            public FirestoreValue? empresaId { get; set; }
            public FirestoreValue? costoPromedio { get; set; }
            public FirestoreValue? costoTotalAcumulado { get; set; }
            public FirestoreValue? unidadesCompradasHistoricas { get; set; }
        }

        private class FirestoreValue
        {
            [JsonPropertyName("stringValue")]
            public string? StringValue { get; set; }

            [JsonPropertyName("integerValue")]
            public string? IntegerValue { get; set; }

            [JsonPropertyName("doubleValue")]
            public object? DoubleValue { get; set; }
        }

        public async Task<ProductoModel?> CalcularYActualizarCostoPromedio(string productoId, int cantidadEntrante, decimal costoEntrante)
        {
            try
            {
                var producto = await ObtenerProductoPorId(productoId);
                if (producto == null) return null;
                
                decimal nuevoCostoPromedio = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(
                    producto.costoPromedio, producto.stock, costoEntrante, cantidadEntrante);
                
                producto.costoPromedio = nuevoCostoPromedio;
                producto.costoTotalAcumulado += (costoEntrante * cantidadEntrante);
                producto.unidadesCompradasHistoricas += cantidadEntrante;
                producto.ultimoCambioCosto = DateTime.Now;
                producto.stock += cantidadEntrante;
                
                var actualizado = await ActualizarProducto(producto);
                return actualizado ? producto : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error en CalcularYActualizarCostoPromedio: {ex.Message}");
                return null;
            }
        }
        
        public async Task<decimal> ObtenerCostoPromedio(string productoId)
        {
            var producto = await ObtenerProductoPorId(productoId);
            return producto?.costoPromedio ?? 0;
        }

        public async Task<IEnumerable<ProductoModel>> ObtenerProductosConStockBajo(int limite)
        {
            var todos = await ObtenerProductos();
            return todos.Where(p => p.stock <= 5).OrderBy(p => p.stock).Take(limite);
        }
    }
}
