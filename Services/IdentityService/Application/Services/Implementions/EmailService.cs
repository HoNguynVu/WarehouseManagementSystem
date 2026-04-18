using Application.Helpers;
using Application.Services.Interfaces;
using Google.Apis.Auth.OAuth2;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Threading.Tasks;

namespace Application.Services.Implementions
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly EmailHelpers _emailHelpers;

        public EmailService(IOptions<EmailSettings> emailSettings, EmailHelpers emailHelpers)
        {
            _emailSettings = emailSettings.Value;
            _emailHelpers = emailHelpers;
        }

        // 1. Đã bỏ tham số 'type' ở hàm này
        private async Task SendEmailAsync(MimeMessage emailMessage)
        {
            var credential = GoogleCredential
                .FromJsonParameters(new JsonCredentialParameters
                {
                    ClientId = _emailSettings.ClientId,
                    ClientSecret = _emailSettings.ClientSecret,
                    RefreshToken = _emailSettings.RefreshToken,
                    Type = "authorized_user" // 2. Bắt buộc phải FIX CỨNG là chữ này
                })
                .CreateScoped("https://mail.google.com/");

            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(new SaslMechanismOAuth2(_emailSettings.From, accessToken));
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }

        public async Task SendVerificationEmail(string email, string otp)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Warehouse System", _emailSettings.From));
            emailMessage.To.Add(MailboxAddress.Parse(email));
            emailMessage.Subject = EmailSubjects.AccountVerification.Subject;

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = _emailHelpers.CreateEmailByTemplate(EmailSubjects.AccountVerification.Title, otp, EmailSubjects.AccountVerification.Purpose);

            emailMessage.Body = bodyBuilder.ToMessageBody();

            // 3. Xóa bỏ tham số thứ 2 khi gọi hàm
            await SendEmailAsync(emailMessage);
        }

        public async Task SendPasswordResetEmail(string email, string otp)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Warehouse System", _emailSettings.From));
            emailMessage.To.Add(MailboxAddress.Parse(email));
            emailMessage.Subject = EmailSubjects.PasswordReset.Subject;

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = _emailHelpers.CreateEmailByTemplate(EmailSubjects.PasswordReset.Title, otp, EmailSubjects.PasswordReset.Purpose);

            emailMessage.Body = bodyBuilder.ToMessageBody();

            // 4. Xóa bỏ tham số thứ 2 khi gọi hàm
            await SendEmailAsync(emailMessage);
        }
    }
}