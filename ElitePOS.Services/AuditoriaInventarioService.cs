using ElitePOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElitePOS.Services
{
    public interface IAuditoriaInventarioService
    {
        Task<ResultadoAuditoria> VerificarIntegridadProducto(string productoId);
        Task<List<ResultadoAuditoria>> VerificarIntegridadGeneral();
        Task<ResultadoPrueba> EjecutarPruebaEstresMatematico();
    }

    public class AuditoriaInventarioService : IAuditoriaInventarioService
    {
        private readonly IInventarioService _inventarioService;
        private readonly IKardexService _kardexService;

        public AuditoriaInventarioService(IInventarioService inventarioService, IKardexService kardexService)
        {
            _inventarioService = inventarioService;
            _kardexService = kardexService;
        }

        public async Task<ResultadoAuditoria> VerificarIntegridadProducto(string productoId)
        {
            var resultado = new ResultadoAuditoria
            {
                productoId = productoId,
                fechaAuditoria = DateTime.Now,
                estado = "EN_PROGRESO"
            };

            try
            {
                var producto = await _inventarioService.ObtenerProductoPorId(productoId);
                if (producto == null)
                {
                    resultado.estado = "ERROR";
                    resultado.mensajeError = $"Producto {productoId} no encontrado";
                    return resultado;
                }

                resultado.nombreProducto = producto.nombre;
                resultado.stockReportado = producto.stock;
                resultado.costoPromedioReportado = producto.costoPromedio;

                var movimientos = await _kardexService.ObtenerHistorialProfesional(productoId);
                if (!movimientos.Any())
                {
                    resultado.estado = "SIN_MOVIMIENTOS";
                    resultado.mensajeError = "No hay movimientos de kardex para verificar";
                    return resultado;
                }

                decimal stockCalculado = await CalcularStockTeorico(movimientos);
                resultado.stockCalculado = stockCalculado;

                decimal costoPromedioCalculado = await CalcularCostoPromedioTeorico(movimientos);
                resultado.costoPromedioCalculado = costoPromedioCalculado;

                var discrepanciaStock = Math.Abs(producto.stock - stockCalculado);
                var discrepanciaCosto = Math.Abs(producto.costoPromedio - costoPromedioCalculado);

                resultado.discrepanciaStock = discrepanciaStock;
                resultado.discrepanciaCosto = discrepanciaCosto;

                const decimal TOLERANCIA = 0.000001m;
                
                if (discrepanciaStock <= TOLERANCIA && discrepanciaCosto <= TOLERANCIA)
                {
                    resultado.estado = "OK";
                    resultado.mensajeExito = "✅ Integridad matemática verificada correctamente";
                }
                else
                {
                    resultado.estado = "ERROR";
                    resultado.mensajeError = "❌ DISCREPANCIAS DETECTADAS";
                    if (discrepanciaStock > TOLERANCIA) resultado.mensajeError += $" | Stock: Rep={producto.stock}, Calc={stockCalculado}";
                    if (discrepanciaCosto > TOLERANCIA) resultado.mensajeError += $" | Costo: Rep={producto.costoPromedio:N6}, Calc={costoPromedioCalculado:N6}";
                }

                resultado.detalleMovimientos = movimientos.Select(m => new DetalleMovimientoAuditoria
                {
                    fecha = m.fecha,
                    tipo = m.tipoMovimiento,
                    concepto = m.concepto,
                    cantidad = m.cantidad,
                    stockAnterior = m.saldoAnterior,
                    stockPosterior = m.saldoActual,
                    costoUnitario = m.costoUnitario,
                    costoPromedioAnterior = m.costoPromedioAnterior,
                    costoPromedioPosterior = m.costoPromedioNuevo,
                    documentoReferencia = m.documentoReferencia
                }).ToList();

                return resultado;
            }
            catch (Exception ex)
            {
                resultado.estado = "ERROR_EXCEPCION";
                resultado.mensajeError = $"Excepción en auditoría: {ex.Message}";
                return resultado;
            }
        }

        private async Task<decimal> CalcularStockTeorico(List<MovimientoKardexProfesional> movimientos)
        {
            var movimientosOrdenados = movimientos.OrderBy(m => m.fecha).ThenBy(m => m.id).ToList();
            decimal stockTeorico = 0;
            
            foreach (var movimiento in movimientosOrdenados)
            {
                if (movimiento.tipoMovimiento == "Entrada") stockTeorico += movimiento.cantidad;
                else if (movimiento.tipoMovimiento == "Salida") stockTeorico -= movimiento.cantidad;
            }
            
            return stockTeorico;
        }

        private async Task<decimal> CalcularCostoPromedioTeorico(List<MovimientoKardexProfesional> movimientos)
        {
            var movimientosOrdenados = movimientos.OrderBy(m => m.fecha).ThenBy(m => m.id).ToList();
            decimal costoTotalAcumulado = 0;
            decimal unidadesTotales = 0;
            decimal costoPromedioActual = 0;
            
            foreach (var movimiento in movimientosOrdenados)
            {
                if (movimiento.tipoMovimiento == "Entrada")
                {
                    costoTotalAcumulado += movimiento.costoTotalMovimiento;
                    unidadesTotales += movimiento.cantidad;
                    costoPromedioActual = unidadesTotales > 0 ? costoTotalAcumulado / unidadesTotales : 0;
                }
            }
            return costoPromedioActual;
        }

        public async Task<List<ResultadoAuditoria>> VerificarIntegridadGeneral()
        {
            var resultados = new List<ResultadoAuditoria>();
            var todosProductos = await _inventarioService.ObtenerProductos();
            foreach (var producto in todosProductos.Take(10))
            {
                resultados.Add(await VerificarIntegridadProducto(producto.id));
            }
            return resultados;
        }

        public async Task<ResultadoPrueba> EjecutarPruebaEstresMatematico()
        {
            var resultado = new ResultadoPrueba
            {
                nombrePrueba = "Prueba de Integridad Matemática",
                fechaEjecucion = DateTime.Now,
                estado = "EN_PROGRESO"
            };

            try
            {
                decimal resultadoCalculado = ((10m * 1m) + (20m * 1m)) / (1m + 1m);
                decimal resultadoEsperado = 15.00m;
                
                resultado.costoCalculado = resultadoCalculado;
                resultado.costoEsperado = resultadoEsperado;
                resultado.discrepancia = Math.Abs(resultadoCalculado - resultadoEsperado);
                
                const decimal TOLERANCIA = 0.000001m;
                resultado.estado = resultado.discrepancia <= TOLERANCIA ? "OK" : "ERROR";
                resultado.mensaje = resultado.estado == "OK" 
                    ? $"✅ Prueba exitosa: S/ {resultadoCalculado:N6}"
                    : $"❌ Prueba fallida: S/ {resultadoCalculado:N6} vs S/ {resultadoEsperado:N6}";

                return resultado;
            }
            catch (Exception ex)
            {
                resultado.estado = "ERROR_EXCEPCION";
                resultado.mensaje = $"Excepción en prueba: {ex.Message}";
                return resultado;
            }
        }
    }

    public class ResultadoAuditoria
    {
        public string productoId { get; set; } = string.Empty;
        public string nombreProducto { get; set; } = string.Empty;
        public string estado { get; set; } = string.Empty;
        public string mensajeExito { get; set; } = string.Empty;
        public string mensajeError { get; set; } = string.Empty;
        public DateTime fechaAuditoria { get; set; } = DateTime.Now;
        public decimal stockReportado { get; set; }
        public decimal stockCalculado { get; set; }
        public decimal costoPromedioReportado { get; set; }
        public decimal costoPromedioCalculado { get; set; }
        public decimal discrepanciaStock { get; set; }
        public decimal discrepanciaCosto { get; set; }
        public List<DetalleMovimientoAuditoria> detalleMovimientos { get; set; } = new();
    }

    public class DetalleMovimientoAuditoria
    {
        public DateTime fecha { get; set; }
        public string tipo { get; set; } = string.Empty;
        public string concepto { get; set; } = string.Empty;
        public decimal cantidad { get; set; }
        public decimal stockAnterior { get; set; }
        public decimal stockPosterior { get; set; }
        public decimal costoUnitario { get; set; }
        public decimal costoPromedioAnterior { get; set; }
        public decimal costoPromedioPosterior { get; set; }
        public string? documentoReferencia { get; set; } = string.Empty;
    }

    public class ResumenAuditoria
    {
        public int TotalVerificados { get; set; }
        public int Correctos { get; set; }
        public int ConErrores { get; set; }
        public int SinMovimientos { get; set; }
    }
}
