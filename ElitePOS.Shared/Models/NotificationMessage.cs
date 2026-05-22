namespace ElitePOS.Shared.Models
{
    public class NotificationMessage
    {
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public int Duration { get; set; } = 4000; // milisegundos
    }

    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

}
