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
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thông báo hoàn tiền</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 20px;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 40px 30px; text-align: center;'>
                            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;'>💰 Thông báo hoàn tiền</h1>
                            <p style='color: #ffffff; margin: 10px 0 0 0; font-size: 16px;'>Địa điểm của bạn đã bị từ chối</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 40px 30px;'>
                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>Xin chào <strong>" + businessName + @"</strong>,</p>
                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin: 0 0 30px 0;'>Chúng tôi rất tiếc phải thông báo rằng địa điểm <strong style='color: #f5576c;'>" + venueName + @"</strong> của bạn đã bị từ chối.</p>
                            
                            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #fff3cd; border-radius: 8px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <h3 style='color: #856404; font-size: 16px; margin: 0 0 10px 0;'>📝 Lý do từ chối</h3>
                                        <p style='color: #856404; font-size: 14px; line-height: 1.6; margin: 0;'>" + rejectionReason + @"</p>
                                    </td>
                                </tr>
                            </table>

                            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #d4edda; border-radius: 8px; border-left: 4px solid #28a745; margin: 30px 0;'>
                                <tr>
                                    <td style='padding: 25px;'>
                                        <h2 style='color: #155724; font-size: 18px; margin: 0 0 15px 0;'>💳 Thông tin hoàn tiền</h2>
                                        <p style='color: #155724; font-size: 14px; line-height: 1.6; margin: 0 0 20px 0;'>Chúng tôi đã hoàn tiền gói đăng ký vào ví của bạn:</p>
                                        
                                        <table width='100%' cellpadding='8' cellspacing='0'>
                                            <tr>
                                                <td style='color: #155724; font-size: 14px; padding: 8px 0;'><strong>Gói đăng ký:</strong></td>
                                                <td style='color: #155724; font-size: 14px; padding: 8px 0;'>" + packageName + @"</td>
                                            </tr>
                                            <tr>
                                                <td style='color: #155724; font-size: 14px; padding: 8px 0;'><strong>Số tiền hoàn:</strong></td>
                                                <td style='color: #155724; font-size: 14px; padding: 8px 0;'><strong style='font-size: 18px;'>" + refundAmount.ToString("N0") + @" VND</strong></td>
                                            </tr>
                                            <tr>
                                                <td style='color: #155724; font-size: 14px; padding: 8px 0;'><strong>Số dư cũ:</strong></td>
                                                <td style='color: #155724; font-size: 14px; padding: 8px 0;'>" + oldBalance.ToString("N0") + @" VND</td>
                                            </tr>
                                            <tr>
                                                <td style='color: #155724; font-size: 14px; padding: 8px 0;'><strong>Số dư mới:</strong></td>
                                                <td style='color: #155724; font-size: 14px; padding: 8px 0;'><strong style='color: #28a745; font-size: 18px;'>" + newBalance.ToString("N0") + @" VND</strong></td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #d1ecf1; border-radius: 8px; border-left: 4px solid #17a2b8; margin: 20px 0;'>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <h3 style='color: #0c5460; font-size: 16px; margin: 0 0 10px 0;'>ℹ️ Bước tiếp theo</h3>
                                        <ul style='color: #0c5460; font-size: 14px; line-height: 1.8; margin: 0; padding-left: 20px;'>
                                            <li>Vui lòng xem lại và chỉnh sửa thông tin địa điểm theo yêu cầu</li>
                                            <li>Sau khi hoàn thiện, bạn có thể gửi lại để được xét duyệt</li>
                                            <li>Số tiền đã hoàn có thể sử dụng cho lần đăng ký tiếp theo</li>
                                            <li>Nếu có thắc mắc, vui lòng liên hệ bộ phận hỗ trợ</li>
                                        </ul>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin: 30px 0 0 0;'>Trân trọng,<br><strong style='color: #f5576c;'>CoupleMood Team</strong></p>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color: #f8f9fa; padding: 20px 30px; text-align: center; border-top: 1px solid #e0e0e0;'>
                            <p style='color: #999999; font-size: 12px; line-height: 1.6; margin: 0;'>Email này được gửi tự động từ hệ thống CoupleMood<br>Vui lòng không trả lời email này</p>
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
