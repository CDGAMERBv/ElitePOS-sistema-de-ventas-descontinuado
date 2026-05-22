using ElitePOS.Shared.Models;

namespace ElitePOS.Services
{
    public interface IAsistenteIAService
    {
        Task<string> ChatWhatsAppAsync(string userMessage, string customPrompt, string empresaId);
        Task<string> ChatAsync(string userMessage, List<ChatMessage> history);
    }
}


