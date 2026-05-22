using ElitePOS.Shared.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;

namespace ElitePOS.Services
{
    public class FacturacionService : IFacturacionService
    {
        private readonly HttpClient _http;
        private readonly IConfiguracionSunatService _sunatService;

        public FacturacionService(HttpClient http, IConfiguracionSunatService sunatService)
        {
            _http = http;
            _sunatService = sunatService;
        }

        public async Task<(bool Exito, string Mensaje, string LinkPdf, string LinkXml)> EnviarVentaASunat(VentaModel venta)
        {
            try
            {
                var config = await _sunatService.ObtenerConfiguracion();

                if (string.IsNullOrEmpty(config.apiUrl) || string.IsNullOrEmpty(config.apiToken) || string.IsNullOrEmpty(config.ruc))
                {
                    return (false, "Faltan credenciales de API SUNAT en la configuración.", "", "");
                }

                int tipoComprobanteNubeFact = venta.tipoComprobante.ToUpper() == "FACTURA" ? 1 : 2;
                string serie = venta.tipoComprobante.ToUpper() == "FACTURA" ? config.serieFactura : config.serieBoleta;
                
                string correlativoLimpo = new string(venta.numeroComprobante.Where(char.IsDigit).ToArray());
                if (correlativoLimpo.Length > 8 && venta.numeroComprobante.Contains("-"))
                {
                    var partes = venta.numeroComprobante.Split('-');
                    if (partes.Length > 1) correlativoLimpo = new string(partes[1].Where(char.IsDigit).ToArray());
                }
                
                string numeroStr = correlativoLimpo.TrimStart('0');
                if (string.IsNullOrEmpty(numeroStr)) numeroStr = "1";
                if (numeroStr.Length > 8) numeroStr = numeroStr.Substring(numeroStr.Length - 8);
                string numeroFinal = numeroStr.PadLeft(8, '0');
                
                if (config.modo != "PRODUCCION")
                {
                    serie = venta.tipoComprobante.ToUpper() == "FACTURA" ? "FFF1" : "BBB1";
                }
                
                string tipoDocCliente = venta.tipoComprobante.ToUpper() == "FACTURA" ? "6" : "1";
                string numDocCliente = venta.numeroDocumentoCliente;
                
                if (venta.tipoComprobante.ToUpper() == "BOLETA" && (string.IsNullOrEmpty(numDocCliente) || venta.cliente == "CLIENTE GENERAL"))
                {
                    tipoDocCliente = "0"; 
                    numDocCliente = "00000000";
                }

                decimal totalGravadaItems = 0;
                decimal totalIgvItems = 0;
                
                var listaItems = venta.items.Select(item => {
                    decimal precUnit = item.precioVenta;
                    decimal valUnit = Math.Round(precUnit / 1.18m, 2);
                    decimal cant = item.cantidad;
                    
                    decimal subt = valUnit * cant;
                    decimal igv = Math.Round(precUnit - valUnit, 2) * cant;
                    decimal totalItem = subt + igv;
                    
                    totalGravadaItems += subt;
                    totalIgvItems += igv;
                    
                    return new {
                        unidad_de_medida = "NIU",
                        codigo = item.productoId,
                        descripcion = item.nombreProducto,
                        cantidad = cant.ToString(CultureInfo.InvariantCulture),
                        valor_unitario = valUnit.ToString("0.00", CultureInfo.InvariantCulture),
                        precio_unitario = precUnit.ToString("0.00", CultureInfo.InvariantCulture),
                        subtotal = subt.ToString("0.00", CultureInfo.InvariantCulture),
                        tipo_de_igv = 1,
                        igv = igv.ToString("0.00", CultureInfo.InvariantCulture),
                        total = totalItem.ToString("0.00", CultureInfo.InvariantCulture),
                        anticipo_regularizacion = "false"
                    };
                }).ToList();

                decimal totalVentaVal = totalGravadaItems + totalIgvItems;

                var payload = new
                {
                    operacion = "generar_comprobante",
                    tipo_de_comprobante = tipoComprobanteNubeFact,
                    serie = serie,
                    numero = numeroFinal,
                    sunat_transaction = 1,
                    cliente_tipo_de_documento = tipoDocCliente,
                    cliente_numero_de_documento = numDocCliente,
                    cliente_denominacion = string.IsNullOrEmpty(venta.cliente) ? "CLIENTE GENERAL" : venta.cliente,
                    cliente_direccion = "",
                    cliente_email = "",
                    fecha_de_emision = venta.fechaHora.ToString("yyyy-MM-dd"),
                    fecha_de_vencimiento = venta.fechaVencimiento?.ToString("yyyy-MM-dd") ?? venta.fechaHora.ToString("yyyy-MM-dd"),
                    moneda = 1,
                    porcentaje_de_igv = "18.00",
                    total_gravada = totalGravadaItems.ToString("0.00", CultureInfo.InvariantCulture),
                    total_igv = totalIgvItems.ToString("0.00", CultureInfo.InvariantCulture),
                    total = totalVentaVal.ToString("0.00", CultureInfo.InvariantCulture),
                    detraccion = "false",
                    enviar_automaticamente_a_la_sunat = config.modo == "PRODUCCION" ? "true" : "false",
                    enviar_automaticamente_al_cliente = "false",
                    condiciones_de_pago = venta.condicionPago == "Crédito" ? "Credito" : "Contado",
                    formato_de_pdf = "TICKET",
                    items = listaItems
                };

                string proxyUrl = "https://cors-anywhere.herokuapp.com/";
                string finalUrl = $"{proxyUrl}{config.apiUrl}";

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, finalUrl);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.apiToken);
                requestMessage.Content = JsonContent.Create(payload);

                var response = await _http.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<SunatApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result != null && !string.IsNullOrEmpty(result.enlace_del_pdf))
                    {
                        return (true, "Documento enviado correctamente a SUNAT", result.enlace_del_pdf, result.enlace_del_xml);
                    }
                    
                    return (true, "Venta procesada, pero no se recuperaron los enlaces de impresión.", "", "");
                }
                else
                {
                    try
                    {
                        var errorResult = JsonSerializer.Deserialize<SunatApiErrorResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        string errorMsg = errorResult?.errors ?? "Error desconocido en la API de SUNAT.";
                        return (false, errorMsg, "", "");
                    }
                    catch
                    {
                        return (false, $"Error HTTP {response.StatusCode}: {responseContent}", "", "");
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                return (false, "Error de Conexión/CORS: El servidor SUNAT o el Proxy rechazaron la conexión.", "", "");
            }
            catch (Exception ex)
            {
                return (false, $"Error interno: {ex.Message}", "", "");
            }
        }

        private class SunatApiResponse
        {
            public int tipo_de_comprobante { get; set; }
            public string serie { get; set; } = string.Empty;
            public long numero { get; set; }
            public string enlace_del_pdf { get; set; } = string.Empty;
            public string enlace_del_xml { get; set; } = string.Empty;
            public string enlace_del_cdr { get; set; } = string.Empty;
            public string sunat_ticket_numero { get; set; } = string.Empty;
            public bool aceptada_por_sunat { get; set; }
            public string sunat_description { get; set; } = string.Empty;
            public string sunat_note { get; set; } = string.Empty;
        }

        private class SunatApiErrorResponse
        {
            public string errors { get; set; } = string.Empty;
            public int codigo { get; set; }
        }
    }
}
