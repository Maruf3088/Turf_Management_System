using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using turf_management_system.Models.Domain;
using turf_management_system.Repositories.Interfaces;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Services.Implementations
{
    public class ExternalNotificationService : IExternalNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ExternalNotificationService> _logger;
        private readonly IHostEnvironment _environment;

        public ExternalNotificationService(
            IUnitOfWork unitOfWork,
            ILogger<ExternalNotificationService> logger,
            IHostEnvironment environment)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _environment = environment;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            string logText = $@"
┌──────────────────────────────────────────────────────────┐
│                     [MOCK EMAIL SENT]                    │
├──────────────────────────────────────────────────────────┤
│ To: {toEmail,-52} │
│ Subject: {subject,-47} │
├──────────────────────────────────────────────────────────┤
│ Body:                                                    │
{IndentText(body, 54)}
└──────────────────────────────────────────────────────────┘";
            
            _logger.LogInformation(logText);
            await AppendToLogFileAsync(logText);
        }

        public async Task SendSmsAsync(string toPhoneNumber, string message)
        {
            string logText = $@"
┌──────────────────────────────────────────────────────────┐
│                      [MOCK SMS SENT]                     │
├──────────────────────────────────────────────────────────┤
│ To: {toPhoneNumber,-52} │
├──────────────────────────────────────────────────────────┤
│ Message:                                                 │
{IndentText(message, 54)}
└──────────────────────────────────────────────────────────┘";
            
            _logger.LogInformation(logText);
            await AppendToLogFileAsync(logText);
        }

        public async Task SendPushNotificationAsync(int userId, string title, string message)
        {
            string logText = $@"
┌──────────────────────────────────────────────────────────┐
│                     [MOCK PUSH SENT]                     │
├──────────────────────────────────────────────────────────┤
│ User ID: {userId,-47} │
│ Title: {title,-49} │
├──────────────────────────────────────────────────────────┤
│ Message:                                                 │
{IndentText(message, 54)}
└──────────────────────────────────────────────────────────┘";
            
            _logger.LogInformation(logText);
            await AppendToLogFileAsync(logText);
        }

        public async Task DispatchNotificationAsync(int userId, string title, string message, NotificationType type)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null) return;

            // Trigger Email (all critical notification types get emails)
            if (!string.IsNullOrEmpty(user.Email))
            {
                string subject = $"Turf Management System - {title}";
                string emailBody = $"Hello {user.FullName},\n\n{message}\n\nBest regards,\nTurf Management Team";
                await SendEmailAsync(user.Email, subject, emailBody);
            }

            // Trigger SMS if phone number is available
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                string smsMessage = $"[TMS] {title}: {message}";
                await SendSmsAsync(user.PhoneNumber, smsMessage);
            }

            // Trigger Push notification
            await SendPushNotificationAsync(userId, title, message);
        }

        private string IndentText(string text, int width)
        {
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = "";
            foreach (var line in lines)
            {
                var remaining = line;
                while (remaining.Length > 0)
                {
                    var chunk = remaining.Length > width - 4 ? remaining.Substring(0, width - 4) : remaining;
                    remaining = remaining.Length > width - 4 ? remaining.Substring(width - 4) : "";
                    result += $"│ {chunk.PadRight(width - 4)} │\n";
                }
            }
            return result.TrimEnd('\n');
        }

        private async Task AppendToLogFileAsync(string text)
        {
            try
            {
                var contentRootPath = _environment.ContentRootPath;
                var logDirectory = Path.Combine(contentRootPath, "wwwroot", "logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                var logFile = Path.Combine(logDirectory, "notifications.log");
                await File.AppendAllTextAsync(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}\n\n");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to external notifications log file.");
            }
        }
    }
}
