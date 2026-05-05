namespace capstone_backend.Business.Common;

/// <summary>
/// Email template cho thông báo hoàn tất rút tiền (hoàn tiền về ví)
/// </summary>
public static class EmailWithdrawCompletedTemplate
{
    /// <summary>
    /// Generate HTML email template cho thông báo hoàn tiền về ví
    /// </summary>
    public static string GetWithdrawCompletedEmailContent(string userName, decimal amount)
    {
        var amountText = amount.ToString("N0");

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Hoàn tiền về ví - CoupleMood</title>
</head>

<body style=""margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;"">

<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:20px 0;background-color:#f3f4f6;"">
<tr>
<td align=""center"">

<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);"">

    <tr>
        <td style=""padding:26px;text-align:center;background:#111827;"">
            <div style=""color:#ffffff;font-size:20px;font-weight:bold;"">
                Hoàn tiền về ví thành công
            </div>
            <div style=""color:#d1d5db;font-size:13px;margin-top:6px;"">
                CoupleMood - Nền tảng hẹn hò thông minh
            </div>
        </td>
    </tr>

    <tr>
        <td style=""padding:24px 28px 10px 28px;"">
            <p style=""margin:0;font-size:15px;color:#111827;line-height:1.6;"">
                Xin chào <strong>{userName}</strong>,
            </p>
            <p style=""margin:10px 0 0 0;color:#4b5563;font-size:14px;line-height:1.6;"">
                Yêu cầu rút tiền của bạn đã được hoàn tất. Số tiền đã được hoàn về ví CoupleMood của bạn.
            </p>
        </td>
    </tr>

    <tr>
        <td style=""padding:0 28px 20px 28px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f9fafb;border:1px solid #e5e7eb;border-radius:10px;"">
                <tr>
                    <td style=""padding:20px;text-align:center;"">
                        <div style=""color:#6b7280;font-size:13px;margin-bottom:8px;"">Số tiền hoàn về ví</div>
                        <div style=""font-size:28px;font-weight:bold;color:#111827;font-family:monospace;"">
                            {amountText} VND
                        </div>
                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <tr>
        <td style=""padding:0 28px 24px 28px;"">
            <p style=""margin:0;color:#6b7280;font-size:13px;line-height:1.6;"">
                Bạn có thể sử dụng số dư ví cho các dịch vụ trong hệ thống. Nếu cần hỗ trợ, vui lòng liên hệ đội ngũ CoupleMood.
            </p>
            <p style=""margin:12px 0 0 0;color:#111827;font-size:13px;"">
                Trân trọng,<br>
                <strong>CoupleMood Team</strong>
            </p>
        </td>
    </tr>

    <tr>
        <td style=""padding:16px 20px;border-top:1px solid #e5e7eb;text-align:center;background:#f9fafb;"">
            <p style=""margin:0;color:#9ca3af;font-size:12px;line-height:1.5;"">
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
    /// Generate plain text email cho thông báo hoàn tiền về ví
    /// </summary>
    public static string GetWithdrawCompletedPlainText(string userName, decimal amount)
    {
        var amountText = amount.ToString("N0");

        return $@"
Xin chào {userName},

Yêu cầu rút tiền của bạn đã được hoàn tất.
Số tiền đã được hoàn về ví CoupleMood của bạn.

SỐ TIỀN HOÀN VỀ VÍ: {amountText} VND

Bạn có thể sử dụng số dư ví cho các dịch vụ trong hệ thống.

Trân trọng,
CoupleMood Team

---
Email này được gửi tự động, vui lòng không trả lời.
";
    }
}
