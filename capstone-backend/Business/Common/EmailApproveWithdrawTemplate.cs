namespace capstone_backend.Business.Common;

/// <summary>
/// Email template cho thông báo phê duyệt yêu cầu rút tiền
/// </summary>
public static class EmailApproveWithdrawTemplate
{
    /// <summary>
    /// Generate HTML email template cho thông báo approve withdraw request
    /// </summary>
    /// <param name="userName">Tên người dùng</param>
    /// <param name="amount">Số tiền được phê duyệt</param>
    /// <param name="bankName">Tên ngân hàng</param>
    /// <param name="accountNumber">Số tài khoản</param>
    /// <param name="accountName">Tên chủ tài khoản</param>
    /// <returns>HTML email content</returns>
    public static string GetApproveWithdrawEmailContent(
        string userName,
        decimal amount,
        string bankName,
        string accountNumber,
        string accountName)
    {
        var amountText = amount.ToString("N0");

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Yêu cầu rút tiền đã được phê duyệt - CoupleMood</title>
</head>

<body style=""margin:0;padding:0;background-color:#f2f3f7;font-family:Arial,Helvetica,sans-serif;"">

<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:20px 0;background-color:#f2f3f7;"">
<tr>
<td align=""center"">

<!-- CONTAINER -->
<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.1);"">

    <!-- HEADER -->
    <tr>
        <td style=""padding:30px;text-align:center;background:linear-gradient(135deg, #10b981 0%, #059669 100%);"">
            <div style=""color:#ffffff;font-size:24px;font-weight:bold;letter-spacing:0.5px;"">
                ✅ Yêu cầu rút tiền đã được phê duyệt
            </div>
            <div style=""color:#d1fae5;font-size:14px;margin-top:8px;"">
                CoupleMood - Nền tảng hẹn hò thông minh
            </div>
        </td>
    </tr>

    <!-- GREETING -->
    <tr>
        <td style=""padding:30px 30px 20px 30px;"">
            <p style=""margin:0;font-size:16px;color:#111827;line-height:1.6;"">
                Xin chào <strong style=""color:#10b981;"">{userName}</strong>,
            </p>
            <p style=""margin:12px 0 0 0;color:#4b5563;font-size:15px;line-height:1.6;"">
                Yêu cầu rút tiền của bạn đã được phê duyệt thành công. 
                Chúng tôi sẽ tiến hành chuyển khoản trong thời gian sớm nhất.
            </p>
        </td>
    </tr>

    <!-- AMOUNT BOX -->
    <tr>
        <td style=""padding:0 30px 20px 30px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f0fdf4;border:2px solid #10b981;border-radius:12px;"">
                <tr>
                    <td style=""padding:30px;text-align:center;"">
                        <div style=""color:#6b7280;font-size:14px;margin-bottom:10px;"">
                            Số tiền được phê duyệt
                        </div>
                        <div style=""font-size:36px;font-weight:bold;color:#10b981;font-family:monospace;"">
                            {amountText} VND
                        </div>
                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <!-- BANK INFO -->
    <tr>
        <td style=""padding:0 30px 20px 30px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid #e5e7eb;border-radius:8px;"">
                <tr>
                    <td style=""padding:20px;"">
                        <div style=""font-size:14px;color:#6b7280;margin-bottom:16px;font-weight:600;"">
                            Thông tin tài khoản nhận tiền
                        </div>
                        
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                            <tr>
                                <td style=""padding:8px 0;color:#374151;font-size:14px;width:40%;"">
                                    Ngân hàng
                                </td>
                                <td style=""padding:8px 0;color:#111827;font-size:14px;font-weight:600;"">
                                    {bankName}
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding:8px 0;color:#374151;font-size:14px;"">
                                    Số tài khoản
                                </td>
                                <td style=""padding:8px 0;color:#111827;font-size:14px;font-weight:600;font-family:monospace;"">
                                    {accountNumber}
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding:8px 0;color:#374151;font-size:14px;"">
                                    Chủ tài khoản
                                </td>
                                <td style=""padding:8px 0;color:#111827;font-size:14px;font-weight:600;"">
                                    {accountName}
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <!-- INFO BOX -->
    <tr>
        <td style=""padding:0 30px 20px 30px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#eff6ff;border-left:4px solid #3b82f6;border-radius:6px;"">
                <tr>
                    <td style=""padding:16px 20px;"">
                        <div style=""font-size:14px;color:#1e40af;font-weight:bold;margin-bottom:8px;"">
                            ℹ️ Thông tin quan trọng
                        </div>
                        <ul style=""margin:0;padding-left:20px;color:#1e3a8a;font-size:13px;line-height:1.8;"">
                            <li>Thời gian xử lý: 1-3 ngày làm việc</li>
                            <li>Bạn sẽ nhận được email xác nhận khi giao dịch hoàn tất</li>
                            <li>Vui lòng kiểm tra tài khoản ngân hàng của bạn</li>
                            <li>Liên hệ hỗ trợ nếu có bất kỳ thắc mắc nào</li>
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
                Nếu bạn cần hỗ trợ hoặc có thắc mắc về giao dịch này, 
                vui lòng liên hệ đội ngũ hỗ trợ của chúng tôi.
            </p>
            <p style=""margin:12px 0 0 0;color:#111827;font-size:14px;"">
                Trân trọng,<br>
                <strong style=""color:#10b981;"">Đội ngũ CoupleMood</strong>
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
    /// Generate plain text email cho approve withdraw request (fallback)
    /// </summary>
    public static string GetApproveWithdrawPlainText(
        string userName,
        decimal amount,
        string bankName,
        string accountNumber,
        string accountName)
    {
        var amountText = amount.ToString("N0");

        return $@"
Xin chào {userName},

Yêu cầu rút tiền của bạn đã được phê duyệt thành công.

SỐ TIỀN ĐƯỢC PHÊ DUYỆT: {amountText} VND

THÔNG TIN TÀI KHOẢN NHẬN TIỀN:
- Ngân hàng: {bankName}
- Số tài khoản: {accountNumber}
- Chủ tài khoản: {accountName}

THÔNG TIN QUAN TRỌNG:
- Thời gian xử lý: 1-3 ngày làm việc
- Bạn sẽ nhận được email xác nhận khi giao dịch hoàn tất
- Vui lòng kiểm tra tài khoản ngân hàng của bạn
- Liên hệ hỗ trợ nếu có bất kỳ thắc mắc nào

Nếu bạn cần hỗ trợ hoặc có thắc mắc về giao dịch này, 
vui lòng liên hệ đội ngũ hỗ trợ của chúng tôi.

Trân trọng,
Đội ngũ CoupleMood

---
© 2024 CoupleMood. All rights reserved.
Email này được gửi tự động, vui lòng không trả lời.
";
    }
}
