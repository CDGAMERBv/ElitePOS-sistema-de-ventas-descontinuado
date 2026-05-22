using System.Net.Http.Json;
using ElitePOS.Services;
using ElitePOS.Shared.Models;

namespace ElitePOS.Client.Services
{
    public class AsistenteIAServiceProxy : IAsistenteIAService
    {
        private readonly HttpClient _httpClient;

        public AsistenteIAServiceProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> ChatAsync(string userMessage, List<ChatMessage> history)
        {
            var response = await _httpClient.PostAsJsonAsync("api/asistente/chat", new { message = userMessage, history = history });
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return "Error al conectar con el asistente.";
        }

        public async Task<string> ChatWhatsAppAsync(string userMessage, string customPrompt, string empresaId)
        {
            var response = await _httpClient.PostAsJsonAsync("api/whatsapp/mensaje", new { sender = "", text = userMessage });
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return "Error al enviar mensaje de WhatsApp.";
        }
    }
}


