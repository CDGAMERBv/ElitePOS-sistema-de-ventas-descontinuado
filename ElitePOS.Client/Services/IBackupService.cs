using ElitePOS.Services;
using ElitePOS.Shared.Models;

namespace ElitePOS.Client.Services
{
    public interface IBackupService
    {
        /// <summary>
        /// Genera un backup completo de toda la data de la empresa en formato JSON
        /// </summary>
        Task<string> GenerarBackupCompleto(string empresaId);

        /// <summary>
        /// Descarga el backup como archivo JSON
        /// </summary>
        Task DescargarBackup(string empresaId, string nombreEmpresa);
    }
}


