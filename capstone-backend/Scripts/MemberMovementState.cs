namespace capstone_backend.Scripts;

internal sealed class MemberMovementState
{
    public LocationSample? LastSample { get; set; }
    public LocationSample? LastNotifiedSample { get; set; }
    public DateTime LastNotificationUtc { get; set; } = DateTime.MinValue;
}
