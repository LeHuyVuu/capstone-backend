public static class EmailVenueStatusTemplate
{
    public static string GetVenueStatusChangeEmailContent(
        string ownerDisplayName,
        string venueName,
        bool isActivated,
        string? reason,
        DateTime sentAtUtc)
    {
        var safeOwnerName = string.IsNullOrWhiteSpace(ownerDisplayName) ? "Venue Owner" : ownerDisplayName;
        var safeVenueName = string.IsNullOrWhiteSpace(venueName) ? "Địa điểm" : venueName;
        var timeText = sentAtUtc.ToString("dd/MM/yyyy HH:mm:ss");

        if (isActivated)
        {
            return $@"<h2>Thông báo kích hoạt địa điểm</h2>
                        <p>Kính gửi {safeOwnerName},</p>
                        <p>Địa điểm <strong>{safeVenueName}</strong> của bạn đã được kích hoạt lại và hiển thị trên hệ thống.</p>
                        <p><strong>Thời gian:</strong> {timeText}</p>";
        }

        return $@"<h2>Thông báo tạm ngừng hoạt động</h2>
                        <p>Kính gửi {safeOwnerName},</p>
                        <p>Địa điểm <strong>{safeVenueName}</strong> của bạn đã bị tạm ngừng hoạt động bởi quản trị viên.</p>
                        <p><strong>Lý do:</strong> {reason}</p>
                        <p><strong>Thời gian:</strong> {timeText}</p>
                        <p>Vui lòng liên hệ hỗ trợ để biết thêm chi tiết.</p>";
    }
}
