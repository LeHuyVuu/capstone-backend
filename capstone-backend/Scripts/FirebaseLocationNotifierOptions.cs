namespace capstone_backend.Scripts;

public sealed class FirebaseLocationNotifierOptions
{
    // Mỗi bước di chuyển phải >= ngưỡng này mới xét gửi noti (lọc rung GPS).
    public double MinStepDistanceMeters { get; init; } = 30;

    // Sau khi gửi noti, phải chờ cooldown mới gửi lại.
    public int NotificationCooldownSeconds { get; init; } = 60;

    // Từ vị trí đã gửi noti gần nhất, phải đi thêm ít nhất ngưỡng này mới gửi lại.
    public double MinDistanceFromLastNotificationMeters { get; init; } = 100;
}
