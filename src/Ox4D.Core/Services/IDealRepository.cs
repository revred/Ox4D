// =============================================================================
// IDealRepository - Storage Abstraction for Sales Pipeline Data
// =============================================================================
// PURPOSE:
//   Defines the contract for deal storage operations. This abstraction allows
//   the application to work with different storage backends (Excel, Supabase,
//   in-memory) without changing the core business logic.
//
// IMPLEMENTATIONS:
//   - ExcelDealRepository: Persists to Excel with sheet-per-table design
//   - InMemoryDealRepository: In-memory for testing and demos
//   - (Future) SupabaseDealRepository: Cloud-based PostgreSQL storage
//
// DESIGN NOTES:
//   - All operations are async for future database compatibility
//   - SaveChangesAsync commits pending changes to storage
//   - Thread-safe implementations required for concurrent access
// =============================================================================

using Ox4D.Core.Models;

namespace Ox4D.Core.Services;

/// <summary>
/// Repository interface for deal CRUD operations.
/// Implementations provide storage-specific persistence logic.
/// </summary>
public interface IDealRepository
{
    /// <summary>Returns all deals in the repository.</summary>
    Task<IReadOnlyList<Deal>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single deal by ID, or null if not found.</summary>
    Task<Deal?> GetByIdAsync(string dealId, CancellationToken ct = default);

    /// <summary>Queries deals matching the filter criteria.</summary>
    Task<IReadOnlyList<Deal>> QueryAsync(DealFilter filter, DateTime referenceDate, CancellationToken ct = default);

    /// <summary>Creates or updates a deal.</summary>
    Task UpsertAsync(Deal deal, CancellationToken ct = default);

    /// <summary>Batch upsert for multiple deals.</summary>
    Task UpsertManyAsync(IEnumerable<Deal> deals, CancellationToken ct = default);

    /// <summary>Removes a deal by ID.</summary>
    Task DeleteAsync(string dealId, CancellationToken ct = default);

    /// <summary>Commits pending changes to persistent storage.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
