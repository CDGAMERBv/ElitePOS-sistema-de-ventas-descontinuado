using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElitePOS.Services
{
    public class ComprasService : IComprasService
    {
        private readonly HttpClient _httpClient;
        private readonly IInventarioService _inventarioService;
        private readonly IKardexService _kardexService;
        private readonly ISesionService _sesionService;
        private const string FIREBASE_URL = "https://firestore.googleapis.com/v1/projects/TU_FIREBASE_PROJECT_ID/databases/(default)/documents/compras";

        public ComprasService(HttpClient httpClient, IInventarioService inventarioService, 
            IKardexService kardexService, ISesionService sesionService)
        {
            _httpClient = httpClient;
            _inventarioService = inventarioService;
            _kardexService = kardexService;
            _sesionService = sesionService;
        }

        public async Task<IEnumerable<CompraModel>> ObtenerCompras()
        {
            try
            {
                var response = await _httpClient.GetAsync(FIREBASE_URL);
                if (!response.IsSuccessStatusCode) return new List<CompraModel>();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var firestoreResponse = JsonSerializer.Deserialize<FirestoreListResponse>(jsonResponse, options);

                if (firestoreResponse?.Documents == null || firestoreResponse.Documents.Length == 0)
                    return new List<CompraModel>();

                var compras = new List<CompraModel>();
                foreach (var doc in firestoreResponse.Documents)
                {
                    try
                    {
                        var fields = doc.Fields;
                        var compra = new CompraModel
                        {
                            id = doc.Name.Split('/').Last(),
                            empresaId = GetStringValue(fields.empresaId ?? fields.EmpresaId),
                            fechaCompra = GetDateTimeValue(fields.fechaCompra ?? fields.FechaCompra),
                            proveedor = GetStringValue(fields.proveedor ?? fields.Proveedor),
                            numeroDocumento = GetStringValue(fields.numeroDocumento ?? fields.NumeroDocumento),
                            productoId = GetStringValue(fields.productoId ?? fields.ProductoId),
                            nombreProducto = GetStringValue(fields.nombreProducto ?? fields.NombreProducto),
                            cantidad = GetIntValue(fields.cantidad ?? fields.Cantidad),
                            costoUnitario = GetDecimalValue(fields.costoUnitario ?? fields.CostoUnitario),
                            total = GetDecimalValue(fields.total ?? fields.Total),
                            usuarioId = GetStringValue(fields.usuarioId ?? fields.UsuarioId),
                            nombreUsuario = GetStringValue(fields.nombreUsuario ?? fields.NombreUsuario)
                        };
                        compras.Add(compra);
                    }
                    catch { }
                }

                return compras.OrderByDescending(c => c.fechaCompra).ToList();
            }
            catch
            {
                return new List<CompraModel>();
            }
        }

        public async Task<IEnumerable<CompraModel>> ObtenerCompras(DateTime? inicio, DateTime? fin)
        {
            var todas = await ObtenerCompras();
            if (inicio.HasValue) todas = todas.Where(c => c.fechaCompra >= inicio.Value);
            if (fin.HasValue) todas = todas.Where(c => c.fechaCompra <= fin.Value);
            return todas.ToList();
        }

        public async Task<ResultadoOperacion> RegistrarCompra(CompraModel compra)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(compra.empresaId)) return ResultadoOperacion.Error("EmpresaId es obligatorio");
                if (string.IsNullOrWhiteSpace(compra.usuarioId)) return ResultadoOperacion.Error("UsuarioId es obligatorio");
                if (string.IsNullOrWhiteSpace(compra.productoId)) return ResultadoOperacion.Error("ProductoId es obligatorio");
                if (string.IsNullOrWhiteSpace(compra.proveedor)) return ResultadoOperacion.Error("Proveedor es obligatorio");

                if (string.IsNullOrWhiteSpace(compra.id)) compra.id = Guid.NewGuid().ToString();
                compra.total = compra.cantidad * compra.costoUnitario;

                var firestoreDoc = new
                {
                    fields = new
                    {
                        empresaId = new { stringValue = compra.empresaId ?? "" },
                        fechaCompra = new { stringValue = compra.fechaCompra.ToString("o") },
                        proveedor = new { stringValue = compra.proveedor ?? "" },
                        numeroDocumento = new { stringValue = compra.numeroDocumento ?? "" },
                        productoId = new { stringValue = compra.productoId ?? "" },
                        nombreProducto = new { stringValue = compra.nombreProducto ?? "" },
                        cantidad = new { integerValue = compra.cantidad.ToString() },
                        costoUnitario = new { doubleValue = (double)compra.costoUnitario },
                        total = new { doubleValue = (double)compra.total },
                        usuarioId = new { stringValue = compra.usuarioId ?? "" },
                        nombreUsuario = new { stringValue = compra.nombreUsuario ?? "" }
                    }
                };

                var url = $"{FIREBASE_URL}?documentId={compra.id}";
                var response = await _httpClient.PostAsJsonAsync(url, firestoreDoc);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var productoActualizado = await _inventarioService.CalcularYActualizarCostoPromedio(
                            compra.productoId ?? "", compra.cantidad, compra.costoUnitario);

                        if (productoActualizado != null)
                        {
                            await _kardexService.RegistrarEntradaCompra(
                                compra.productoId ?? "",
                                compra.cantidad,
                                compra.costoUnitario,
                                "", // ProveedorId
                                compra.proveedor ?? "Proveedor Desconocido",
                                compra.numeroDocumento ?? "S/N"
                            );
                        }
                    }
                    catch { }

                    return ResultadoOperacion.Exito();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return ResultadoOperacion.Error(response.ReasonPhrase ?? "Error desconocido", (int)response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                return ResultadoOperacion.Error($"Excepción: {ex.Message}", null, ex.StackTrace);
            }
        }

        public async Task<bool> EliminarCompra(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{FIREBASE_URL}/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private string GetStringValue(FirestoreValue? value) => value?.StringValue ?? "";
        private int GetIntValue(FirestoreValue? value) => (value?.IntegerValue != null && int.TryParse(value.IntegerValue, out var result)) ? result : 0;
        private decimal GetDecimalValue(FirestoreValue? value)
        {
            if (value?.DoubleValue.HasValue == true) return (decimal)value.DoubleValue.Value;
            if (value?.IntegerValue != null && decimal.TryParse(value.IntegerValue, out var result)) return result;
            return 0m;
        }
        private DateTime GetDateTimeValue(FirestoreValue? value) => (value?.StringValue != null && DateTime.TryParse(value.StringValue, out var result)) ? result : DateTime.Now;

        private class FirestoreListResponse { public FirestoreDocument[]? Documents { get; set; } }
        private class FirestoreDocument { 
            public string Name { get; set; } = string.Empty; 
            public FirestoreFields Fields { get; set; } = new FirestoreFields(); 
        }
        private class FirestoreFields {
            public FirestoreValue? empresaId { get; set; }
            public FirestoreValue? EmpresaId { get; set; }
            public FirestoreValue? fechaCompra { get; set; }
            public FirestoreValue? FechaCompra { get; set; }
            public FirestoreValue? proveedor { get; set; }
            public FirestoreValue? Proveedor { get; set; }
            public FirestoreValue? numeroDocumento { get; set; }
            public FirestoreValue? NumeroDocumento { get; set; }
            public FirestoreValue? productoId { get; set; }
            public FirestoreValue? ProductoId { get; set; }
            public FirestoreValue? nombreProducto { get; set; }
            public FirestoreValue? NombreProducto { get; set; }
            public FirestoreValue? cantidad { get; set; }
            public FirestoreValue? Cantidad { get; set; }
            public FirestoreValue? costoUnitario { get; set; }
            public FirestoreValue? CostoUnitario { get; set; }
            public FirestoreValue? total { get; set; }
            public FirestoreValue? Total { get; set; }
            public FirestoreValue? usuarioId { get; set; }
            public FirestoreValue? UsuarioId { get; set; }
            public FirestoreValue? nombreUsuario { get; set; }
            public FirestoreValue? NombreUsuario { get; set; }
        }
        private class FirestoreValue {
            public string? StringValue { get; set; }
            public string? IntegerValue { get; set; }
            public double? DoubleValue { get; set; }
            public bool? BooleanValue { get; set; }
        }

        public async Task<bool> RegistrarEntradaProducto(string productoId, int cantidad, decimal costoUnitario, 
            string proveedorId = "", string proveedorNombre = "", string numeroGuia = "", 
            DateTime? fechaVencimiento = null)
        {
            try
            {
                if (cantidad <= 0 || costoUnitario <= 0) return false;

                var producto = await _inventarioService.ObtenerProductoPorId(productoId);
                if (producto == null) return false;

                var productoActualizado = await _inventarioService.CalcularYActualizarCostoPromedio(
                    productoId, cantidad, costoUnitario);

                if (productoActualizado == null) return false;

                return await _kardexService.RegistrarEntradaCompra(
                    productoId, cantidad, costoUnitario, proveedorId, proveedorNombre, 
                    numeroGuia, fechaVencimiento);
            }
            catch
            {
                return false;
            }
        }
    }
}
