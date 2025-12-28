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

    /// <summary>
    /// Patches a deal using typed validation. Returns detailed result with applied/rejected fields.
    /// </summary>
    public async Task<PatchResult> PatchDealWithResultAsync(string dealId, Dictionary<string, object?> patch, CancellationToken ct = default)
    {
        var deal = await _repository.GetByIdAsync(dealId, ct);
        if (deal == null)
            return PatchResult.NotFound(dealId);

        // Parse patch with validation
        var (dealPatch, parseRejected) = DealPatch.FromDictionary(patch);

        // Apply the typed patch and collect changes
        var applied = ApplyTypedPatch(deal, dealPatch);

        if (applied.Count == 0 && parseRejected.Count > 0)
        {
            // Nothing could be applied - return validation failure
            return PatchResult.ValidationFailed(parseRejected);
        }

        // Normalize and track changes
        var result = _normalizer.NormalizeWithTracking(deal);
        await _repository.UpsertAsync(result.Deal, ct);
        await _repository.SaveChangesAsync(ct);

        return PatchResult.Succeeded(result.Deal, applied, parseRejected, result.Changes);
    }

    /// <summary>
    /// Legacy patch method for backward compatibility. Logs warnings for rejected fields.
    /// </summary>
    public async Task<Deal?> PatchDealAsync(string dealId, Dictionary<string, object?> patch, CancellationToken ct = default)
    {
        var result = await PatchDealWithResultAsync(dealId, patch, ct);
        return result.Deal;
    }

    private List<AppliedField> ApplyTypedPatch(Deal deal, DealPatch patch)
    {
        var applied = new List<AppliedField>();

        // Helper to apply a field if the patch value is non-null
        void Apply(string name, string? patchValue, Func<string?> getter, Action<string?> setter)
        {
            if (patchValue != null)
            {
                var oldValue = getter();
                setter(patchValue);
                applied.Add(new AppliedField(name, oldValue, patchValue));
            }
        }

        void ApplyInt(string name, int? patchValue, Func<int> getter, Action<int> setter)
        {
            if (patchValue.HasValue)
            {
                var oldValue = getter().ToString();
                setter(patchValue.Value);
                applied.Add(new AppliedField(name, oldValue, patchValue.Value.ToString()));
            }
        }

        void ApplyDecimal(string name, decimal? patchValue, Func<decimal?> getter, Action<decimal?> setter)
        {
            if (patchValue.HasValue)
            {
                var oldValue = getter()?.ToString();
                setter(patchValue.Value);
                applied.Add(new AppliedField(name, oldValue, patchValue.Value.ToString()));
            }
        }

        void ApplyDate(string name, DateTime? patchValue, Func<DateTime?> getter, Action<DateTime?> setter)
        {
            if (patchValue.HasValue)
            {
                var oldValue = getter()?.ToString("yyyy-MM-dd");
                setter(patchValue.Value);
                applied.Add(new AppliedField(name, oldValue, patchValue.Value.ToString("yyyy-MM-dd")));
            }
        }

        void ApplyBool(string name, bool? patchValue, Func<bool> getter, Action<bool> setter)
        {
            if (patchValue.HasValue)
            {
                var oldValue = getter().ToString();
                setter(patchValue.Value);
                applied.Add(new AppliedField(name, oldValue, patchValue.Value.ToString()));
            }
        }

        // Identity
        Apply("OrderNo", patch.OrderNo, () => deal.OrderNo, v => deal.OrderNo = v);
        Apply("UserId", patch.UserId, () => deal.UserId, v => deal.UserId = v);

        // Account & Contact
        Apply("AccountName", patch.AccountName, () => deal.AccountName, v => deal.AccountName = v ?? string.Empty);
        Apply("ContactName", patch.ContactName, () => deal.ContactName, v => deal.ContactName = v);
        Apply("Email", patch.Email, () => deal.Email, v => deal.Email = v);
        Apply("Phone", patch.Phone, () => deal.Phone, v => deal.Phone = v);

        // Location
        Apply("Postcode", patch.Postcode, () => deal.Postcode, v => deal.Postcode = v);
        Apply("InstallationLocation", patch.InstallationLocation, () => deal.InstallationLocation, v => deal.InstallationLocation = v);

        // Deal Details
        Apply("LeadSource", patch.LeadSource, () => deal.LeadSource, v => deal.LeadSource = v);
        Apply("ProductLine", patch.ProductLine, () => deal.ProductLine, v => deal.ProductLine = v);
        Apply("DealName", patch.DealName, () => deal.DealName, v => deal.DealName = v ?? string.Empty);

        // Stage & Value
        if (patch.Stage != null)
        {
            var oldStage = deal.Stage.ToString();
            deal.Stage = DealStageExtensions.ParseStage(patch.Stage);
            applied.Add(new AppliedField("Stage", oldStage, deal.Stage.ToString()));
        }
        ApplyInt("Probability", patch.Probability, () => deal.Probability, v => deal.Probability = v);
        ApplyDecimal("AmountGBP", patch.AmountGBP, () => deal.AmountGBP, v => deal.AmountGBP = v);

        // Ownership & Dates
        Apply("Owner", patch.Owner, () => deal.Owner, v => deal.Owner = v);
        ApplyDate("CreatedDate", patch.CreatedDate, () => deal.CreatedDate, v => deal.CreatedDate = v);
        ApplyDate("LastContactedDate", patch.LastContactedDate, () => deal.LastContactedDate, v => deal.LastContactedDate = v);

        // Next Steps
        Apply("NextStep", patch.NextStep, () => deal.NextStep, v => deal.NextStep = v);
        ApplyDate("NextStepDueDate", patch.NextStepDueDate, () => deal.NextStepDueDate, v => deal.NextStepDueDate = v);
        ApplyDate("CloseDate", patch.CloseDate, () => deal.CloseDate, v => deal.CloseDate = v);

        // Service
        Apply("ServicePlan", patch.ServicePlan, () => deal.ServicePlan, v => deal.ServicePlan = v);
        ApplyDate("LastServiceDate", patch.LastServiceDate, () => deal.LastServiceDate, v => deal.LastServiceDate = v);
        ApplyDate("NextServiceDueDate", patch.NextServiceDueDate, () => deal.NextServiceDueDate, v => deal.NextServiceDueDate = v);

        // Metadata
        Apply("Comments", patch.Comments, () => deal.Comments, v => deal.Comments = v);
        if (patch.Tags != null)
        {
            var oldTags = string.Join(",", deal.Tags);
            deal.Tags = patch.Tags;
            applied.Add(new AppliedField("Tags", oldTags, string.Join(",", patch.Tags)));
        }

        // Promoter/Referral
        Apply("PromoterId", patch.PromoterId, () => deal.PromoterId, v => deal.PromoterId = v);
        Apply("PromoCode", patch.PromoCode, () => deal.PromoCode, v => deal.PromoCode = v);
        ApplyDecimal("PromoterCommission", patch.PromoterCommission, () => deal.PromoterCommission, v => deal.PromoterCommission = v);
        ApplyBool("CommissionPaid", patch.CommissionPaid, () => deal.CommissionPaid, v => deal.CommissionPaid = v);
        ApplyDate("CommissionPaidDate", patch.CommissionPaidDate, () => deal.CommissionPaidDate, v => deal.CommissionPaidDate = v);

        return applied;
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
