using Microsoft.AspNetCore.Mvc;
using ElitePOS.Shared.Models;
using ElitePOS.Services;
using System.Text.Json.Serialization;

namespace ElitePOS.Server.Controllers;

[ApiController]
[Route("api/asistente")]
public class AsistenteController : ControllerBase
{
    private readonly IAsistenteIAService _aiService;

    public AsistenteController(IAsistenteIAService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("chat")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Chat([FromBody] ChatRequestDTO request)
    {
        if (string.IsNullOrEmpty(request.Message)) return BadRequest("Mensaje vacío");

        try
        {
            var response = await _aiService.ChatAsync(request.Message, request.History);
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IA ASISTENTE ERROR] {ex.Message}");
            return StatusCode(500, "Error interno al procesar el chat con IA");
        }
    }
}

public class ChatRequestDTO {
    [JsonPropertyName("message")] public string Message { get; set; } = "";
    [JsonPropertyName("history")] public List<ChatMessage> History { get; set; } = new();
}
