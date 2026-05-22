using System;
using System.Text.Json.Serialization;

namespace ElitePOS.Shared.Models
{
    /// <summary>
    /// Movimiento de Kardex de Nivel Empresarial con Costeo Profesional
    /// Implementa Promedio Ponderado según normas contables
    /// </summary>
    public class MovimientoKardexProfesional : MovimientoKardexModel
    {
        // 🆕 CAMPOS DE COSTEO PROFESIONAL
        [JsonPropertyName("costoUnitario")]
        public decimal costoUnitario { get; set; } = 0;              // 💰 Costo unitario al momento del movimiento

        [JsonPropertyName("costoTotalMovimiento")]
        public decimal costoTotalMovimiento { get; set; } = 0;       // 💰 Costo total del movimiento (Cantidad * CostoUnitario)

        [JsonPropertyName("costoPromedioAnterior")]
        public decimal costoPromedioAnterior { get; set; } = 0;       // 📈 Costo promedio ANTES del movimiento

        [JsonPropertyName("costoPromedioNuevo")]
        public decimal costoPromedioNuevo { get; set; } = 0;           // 📈 Costo promedio DESPUÉS del movimiento
        
        // 📊 Campos adicionales para análisis avanzado
        [JsonPropertyName("margenUtilidadUnitario")]
        public decimal margenUtilidadUnitario { get; set; } = 0;     // 💵 Margen por unidad (solo para ventas)

        [JsonPropertyName("margenUtilidadTotal")]
        public decimal margenUtilidadTotal { get; set; } = 0;         // 💵 Margen total del movimiento

        [JsonPropertyName("porcentajeMargen")]
        public decimal porcentajeMargen { get; set; } = 0;            // 📊 Porcentaje de margen
        
        // 🏷️ Referencias para trazabilidad completa
        [JsonPropertyName("proveedorId")]
        public string? proveedorId { get; set; }                     // 🏢 ID del proveedor (compras)

        [JsonPropertyName("proveedorNombre")]
        public string? proveedorNombre { get; set; } = string.Empty;  // 🏢 Nombre del proveedor

        [JsonPropertyName("loteNumero")]
        public string? loteNumero { get; set; } = string.Empty;       // 📦 Número de lote (si aplica)

        [JsonPropertyName("loteFechaVencimiento")]
        public DateTime? loteFechaVencimiento { get; set; }          // 📅 Fecha vencimiento del lote
        
        // 📋 Campos de auditoría mejorados
        [JsonPropertyName("ipDireccion")]
        public string ipDireccion { get; set; } = string.Empty;       // 🌐 IP del usuario

        [JsonPropertyName("navegador")]
        public string navegador { get; set; } = string.Empty;        // 🌐 Navegador usado

        [JsonPropertyName("dispositivo")]
        public string dispositivo { get; set; } = string.Empty;       // 💻 Tipo de dispositivo
        
        // 🛡️ CAMPOS DE PROTECCIÓN DE INTEGRIDAD
        [JsonPropertyName("hashVerificacion")]
        public string hashVerificacion { get; set; } = string.Empty;   // 🔐 Hash para detectar manipulación

        [JsonPropertyName("fechaCreacion")]
        public DateTime fechaCreacion { get; set; } = DateTime.Now;   // 📅 Fecha de creación (inmutable)

        [JsonPropertyName("creadoPor")]
        public string creadoPor { get; set; } = string.Empty;         // 👤 Usuario que creó el movimiento

        [JsonPropertyName("esInmutable")]
        public bool esInmutable { get; set; } = true;                 // 🔒 Marca explícita de inmutabilidad
        
        // 🧮 Métodos de cálculo automático
        public void CalcularCostosMovimiento(decimal costoUnitarioP, int cantidadP)
        {
            costoUnitario = costoUnitarioP;
            costoTotalMovimiento = costoUnitarioP * cantidadP;
        }
        
        public void CalcularMargenes(decimal precioVenta, int cantidadP)
        {
            if (precioVenta > 0 && costoUnitario > 0)
            {
                margenUtilidadUnitario = precioVenta - costoUnitario;
                margenUtilidadTotal = margenUtilidadUnitario * cantidadP;
                porcentajeMargen = (margenUtilidadUnitario / precioVenta) * 100;
            }
        }
        
        /// <summary>
        /// Calcula el nuevo costo promedio usando la fórmula de Promedio Ponderado
        /// </summary>
        public static decimal CalcularCostoPromedioPonderado(
            decimal costoPromedioActual, 
            int stockActual, 
            decimal costoEntrante, 
            int cantidadEntrante)
        {
            decimal costoTotalActual = costoPromedioActual * stockActual;
            decimal costoTotalEntrante = costoEntrante * cantidadEntrante;
            decimal nuevoCostoTotal = costoTotalActual + costoTotalEntrante;
            int nuevoStockTotal = stockActual + cantidadEntrante;
            
            return nuevoStockTotal > 0 ? nuevoCostoTotal / nuevoStockTotal : 0;
        }
        
        /// <summary>
        /// Valida que el movimiento sea consistente con las normas contables
        /// </summary>
        public bool EsValido()
        {
            return 
                !string.IsNullOrEmpty(productoId) &&
                !string.IsNullOrEmpty(tipoMovimiento) &&
                !string.IsNullOrEmpty(concepto) &&
                cantidad > 0 &&
                costoUnitario >= 0 &&
                costoTotalMovimiento >= 0 &&
                costoPromedioAnterior >= 0 &&
                costoPromedioNuevo >= 0;
        }
        
        /// <summary>
        /// Genera una descripción detallada del movimiento para auditoría
        /// </summary>
        public string GenerarDescripcionAuditoria()
        {
            return $"[{fecha:dd/MM/yyyy HH:mm}] {tipoMovimiento.ToUpper()}: {concepto} " +
                   $"| Producto: {productoNombre} " +
                   $"| Cantidad: {cantidad:N2} " +
                   $"| Costo Unit: S/ {costoUnitario:N2} " +
                   $"| Costo Total: S/ {costoTotalMovimiento:N2} " +
                   $"| Costo Prom Ant: S/ {costoPromedioAnterior:N2} " +
                   $"| Costo Prom Nuevo: S/ {costoPromedioNuevo:N2} " +
                   $"| Usuario: {usuarioNombre}";
        }
    }
}
