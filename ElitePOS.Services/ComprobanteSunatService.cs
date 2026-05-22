using ElitePOS.Shared.Models;
using System.Text.Json;

namespace ElitePOS.Services
{
    public class ComprobanteSunatService : IComprobanteSunatService
    {
        private const decimal IGV_PERU = 0.18m; // 18% de IGV en Perú
        private const decimal ICBPER = 0.50m; // S/ 0.50 por bolsa plástica (vigente desde 2024)

        public async Task<ComprobanteSunatModel> GenerarComprobanteSunat(VentaModel venta, ClienteModel cliente)
        {
            var (baseImponible, igv, total) = CalcularTotalesSunat(venta.total);

            var tipoDoc = venta.tipoComprobante == "Factura" ? "01" : "03"; // 01=Factura, 03=Boleta
            var clienteTipoDoc = cliente.dniRuc.Length == 11 ? "6" : "1"; // 6=RUC, 1=DNI

            var comprobante = new ComprobanteSunatModel
            {
                id = venta.id,
                tipoDocumento = tipoDoc,
                serie = venta.numeroComprobante.Contains("-") ? venta.numeroComprobante.Split('-')[0] : "B001",
                numero = venta.numeroComprobante.Contains("-") ? venta.numeroComprobante.Split('-')[1] : "000000",
                fechaEmision = venta.fechaHora,
                monedaId = "PEN",

                clienteTipoDocumento = clienteTipoDoc,
                clienteNumeroDocumento = cliente.dniRuc,
                clienteRazonSocial = cliente.nombre,

                totalOperacionesGravadas = baseImponible,
                totalOperacionesInafectas = 0,
                totalOperacionesExoneradas = 0,
                totalIgv = igv,
                totalIcbper = 0,
                totalVenta = total,

                importeEnLetras = ConvertirNumeroALetras(total)
            };

            int itemNumero = 1;
            foreach (var item in venta.items)
            {
                var (itemBase, itemIgv, itemTotal) = CalcularTotalesSunat(item.subtotal);

                comprobante.items.Add(new ItemComprobanteSunat
                {
                    numero = itemNumero++,
                    unidadMedida = "NIU",
                    cantidad = item.cantidad,
                    descripcion = item.nombreProducto,
                    valorUnitario = item.cantidad > 0 ? itemBase / item.cantidad : 0,
                    precioUnitario = item.precioUnitario,
                    totalIgv = itemIgv,
                    totalVenta = itemTotal,
                    codigoTipoAfectacionIgv = "10"
                });
            }

            comprobante.codigoQr = GenerarCodigoQR(comprobante);

            return await Task.FromResult(comprobante);
        }

        public string GenerarCodigoQR(ComprobanteSunatModel comprobante)
        {
            string rucEmisor = "20123456789"; 

            var qrData = $"{rucEmisor}|" +
                         $"{comprobante.tipoDocumento}|" +
                         $"{comprobante.serie}|" +
                         $"{comprobante.numero}|" +
                         $"{comprobante.totalIgv:F2}|" +
                         $"{comprobante.totalVenta:F2}|" +
                         $"{comprobante.fechaEmision:dd/MM/yyyy}|" +
                         $"{comprobante.clienteTipoDocumento}|" +
                         $"{comprobante.clienteNumeroDocumento}|";

            return qrData;
        }

        public async Task<ResumenDiarioModel> GenerarResumenDiario(DateTime fecha, List<VentaModel> ventas)
        {
            var boletasDelDia = ventas.Where(v => 
                v.fechaHora.Date == fecha.Date && 
                v.tipoComprobante == "Boleta" &&
                !v.anulada
            ).ToList();

            var resumen = new ResumenDiarioModel
            {
                fechaGeneracion = DateTime.Now,
                fechaResumen = fecha,
                identificador = $"RC-{fecha:yyyyMMdd}-001",
                cantidadComprobantes = boletasDelDia.Count
            };

            decimal totalGravadas = 0;
            decimal totalIGV = 0;
            decimal totalVentas = 0;

            foreach (var venta in boletasDelDia)
            {
                var (baseImponible, igv, total) = CalcularTotalesSunat(venta.total);

                totalGravadas += baseImponible;
                totalIGV += igv;
                totalVentas += total;

                var cliente = new ClienteModel
                {
                    dniRuc = "00000000",
                    nombre = venta.cliente
                };

                var comprobante = await GenerarComprobanteSunat(venta, cliente);
                resumen.boletas.Add(comprobante);
            }

            resumen.totalGravadas = totalGravadas;
            resumen.totalIgv = totalIGV;
            resumen.totalVentas = totalVentas;

            return resumen;
        }

        public (decimal baseImponible, decimal igv, decimal total) CalcularTotalesSunat(decimal montoTotal)
        {
            var baseImponible = Math.Round(montoTotal / (1 + IGV_PERU), 2);
            var igv = Math.Round(baseImponible * IGV_PERU, 2);

            var totalCalculado = baseImponible + igv;
            if (totalCalculado != montoTotal)
            {
                var diferencia = montoTotal - totalCalculado;
                baseImponible += diferencia;
            }

            return (baseImponible, igv, montoTotal);
        }

        public string ConvertirNumeroALetras(decimal monto)
        {
            var parteEntera = (int)Math.Floor(monto);
            var parteDecimal = (int)Math.Round((monto - parteEntera) * 100);

            string letras = ConvertirEnteroALetras(parteEntera).ToUpper();
            return $"SON: {letras} Y {parteDecimal:D2}/100 SOLES";
        }

        private string ConvertirEnteroALetras(int numero)
        {
            if (numero == 0) return "CERO";

            string[] unidades = { "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE" };
            string[] decenas = { "", "", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
            string[] especiales = { "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" };
            string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

            if (numero < 10) return unidades[numero];
            if (numero < 20) return especiales[numero - 10];
            if (numero < 100)
            {
                int unidad = numero % 10;
                int decena = numero / 10;
                if (unidad == 0) return decenas[decena];
                return $"{decenas[decena]} Y {unidades[unidad]}";
            }
            if (numero < 1000)
            {
                int centena = numero / 100;
                int resto = numero % 100;
                if (numero == 100) return "CIEN";
                if (resto == 0) return centenas[centena];
                return $"{centenas[centena]} {ConvertirEnteroALetras(resto)}";
            }
            if (numero < 1000000)
            {
                int miles = numero / 1000;
                int resto = numero % 1000;
                string textoMiles = miles == 1 ? "MIL" : $"{ConvertirEnteroALetras(miles)} MIL";
                if (resto == 0) return textoMiles;
                return $"{textoMiles} {ConvertirEnteroALetras(resto)}";
            }
            return numero.ToString();
        }
    }
}
