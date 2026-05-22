using Microsoft.AspNetCore.Mvc;
using ElitePOS.Shared.Models;
using ElitePOS.Services;
using ElitePOS.Server.Services;
using System.Text.Json.Serialization;

namespace ElitePOS.Server.Controllers;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private readonly IAsistenteIAService _aiService;
    private readonly IConfiguracionEmpresaService _configService;
    private readonly IWhatsAppQRService _qrService;

    public WhatsAppController(
        IAsistenteIAService aiService, 
        IConfiguracionEmpresaService configService,
        IWhatsAppQRService qrService)
    {
        _aiService = aiService;
        _configService = configService;
        _qrService = qrService;
    }

    [HttpPost("mensaje")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReceiveMessage([FromBody] WhatsAppMensajeDTO payload)
    {
        // 1. Validar mensaje
        if (string.IsNullOrEmpty(payload.Text)) return Ok();

        // 2. EmpresaId (default para bot local)
        string empresaId = "default_empresa"; 
        var config = await _configService.ObtenerConfiguracionPorEmpresa(empresaId);

        // 3. Regla de Negocio: Toggle Opcional
        if (config == null || !config.Integraciones.WhatsApp.IsEnabled)
        {
            return Ok();
        }

        // 4. Procesar con IA
        string customPrompt = !string.IsNullOrEmpty(config.Integraciones.WhatsApp.PromptPersonalizado) 
            ? config.Integraciones.WhatsApp.PromptPersonalizado 
            : config.Integraciones.WhatsApp.BotPrompt;

        string aiResponse = await _aiService.ChatWhatsAppAsync(payload.Text, customPrompt, empresaId);

        // 5. Retornar respuesta directa para el bot de Node.js
        Console.WriteLine($"[WHATSAPP BOT] IA Response for {payload.Sender}: {aiResponse}");

        return Ok(aiResponse);
    }

    // --- NUEVOS ENDPOINTS PARA EL FLUJO DE QR ---

    [HttpPost("qr")]
    [IgnoreAntiforgeryToken]
    public IActionResult UpdateQR([FromBody] WhatsAppQRUpdateDTO payload)
    {
        Console.WriteLine($"[WHATSAPP BOT] Nuevo QR recibido para {payload.EmpresaId}");
        _qrService.UpdateQR(payload.EmpresaId, payload.Qr);
        return Ok();
    }

    [HttpPost("status")]
    [IgnoreAntiforgeryToken]
    public IActionResult UpdateStatus([FromBody] WhatsAppStatusUpdateDTO payload)
    {
        Console.WriteLine($"[WHATSAPP BOT] Nuevo estado recibido para {payload.EmpresaId}: {payload.Status}");
        _qrService.UpdateStatus(payload.EmpresaId, payload.Status);
        return Ok();
    }

    [HttpGet("qr-sync/{empresaId}")]
    public IActionResult GetQRSync(string empresaId)
    {
        var qr = _qrService.GetQR(empresaId);
        var status = _qrService.GetStatus(empresaId);
        return Ok(new { qr, status });
    }

}

// --- DTO Simplificado para Bot Local ---
public class WhatsAppMensajeDTO {
    [JsonPropertyName("sender")] public string Sender { get; set; } = "";
    [JsonPropertyName("text")] public string Text { get; set; } = "";
}

public class WhatsAppQRUpdateDTO {
    [JsonPropertyName("empresaId")] public string EmpresaId { get; set; } = "";
    [JsonPropertyName("qr")] public string Qr { get; set; } = "";
}

public class WhatsAppStatusUpdateDTO {
    [JsonPropertyName("empresaId")] public string EmpresaId { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
}
