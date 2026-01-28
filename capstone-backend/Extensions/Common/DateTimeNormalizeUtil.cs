namespace capstone_backend.Extensions.Common
{
    public static class DateTimeNormalizeUtil
    {
        public static DateTime NormalizeToUtc(DateTime dateTime)
        {
            if (dateTime == default)
                return dateTime;

            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,

                DateTimeKind.Local => dateTime.ToUniversalTime(),

                DateTimeKind.Unspecified =>

                    DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),

                _ => dateTime
            };
        }
    }
}
