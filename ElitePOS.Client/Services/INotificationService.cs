using ElitePOS.Services;
using ElitePOS.Shared.Models;
using System.Threading.Tasks;

namespace ElitePOS.Client.Services
{
    public interface INotificationService
    {
        event Action<NotificationMessage>? OnNotification;
        Task ShowSuccess(string message, int duration = 4000);
        Task ShowError(string message, int duration = 4000);
        Task ShowWarning(string message, int duration = 5000);
        Task ShowInfo(string message, int duration = 4000);
    }

    public class NotificationService : INotificationService
    {
        public event Action<NotificationMessage>? OnNotification;

        public Task ShowSuccess(string message, int duration = 4000)
        {
            Notify(message, NotificationType.Success, duration);
            return Task.CompletedTask;
        }

        public Task ShowError(string message, int duration = 4000)
        {
            Notify(message, NotificationType.Error, duration);
            return Task.CompletedTask;
        }

        public Task ShowWarning(string message, int duration = 5000)
        {
            Notify(message, NotificationType.Warning, duration);
            return Task.CompletedTask;
        }

        public Task ShowInfo(string message, int duration = 4000)
        {
            Notify(message, NotificationType.Info, duration);
            return Task.CompletedTask;
        }

        private void Notify(string message, NotificationType type, int duration)
        {
            OnNotification?.Invoke(new NotificationMessage
            {
                Message = message,
                Type = type,
                Duration = duration
            });
        }
    }
}



