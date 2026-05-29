namespace Bảo_Tàng_Đà_Nẵng.Services
{
    /// <summary>Interface dịch vụ gửi email OTP.</summary>
    public interface IEmailService
    {
        Task SendOtpAsync(string toEmail, string toName, string otpCode);
    }

    /// <summary>
    /// Triển khai dịch vụ email dùng Gmail SMTP.
    /// Yêu cầu cấu hình trong appsettings.json:
    ///   EmailSettings:SenderEmail    → Gmail dùng để gửi
    ///   EmailSettings:SenderName     → Tên hiển thị người gửi
    ///   EmailSettings:AppPassword    → App Password của Gmail (không phải mật khẩu thường)
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendOtpAsync(string toEmail, string toName, string otpCode)
        {
            var senderEmail = _config["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("Chưa cấu hình EmailSettings:SenderEmail");
            var senderName  = _config["EmailSettings:SenderName"] ?? "Bảo Tàng Mỹ Thuật Đà Nẵng";
            var appPassword = _config["EmailSettings:AppPassword"] ?? throw new InvalidOperationException("Chưa cấu hình EmailSettings:AppPassword");

            var subject = "🔐 Mã OTP Xác thực Admin - Bảo Tàng Mỹ Thuật Đà Nẵng";
            var body = BuildOtpEmailBody(toName, otpCode);

            using var smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new System.Net.NetworkCredential(senderEmail, appPassword),
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network
            };

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(new System.Net.Mail.MailAddress(toEmail, toName));

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Đã gửi OTP tới {Email} thành công.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi OTP tới {Email}.", toEmail);
                throw;
            }
        }

        private static string BuildOtpEmailBody(string toName, string otpCode)
        {
            return $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
            </head>
            <body style="margin:0;padding:0;background:#FAF8F5;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#FAF8F5;padding:40px 0;">
                <tr>
                  <td align="center">
                    <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;border:1px solid #EFECE6;box-shadow:0 8px 30px rgba(128,90,18,0.1);overflow:hidden;">
                      <!-- Header -->
                      <tr>
                        <td style="background:linear-gradient(135deg,#8B1E19,#805A12);padding:32px 40px;text-align:center;">
                          <div style="font-size:2rem;margin-bottom:8px;">🏛️</div>
                          <h1 style="color:#FFF8E8;font-size:1.3rem;margin:0;font-weight:700;letter-spacing:0.5px;">Bảo Tàng Mỹ Thuật Đà Nẵng</h1>
                          <p style="color:rgba(255,248,232,0.75);margin:6px 0 0;font-size:0.85rem;">Hệ thống xác thực Quản trị viên</p>
                        </td>
                      </tr>
                      <!-- Body -->
                      <tr>
                        <td style="padding:36px 40px;">
                          <p style="color:#2C1E0A;font-size:1rem;margin:0 0 12px;">Xin chào <strong>{toName}</strong>,</p>
                          <p style="color:#4b5563;font-size:0.95rem;line-height:1.6;margin:0 0 28px;">
                            Bạn đã yêu cầu đăng nhập vào khu vực Quản trị. Vui lòng sử dụng mã OTP dưới đây để hoàn tất xác thực:
                          </p>

                          <!-- OTP Box -->
                          <div style="text-align:center;margin:0 0 28px;">
                            <div style="display:inline-block;background:linear-gradient(135deg,#FFF2D6,#F5DCA6);border:2px solid rgba(197,160,89,0.5);border-radius:12px;padding:20px 40px;">
                              <p style="margin:0 0 6px;color:#805A12;font-size:0.78rem;font-weight:600;text-transform:uppercase;letter-spacing:1.5px;">Mã xác thực OTP</p>
                              <div style="font-size:2.8rem;font-weight:800;letter-spacing:10px;color:#8B1E19;font-family:'Courier New',monospace;">{otpCode}</div>
                            </div>
                          </div>

                          <div style="background:#FFF9F0;border:1px solid rgba(197,160,89,0.3);border-radius:8px;padding:14px 18px;margin-bottom:24px;">
                            <p style="margin:0;color:#805A12;font-size:0.85rem;">
                              ⏱️ Mã này có hiệu lực trong <strong>5 phút</strong> kể từ khi được gửi.
                            </p>
                          </div>

                          <p style="color:#6b7280;font-size:0.85rem;line-height:1.6;margin:0;">
                            Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này. Mã OTP sẽ tự động hết hiệu lực.
                          </p>
                        </td>
                      </tr>
                      <!-- Footer -->
                      <tr>
                        <td style="background:#FAF8F5;padding:20px 40px;border-top:1px solid #EFECE6;text-align:center;">
                          <p style="margin:0;color:#9ca3af;font-size:0.78rem;">
                            Email này được gửi tự động từ hệ thống Bảo Tàng Mỹ Thuật Đà Nẵng.<br/>
                            Vui lòng không trả lời email này.
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
        }
    }
}
