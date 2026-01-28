using capstone_backend.Extensions.Common;

namespace capstone_backend.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime ToVietnamTime(this DateTime utcDateTime)
        => TimezoneUtil.ToVietNamTime(utcDateTime);

        public static DateTimeOffset ToVietnamTime(this DateTimeOffset utcDateTime)
            => TimezoneUtil.ToVietnamTime(utcDateTime);
    }
}
