using ElitePOS.Services;
using ElitePOS.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;
using System.Linq;

namespace ElitePOS.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImpresionController : ControllerBase
    {
        private readonly IVentasService _ventasService;
        private readonly IConfiguracionEmpresaService _configService;
        private readonly IPdfImpresionService _pdfService;

        public ImpresionController(IVentasService ventasService, 
                                 IConfiguracionEmpresaService configService, 
                                 IPdfImpresionService pdfService)
        {
            _ventasService = ventasService;
            _configService = configService;
            _pdfService = pdfService;
        }

        [HttpGet("venta/{id}/{formato}")]
        public async Task<IActionResult> GetPdfVenta(string id, string formato)
        {
            // 🛡️ Asegurar consulta fresca a los servicios (Backend Firebase)
            var venta = await _ventasService.ObtenerVentaPorId(id);
            if (venta == null) return NotFound("Error: No se encontró la venta en la base de datos.");

            var config = await _configService.ObtenerConfiguracion(forceRefresh: true);
            if (config == null) return NotFound("Error: No se encontró la configuración de la empresa.");

            byte[] pdfBytes;
            string fileName = $"Comprobante_{venta.NumeroComprobante}_{formato.ToUpper()}.pdf";

            switch (formato.ToLower())
            {
                case "a4":
                    pdfBytes = await _pdfService.GenerarFacturaA4(venta, config);
                    break;
                case "a5":
                    pdfBytes = await _pdfService.GenerarFacturaA5(venta, config);
                    break;
                case "ticket":
                    pdfBytes = await _pdfService.GenerarTicket(venta, config);
                    break;
                default:
                    return BadRequest("Formato no soportado.");
            }

            // 💎 CONFIGURACIÓN PROFESIONAL: Abrir en el navegador (inline) en lugar de descargar
            Response.Headers.Append("Content-Disposition", new Microsoft.Extensions.Primitives.StringValues($"inline; filename=\"{fileName}\""));
            return File(pdfBytes, "application/pdf");
        }
    }
}
