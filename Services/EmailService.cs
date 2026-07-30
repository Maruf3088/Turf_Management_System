using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using turf_management_system.Services.Interfaces;

namespace turf_management_system.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string otpCode)
        {
            var host = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var portStr = _configuration["SmtpSettings:Port"];
            int port = string.IsNullOrEmpty(portStr) ? 587 : int.Parse(portStr);
            var enableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"] ?? "true");
            var senderEmail = _configuration["SmtpSettings:SenderEmail"];
            var senderName = _configuration["SmtpSettings:SenderName"] ?? "Turf Management System";
            var password = _configuration["SmtpSettings:Password"];

            var subject = "Password Reset Request";
            var body = $@"
                <div style='font-family: ""Inter"", ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; background: linear-gradient(to bottom right, #0f1c18, #1a2f27); padding: 40px; border-radius: 20px; box-shadow: 0 10px 30px rgba(0,0,0,0.5); color: #ffffff;'>
                    <div style='text-align: center; margin-bottom: 30px;'>
                        <h2 style='color: #10b981; margin: 0; font-size: 28px; font-weight: 800; letter-spacing: 1px;'>Turf Management</h2>
                    </div>
                    <hr style='border: 0; border-top: 1px solid rgba(255,255,255,0.1); margin: 20px 0;' />
                    <p style='font-size: 15px; line-height: 1.6; color: #e2e8f0;'>Hello,</p>
                    <p style='font-size: 15px; line-height: 1.6; color: #cbd5e1;'>We received a request to reset your password. Use the following 6-digit OTP to proceed:</p>
                    <div style='text-align: center; margin: 35px 0;'>
                        <div style='background: rgba(201,168,76,0.1); color: #C9A84C; padding: 20px; font-size: 32px; font-weight: bold; letter-spacing: 5px; border-radius: 12px; border: 1px dashed #C9A84C; display: inline-block;'>{otpCode}</div>
                    </div>
                    <p style='font-size: 13px; line-height: 1.5; color: #94a3b8;'>This OTP is valid for <strong>15 minutes</strong> and can only be used once.</p>
                    <hr style='border: 0; border-top: 1px solid rgba(255,255,255,0.1); margin: 30px 0 20px 0;' />
                    <p style='font-size: 12px; color: #64748b; text-align: center;'>If you did not request a password reset, please ignore this email. Your password will remain unchanged.</p>
                </div>";

            _logger.LogInformation("Password Reset OTP generated for {ToEmail}", toEmail);

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("SMTP credentials not configured in appsettings.json. Reset Email output logged above.");
                return false;
            }

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(senderEmail, password),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Password reset email sent to {ToEmail}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {ToEmail}", toEmail);
                return false;
            }
        }
    }
}
