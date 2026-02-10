namespace capstone_backend.Extensions.Common
{
    public class TimezoneUtil
    {
        private static readonly TimeZoneInfo VietnamTimeZone = InitVietnamTimeZone();

        private static TimeZoneInfo InitVietnamTimeZone()
        {
            // Windows vs Linux (Docker, VPS)
            var tzId = OperatingSystem.IsWindows()
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";

            return TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }

        public static DateTime ToVietNamTime(DateTime utcDateTime)
        {
            if (utcDateTime == default) 
                return default;

            if (utcDateTime.Kind == DateTimeKind.Unspecified)
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            if (utcDateTime.Kind != DateTimeKind.Utc)
                utcDateTime = utcDateTime.ToUniversalTime();

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
        }

        public static DateTimeOffset ToVietNamTime(DateTimeOffset utcDateTime)
        {
            return TimeZoneInfo.ConvertTime(utcDateTime, VietnamTimeZone);
        }
    }
}
