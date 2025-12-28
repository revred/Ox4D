namespace Ox4D.Core.Services;

/// <summary>
/// Abstraction for generating unique Deal IDs.
/// Allows injection of deterministic generators for testing and reproducible synthetic data.
/// </summary>
public interface IDealIdGenerator
{
    /// <summary>
    /// Generates a unique Deal ID.
    /// Format: D-{date}-{unique-suffix}
    /// </summary>
    string Generate();
}

/// <summary>
/// Default ID generator using current UTC time and GUID.
/// Produces unique but non-reproducible IDs.
/// </summary>
public class DefaultDealIdGenerator : IDealIdGenerator
{
    private readonly IClock _clock;

    public DefaultDealIdGenerator() : this(SystemClock.Instance) { }

    public DefaultDealIdGenerator(IClock clock)
    {
        _clock = clock;
    }

    public string Generate()
    {
        var date = _clock.UtcNow;
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"D-{date:yyyyMMdd}-{suffix}";
    }
}

/// <summary>
/// Deterministic ID generator using a seeded random number generator.
/// Produces the same sequence of IDs for the same seed, enabling reproducible tests
/// and synthetic data generation.
/// </summary>
public class SeededDealIdGenerator : IDealIdGenerator
{
    private readonly Random _random;
    private readonly DateTime _baseDate;
    private int _counter;

    /// <summary>
    /// Creates a seeded ID generator with a specific base date and seed.
    /// </summary>
    /// <param name="seed">Random seed for reproducibility</param>
    /// <param name="baseDate">Base date for generated IDs (default: 2025-01-01)</param>
    public SeededDealIdGenerator(int seed, DateTime? baseDate = null)
    {
        _random = new Random(seed);
        _baseDate = baseDate ?? new DateTime(2025, 1, 1);
        _counter = 0;
    }

    public string Generate()
    {
        _counter++;
        // Generate a deterministic suffix using the seeded random
        var suffix = GenerateSuffix();
        // Spread dates across a range for realistic distribution
        var dayOffset = _random.Next(0, 365);
        var date = _baseDate.AddDays(dayOffset);
        return $"D-{date:yyyyMMdd}-{suffix}";
    }

    private string GenerateSuffix()
    {
        const string chars = "0123456789ABCDEF";
        var suffix = new char[8];
        for (int i = 0; i < 8; i++)
        {
            suffix[i] = chars[_random.Next(chars.Length)];
        }
        return new string(suffix);
    }
}

/// <summary>
/// Sequential ID generator for testing.
/// Produces predictable IDs like D-20250101-00000001, D-20250101-00000002, etc.
/// </summary>
public class SequentialDealIdGenerator : IDealIdGenerator
{
    private readonly DateTime _baseDate;
    private int _counter;

    public SequentialDealIdGenerator(DateTime? baseDate = null)
    {
        _baseDate = baseDate ?? new DateTime(2025, 1, 1);
        _counter = 0;
    }

    public string Generate()
    {
        _counter++;
        return $"D-{_baseDate:yyyyMMdd}-{_counter:D8}";
    }

    /// <summary>
    /// Resets the counter to zero.
    /// </summary>
    public void Reset() => _counter = 0;
}
