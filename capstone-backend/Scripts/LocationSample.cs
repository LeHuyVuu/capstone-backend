namespace capstone_backend.Scripts;

public readonly record struct LocationSample(double Lat, double Lng, long UpdatedAt)
{
    public DateTime GetTimestampUtc()
    {
        try
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(UpdatedAt).UtcDateTime;
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }
}
