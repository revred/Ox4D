using Ox4D.Core.Models;
using Ox4D.Core.Services;

namespace Ox4D.Store;

public class InMemoryDealRepository : IDealRepository
{
    private readonly List<Deal> _deals = new();
    private readonly object _lock = new();

    public Task<IReadOnlyList<Deal>> GetAllAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Deal>>(_deals.Select(d => d.Clone()).ToList().AsReadOnly());
        }
    }

    public Task<Deal?> GetByIdAsync(string dealId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var deal = _deals.FirstOrDefault(d => d.DealId.Equals(dealId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(deal?.Clone());
        }
    }

    public Task<IReadOnlyList<Deal>> QueryAsync(DealFilter filter, DateTime referenceDate, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var results = _deals.Where(d => filter.Matches(d, referenceDate))
                                .Select(d => d.Clone())
                                .ToList()
                                .AsReadOnly();
            return Task.FromResult<IReadOnlyList<Deal>>(results);
        }
    }

    public Task UpsertAsync(Deal deal, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var existing = _deals.FindIndex(d => d.DealId.Equals(deal.DealId, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
            {
                _deals[existing] = deal.Clone();
            }
            else
            {
                _deals.Add(deal.Clone());
            }
        }
        return Task.CompletedTask;
    }

    public Task UpsertManyAsync(IEnumerable<Deal> deals, CancellationToken ct = default)
    {
        lock (_lock)
        {
            foreach (var deal in deals)
            {
                var existing = _deals.FindIndex(d => d.DealId.Equals(deal.DealId, StringComparison.OrdinalIgnoreCase));
                if (existing >= 0)
                {
                    _deals[existing] = deal.Clone();
                }
                else
                {
                    _deals.Add(deal.Clone());
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string dealId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _deals.RemoveAll(d => d.DealId.Equals(dealId, StringComparison.OrdinalIgnoreCase));
        }
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        // In-memory repo doesn't need to save
        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _deals.Clear();
        }
    }

    public void LoadDeals(IEnumerable<Deal> deals)
    {
        lock (_lock)
        {
            _deals.Clear();
            _deals.AddRange(deals.Select(d => d.Clone()));
        }
    }
}
