using ElitePOS.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElitePOS.Services
{
    public interface IAuditoriaService
    {
        Task RegistrarAccionAsync(AuditoriaModel accion);
        Task<List<AuditoriaModel>> ObtenerAuditoriaAsync(DateTime desde, DateTime hasta, string? usuario = null, string? modulo = null);
    }
}


