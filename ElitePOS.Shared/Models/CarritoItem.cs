namespace ElitePOS.Shared.Models
{
    public class CarritoItem
    {
        public string ProductoId { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public int Cantidad { get; set; }
        public int StockDisponible { get; set; }
        public string UnidadMedida { get; set; } = "Unidad";

        public decimal Subtotal => PrecioVenta * Cantidad;
    }
}
