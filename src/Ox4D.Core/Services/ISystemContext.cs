// =============================================================================
// ISystemContext - Unified Context for Deterministic Operations
// =============================================================================
// PURPOSE:
//   Provides a single abstraction combining time (IClock) and ID generation
//   (IDealIdGenerator). By injecting ISystemContext, the entire system becomes
//   replayable with deterministic behavior for testing, backtests, and demos.
//
// BENEFITS:
//   - Single injection point for all non-deterministic operations
//   - Entire system becomes replayable with fixed context
//   - Tests produce identical results every run
//   - "Why did this change?" becomes answerable via context inspection
//
// USAGE:
//   Production: SystemContext.Default
//   Testing: new SystemContext(FixedClock.At(...), new SequentialDealIdGenerator())
// =============================================================================

namespace Ox4D.Core.Services;

/// <summary>
/// Unified context for system-level abstractions that affect determinism.
/// Combines time operations and ID generation into a single injectable unit.
/// </summary>
public interface ISystemContext
{
    /// <summary>Gets the clock abstraction for all date/time operations.</summary>
    IClock Clock { get; }

    /// <summary>Gets the ID generator for creating deal identifiers.</summary>
    IDealIdGenerator DealIdGenerator { get; }
}

/// <summary>
/// Production implementation using real system time and GUID-based IDs.
/// </summary>
public sealed class SystemContext : ISystemContext
{
    /// <summary>
    /// Default production context using system clock and GUID-based ID generation.
    /// </summary>
    public static readonly ISystemContext Default = new SystemContext();

    public IClock Clock { get; }
    public IDealIdGenerator DealIdGenerator { get; }

    /// <summary>
    /// Creates the default production context.
    /// </summary>
    public SystemContext()
        : this(SystemClock.Instance, new DefaultDealIdGenerator()) { }

    /// <summary>
    /// Creates a context with custom clock and ID generator.
    /// Use this for testing or controlled environments.
    /// </summary>
    public SystemContext(IClock clock, IDealIdGenerator idGenerator)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        DealIdGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    /// <summary>
    /// Creates a deterministic context for testing.
    /// Uses a fixed clock and sequential ID generator for reproducible results.
    /// </summary>
    /// <param name="fixedDate">The fixed date to use for all time operations.</param>
    public static ISystemContext ForTesting(DateTime fixedDate)
    {
        var clock = new FixedClock(fixedDate);
        var idGenerator = new SequentialDealIdGenerator(fixedDate);
        return new SystemContext(clock, idGenerator);
    }

    /// <summary>
    /// Creates a deterministic context with a specific seed for reproducibility.
    /// Same seed + same operations = identical results.
    /// </summary>
    /// <param name="fixedDate">The fixed date to use for all time operations.</param>
    /// <param name="seed">Random seed for ID generation.</param>
    public static ISystemContext ForSyntheticData(DateTime fixedDate, int seed)
    {
        var clock = new FixedClock(fixedDate);
        var idGenerator = new SeededDealIdGenerator(seed, fixedDate);
        return new SystemContext(clock, idGenerator);
    }
}
