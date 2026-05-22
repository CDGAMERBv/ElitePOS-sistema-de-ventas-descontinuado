using ElitePOS.Services;
using ElitePOS.Shared.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using System;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;

namespace ElitePOS.Server.Services
{
    public class PdfImpresionService : IPdfImpresionService
    {
        private readonly HttpClient _httpClient;

        public PdfImpresionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<byte[]?> DescargarLogo(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            if (url.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) && url.Contains("base64,"))
            {
                try
                {
                    var base64Part = url.Substring(url.IndexOf("base64,") + 7);
                    return Convert.FromBase64String(base64Part);
                }
                catch { return null; }
            }

            try 
            {
                return await _httpClient.GetByteArrayAsync(url);
            }
            catch 
            {
                return null;
            }
        }

        public async Task<byte[]> GenerarFacturaA4(VentaModel venta, ConfiguracionEmpresaModel config)
        {
            PrepareVentaData(venta);
            var logoBytes = await DescargarLogo(config.LogoUrl);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    ConfigurarPaginaA4A5(page, venta, config, logoBytes, false);
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerarFacturaA5(VentaModel venta, ConfiguracionEmpresaModel config)
        {
            PrepareVentaData(venta);
            var logoBytes = await DescargarLogo(config.LogoUrl);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5.Landscape());
                    ConfigurarPaginaA4A5(page, venta, config, logoBytes, true);
                });
            });

            return document.GeneratePdf();
        }

        private void ConfigurarPaginaA4A5(PageDescriptor page, VentaModel venta, ConfiguracionEmpresaModel config, byte[]? logoBytes, bool isA5)
        {
            page.Margin(1, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(isA5 ? 7 : 8).FontFamily("Arial").FontColor(Colors.Black));

            page.Header().Row(row =>
            {
                row.RelativeItem().Row(r =>
                {
                    if (logoBytes != null)
                    {
                        r.ConstantItem(isA5 ? 60 : 80).MaxHeight(isA5 ? 60 : 80).Image(logoBytes).FitArea();
                    }
                    r.RelativeItem().PaddingLeft(10).Column(c =>
                    {
                        c.Item().Text(config.NombreComercial).FontSize(isA5 ? 12 : 14).Bold();
                        c.Item().Text($"PRINCIPAL » {config.Direccion} - {config.Distrito} {config.Provincia} {config.Departamento}").FontSize(isA5 ? 7 : 8);
                        if (!string.IsNullOrEmpty(config.RazonSocial)) c.Item().Text(config.RazonSocial).FontSize(isA5 ? 7 : 8);
                        if (!string.IsNullOrEmpty(config.Telefono)) c.Item().Text($"Cel: {config.Telefono}").FontSize(isA5 ? 7 : 8);
                        if (!string.IsNullOrEmpty(config.Correo)) c.Item().Text($"Email: {config.Correo}").FontSize(isA5 ? 7 : 8);
                    });
                });

                row.ConstantItem(isA5 ? 150 : 200).Border(1).BorderColor(Colors.Black).Column(c =>
                {
                    c.Item().Padding(5).AlignCenter().Text($"RUC {config.Ruc}").FontSize(isA5 ? 9 : 11).Bold();
                    bool enviadoSunat = venta.EstadoSunat == "ENVIADO" || venta.EstadoSunat == "ACEPTADO" || venta.EstadoSunat == "Aceptado";
                    string tituloComprobante = enviadoSunat ? $"{venta.TipoComprobante.ToUpper()} ELECTRÓNICA" : "TICKET DE VENTA (CONTROL INTERNO)";
                    c.Item().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text(tituloComprobante).FontSize(isA5 ? 9 : 11).Bold();
                    c.Item().Padding(5).AlignCenter().Text(venta.NumeroComprobante).FontSize(isA5 ? 10 : 12).Bold();
                });
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Row(r => { r.ConstantItem(60).Text("DOCUMENTO").Bold(); r.RelativeItem().Text($"{venta.TipoDocumentoCliente} {venta.NumeroDocumentoCliente}"); });
                        c.Item().Row(r => { r.ConstantItem(60).Text("CLIENTE").Bold(); r.RelativeItem().Text(venta.Cliente); });
                        c.Item().Row(r => { r.ConstantItem(60).Text("DIRECCIÓN").Bold(); r.RelativeItem().Text(venta.DireccionCliente ?? "-"); });
                    });
                    row.ConstantItem(isA5 ? 130 : 150).Column(c =>
                    {
                        c.Item().Row(r => { r.ConstantItem(isA5 ? 90 : 100).Text("FECHA EMISIÓN").Bold(); r.RelativeItem().Text(venta.FechaHora.ToString("dd/MM/yyyy")); });
                        c.Item().Row(r => { r.ConstantItem(isA5 ? 90 : 100).Text("FECHA VENCIMIENTO").Bold(); r.RelativeItem().Text(venta.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "-"); });
                        c.Item().Row(r => { r.ConstantItem(isA5 ? 90 : 100).Text("MONEDA").Bold(); r.RelativeItem().Text(venta.Moneda == "PEN" ? "SOLES" : venta.Moneda ?? "SOLES"); });
                    });
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25); // Nº
                        columns.ConstantColumn(50); // UNIDAD
                        columns.ConstantColumn(60); // CÓDIGO
                        columns.RelativeColumn();   // DESCRIPCIÓN
                        columns.ConstantColumn(35); // CANT.
                        columns.ConstantColumn(50); // P. UNIT.
                        columns.ConstantColumn(55); // TOTAL
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Black).Padding(3).AlignCenter().Text("Nº").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Black).Padding(3).AlignCenter().Text("UNIDAD").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Black).Padding(3).AlignCenter().Text("CÓDIGO").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Black).Padding(3).Text("DESCRIPCIÓN").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Black).Padding(3).AlignCenter().Text("CANT.").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Black).Padding(3).AlignRight().Text("P. UNIT.").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Black).Padding(3).AlignRight().Text("TOTAL").FontColor(Colors.White).Bold();
                    });

                    int i = 1;
                    foreach (var item in venta.Items)
                    {
                        table.Cell().BorderLeft(1).BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(i++.ToString());
                        table.Cell().BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.UnidadMedida ?? "UNIDADES");
                        table.Cell().BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.CodigoInterno ?? "-");
                        table.Cell().BorderRight(1).BorderColor(Colors.Black).Padding(3).Text(item.NombreProducto);
                        table.Cell().BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.Cantidad.ToString("N2"));
                        table.Cell().BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(item.PrecioUnitario.ToString("N2"));
                        table.Cell().BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(item.Subtotal.ToString("N2"));
                    }
                    
                    table.Cell().ColumnSpan(7).BorderLeft(1).BorderRight(1).BorderBottom(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text($"SON {NumberToText.Convert(venta.Total, "SOLES").ToUpper()}").FontSize(isA5 ? 7 : 8);
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem(); 
                    row.ConstantItem(180).Column(c =>
                    {
                        var gravadaStr = Math.Round(venta.SubtotalGravada > 0 ? venta.SubtotalGravada : venta.Subtotal, 2).ToString("N2");
                        c.Item().Row(r => { r.RelativeItem().AlignRight().Text("EXONERADO").Bold(); r.ConstantItem(25).AlignRight().Text("S/"); r.ConstantItem(55).AlignRight().Text("0.00"); });
                        c.Item().Row(r => { r.RelativeItem().AlignRight().Text("I.G.V. 18%").Bold(); r.ConstantItem(25).AlignRight().Text("S/"); r.ConstantItem(55).AlignRight().Text(venta.IGV.ToString("N2")); });
                        c.Item().Row(r => { r.RelativeItem().AlignRight().Text("TOTAL").Bold(); r.ConstantItem(25).AlignRight().Text("S/").Bold(); r.ConstantItem(55).AlignRight().Text(venta.Total.ToString("N2")).Bold(); });
                    });
                });

                col.Item().PaddingTop(isA5 ? 5 : 15).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Row(r => { r.ConstantItem(110).Text("USUARIO").Bold(); r.RelativeItem().Text($"{venta.NombreUsuario ?? "-"} - {venta.FechaHora:dd/MM/yyyy hh:mm tt}"); });
                        c.Item().Row(r => { r.ConstantItem(110).Text("CONDICIÓN DE PAGO").Bold(); r.RelativeItem().Text(venta.TipoPago ?? "CONTADO"); });
                        if (!string.IsNullOrEmpty(config.CuentasBancarias)) {
                            c.Item().Row(r => { r.ConstantItem(110).Text("CUENTAS BANCARIAS").Bold(); r.RelativeItem().Text(config.CuentasBancarias.Replace("\n", "  -  ")); });
                        }
                        // ── BLOQUE DE DATOS EXTRA (condicional por campo) ───────────────
                        if (!string.IsNullOrWhiteSpace(venta.Observaciones))
                            c.Item().Row(r => { r.ConstantItem(110).Text("OBSERVACIONES").Bold(); r.RelativeItem().Text(venta.Observaciones); });
                        // ────────────────────────────────────────────────────────────────
                        bool enviadoSunat = venta.EstadoSunat == "ENVIADO" || venta.EstadoSunat == "ACEPTADO" || venta.EstadoSunat == "Aceptado";
                        if (enviadoSunat)
                        {
                            string msjSunat = string.IsNullOrEmpty(venta.MensajeSunat) ? $"La {venta.TipoComprobante} numero {venta.NumeroComprobante}, ha sido aceptada" : venta.MensajeSunat;
                            c.Item().PaddingTop(isA5 ? 5 : 10).Row(r => { r.ConstantItem(110).Text("RESPUESTA SUNAT").Bold(); r.RelativeItem().Text(msjSunat); });
                        }
                    });
                    
                    row.ConstantItem(100).AlignRight().Column(c =>
                    {
                         bool enviadoSunat = venta.EstadoSunat == "ENVIADO" || venta.EstadoSunat == "ACEPTADO" || venta.EstadoSunat == "Aceptado";
                         if (enviadoSunat)
                         {
                             c.Item().AlignRight().Width(80).Height(80).Image(GenerarCodigoQR(GetQrContent(venta, config)));
                             c.Item().AlignRight().PaddingTop(2).Text(venta.HashSunat ?? "-").FontSize(isA5 ? 5 : 6);
                         }
                    });
                });
            });

            page.Footer().AlignCenter().Column(c =>
            {
                if (!string.IsNullOrEmpty(config.ResolucionAutorizacion))
                    c.Item().Text(config.ResolucionAutorizacion).FontSize(isA5 ? 6 : 7);

                c.Item().Text($"Representación impresa de la {venta.TipoComprobante.ToUpper()} ELECTRÓNICA").FontSize(isA5 ? 6 : 7);
                
                var urlConsulta = !string.IsNullOrEmpty(config.UrlConsulta) ? config.UrlConsulta : "Consulte su comprobante en nuestro local";
                c.Item().Text(x => { 
                    x.Span("Para consultar el comprobante visita ").FontSize(isA5 ? 6 : 7); 
                    x.Span(urlConsulta).Bold().FontSize(isA5 ? 6 : 7); 
                });

                c.Item().PaddingTop(5).Text("ElitePOS").Bold().FontSize(isA5 ? 9 : 10);
                c.Item().Text("Emitido desde ElitePOS - Tu Sistema de Ventas").FontSize(isA5 ? 5 : 6);
            });
        }

        public async Task<byte[]> GenerarTicket(VentaModel venta, ConfiguracionEmpresaModel config)
        {
            PrepareVentaData(venta);
            var logoBytes = await DescargarLogo(config.LogoUrl);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(226, 800, Unit.Point);
                    page.Margin(10, Unit.Point);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Arial").FontColor(Colors.Black));

                    page.Content().Column(col =>
                    {
                        if (logoBytes != null) col.Item().AlignCenter().MaxHeight(50).Image(logoBytes).FitArea();
                        
                        col.Item().AlignCenter().PaddingTop(3).Text(config.NombreComercial).Bold().FontSize(10);
                        col.Item().AlignCenter().Text($"RUC: {config.Ruc}").Bold().FontSize(9);
                        col.Item().AlignCenter().Text("PRINCIPAL").FontSize(7);
                        if (!string.IsNullOrEmpty(config.Direccion)) col.Item().AlignCenter().Text(config.Direccion).FontSize(7);
                        if (!string.IsNullOrEmpty(config.RazonSocial)) col.Item().AlignCenter().Text(config.RazonSocial).FontSize(7);
                        if (!string.IsNullOrEmpty(config.Telefono)) col.Item().AlignCenter().Text($"Cel: {config.Telefono}").FontSize(7);
                        if (!string.IsNullOrEmpty(config.Correo)) col.Item().AlignCenter().Text($"Email: {config.Correo}").FontSize(7);

                        bool enviadoSunat = venta.EstadoSunat == "ENVIADO" || venta.EstadoSunat == "ACEPTADO" || venta.EstadoSunat == "Aceptado";
                        string tituloComprobante = enviadoSunat ? $"{venta.TipoComprobante.ToUpper()} ELECTRÓNICA" : "TICKET DE VENTA (CONTROL INTERNO)";
                        col.Item().PaddingVertical(5).AlignCenter().Text(tituloComprobante).Bold().FontSize(9);
                        col.Item().AlignCenter().Text(venta.NumeroComprobante).Bold().FontSize(10);
                        
                        col.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Black);

                        col.Item().Row(r => { r.ConstantItem(60).Text("DOCUMENTO").Bold(); r.RelativeItem().Text($"{venta.TipoDocumentoCliente} {venta.NumeroDocumentoCliente}"); });
                        col.Item().Row(r => { r.ConstantItem(60).Text("CLIENTE").Bold(); r.RelativeItem().Text(venta.Cliente); });
                        col.Item().Row(r => { r.ConstantItem(60).Text("DIRECCIÓN").Bold(); r.RelativeItem().Text(venta.DireccionCliente ?? "-"); });
                        col.Item().Row(r => { r.ConstantItem(60).Text("F. EMISIÓN").Bold(); r.RelativeItem().Text(venta.FechaHora.ToString("dd/MM/yyyy")); });
                        col.Item().Row(r => { r.ConstantItem(60).Text("MONEDA").Bold(); r.RelativeItem().Text(venta.Moneda == "PEN" ? "SOLES" : venta.Moneda ?? "SOLES"); });

                        col.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Black);

                        col.Item().Table(table => {
                            table.ColumnsDefinition(columns => {
                                columns.RelativeColumn(2); // DESC + CANT
                                columns.RelativeColumn(1); // P/U
                                columns.RelativeColumn(1); // TOTAL
                            });
                            table.Header(h => {
                                h.Cell().Text("DESCRIPCIÓN").Bold();
                                h.Cell().AlignRight().Text("P/U").Bold();
                                h.Cell().AlignRight().Text("TOTAL").Bold();
                            });
                            foreach (var item in venta.Items) {
                                table.Cell().ColumnSpan(3).Text($"[{item.Cantidad:N2}] {item.NombreProducto}");
                                table.Cell().Text("");
                                table.Cell().AlignRight().Text(item.PrecioUnitario.ToString("N2"));
                                table.Cell().AlignRight().Text(item.Subtotal.ToString("N2"));
                            }
                        });

                        col.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Black);

                        col.Item().Row(r => { r.RelativeItem().Text("EXONERADO").Bold(); r.ConstantItem(15).AlignRight().Text("S/"); r.ConstantItem(45).AlignRight().Text("0.00"); });
                        col.Item().Row(r => { r.RelativeItem().Text("I.G.V. 18%").Bold(); r.ConstantItem(15).AlignRight().Text("S/"); r.ConstantItem(45).AlignRight().Text(venta.IGV.ToString("N2")); });
                        col.Item().Row(r => { r.RelativeItem().Text("TOTAL").Bold().FontSize(8); r.ConstantItem(15).AlignRight().Text("S/").Bold().FontSize(8); r.ConstantItem(45).AlignRight().Text(venta.Total.ToString("N2")).Bold().FontSize(8); });
                        
                        col.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Black);

                        col.Item().AlignCenter().Text($"SON {NumberToText.Convert(venta.Total, "SOLES").ToUpper()}").Bold().FontSize(7);
                        
                        col.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor(Colors.Black);

                        col.Item().Row(r => { r.ConstantItem(90).Text("USUARIO").Bold(); r.RelativeItem().Text($"{venta.NombreUsuario} - {venta.FechaHora:dd/MM/yyyy hh:mm tt}"); });
                        col.Item().Row(r => { r.ConstantItem(90).Text("CONDICIÓN DE PAGO").Bold(); r.RelativeItem().Text(venta.TipoPago ?? "CONTADO"); });
                        if (!string.IsNullOrEmpty(config.CuentasBancarias)) {
                            col.Item().PaddingTop(3).Text("CUENTAS BANCARIAS").Bold();
                            col.Item().Text(config.CuentasBancarias);
                        }

                        // ── BLOQUE DE DATOS EXTRA (condicional por campo) ───────────────
                        if (!string.IsNullOrWhiteSpace(venta.Observaciones))
                            col.Item().Row(r => { r.ConstantItem(90).Text("OBSERVACIONES").Bold(); r.RelativeItem().Text(venta.Observaciones); });
                        // ────────────────────────────────────────────────────────────────

                        bool enviadoSunatObj = venta.EstadoSunat == "ENVIADO" || venta.EstadoSunat == "ACEPTADO" || venta.EstadoSunat == "Aceptado";
                        if (enviadoSunatObj)
                        {
                            string msjSunat = string.IsNullOrEmpty(venta.MensajeSunat) ? $"La {venta.TipoComprobante} numero {venta.NumeroComprobante}, ha sido aceptada" : venta.MensajeSunat;
                            col.Item().PaddingTop(3).Text("RESPUESTA SUNAT").Bold();
                            col.Item().Text(msjSunat);
                        }

                        if (!string.IsNullOrEmpty(config.ResolucionAutorizacion))
                        {
                            col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                            col.Item().PaddingTop(3).AlignCenter().Text(config.ResolucionAutorizacion).FontSize(6);
                        }

                        if (enviadoSunatObj)
                        {
                            col.Item().AlignCenter().Text($"Representación impresa de la {venta.TipoComprobante.ToUpper()} ELECTRÓNICA").FontSize(6);
                        }
                        
                        var urlConsultaTic = !string.IsNullOrEmpty(config.UrlConsulta) ? config.UrlConsulta : "Consulte su local de emisión";
                        col.Item().AlignCenter().Text($"Para consultar el comprobante visita {urlConsultaTic}").FontSize(6);

                        // Mover QR al Final del pie de página
                        if (enviadoSunatObj)
                        {
                            col.Item().PaddingTop(8).AlignCenter().Width(90).Height(90).Image(GenerarCodigoQR(GetQrContent(venta, config)));
                            col.Item().PaddingTop(2).AlignCenter().Text(venta.HashSunat ?? "-").FontSize(6);
                        }
                        
                        col.Item().PaddingTop(5).AlignCenter().Text("ElitePOS").Bold().FontSize(9);
                        col.Item().AlignCenter().Text("Emitido desde ElitePOS - Tu Sistema de Ventas").FontSize(5);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void PrepareVentaData(VentaModel venta)
        {
            if (venta.Subtotal == 0 && venta.Total > 0) {
                venta.Subtotal = Math.Round(venta.Total / 1.18m, 2);
                venta.IGV = venta.Total - venta.Subtotal;
            }
            if (string.IsNullOrEmpty(venta.NumeroDocumentoCliente) && venta.TipoComprobante != "Boleta") 
                venta.NumeroDocumentoCliente = "00000000";
        }

        private string GetQrContent(VentaModel venta, ConfiguracionEmpresaModel config)
        {
            var numeroParts = (venta.NumeroComprobante ?? "").Split('-');
            var serie = numeroParts.FirstOrDefault() ?? "0001";
            var correlativo = numeroParts.Length > 1 ? numeroParts[1] : "1";
            return $"{config.Ruc}|{GetTipoDocSunat(venta.TipoComprobante)}|{serie}|{correlativo}|{venta.IGV:F2}|{venta.Total:F2}|{venta.FechaHora:yyyy-MM-dd}|{GetTipoDocClienteSunat(venta.TipoDocumentoCliente)}|{venta.NumeroDocumentoCliente}";
        }

        private byte[] GenerarCodigoQR(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }

        private string GetTipoDocSunat(string tipo) => (tipo ?? "").ToUpper() switch { "FACTURA" => "01", "BOLETA" => "03", _ => "01" };
        private string GetTipoDocClienteSunat(string tipo) => (tipo ?? "DNI").ToUpper() switch { "DNI" => "1", "RUC" => "6", _ => "1" };
    }

    public static class NumberToText
    {
        public static string Convert(decimal number, string currency)
        {
            long intPart = (long)Math.Truncate(number);
            int decimalPart = (int)Math.Round((number - intPart) * 100);
            return $"{NumeroALetras(intPart)} Y {decimalPart:00}/100 {currency}";
        }

        private static string NumeroALetras(long value)
        {
            if (value == 0) return "CERO";
            if (value == 1) return "UNO";
            if (value == 2) return "DOS";
            
            string[] unidades = { "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE", "VEINTE", "VEINTIUN", "VEINTIDOS", "VEINTITRES", "VEINTICUATRO", "VEINTICINCO", "VEINTISEIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE" };
            string[] decenas = { "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
            string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

            if (value < 30) return unidades[value];
            if (value < 100) return decenas[value / 10] + ((value % 10 > 0) ? " Y " + unidades[value % 10] : "");
            if (value == 100) return "CIEN";
            if (value < 1000) return centenas[value / 100] + ((value % 100 > 0) ? " " + NumeroALetras(value % 100) : "");
            if (value == 1000) return "MIL";
            if (value < 1000000) return ((value / 1000 == 1) ? "MIL" : NumeroALetras(value / 1000).Replace("UNO", "UN") + " MIL") + ((value % 1000 > 0) ? " " + NumeroALetras(value % 1000) : "");
            if (value == 1000000) return "UN MILLON";
            if (value < 1000000000000) return ((value / 1000000 == 1) ? "UN MILLON" : NumeroALetras(value / 1000000).Replace("UNO", "UN") + " MILLONES") + ((value % 1000000 > 0) ? " " + NumeroALetras(value % 1000000) : "");

            return value.ToString();
        }
    }
}
