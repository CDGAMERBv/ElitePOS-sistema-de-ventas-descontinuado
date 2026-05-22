namespace ElitePOS.Server.Services
{
    public interface IWhatsAppQRService
    {
        void UpdateQR(string empresaId, string qrBase64);
        void UpdateStatus(string empresaId, string status);
        string? GetQR(string empresaId);
        string GetStatus(string empresaId);
        void Clear(string empresaId);
    }
}
