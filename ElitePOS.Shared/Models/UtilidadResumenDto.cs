namespace ElitePOS.Shared.Models
{
    public class UtilidadResumenDto
    {
        public string productoId { get; set; } = "";
        public string nombreProducto { get; set; } = "";
        public string categoria { get; set; } = "";
        public int cantidadVendida { get; set; }
        public decimal costoTotal { get; set; }
        public decimal ingresoTotal { get; set; }
        public decimal utilidadBruta => ingresoTotal - costoTotal;
        public decimal margen => (ingresoTotal > 0) ? (utilidadBruta / ingresoTotal) * 100 : 0;
    }
}
