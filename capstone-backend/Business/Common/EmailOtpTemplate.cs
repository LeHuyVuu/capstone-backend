namespace capstone_backend.Business.Common;

/// <summary>
/// Email template cho OTP verification
/// </summary>
public static class EmailOtpTemplate
{
    /// <summary>
    /// Generate HTML email template cho OTP reset password
    /// </summary>
    /// <param name="otpCode">Mã OTP 6 chữ số</param>
    /// <param name="userName">Tên người dùng</param>
    /// <returns>HTML email content</returns>
    public static string GetPasswordResetOtpEmail(string otpCode, string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Đặt lại mật khẩu - CoupleMood</title>
</head>

<body style=""margin:0;padding:0;background-color:#f2f3f7;font-family:Arial,Helvetica,sans-serif;"">

<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:20px 0;background-color:#f2f3f7;"">
<tr>
<td align=""center"">

<!-- CONTAINER -->
<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);"">

    <!-- HEADER -->
    <tr>
        <td style=""padding:30px;text-align:center;background:linear-gradient(135deg, #667eea 0%, #764ba2 100%);"">
            <div style=""color:#ffffff;font-size:24px;font-weight:bold;letter-spacing:0.5px;"">
                🔐 Đặt lại mật khẩu
            </div>
            <div style=""color:#e0e7ff;font-size:14px;margin-top:8px;"">
                CoupleMood - Nền tảng hẹn hò thông minh
            </div>
        </td>
    </tr>

    <!-- GREETING -->
    <tr>
        <td style=""padding:30px 30px 20px 30px;"">
            <p style=""margin:0;font-size:16px;color:#111827;line-height:1.6;"">
                Xin chào <strong style=""color:#667eea;"">{userName}</strong>,
            </p>
            <p style=""margin:12px 0 0 0;color:#4b5563;font-size:15px;line-height:1.6;"">
                Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản CoupleMood của mình. 
                Vui lòng sử dụng mã OTP bên dưới để tiếp tục:
            </p>
        </td>
    </tr>

    <!-- OTP BOX -->
    <tr>
        <td style=""padding:0 30px 20px 30px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f9fafb;border:2px dashed #667eea;border-radius:12px;"">
                <tr>
                    <td style=""padding:30px;text-align:center;"">
                        <div style=""color:#6b7280;font-size:14px;margin-bottom:10px;"">
                            Mã OTP của bạn
                        </div>
                        <div style=""font-size:36px;font-weight:bold;color:#667eea;letter-spacing:8px;font-family:monospace;"">
                            {otpCode}
                        </div>
                        <div style=""margin-top:12px;color:#9ca3af;font-size:13px;"">
                            ⏱️ Mã có hiệu lực trong <strong>10 phút</strong>
                        </div>
                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <!-- WARNING BOX -->
    <tr>
        <td style=""padding:0 30px 20px 30px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#fff7ed;border-left:4px solid #f59e0b;border-radius:6px;"">
                <tr>
                    <td style=""padding:16px 20px;"">
                        <div style=""font-size:14px;color:#92400e;font-weight:bold;margin-bottom:8px;"">
                            ⚠️ Lưu ý bảo mật
                        </div>
                        <ul style=""margin:0;padding-left:20px;color:#78350f;font-size:13px;line-height:1.8;"">
                            <li>Không chia sẻ mã OTP này với bất kỳ ai</li>
                            <li>CoupleMood sẽ không bao giờ yêu cầu mã OTP qua điện thoại</li>
                            <li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>
                        </ul>
                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <!-- SUPPORT -->
    <tr>
        <td style=""padding:0 30px 25px 30px;"">
            <p style=""margin:0;color:#6b7280;font-size:14px;line-height:1.6;"">
                Nếu bạn cần hỗ trợ, vui lòng liên hệ đội ngũ hỗ trợ của chúng tôi.
            </p>
            <p style=""margin:12px 0 0 0;color:#111827;font-size:14px;"">
                Trân trọng,<br>
                <strong style=""color:#667eea;"">Đội ngũ CoupleMood</strong>
            </p>
        </td>
    </tr>

    <!-- FOOTER -->
    <tr>
        <td style=""padding:20px 30px;border-top:1px solid #e5e7eb;text-align:center;background:#f9fafb;"">
            <p style=""margin:0;color:#9ca3af;font-size:12px;line-height:1.5;"">
                © 2024 CoupleMood. All rights reserved.
            </p>
            <p style=""margin:6px 0 0 0;color:#9ca3af;font-size:12px;"">
                Email này được gửi tự động, vui lòng không trả lời.
            </p>
        </td>
    </tr>

</table>

</td>
</tr>
</table>

</body>
</html>";
    }

    /// <summary>
    /// Generate plain text email cho OTP (fallback)
    /// </summary>
    /// <param name="otpCode">Mã OTP 6 chữ số</param>
    /// <param name="userName">Tên người dùng</param>
    /// <returns>Plain text email content</returns>
    public static string GetPasswordResetOtpPlainText(string otpCode, string userName)
    {
        return $@"
Xin chào {userName},

Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản CoupleMood của mình.

MÃ OTP CỦA BẠN: {otpCode}

Mã có hiệu lực trong 10 phút.

LƯU Ý BẢO MẬT:
- Không chia sẻ mã OTP này với bất kỳ ai
- CoupleMood sẽ không bao giờ yêu cầu mã OTP qua điện thoại
- Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này

Nếu bạn cần hỗ trợ, vui lòng liên hệ đội ngũ hỗ trợ của chúng tôi.

Trân trọng,
Đội ngũ CoupleMood

---
© 2024 CoupleMood. All rights reserved.
Email này được gửi tự động, vui lòng không trả lời.
";
    }
}
