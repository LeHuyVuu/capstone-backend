using System.Collections.Concurrent;

namespace capstone_backend.Scripts;

internal sealed class MovementDecisionEngine
{
    private readonly FirebaseLocationNotifierOptions _options;
    private readonly ConcurrentDictionary<string, MemberMovementState> _states = new();

    public MovementDecisionEngine(FirebaseLocationNotifierOptions options)
    {
        _options = options;
    }

    public void Seed(string key, LocationSample sample)
    {
        _states.AddOrUpdate(
            key,
            _ => new MemberMovementState
            {
                LastSample = sample
            },
            (_, existing) =>
            {
                existing.LastSample = sample;
                return existing;
            });
    }

    public bool ShouldNotify(string key, LocationSample sample)
    {
        var state = _states.GetOrAdd(key, _ => new MemberMovementState
        {
            LastSample = sample
        });

        if (!state.LastSample.HasValue)
        {
            state.LastSample = sample;
            return false;
        }

        var prev = state.LastSample.Value;
        if (sample.UpdatedAt <= prev.UpdatedAt)
            return false;

        var now = sample.GetTimestampUtc();
        var stepMeters = DistanceMeters(prev, sample);

        state.LastSample = sample;

        // Rule 1: step move must be large enough to ignore GPS jitter.
        if (stepMeters < _options.MinStepDistanceMeters)
            return false;

        // Rule 2: respect cooldown between notifications.
        var inCooldown = (now - state.LastNotificationUtc).TotalSeconds < _options.NotificationCooldownSeconds;
        if (inCooldown)
            return false;

        // Rule 3: must be far enough from the last notified location.
        var fromLastNoti = state.LastNotifiedSample.HasValue
            ? DistanceMeters(state.LastNotifiedSample.Value, sample)
            : stepMeters;

        if (fromLastNoti < _options.MinDistanceFromLastNotificationMeters)
            return false;

        state.LastNotifiedSample = sample;
        state.LastNotificationUtc = now;

        return true;
    }

    private static double DistanceMeters(LocationSample a, LocationSample b)
    {
        const double radius = 6371000;
        var dLat = ToRad(b.Lat - a.Lat);
        var dLon = ToRad(b.Lng - a.Lng);

        var x = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(a.Lat)) * Math.Cos(ToRad(b.Lat))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(x), Math.Sqrt(1 - x));
        return radius * c;
    }

    private static double ToRad(double deg) => deg * (Math.PI / 180);
}
