using Ox4D.Core.Models;

namespace Ox4D.Core.Services;

/// <summary>
/// Interface for synthetic data generation.
/// Allows different implementations (simple vs Bogus-powered) to be used interchangeably.
/// </summary>
public interface ISyntheticDataGenerator
{
    /// <summary>
    /// Generates a list of synthetic deals.
    /// </summary>
    /// <param name="count">Number of deals to generate</param>
    /// <param name="seed">Random seed for reproducibility</param>
    /// <returns>List of generated deals</returns>
    List<Deal> Generate(int count, int seed);
}
