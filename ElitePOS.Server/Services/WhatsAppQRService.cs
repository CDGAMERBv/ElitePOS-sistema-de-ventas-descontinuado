using System.Collections.Concurrent;

namespace ElitePOS.Server.Services
{
    public class WhatsAppQRService : IWhatsAppQRService
    {
        private readonly ConcurrentDictionary<string, string> _qrCodes = new();
        private readonly ConcurrentDictionary<string, string> _statuses = new();

        public void UpdateQR(string empresaId, string qrBase64)
        {
            _qrCodes[empresaId] = qrBase64;
            _statuses[empresaId] = "QR_READY";
        }

        public void UpdateStatus(string empresaId, string status)
        {
            _statuses[empresaId] = status;
            if (status == "CONNECTED")
            {
                _qrCodes.TryRemove(empresaId, out _);
            }
        }

        public string? GetQR(string empresaId)
        {
            _qrCodes.TryGetValue(empresaId, out var qr);
            return qr;
        }

        public string GetStatus(string empresaId)
        {
            _statuses.TryGetValue(empresaId, out var status);
            return status ?? "DISCONNECTED";
        }

        public void Clear(string empresaId)
        {
            _qrCodes.TryRemove(empresaId, out _);
            _statuses.TryRemove(empresaId, out _);
        }
    }
}
