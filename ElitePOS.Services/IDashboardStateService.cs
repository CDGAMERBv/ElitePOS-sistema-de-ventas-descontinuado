using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IDashboardStateService
    {
        // Datos cacheados
        decimal VentasHoy { get; }
        decimal VentasSemana { get; }
        int TransaccionesHoy { get; }
        int StockBajoCount { get; }
        int TotalClientes { get; }
        List<VentaModel> VentasRecientes { get; }
        List<ProductoTopModel> ProductosMasVendidos { get; }
        List<ProductoModel> ProductosBajoStock { get; }
        List<VentaMensualModel> VentasMensuales { get; }
        
        bool DatosCargados { get; }

        // MÉTODOS
        Task CargarDatosAsync(bool forzar = false);
        void MarcarComoDesactualizado();
        
        // Eventos
        event Action? OnChange;
    }
}


