using ElitePOS.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElitePOS.Services
{
    public class AbonosService : IAbonosService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://firestore.googleapis.com/v1/projects/TU_FIREBASE_PROJECT_ID/databases/(default)/documents/abonos";

        public AbonosService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> RegistrarAbono(AbonoModel abono)
        {
            var firestoreDoc = new
            {
                fields = new
                {
                    id = new { stringValue = abono.id },
                    empresaId = new { stringValue = abono.empresaId },
                    ventaId = new { stringValue = abono.ventaId },
                    numeroComprobante = new { stringValue = abono.numeroComprobante },
                    clienteId = new { stringValue = abono.clienteId },
                    nombreCliente = new { stringValue = abono.nombreCliente },
                    montoAbono = new { doubleValue = (double)abono.montoAbono },
                    fechaAbono = new { timestampValue = abono.fechaAbono.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
                    usuarioId = new { stringValue = abono.usuarioId },
                    nombreUsuario = new { stringValue = abono.nombreUsuario },
                    observaciones = new { stringValue = abono.observaciones },
                    metodoPago = new { stringValue = abono.metodoPago }
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}?documentId={abono.id}", firestoreDoc);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<AbonoModel>> ObtenerAbonosPorVenta(string ventaId)
        {
            var response = await _httpClient.GetAsync(_baseUrl);
            if (!response.IsSuccessStatusCode) return new List<AbonoModel>();

            var json = await response.Content.ReadAsStringAsync();
            var firestoreResponse = JsonSerializer.Deserialize<FirestoreDocsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (firestoreResponse?.Documents == null)
                return new List<AbonoModel>();

            return firestoreResponse.Documents
                .Select(doc => ConvertirAAbono(doc))
                .Where(a => a.ventaId == ventaId)
                .OrderByDescending(a => a.fechaAbono)
                .ToList();
        }

        public async Task<List<AbonoModel>> ObtenerAbonosPorCliente(string clienteId)
        {
            var response = await _httpClient.GetAsync(_baseUrl);
            if (!response.IsSuccessStatusCode) return new List<AbonoModel>();

            var json = await response.Content.ReadAsStringAsync();
            var firestoreResponse = JsonSerializer.Deserialize<FirestoreDocsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (firestoreResponse?.Documents == null)
                return new List<AbonoModel>();

            return firestoreResponse.Documents
                .Select(doc => ConvertirAAbono(doc))
                .Where(a => a.clienteId == clienteId)
                .OrderByDescending(a => a.fechaAbono)
                .ToList();
        }

        public async Task<decimal> CalcularTotalAbonos(string ventaId)
        {
            var abonos = await ObtenerAbonosPorVenta(ventaId);
            return abonos.Sum(a => a.montoAbono);
        }

        private AbonoModel ConvertirAAbono(FirestoreDoc doc)
        {
            return new AbonoModel
            {
                id = (doc.Fields.ContainsKey("id") ? doc.Fields["id"].StringValue : (doc.Fields.ContainsKey("Id") ? doc.Fields["Id"].StringValue : "")),
                empresaId = (doc.Fields.ContainsKey("empresaId") ? doc.Fields["empresaId"].StringValue : (doc.Fields.ContainsKey("EmpresaId") ? doc.Fields["EmpresaId"].StringValue : "")),
                ventaId = (doc.Fields.ContainsKey("ventaId") ? doc.Fields["ventaId"].StringValue : (doc.Fields.ContainsKey("VentaId") ? doc.Fields["VentaId"].StringValue : "")),
                numeroComprobante = (doc.Fields.ContainsKey("numeroComprobante") ? doc.Fields["numeroComprobante"].StringValue : (doc.Fields.ContainsKey("NumeroComprobante") ? doc.Fields["NumeroComprobante"].StringValue : "")),
                clienteId = (doc.Fields.ContainsKey("clienteId") ? doc.Fields["clienteId"].StringValue : (doc.Fields.ContainsKey("ClienteId") ? doc.Fields["ClienteId"].StringValue : "")),
                nombreCliente = (doc.Fields.ContainsKey("nombreCliente") ? doc.Fields["nombreCliente"].StringValue : (doc.Fields.ContainsKey("NombreCliente") ? doc.Fields["NombreCliente"].StringValue : "")),
                montoAbono = (doc.Fields.ContainsKey("montoAbono") ? (decimal)doc.Fields["montoAbono"].DoubleValue : (doc.Fields.ContainsKey("MontoAbono") ? (decimal)doc.Fields["MontoAbono"].DoubleValue : 0)),
                fechaAbono = (doc.Fields.ContainsKey("fechaAbono") ? DateTime.Parse(doc.Fields["fechaAbono"].TimestampValue) : (doc.Fields.ContainsKey("FechaAbono") ? DateTime.Parse(doc.Fields["FechaAbono"].TimestampValue) : DateTime.MinValue)),
                usuarioId = (doc.Fields.ContainsKey("usuarioId") ? doc.Fields["usuarioId"].StringValue : (doc.Fields.ContainsKey("UsuarioId") ? doc.Fields["UsuarioId"].StringValue : "")),
                nombreUsuario = (doc.Fields.ContainsKey("nombreUsuario") ? doc.Fields["nombreUsuario"].StringValue : (doc.Fields.ContainsKey("NombreUsuario") ? doc.Fields["NombreUsuario"].StringValue : "")),
                observaciones = (doc.Fields.ContainsKey("observaciones") ? doc.Fields["observaciones"].StringValue : (doc.Fields.ContainsKey("Observaciones") ? doc.Fields["Observaciones"].StringValue : "")),
                metodoPago = (doc.Fields.ContainsKey("metodoPago") ? doc.Fields["metodoPago"].StringValue : (doc.Fields.ContainsKey("MetodoPago") ? doc.Fields["MetodoPago"].StringValue : "Efectivo"))
            };
        }

        // Clases auxiliares de Firestore (privadas dentro de la clase)
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
        }

        private class FirestoreValue
        {
            [JsonPropertyName("stringValue")]
            public string StringValue { get; set; } = "";

            [JsonPropertyName("doubleValue")]
            public double DoubleValue { get; set; } = 0;

            [JsonPropertyName("timestampValue")]
            public string TimestampValue { get; set; } = "";
        }
    }
}


