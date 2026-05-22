using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    /// <summary>
    /// Servicio de Cola Serializada para movimientos de Kardex.
    /// Garantiza que los movimientos de una misma empresa se procesen
    /// de forma SECUENCIAL, eliminando las condiciones de carrera (race conditions).
    /// </summary>
    public interface IKardexQueueService
    {
        /// <summary>
        /// Registra una SALIDA de inventario de forma serializada y segura.
        /// Calcula el saldo correcto y persiste el movimiento con la estructura Keyfacil.
        /// </summary>
        Task<RegistroKardexResult> RegistrarSalidaAsync(
            string empresaId,
            string productoId,
            string productoNombre,
            int cantidad,
            string concepto,
            string? numeroOperacion = null,
            string? usuarioId = null,
            string? usuarioNombre = null,
            bool permitirVentaSinStock = false);

        /// <summary>
        /// Registra una ENTRADA de inventario de forma serializada y segura.
        /// </summary>
        Task<RegistroKardexResult> RegistrarEntradaAsync(
            string empresaId,
            string productoId,
            string productoNombre,
            int cantidad,
            string concepto,
            string? numeroOperacion = null,
            string? usuarioId = null,
            string? usuarioNombre = null);

        /// <summary>
        /// Registra un AJUSTE de inventario (corrección directa de saldo).
        /// </summary>
        Task<RegistroKardexResult> RegistrarAjusteAsync(
            string empresaId,
            string productoId,
            string productoNombre,
            int cantidadAjuste,
            string tipoAjuste, // "Entrada" o "Salida"
            string concepto = "Ajuste Manual",
            string? usuarioId = null,
            string? usuarioNombre = null);
    }

    /// <summary>
    /// Resultado de una operación de Kardex serializada.
    /// </summary>
    public class RegistroKardexResult
    {
        public bool exitoso { get; set; }
        public string mensaje { get; set; } = "";
        public int stockResultante { get; set; }
        public string numeroOperacion { get; set; } = "";
        public string? error { get; set; }
    }
}


