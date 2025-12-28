using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Models.Reports;

namespace Ox4D.Core.Services;

public class ReportService
{
    private readonly IDealRepository _repository;
    private readonly PipelineSettings _settings;
    private readonly IClock _clock;

    public ReportService(IDealRepository repository, PipelineSettings settings, IClock? clock = null)
    {
        _repository = repository;
        _settings = settings;
        _clock = clock ?? SystemClock.Instance;
    }

    public async Task<DailyBrief> GenerateDailyBriefAsync(DateTime referenceDate, CancellationToken ct = default)
    {
        var deals = await _repository.GetAllAsync(ct);
        var openDeals = deals.Where(d => !d.Stage.IsClosed()).ToList();

        var today = referenceDate.Date;

        // Due today
        var dueToday = openDeals
            .Where(d => d.NextStepDueDate?.Date == today)
            .OrderByDescending(d => d.AmountGBP ?? 0)
            .Select(d => DealAction.FromDeal(d, referenceDate))
            .ToList();

        // Overdue
        var overdue = openDeals
            .Where(d => d.NextStepDueDate.HasValue && d.NextStepDueDate.Value.Date < today)
            .OrderBy(d => d.NextStepDueDate)
            .ThenByDescending(d => d.AmountGBP ?? 0)
            .Select(d => DealAction.FromDeal(d, referenceDate))
            .ToList();

        // No contact
        var noContact = openDeals
            .Where(d => !d.LastContactedDate.HasValue ||
                       (today - d.LastContactedDate.Value.Date).TotalDays >= _settings.NoContactThresholdDays)
            .OrderByDescending(d => d.AmountGBP ?? 0)
            .Select(d => DealAction.FromDeal(d, referenceDate))
            .ToList();

        // High value at risk
        var highValueAtRisk = openDeals
            .Where(d => d.AmountGBP >= _settings.HighValueThreshold)
            .Where(d =>
                (!d.LastContactedDate.HasValue || (today - d.LastContactedDate.Value.Date).TotalDays >= _settings.NoContactThresholdDays) ||
                (d.NextStepDueDate.HasValue && d.NextStepDueDate.Value.Date < today))
            .OrderByDescending(d => d.AmountGBP ?? 0)
            .Take(_settings.HighValueTopN)
            .Select(d =>
            {
                var action = DealAction.FromDeal(d, referenceDate);
                action.RiskReason = DetermineRiskReason(d, today);
                return action;
            })
            .ToList();

        return new DailyBrief
        {
            ReferenceDate = referenceDate,
            DueToday = dueToday,
            Overdue = overdue,
            NoContactDeals = noContact,
            HighValueAtRisk = highValueAtRisk
        };
    }

    private string DetermineRiskReason(Deal deal, DateTime today)
    {
        var reasons = new List<string>();

        if (!deal.LastContactedDate.HasValue)
            reasons.Add("Never contacted");
        else if ((today - deal.LastContactedDate.Value.Date).TotalDays >= _settings.NoContactThresholdDays)
            reasons.Add($"No contact for {(today - deal.LastContactedDate.Value.Date).TotalDays} days");

        if (deal.NextStepDueDate.HasValue && deal.NextStepDueDate.Value.Date < today)
            reasons.Add($"Next step overdue by {(today - deal.NextStepDueDate.Value.Date).TotalDays} days");

        return string.Join("; ", reasons);
    }

    public async Task<HygieneReport> GenerateHygieneReportAsync(CancellationToken ct = default)
    {
        var deals = await _repository.GetAllAsync(ct);
        var issues = new List<HygieneIssue>();

        foreach (var deal in deals)
        {
            // Skip closed deals for most checks
            var isClosed = deal.Stage.IsClosed();

            // Missing amount
            if (!deal.AmountGBP.HasValue && !isClosed)
            {
                issues.Add(CreateIssue(deal, HygieneIssueType.MissingAmount,
                    "Deal has no amount specified", HygieneSeverity.Medium));
            }

            // Missing close date for late-stage deals
            if (!deal.CloseDate.HasValue && deal.Stage >= DealStage.Proposal && !isClosed)
            {
                issues.Add(CreateIssue(deal, HygieneIssueType.MissingCloseDate,
                    $"Deal in {deal.Stage.ToDisplayString()} stage has no close date", HygieneSeverity.High));
            }

            // Missing next step
            if (string.IsNullOrWhiteSpace(deal.NextStep) && !isClosed)
            {
                issues.Add(CreateIssue(deal, HygieneIssueType.MissingNextStep,
                    "Deal has no next step defined", HygieneSeverity.Medium));
            }

            // Missing next step due date
            if (!deal.NextStepDueDate.HasValue && !string.IsNullOrWhiteSpace(deal.NextStep) && !isClosed)
            {
                issues.Add(CreateIssue(deal, HygieneIssueType.MissingNextStepDueDate,
                    "Next step has no due date", HygieneSeverity.Low));
            }

            // Probability-stage mismatch
            var expectedProb = deal.Stage.GetDefaultProbability();
            var probDiff = Math.Abs(deal.Probability - expectedProb);
            if (probDiff > 30 && !isClosed)
            {
                var severity = probDiff > 50 ? HygieneSeverity.High : HygieneSeverity.Medium;
                issues.Add(CreateIssue(deal, HygieneIssueType.ProbabilityStageMismatch,
                    $"Probability {deal.Probability}% unusual for {deal.Stage.ToDisplayString()} stage (expected ~{expectedProb}%)",
                    severity));
            }

            // Missing postcode
            if (string.IsNullOrWhiteSpace(deal.Postcode) && !isClosed)
            {
                issues.Add(CreateIssue(deal, HygieneIssueType.MissingPostcode,
                    "Deal has no postcode - region cannot be determined", HygieneSeverity.Low));
            }

            // Missing contact info
            if (string.IsNullOrWhiteSpace(deal.Email) && string.IsNullOrWhiteSpace(deal.Phone) && !isClosed)
            {
                issues.Add(CreateIssue(deal, HygieneIssueType.MissingContactInfo,
                    "Deal has no email or phone contact", HygieneSeverity.Medium));
            }

            // Missing owner
            if (string.IsNullOrWhiteSpace(deal.Owner) && !isClosed)
            {
                issues.Add(CreateIssue(deal, HygieneIssueType.MissingOwner,
                    "Deal has no owner assigned", HygieneSeverity.High));
            }
        }

        var dealsWithIssues = issues.Select(i => i.DealId).Distinct().Count();

        return new HygieneReport
        {
            GeneratedAt = _clock.Now,
            TotalDeals = deals.Count,
            DealsWithIssues = dealsWithIssues,
            Issues = issues.OrderByDescending(i => i.Severity)
                          .ThenByDescending(i => i.Amount ?? 0)
                          .ToList()
        };
    }

