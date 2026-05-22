namespace ElitePOS.Services
{
    public interface IValidacionDocumentosService
    {
        /// <summary>
        /// Simula la validación de RUC/DNI (futuro: integrar con API de SUNAT/RENIEC)
        /// </summary>
        Task<ValidacionDocumentoResult> ValidarDocumento(string numeroDocumento);

        /// <summary>
        /// Valida formato de RUC (11 dígitos)
        /// </summary>
        bool EsRucValido(string ruc);

        /// <summary>
        /// Valida formato de DNI (8 dígitos)
        /// </summary>
        bool EsDniValido(string dni);
    }

    public class ValidacionDocumentoResult
    {
        public bool esValido { get; set; }
        public string tipoDocumento { get; set; } = string.Empty; // "RUC" o "DNI"
        public string razonSocial { get; set; } = string.Empty; // Nombre/razón social
        public string estado { get; set; } = string.Empty; // "ACTIVO", "BAJA"
        public string direccion { get; set; } = string.Empty;
        public string mensaje { get; set; } = string.Empty; // Mensaje de error o información
    }
}
