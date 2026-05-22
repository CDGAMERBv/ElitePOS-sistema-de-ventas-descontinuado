namespace ElitePOS.Shared.Models
{
    public class ConfiguracionImpresionModel
    {
        public string Id { get; set; } = "config-impresion";
        public string EmpresaId { get; set; } = "empresa-demo"; // Multi-empresa
        public string FormatoTicket { get; set; } = "80mm";
        public string Encabezado { get; set; } = string.Empty;
        public string PieDePagina { get; set; } = string.Empty;
        public bool ImprimirAutomaticamente { get; set; } = true; // ✅ Habilitado por defecto
        public bool MostrarLogo { get; set; } = true;
    }
}
