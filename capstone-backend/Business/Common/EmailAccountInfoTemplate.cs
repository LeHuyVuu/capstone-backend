public class EmailAccountInfoTemplate
{
    public static string GetStaffAccountInfoEmailContent(
        string businessName,
        string venueName,
        string staffEmail,
        string staffPassword)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Địa điểm được phê duyệt</title>
</head>

<body style=""margin:0;padding:0;background-color:#f2f3f7;font-family:Arial,Helvetica,sans-serif;"">

<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:20px 0;background-color:#f2f3f7;"">
<tr>
<td align=""center"">

<!-- CONTAINER -->
<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background:#ffffff;border-radius:10px;overflow:hidden;"">

    <!-- HEADER -->
    <tr>
        <td style=""padding:30px 30px 20px 30px;text-align:center;background:#111827;"">
            <div style=""color:#ffffff;font-size:22px;font-weight:bold;letter-spacing:0.5px;"">
                CoupleMood
            </div>
            <div style=""color:#9ca3af;font-size:13px;margin-top:6px;"">
                Nền tảng gợi ý địa điểm hẹn hò
            </div>
        </td>
    </tr>

    <!-- TITLE -->
    <tr>
        <td style=""padding:30px;"">
            <h2 style=""margin:0;font-size:22px;color:#111827;"">
                🎉 Địa điểm của bạn đã được phê duyệt
            </h2>
            <p style=""margin:10px 0 0 0;color:#4b5563;font-size:15px;line-height:1.6;"">
                Xin chào <strong>{businessName}</strong>,
                chúng tôi đã xác nhận địa điểm <strong>{venueName}</strong> của bạn.
            </p>
        </td>
    </tr>

    <!-- INFO CARD -->
    <tr>
        <td style=""padding:0 30px 20px 30px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid #e5e7eb;border-radius:8px;"">
                <tr>
                    <td style=""padding:20px;"">
                        <div style=""font-size:16px;font-weight:bold;color:#111827;margin-bottom:10px;"">
                            Thông tin tài khoản STAFF
                        </div>

                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                            <tr>
                                <td style=""padding:6px 0;color:#6b7280;font-size:14px;"">Email</td>
                                <td style=""padding:6px 0;color:#111827;font-size:14px;text-align:right;"">
                                    {staffEmail}
                                </td>
                            </tr>

                            <tr>
                                <td style=""padding:6px 0;color:#6b7280;font-size:14px;"">Mật khẩu</td>
                                <td style=""padding:6px 0;text-align:right;"">
                                    <span style=""display:inline-block;background:#f9fafb;border:1px solid #e5e7eb;padding:6px 10px;border-radius:6px;font-family:monospace;font-size:13px;color:#111827;"">
                                        {staffPassword}
                                    </span>
                                </td>
                            </tr>

                            <tr>
                                <td style=""padding:6px 0;color:#6b7280;font-size:14px;"">Vai trò</td>
                                <td style=""padding:6px 0;color:#111827;font-size:14px;text-align:right;"">
                                    STAFF
                                </td>
                            </tr>
                        </table>

                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <!-- WARNING -->
    <tr>
        <td style=""padding:0 30px 20px 30px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#fff7ed;border:1px solid #fed7aa;border-radius:8px;"">
                <tr>
                    <td style=""padding:16px;"">
                        <div style=""font-size:14px;color:#9a3412;font-weight:bold;margin-bottom:6px;"">
                            ⚠️ Lưu ý
                        </div>
                        <ul style=""margin:0;padding-left:18px;color:#7c2d12;font-size:13px;line-height:1.7;"">
                            <li>Đổi mật khẩu ngay sau lần đăng nhập đầu tiên</li>
                            <li>Không chia sẻ thông tin tài khoản</li>
                            <li>Email này chỉ gửi một lần duy nhất</li>
                        </ul>
                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <!-- FOOTER -->
    <tr>
        <td style=""padding:25px 30px;border-top:1px solid #e5e7eb;text-align:center;"">
            <p style=""margin:0;color:#6b7280;font-size:13px;"">
                © CoupleMood • Hệ thống gửi tự động
            </p>
        </td>
    </tr>

</table>

</td>
</tr>
</table>

</body>
</html>
";
    }
}