    private static HygieneIssue CreateIssue(Deal deal, HygieneIssueType type, string description, HygieneSeverity severity) =>
        new()
        {
            DealId = deal.DealId,
            DealName = deal.DealName,
            AccountName = deal.AccountName,
            Owner = deal.Owner,
            Stage = deal.Stage,
            Amount = deal.AmountGBP,
            IssueType = type,
            Description = description,
            Severity = severity
        };

    public async Task<ForecastSnapshot> GenerateForecastSnapshotAsync(DateTime referenceDate, CancellationToken ct = default)
    {
        var deals = await _repository.GetAllAsync(ct);
        var openDeals = deals.Where(d => !d.Stage.IsClosed()).ToList();
        var allDealsWithAmount = deals.Where(d => d.AmountGBP.HasValue).ToList();

        var totalPipeline = openDeals.Sum(d => d.AmountGBP ?? 0);
        var weightedPipeline = openDeals.Sum(d => d.WeightedAmountGBP ?? 0);

        // By Stage
        var byStage = openDeals
            .GroupBy(d => d.Stage)
            .Select(g => new StageBreakdown
            {
                Stage = g.Key,
                DealCount = g.Count(),
                TotalAmount = g.Sum(d => d.AmountGBP ?? 0),
                WeightedAmount = g.Sum(d => d.WeightedAmountGBP ?? 0),
                PercentageOfPipeline = totalPipeline > 0
                    ? Math.Round((double)g.Sum(d => d.AmountGBP ?? 0) / (double)totalPipeline * 100, 1)
                    : 0
            })
            .OrderBy(s => s.Stage)
            .ToList();

        // By Owner
        var byOwner = allDealsWithAmount
            .GroupBy(d => d.Owner ?? "Unassigned")
            .Select(g =>
            {
                var closed = g.Where(d => d.Stage.IsClosed()).ToList();
                var won = closed.Count(d => d.Stage == DealStage.ClosedWon);
                var lost = closed.Count(d => d.Stage == DealStage.ClosedLost);
                var openForOwner = g.Where(d => !d.Stage.IsClosed()).ToList();

                return new OwnerBreakdown
                {
                    Owner = g.Key,
                    DealCount = openForOwner.Count,
                    TotalAmount = openForOwner.Sum(d => d.AmountGBP ?? 0),
                    WeightedAmount = openForOwner.Sum(d => d.WeightedAmountGBP ?? 0),
                    ClosedWon = won,
                    ClosedLost = lost,
                    WinRate = (won + lost) > 0 ? Math.Round((double)won / (won + lost) * 100, 1) : 0
                };
            })
            .OrderByDescending(o => o.TotalAmount)
            .ToList();

        // By Close Month
        var byMonth = openDeals
            .Where(d => d.CloseDate.HasValue)
            .GroupBy(d => new { d.CloseDate!.Value.Year, d.CloseDate!.Value.Month })
            .Select(g => new MonthBreakdown
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                DealCount = g.Count(),
                TotalAmount = g.Sum(d => d.AmountGBP ?? 0),
                WeightedAmount = g.Sum(d => d.WeightedAmountGBP ?? 0)
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        // By Region
        var byRegion = openDeals
            .GroupBy(d => d.Region ?? "Unknown")
            .Select(g => new RegionBreakdown
            {
                Region = g.Key,
                DealCount = g.Count(),
                TotalAmount = g.Sum(d => d.AmountGBP ?? 0),
                WeightedAmount = g.Sum(d => d.WeightedAmountGBP ?? 0)
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        // By Product
        var byProduct = openDeals
            .GroupBy(d => d.ProductLine ?? "Unknown")
            .Select(g => new ProductBreakdown
            {
                ProductLine = g.Key,
                DealCount = g.Count(),
                TotalAmount = g.Sum(d => d.AmountGBP ?? 0),
                WeightedAmount = g.Sum(d => d.WeightedAmountGBP ?? 0),
                PercentageOfPipeline = totalPipeline > 0
                    ? Math.Round((double)g.Sum(d => d.AmountGBP ?? 0) / (double)totalPipeline * 100, 1)
                    : 0
            })
            .OrderByDescending(p => p.TotalAmount)
            .ToList();

        return new ForecastSnapshot
        {
            ReferenceDate = referenceDate,
            TotalDeals = deals.Count,
            OpenDeals = openDeals.Count,
            TotalPipeline = totalPipeline,
            WeightedPipeline = weightedPipeline,
            ByStage = byStage,
            ByOwner = byOwner,
            ByCloseMonth = byMonth,
            ByRegion = byRegion,
            ByProduct = byProduct
        };
    }
}
