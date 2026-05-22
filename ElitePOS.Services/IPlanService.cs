using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IPlanService
    {
        Task<PlanModel> ObtenerPlanActual();
        Task<bool> ActualizarContadores();
    }
}


