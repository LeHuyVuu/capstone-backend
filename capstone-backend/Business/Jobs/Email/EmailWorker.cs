
using capstone_backend.Business.DTOs.Email;
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Business.Jobs.Email
{
    public class EmailWorker : IEmailWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailWorker> _logger;

        public EmailWorker(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<EmailWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendCommissionUpdateEmailAsync(string newCommission)
        {
            try
            {
                var venueOwners = await _unitOfWork.Users.GetAsync(u => u.Role == "VENUEOWNER" && u.IsActive == true && u.IsDeleted == false);
                if (!venueOwners.Any())
                    return;

                string htmlTemplate = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #f5f3ff;
            font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;
            color: #1a1a1a;
        }}

        .wrapper {{
            width: 100%;
            padding: 40px 0;
        }}

        .container {{
            max-width: 600px;
            margin: 0 auto;
            background: #ffffff;
            border-radius: 14px;
            overflow: hidden;
            box-shadow: 0 10px 30px rgba(124, 58, 237, 0.08);
        }}

        .header {{
            background: linear-gradient(135deg, #a78bfa, #7c3aed);
            padding: 28px 20px;
            text-align: center;
            color: white;
        }}

        .header h2 {{
            margin: 0;
            font-size: 22px;
            font-weight: 600;
        }}

        .content {{
            padding: 30px 28px;
            font-size: 15px;
            line-height: 1.7;
            color: #444;
        }}

        .content p {{
            margin: 0 0 16px;
        }}

        .highlight-box {{
            text-align: center;
            margin: 30px 0;
            padding: 20px;
            background: #f3f0ff;
            border: 1px dashed #a78bfa;
            border-radius: 10px;
        }}

        .highlight-value {{
            display: inline-block;
            margin-top: 10px;
            font-size: 32px;
            font-weight: 700;
            color: #7c3aed;
        }}

        .divider {{
            height: 1px;
            background: #eeeeee;
            margin: 30px 0;
        }}

        .footer {{
            padding: 20px;
            text-align: center;
            font-size: 13px;
            color: #888;
            background-color: #fafafa;
        }}

        .brand {{
            font-weight: 600;
            color: #7c3aed;
        }}

        @media only screen and (max-width: 600px) {{
            .content {{
                padding: 24px 20px;
            }}

            .highlight-value {{
                font-size: 26px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">

            <div class=""header"">
                <h2>Cập nhật tỷ lệ chiết khấu</h2>
            </div>

            <div class=""content"">
                <p>Kính gửi Quý đối tác,</p>

                <p>
                    Chúng tôi xin thông báo về việc điều chỉnh mức chiết khấu (hoa hồng)
                    áp dụng cho các giao dịch quyết toán voucher trên hệ thống 
                    <span class=""brand"">CoupleMood</span>.
                </p>

                <div class=""highlight-box"">
                    <div>Mức chiết khấu mới:</div>
                    <div class=""highlight-value"">{newCommission}%</div>
                </div>

                <p>
                    Mức tỷ lệ này sẽ được áp dụng trong các chu kỳ quyết toán sắp tới.
                    Nếu cần hỗ trợ, vui lòng liên hệ đội ngũ của chúng tôi.
                </p>

                <p>
                    Cảm ơn Quý đối tác đã luôn đồng hành.
                </p>

                <p>
                    Trân trọng,<br>
                    <strong>CoupleMood Team</strong>
                </p>

                <div class=""divider""></div>

                <p style=""font-size:13px; color:#999;"">
                    Email này được gửi tự động, vui lòng không phản hồi trực tiếp.
                </p>
            </div>

            <div class=""footer"">
                © 2026 CoupleMood. All rights reserved.
            </div>

        </div>
    </div>
</body>
</html>";

                foreach (var owner in venueOwners.Where(x => !string.IsNullOrWhiteSpace(x.Email)))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(new SendEmailRequest
                        {
                            To = owner.Email,
                            Subject = "CoupleMood - Cập nhật tỷ lệ chiết khấu giao dịch",
                            HtmlBody = htmlTemplate,
                            FromName = "CoupleMood Admin"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error resend to {Email}", owner.Email);
                    }

                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email cập nhật chiết khấu quyết toán");
            }
        }
    }
}
