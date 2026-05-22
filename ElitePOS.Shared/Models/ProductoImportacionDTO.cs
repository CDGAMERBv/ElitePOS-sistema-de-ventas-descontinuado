namespace ElitePOS.Shared.Models
{
    public class ProductoImportacionDTO : ProductoModel
    {
        public bool seleccionado { get; set; } = true;
        public string? mensajeError { get; set; }
        public bool tieneError => !string.IsNullOrEmpty(mensajeError);
    }
}
