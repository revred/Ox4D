using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;

namespace Ox4D.Core.Services;

public class PipelineService
{
    private readonly IDealRepository _repository;
    private readonly DealNormalizer _normalizer;
    private readonly ReportService _reportService;
    private readonly ISyntheticDataGenerator _generator;
    private readonly PipelineSettings _settings;

    public PipelineService(
        IDealRepository repository,
        LookupTables lookups,
        PipelineSettings settings,
        ISyntheticDataGenerator? generator = null)
    {
        _repository = repository;
        _normalizer = new DealNormalizer(lookups);
        _reportService = new ReportService(repository, settings);
        _generator = generator ?? new SyntheticDataGenerator(lookups, settings);
        _settings = settings;
    }

    // Deal operations
    public Task<IReadOnlyList<Deal>> ListDealsAsync(DealFilter? filter = null, CancellationToken ct = default)
    {
        return filter == null
            ? _repository.GetAllAsync(ct)
            : _repository.QueryAsync(filter, DateTime.Today, ct);
    }

    public Task<Deal?> GetDealAsync(string dealId, CancellationToken ct = default) =>
        _repository.GetByIdAsync(dealId, ct);

    public async Task<Deal> UpsertDealAsync(Deal deal, CancellationToken ct = default)
    {
        var normalized = _normalizer.Normalize(deal);
        await _repository.UpsertAsync(normalized, ct);
        await _repository.SaveChangesAsync(ct);
        return normalized;
    }

    public async Task<Deal?> PatchDealAsync(string dealId, Dictionary<string, object?> patch, CancellationToken ct = default)
    {
        var deal = await _repository.GetByIdAsync(dealId, ct);
        if (deal == null) return null;

        ApplyPatch(deal, patch);
        var normalized = _normalizer.Normalize(deal);
        await _repository.UpsertAsync(normalized, ct);
        await _repository.SaveChangesAsync(ct);
        return normalized;
    }

    private void ApplyPatch(Deal deal, Dictionary<string, object?> patch)
    {
        foreach (var (key, value) in patch)
        {
            var prop = typeof(Deal).GetProperty(key);
            if (prop == null || !prop.CanWrite) continue;

            try
            {
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                if (value == null)
                {
                    prop.SetValue(deal, null);
                }
                else if (targetType == typeof(DateTime))
                {
                    prop.SetValue(deal, DealNormalizer.ParseDate(value.ToString()));
                }
                else if (targetType == typeof(decimal))
                {
                    prop.SetValue(deal, DealNormalizer.ParseAmount(value.ToString()));
                }
                else if (targetType == typeof(int))
                {
                    prop.SetValue(deal, int.TryParse(value.ToString(), out var i) ? i : 0);
                }
                else if (targetType == typeof(DealStage))
                {
                    prop.SetValue(deal, DealStageExtensions.ParseStage(value.ToString()));
                }
                else if (targetType == typeof(List<string>))
                {
                    if (value is IEnumerable<string> list)
                        prop.SetValue(deal, list.ToList());
                    else if (value is string str)
                        prop.SetValue(deal, str.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList());
                }
                else
                {
                    prop.SetValue(deal, Convert.ChangeType(value, targetType));
                }
            }
            catch
            {
                // Skip invalid patches silently
            }
        }
    }

    public async Task DeleteDealAsync(string dealId, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(dealId, ct);
        await _repository.SaveChangesAsync(ct);
    }

    // Reports
    public Task<Models.Reports.DailyBrief> GetDailyBriefAsync(DateTime? referenceDate = null, CancellationToken ct = default) =>
        _reportService.GenerateDailyBriefAsync(referenceDate ?? DateTime.Today, ct);

    public Task<Models.Reports.HygieneReport> GetHygieneReportAsync(CancellationToken ct = default) =>
        _reportService.GenerateHygieneReportAsync(ct);

    public Task<Models.Reports.ForecastSnapshot> GetForecastSnapshotAsync(DateTime? referenceDate = null, CancellationToken ct = default) =>
        _reportService.GenerateForecastSnapshotAsync(referenceDate ?? DateTime.Today, ct);

    // Synthetic data
    public async Task<int> GenerateSyntheticDataAsync(int count, int seed, CancellationToken ct = default)
    {
        var deals = _generator.Generate(count, seed);
        await _repository.UpsertManyAsync(deals, ct);
        await _repository.SaveChangesAsync(ct);
        return deals.Count;
    }

    // Statistics
    public async Task<PipelineStats> GetStatsAsync(CancellationToken ct = default)
    {
        var deals = await _repository.GetAllAsync(ct);
        var open = deals.Where(d => !d.Stage.IsClosed()).ToList();

        return new PipelineStats
        {
            TotalDeals = deals.Count,
            OpenDeals = open.Count,
            ClosedWonDeals = deals.Count(d => d.Stage == DealStage.ClosedWon),
            ClosedLostDeals = deals.Count(d => d.Stage == DealStage.ClosedLost),
            TotalPipeline = open.Sum(d => d.AmountGBP ?? 0),
            WeightedPipeline = open.Sum(d => d.WeightedAmountGBP ?? 0),
            ClosedWonValue = deals.Where(d => d.Stage == DealStage.ClosedWon).Sum(d => d.AmountGBP ?? 0),
            AverageDealsValue = open.Any() ? open.Average(d => d.AmountGBP ?? 0) : 0,
            Owners = deals.Where(d => !string.IsNullOrEmpty(d.Owner)).Select(d => d.Owner!).Distinct().ToList(),
            Regions = deals.Where(d => !string.IsNullOrEmpty(d.Region)).Select(d => d.Region!).Distinct().ToList()
        };
    }
}

public class PipelineStats
{
    public int TotalDeals { get; set; }
    public int OpenDeals { get; set; }
    public int ClosedWonDeals { get; set; }
    public int ClosedLostDeals { get; set; }
    public decimal TotalPipeline { get; set; }
    public decimal WeightedPipeline { get; set; }
    public decimal ClosedWonValue { get; set; }
    public decimal AverageDealsValue { get; set; }
    public List<string> Owners { get; set; } = new();
    public List<string> Regions { get; set; } = new();
}
