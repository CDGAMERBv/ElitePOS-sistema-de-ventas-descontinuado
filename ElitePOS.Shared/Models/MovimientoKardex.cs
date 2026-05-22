using System;

namespace ElitePOS.Shared.Models
{
    public class MovimientoKardex
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProductoId { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Tipo { get; set; } = string.Empty; // Entrada/Salida
        public string Motivo { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public int StockAnterior { get; set; }
        public int StockResultante { get; set; }
    }
}
