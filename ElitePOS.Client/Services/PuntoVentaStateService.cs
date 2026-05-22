using ElitePOS.Services;
using ElitePOS.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElitePOS.Client.Services
{
    public class PuntoVentaStateService
    {
        private readonly INumeracionService _numeracionService;

        public PuntoVentaStateService(INumeracionService numeracionService)
        {
            _numeracionService = numeracionService;
        }

        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();
        public void InvokeStateChange() => NotifyStateChanged();

        // ═══════════════════════════════════════════════════════════
        // ESTADO DE LA VENTA
        // ═══════════════════════════════════════════════════════════
        public List<VentaItemModel> items { get; set; } = new();
        public ClienteModel clienteSeleccionado { get; set; } = new();
        
        public string tipoComprobante { get; set; } = "Boleta";
        public string pagoForma { get; set; } = "Contado";
        public string metodoPago { get; set; } = "Efectivo";
        
        public decimal subtotal { get; set; }
        public decimal igv { get; set; }
        public decimal totalVenta { get; set; }
        public decimal montoRecibido { get; set; }
        public decimal vuelto { get; set; }

        // Metadata
        public string serieComprobante { get; set; } = "B001";
        public string ultimoComprobante { get; set; } = string.Empty;
        public string observaciones { get; set; } = string.Empty;
        public string ordenCompra { get; set; } = string.Empty;
        public string guiaRemision { get; set; } = string.Empty;
        public string placa { get; set; } = string.Empty;

        // UI Toggles & Dates (Avanzado)
        public DateTime fechaEmision { get; set; } = DateTime.Now;
        public DateTime? fechaVencimiento { get; set; }
        public bool showObservaciones { get; set; }
        public bool showOrdenCompra { get; set; }
        public bool showGuiaRemision { get; set; }
        public bool showPlaca { get; set; }

        // ═══════════════════════════════════════════════════════════
        // LÓGICA
        // ═══════════════════════════════════════════════════════════

        public void AgregarProducto(ProductoModel p)
        {
            var existente = items.FirstOrDefault(i => i.productoId == p.id);
            if (existente != null)
            {
                existente.cantidad++;
                existente.subtotal = existente.precioUnitario * existente.cantidad;
            }
            else
            {
                items.Add(new VentaItemModel
                {
                    productoId = p.id,
                    nombreProducto = p.nombre,
                    precioUnitario = p.precioVenta,
                    cantidad = 1,
                    subtotal = p.precioVenta
                });
            }
            ActualizarTotales();
        }

        public void ActualizarTotales()
        {
            totalVenta = items.Sum(i => i.subtotal);
            igv = totalVenta * 0.18m; // Simplificado para demo, debería ser configurable
            subtotal = totalVenta - igv;
            vuelto = Math.Max(0, montoRecibido - totalVenta);
            NotifyStateChanged();
        }

        public void LimpiarVenta()
        {
            items.Clear();
            clienteSeleccionado = new ClienteModel();
            pagoForma = "Contado";
            metodoPago = "Efectivo";
            montoRecibido = 0;
            vuelto = 0;
            observaciones = string.Empty;
            ordenCompra = string.Empty;
            guiaRemision = string.Empty;
            placa = string.Empty;

            // Reset UI Elite
            fechaEmision = DateTime.Now;
            fechaVencimiento = null;
            showObservaciones = false;
            showOrdenCompra = false;
            showGuiaRemision = false;
            showPlaca = false;

            ActualizarTotales();
        }
    }
}
