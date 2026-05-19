using System.Threading.Tasks;
using turf_management_system.Models.Domain;

namespace turf_management_system.Services.Interfaces
{
    public interface IExternalNotificationService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendSmsAsync(string toPhoneNumber, string message);
        Task SendPushNotificationAsync(int userId, string title, string message);
        Task DispatchNotificationAsync(int userId, string title, string message, NotificationType type);
    }
}
