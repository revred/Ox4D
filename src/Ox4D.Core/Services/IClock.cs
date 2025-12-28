// =============================================================================
// IClock - Time Abstraction for Testable Date/Time Operations
// =============================================================================
// PURPOSE:
//   Provides a testable abstraction over DateTime.Now/UtcNow. By injecting
//   IClock instead of using DateTime directly, services become deterministic
//   and testable with fixed or controlled time values.
//
// IMPLEMENTATIONS:
//   - SystemClock: Returns actual system time (production use)
//   - FixedClock: Returns a fixed time value (testing use)
//
// USAGE:
//   Instead of: DateTime.Now
//   Use: _clock.Now
// =============================================================================

namespace Ox4D.Core.Services;

/// <summary>
/// Abstraction for time-related operations to enable deterministic testing.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current local date and time.</summary>
    DateTime Now { get; }

    /// <summary>Gets the current UTC date and time.</summary>
    DateTime UtcNow { get; }

    /// <summary>Gets today's date (local time).</summary>
    DateTime Today { get; }
}

/// <summary>
/// Production implementation that uses the system clock.
/// </summary>
public class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.Today;
}

/// <summary>
/// Test implementation that returns a fixed time.
/// </summary>
public class FixedClock : IClock
{
    private readonly DateTime _fixedTime;

    public FixedClock(DateTime fixedTime)
    {
        _fixedTime = fixedTime;
    }

    public DateTime Now => _fixedTime;
    public DateTime UtcNow => _fixedTime.ToUniversalTime();
    public DateTime Today => _fixedTime.Date;

    /// <summary>Creates a FixedClock with the specified date at midnight.</summary>
    public static FixedClock AtDate(int year, int month, int day)
        => new(new DateTime(year, month, day));

    /// <summary>Creates a FixedClock with the specified date and time.</summary>
    public static FixedClock At(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
        => new(new DateTime(year, month, day, hour, minute, second));
}
