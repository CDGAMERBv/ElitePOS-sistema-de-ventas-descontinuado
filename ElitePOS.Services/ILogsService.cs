using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface ILogsService
    {
        Task<bool> RegistrarLog(string empresaId, string usuarioId, string nombreUsuario, string accion, string detalle, string modulo = "", string ip = "");
        Task<bool> RegistrarAccesoRestringido(string empresaId, string usuarioId, string nombreUsuario, string moduloIntentado);
        Task<bool> RegistrarEdicionPrecio(string empresaId, string usuarioId, string nombreUsuario, string productoId, decimal precioAnterior, decimal precioNuevo);
        Task<IEnumerable<LogModel>> ObtenerLogs(string empresaId);
    }
}


