public class EmailRefundTemplate
{
    public static string GetVenueRefundEmailContent(
        string businessName,
        string venueName,
        string packageName,
        decimal refundAmount,
        decimal oldBalance,
        decimal newBalance,
        string rejectionReason)
    {
        var refund = refundAmount.ToString("N0");
        var oldBal = oldBalance.ToString("N0");
        var newBal = newBalance.ToString("N0");

        return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Thông báo hoàn tiền</title>
</head>

<body style=""margin:0;padding:0;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;"">

<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:30px 0;background:#f4f5f7;"">
<tr>
<td align=""center"">

<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb;"">

    <tr>
        <td style=""padding:18px 24px;background:#111827;color:#dc2626;font-size:14px;"">
            <strong>CoupleMood</strong>
        </td>
    </tr>

    <tr>
        <td style=""padding:28px 24px 10px 24px;"">
            <h2 style=""margin:0;font-size:20px;color:#111827;font-weight:600;"">
                Thông báo kết quả xét duyệt
            </h2>
            <p style=""margin:10px 0 0 0;color:#374151;font-size:14px;line-height:1.6;"">
                Xin chào <strong>{businessName}</strong>,<br>
                Địa điểm <strong>{venueName}</strong> không được phê duyệt.
            </p>
        </td>
    </tr>

    <tr>
        <td style=""padding:0 24px;"">
            <div style=""height:1px;background:#e5e7eb;""></div>
        </td>
    </tr>

    <tr>
        <td style=""padding:20px 24px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid #e5e7eb;border-radius:6px;"">
                <tr>
                    <td style=""padding:16px;"">
                        <div style=""font-size:13px;color:#6b7280;margin-bottom:6px;"">
                            Lý do từ chối
                        </div>
                        <div style=""font-size:14px;color:#111827;line-height:1.6;"">
                            {rejectionReason}
                        </div>
                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <tr>
        <td style=""padding:0 24px 20px 24px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid #e5e7eb;border-radius:6px;"">
                <tr>
                    <td style=""padding:16px;"">

                        <div style=""font-size:13px;color:#6b7280;margin-bottom:12px;"">
                            Thông tin hoàn tiền
                        </div>

                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">

                            <tr>
                                <td style=""padding:6px 0;color:#374151;font-size:14px;"">
                                    Gói đăng ký
                                </td>
                                <td style=""padding:6px 0;text-align:right;color:#111827;font-size:14px;"">
                                    {packageName}
                                </td>
                            </tr>

                            <tr>
                                <td style=""padding:6px 0;color:#374151;font-size:14px;"">
                                    Số tiền hoàn
                                </td>
                                <td style=""padding:6px 0;text-align:right;"">
                                    <strong style=""font-size:16px;color:#111827;"">
                                        {refund} VND
                                    </strong>
                                </td>
                            </tr>

                            <tr>
                                <td style=""padding:6px 0;color:#374151;font-size:14px;"">
                                    Số dư trước đó
                                </td>
                                <td style=""padding:6px 0;text-align:right;color:#111827;"">
                                    {oldBal} VND
                                </td>
                            </tr>

                            <tr>
                                <td style=""padding:6px 0;color:#374151;font-size:14px;"">
                                    Số dư hiện tại
                                </td>
                                <td style=""padding:6px 0;text-align:right;"">
                                    <strong style=""font-size:16px;color:#111827;"">
                                        {newBal} VND
                                    </strong>
                                </td>
                            </tr>

                        </table>

                    </td>
                </tr>
            </table>
        </td>
    </tr>

    <tr>
        <td style=""padding:0 24px 24px 24px;"">
            <div style=""font-size:13px;color:#6b7280;line-height:1.6;"">
                Bạn có thể cập nhật lại thông tin địa điểm và gửi lại yêu cầu xét duyệt. 
                Số tiền đã hoàn có thể sử dụng cho lần đăng ký tiếp theo.
            </div>
        </td>
    </tr>

    <tr>
        <td style=""padding:16px 24px;background:#f9fafb;text-align:center;border-top:1px solid #e5e7eb;"">
            <div style=""font-size:12px;color:#9ca3af;"">
                Email được gửi tự động từ hệ thống CoupleMood
            </div>
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

    public static string GetAdvertisementRejectionEmailContent(
        string businessName,
        string advertisementTitle,
        string rejectionReason,
        decimal? refundAmount,
        decimal? oldBalance,
        decimal? newBalance)
    {
        var hasRefund = refundAmount.HasValue && oldBalance.HasValue && newBalance.HasValue;
        var refundAmountText = refundAmount.GetValueOrDefault().ToString("N0");
        var oldBalanceText = oldBalance.GetValueOrDefault().ToString("N0");
        var newBalanceText = newBalance.GetValueOrDefault().ToString("N0");

        var refundSection = hasRefund
            ? $@"
    <tr>
        <td style=""padding:0 24px 20px 24px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid #e5e7eb;border-radius:6px;"">
                <tr>
                    <td style=""padding:16px;"">
                        <div style=""font-size:13px;color:#6b7280;margin-bottom:12px;"">Thông tin hoàn tiền</div>
                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                            <tr>
                                <td style=""padding:6px 0;color:#374151;font-size:14px;"">Số tiền hoàn</td>
                                <td style=""padding:6px 0;text-align:right;"">
                                    <strong style=""font-size:16px;color:#111827;"">{refundAmountText} VND</strong>
                                </td>
                            </tr>
                            <tr>
                                <td style=""padding:6px 0;color:#374151;font-size:14px;"">Số dư trước đó</td>
                                <td style=""padding:6px 0;text-align:right;color:#111827;"">{oldBalanceText} VND</td>
                            </tr>
                            <tr>
                                <td style=""padding:6px 0;color:#374151;font-size:14px;"">Số dư hiện tại</td>
                                <td style=""padding:6px 0;text-align:right;"">
                                    <strong style=""font-size:16px;color:#111827;"">{newBalanceText} VND</strong>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </td>
    </tr>"
            : "";

        var footerMessage = hasRefund
            ? "Số tiền đã hoàn vào ví CoupleMood của bạn và có thể dùng cho lần chạy quảng cáo tiếp theo."
            : "Bạn có thể chỉnh sửa nội dung quảng cáo và gửi lại để được xét duyệt.";

        return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Thông báo từ chối quảng cáo</title>
</head>

<body style=""margin:0;padding:0;background:#f4f5f7;font-family:Arial,Helvetica,sans-serif;"">

<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""padding:30px 0;background:#f4f5f7;"">
<tr>
<td align=""center"">

<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb;"">

    <tr>
        <td style=""padding:18px 24px;background:#111827;color:#dc2626;font-size:14px;"">
            <strong>CoupleMood</strong>
        </td>
    </tr>

    <tr>
        <td style=""padding:28px 24px 10px 24px;"">
            <h2 style=""margin:0;font-size:20px;color:#111827;font-weight:600;"">
                Thông báo kết quả xét duyệt quảng cáo
            </h2>
            <p style=""margin:10px 0 0 0;color:#374151;font-size:14px;line-height:1.6;"">
                Xin chào <strong>{businessName}</strong>,<br>
                Quảng cáo <strong>{advertisementTitle}</strong> không được phê duyệt.
            </p>
        </td>
    </tr>

    <tr>
        <td style=""padding:0 24px;"">
            <div style=""height:1px;background:#e5e7eb;""></div>
        </td>
    </tr>

    <tr>
        <td style=""padding:20px 24px;"">
            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border:1px solid #e5e7eb;border-radius:6px;"">
                <tr>
                    <td style=""padding:16px;"">
                        <div style=""font-size:13px;color:#6b7280;margin-bottom:6px;"">Lý do từ chối</div>
                        <div style=""font-size:14px;color:#111827;line-height:1.6;"">{rejectionReason}</div>
                    </td>
                </tr>
            </table>
        </td>
    </tr>

    {refundSection}

    <tr>
        <td style=""padding:0 24px 24px 24px;"">
            <div style=""font-size:13px;color:#6b7280;line-height:1.6;"">{footerMessage}</div>
        </td>
    </tr>

    <tr>
        <td style=""padding:16px 24px;background:#f9fafb;text-align:center;border-top:1px solid #e5e7eb;"">
            <div style=""font-size:12px;color:#9ca3af;"">Email được gửi tự động từ hệ thống CoupleMood</div>
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