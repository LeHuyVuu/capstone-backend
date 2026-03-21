public class EmailAccountInfoTemplate
{
    public static string GetPasswordResetEmailContent(string recipientName, string resetLink)
    {
        return $@"

";
    }

    public static string GetStaffAccountInfoEmailContent(string businessName, string venueName, string staffEmail, string staffPassword)
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Địa điểm đã được phê duyệt</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 20px;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center;'>
                            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;'>🎉 Chúc mừng!</h1>
                            <p style='color: #ffffff; margin: 10px 0 0 0; font-size: 16px;'>Địa điểm của bạn đã được phê duyệt</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 40px 30px;'>
                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin: 0 0 20px 0;'>Xin chào <strong>" + businessName + @"</strong>,</p>
                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin: 0 0 30px 0;'>Chúng tôi vui mừng thông báo rằng địa điểm <strong style='color: #667eea;'>" + venueName + @"</strong> của bạn đã được phê duyệt thành công trên hệ thống CoupleMood! 🎊</p>
                            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; border-radius: 8px; border-left: 4px solid #667eea; margin: 30px 0;'>
                                <tr>
                                    <td style='padding: 25px;'>
                                        <h2 style='color: #333333; font-size: 18px; margin: 0 0 15px 0;'>📋 Thông tin tài khoản STAFF</h2>
                                        <p style='color: #666666; font-size: 14px; line-height: 1.6; margin: 0 0 20px 0;'>Chúng tôi đã tạo tài khoản STAFF để bạn quản lý địa điểm:</p>
                                        <table width='100%' cellpadding='8' cellspacing='0'>
                                            <tr>
                                                <td style='color: #666666; font-size: 14px; padding: 8px 0;'><strong>Email:</strong></td>
                                                <td style='color: #333333; font-size: 14px; padding: 8px 0;'>" + staffEmail + @"</td>
                                            </tr>
                                            <tr>
                                                <td style='color: #666666; font-size: 14px; padding: 8px 0;'><strong>Mật khẩu:</strong></td>
                                                <td style='color: #333333; font-size: 14px; padding: 8px 0;'><code style='background-color: #ffffff; padding: 8px 12px; border-radius: 4px; border: 1px solid #e0e0e0; font-family: monospace; font-size: 14px; display: inline-block;'>" + staffPassword + @"</code></td>
                                            </tr>
                                            <tr>
                                                <td style='color: #666666; font-size: 14px; padding: 8px 0;'><strong>Vai trò:</strong></td>
                                                <td style='color: #333333; font-size: 14px; padding: 8px 0;'>STAFF</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #fff3cd; border-radius: 8px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                                <tr>
                                    <td style='padding: 20px;'>
                                        <h3 style='color: #856404; font-size: 16px; margin: 0 0 10px 0;'>⚠️ Lưu ý quan trọng</h3>
                                        <ul style='color: #856404; font-size: 14px; line-height: 1.8; margin: 0; padding-left: 20px;'>
                                            <li>Vui lòng lưu lại thông tin này ở nơi an toàn</li>
                                            <li>Đổi mật khẩu ngay sau lần đăng nhập đầu tiên</li>
                                            <li>Không chia sẻ thông tin này với người khác</li>
                                            <li>Email này chỉ được gửi một lần duy nhất</li>
                                        </ul>
                                    </td>
                                </tr>
                            </table>
                            <p style='color: #333333; font-size: 16px; line-height: 1.6; margin: 30px 0 0 0;'>Trân trọng,<br><strong style='color: #667eea;'>CoupleMood Team</strong></p>
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
