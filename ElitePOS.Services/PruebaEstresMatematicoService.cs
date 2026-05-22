using ElitePOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElitePOS.Services
{
    public class PruebaEstresMatematicoService
    {
        public async Task<ResultadoPrueba> EjecutarPruebaPrincipal()
        {
            var resultado = new ResultadoPrueba
            {
                nombrePrueba = "Promedio Ponderado Básico",
                fechaEjecucion = DateTime.Now
            };

            try
            {
                decimal costoPromedioInicial = 0m;
                int stockInicial = 0;
                
                decimal nuevoCostoPromedio1 = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(
                    costoPromedioInicial, stockInicial, 10m, 1);
                
                decimal nuevoCostoPromedio2 = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(
                    nuevoCostoPromedio1, 1, 20m, 1);
                
                decimal costoEsperado = 15.00m;
                decimal discrepanciaVal = Math.Abs(nuevoCostoPromedio2 - costoEsperado);
                
                resultado.costoCalculado = nuevoCostoPromedio2;
                resultado.costoEsperado = costoEsperado;
                resultado.discrepancia = discrepanciaVal;
                
                const decimal TOLERANCIA = 0.000001m;
                resultado.estado = discrepanciaVal <= TOLERANCIA ? "OK" : "ERROR";
                resultado.mensaje = resultado.estado == "OK" 
                    ? $" Prueba exitosa: S/ {nuevoCostoPromedio2:N6} (esperado: S/ {costoEsperado:N6})"
                    : $" Prueba fallida: S/ {nuevoCostoPromedio2:N6} vs S/ {costoEsperado:N6}";
                
                return resultado;
            }
            catch (Exception ex)
            {
                resultado.estado = "ERROR";
                resultado.mensaje = $"Excepción: {ex.Message}";
                return resultado;
            }
        }

        public async Task<List<ResultadoPrueba>> EjecutarPruebasAdicionales()
        {
            var resultados = new List<ResultadoPrueba>();
            resultados.Add(await EjecutarPruebaDecimales());
            resultados.Add(await EjecutarPruebaValoresGrandes());
            resultados.Add(await EjecutarPruebaPrecisionExtrema());
            resultados.Add(await EjecutarPruebaCantidadVariable());
            resultados.Add(await EjecutarPruebaEscenarioComplejo());
            return resultados;
        }

        private async Task<ResultadoPrueba> EjecutarPruebaDecimales()
        {
            var resultado = new ResultadoPrueba
            {
                nombrePrueba = "Decimales Múltiples",
                fechaEjecucion = DateTime.Now
            };

            decimal calculado = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(0, 0, 10.50m, 2);
            calculado = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(calculado, 2, 15.75m, 3);
            decimal esperado = 13.65m;
            
            resultado.costoCalculado = calculado;
            resultado.costoEsperado = esperado;
            resultado.discrepancia = Math.Abs(calculado - esperado);
            resultado.estado = resultado.discrepancia <= 0.000001m ? "OK" : "ERROR";
            resultado.mensaje = $"Decimales: S/ {calculado:N6} (esperado: S/ {esperado:N6})";

            return resultado;
        }

        private async Task<ResultadoPrueba> EjecutarPruebaValoresGrandes()
        {
            var resultado = new ResultadoPrueba
            {
                nombrePrueba = "Valores Grandes",
                fechaEjecucion = DateTime.Now
            };

            decimal calculado = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(0, 0, 1000.123456m, 100);
            calculado = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(calculado, 100, 2000.654321m, 50);
            decimal esperado = 1333.633711m;
            
            resultado.costoCalculado = calculado;
            resultado.costoEsperado = esperado;
            resultado.discrepancia = Math.Abs(calculado - esperado);
            resultado.estado = resultado.discrepancia <= 0.000001m ? "OK" : "ERROR";
            resultado.mensaje = $"Grandes: S/ {calculado:N6} (esperado: S/ {esperado:N6})";

            return resultado;
        }

        private async Task<ResultadoPrueba> EjecutarPruebaPrecisionExtrema()
        {
            var resultado = new ResultadoPrueba
            {
                nombrePrueba = "Precisión Extrema",
                fechaEjecucion = DateTime.Now
            };

            decimal calculado = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(0, 0, 0.000001m, 1);
            calculado = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(calculado, 1, 0.000002m, 1);
            decimal esperado = 0.000002m;
            
            resultado.costoCalculado = calculado;
            resultado.costoEsperado = esperado;
            resultado.discrepancia = Math.Abs(calculado - esperado);
            resultado.estado = resultado.discrepancia <= 0.000001m ? "OK" : "ERROR";
            resultado.mensaje = $"Mínimos: S/ {calculado:N6} (esperado: S/ {esperado:N6})";

            return resultado;
        }

        private async Task<ResultadoPrueba> EjecutarPruebaCantidadVariable()
        {
            var resultado = new ResultadoPrueba
            {
                nombrePrueba = "Cantidad Variable",
                fechaEjecucion = DateTime.Now
            };

            decimal calculado = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(0, 0, 10m, 100);
            calculado = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(calculado, 100, 100m, 1);
            decimal esperado = 10.990099m;
            
            resultado.costoCalculado = calculado;
            resultado.costoEsperado = esperado;
            resultado.discrepancia = Math.Abs(calculado - esperado);
            resultado.estado = resultado.discrepancia <= 0.000001m ? "OK" : "ERROR";
            resultado.mensaje = $"Variable: S/ {calculado:N6} (esperado: S/ {esperado:N6})";

            return resultado;
        }

        private async Task<ResultadoPrueba> EjecutarPruebaEscenarioComplejo()
        {
            var resultado = new ResultadoPrueba
            {
                nombrePrueba = "Escenario Complejo",
                fechaEjecucion = DateTime.Now
            };

            decimal costo = 0m;
            int stockTotal = 0;
            var compras = new[]
            {
                new { CantidadVal = 10, CostoVal = 5.50m },
                new { CantidadVal = 25, CostoVal = 6.25m },
                new { CantidadVal = 15, CostoVal = 4.75m },
                new { CantidadVal = 30, CostoVal = 7.80m },
                new { CantidadVal = 20, CostoVal = 5.95m }
            };

            foreach (var compra in compras)
            {
                costo = MovimientoKardexProfesional.CalcularCostoPromedioPonderado(costo, stockTotal, compra.CostoVal, compra.CantidadVal);
                stockTotal += compra.CantidadVal;
            }

            decimal esperado = 6.244444m;
            resultado.costoCalculado = costo;
            resultado.costoEsperado = esperado;
            resultado.discrepancia = Math.Abs(costo - esperado);
            resultado.estado = resultado.discrepancia <= 0.000001m ? "OK" : "ERROR";
            resultado.mensaje = $"Complejo: S/ {costo:N6} (esperado: S/ {esperado:N6})";

            return resultado;
        }
    }
}
