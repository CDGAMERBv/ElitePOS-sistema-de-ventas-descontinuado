using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IFacturacionService
    {
        Task<(bool Exito, string Mensaje, string LinkPdf, string LinkXml)> EnviarVentaASunat(VentaModel venta);
    }
}


