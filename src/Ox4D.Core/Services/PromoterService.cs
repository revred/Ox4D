// =============================================================================
// PromoterService - Promoter/Referral Partner Operations
// =============================================================================
// PURPOSE:
//   Provides services for promoters/referral partners including:
//   - Dashboard generation with performance metrics
//   - Action recommendations to help move referred deals through the pipeline
//   - Commission tracking and projections
//   - Deal health monitoring
//
// PROMOTER ACTIONS:
//   Unlike sales managers who directly work deals, promoters support deals by:
//   - Checking in with sales reps on progress
//   - Providing additional context about their referrals
//   - Facilitating introductions to decision makers
//   - Sharing relevant content with prospects
// =============================================================================

using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Models.Reports;

namespace Ox4D.Core.Services;

/// <summary>
/// Service for promoter/referral partner operations.
/// </summary>
public class PromoterService
{
    private readonly IDealRepository _repository;
    private readonly PipelineSettings _settings;

    // Thresholds for action recommendations
    private const int StaleLeadDays = 7;
    private const int StaleQualifiedDays = 10;
    private const int StaleDiscoveryDays = 14;
    private const int StaleProposalDays = 7;
    private const int StaleNegotiationDays = 5;
    private const int NoProgressWarningDays = 7;
    private const int NoProgressCriticalDays = 14;

    public PromoterService(IDealRepository repository, PipelineSettings settings)
    {
        _repository = repository;
        _settings = settings;
    }

    /// <summary>
    /// Generates a comprehensive dashboard for a promoter.
    /// </summary>
    public async Task<PromoterDashboard> GetPromoterDashboardAsync(
        string promoterId,
        string promoterName,
        string promoCode,
        PromoterTier tier,
        DateTime? referenceDate = null,
        CancellationToken ct = default)
    {
        var refDate = referenceDate ?? DateTime.Today;
        var commissionRate = tier.GetCommissionRate();

        // Get all deals for this promoter
        var allDeals = await _repository.GetAllAsync(ct);
        var promoterDeals = allDeals
            .Where(d => string.Equals(d.PromoterId, promoterId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.PromoCode, promoCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var openDeals = promoterDeals.Where(d => !d.Stage.IsClosed()).ToList();
        var wonDeals = promoterDeals.Where(d => d.Stage == DealStage.ClosedWon).ToList();
        var lostDeals = promoterDeals.Where(d => d.Stage == DealStage.ClosedLost).ToList();

        var dashboard = new PromoterDashboard
        {
            GeneratedAt = DateTime.UtcNow,
            PromoterId = promoterId,
            PromoterName = promoterName,
            PromoCode = promoCode,
            Tier = tier,
            CommissionRate = commissionRate,
            Summary = GenerateSummary(promoterDeals, openDeals, wonDeals, lostDeals, commissionRate, refDate),
            ByStage = GenerateStageBreakdown(openDeals, commissionRate, refDate),
            RecommendedActions = GenerateRecommendedActions(openDeals, commissionRate, refDate),
            DealsNeedingAttention = GetDealsNeedingAttention(openDeals, commissionRate, refDate),
            CommissionSummary = GenerateCommissionSummary(promoterDeals, openDeals, wonDeals, commissionRate, refDate),
            RecentDeals = GetRecentDeals(promoterDeals, commissionRate, refDate, 10)
        };

        return dashboard;
    }

    /// <summary>
    /// Gets all deals for a promoter with status information.
    /// </summary>
    public async Task<List<PromoterDealStatus>> GetPromoterDealsAsync(
        string promoterId,
        string promoCode,
        decimal commissionRate,
        DateTime? referenceDate = null,
        CancellationToken ct = default)
    {
        var refDate = referenceDate ?? DateTime.Today;
        var allDeals = await _repository.GetAllAsync(ct);
        var promoterDeals = allDeals
            .Where(d => string.Equals(d.PromoterId, promoterId, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.PromoCode, promoCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return promoterDeals
            .Select(d => CreateDealStatus(d, commissionRate, refDate))
            .OrderByDescending(d => d.Amount ?? 0)
            .ToList();
    }

    /// <summary>
    /// Gets recommended actions for a promoter to help their referred deals progress.
    /// </summary>
    public async Task<List<PromoterAction>> GetRecommendedActionsAsync(
        string promoterId,
        string promoCode,
        decimal commissionRate,
        DateTime? referenceDate = null,
        CancellationToken ct = default)
    {
        var refDate = referenceDate ?? DateTime.Today;
        var allDeals = await _repository.GetAllAsync(ct);
        var openDeals = allDeals
            .Where(d => !d.Stage.IsClosed() &&
                        (string.Equals(d.PromoterId, promoterId, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(d.PromoCode, promoCode, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return GenerateRecommendedActions(openDeals, commissionRate, refDate);
    }

    private PromoterSummary GenerateSummary(
        List<Deal> allDeals,
        List<Deal> openDeals,
        List<Deal> wonDeals,
        List<Deal> lostDeals,
        decimal commissionRate,
        DateTime refDate)
    {
        var closedDeals = wonDeals.Count + lostDeals.Count;
        var thisMonth = new DateTime(refDate.Year, refDate.Month, 1);
        var nextMonth = thisMonth.AddMonths(1);

        var dealsClosingThisMonth = openDeals
            .Where(d => d.CloseDate >= thisMonth && d.CloseDate < nextMonth)
            .ToList();

        return new PromoterSummary
        {
            TotalReferrals = allDeals.Count,
            ActiveDeals = openDeals.Count,
            ClosedWon = wonDeals.Count,
            ClosedLost = lostDeals.Count,
            ConversionRate = closedDeals > 0
                ? Math.Round((decimal)wonDeals.Count / closedDeals * 100, 2)
                : 0,
            TotalPipelineValue = openDeals.Sum(d => d.AmountGBP ?? 0),
            WeightedPipelineValue = openDeals.Sum(d => d.WeightedAmountGBP ?? 0),
            TotalWonValue = wonDeals.Sum(d => d.AmountGBP ?? 0),
            AverageDealsValue = openDeals.Any()
                ? openDeals.Where(d => d.AmountGBP.HasValue).Average(d => d.AmountGBP!.Value)
                : 0,
            DealsClosingThisMonth = dealsClosingThisMonth.Count,
            ValueClosingThisMonth = dealsClosingThisMonth.Sum(d => d.AmountGBP ?? 0)
        };
    }

    private List<PromoterStageBreakdown> GenerateStageBreakdown(
        List<Deal> openDeals,
        decimal commissionRate,
        DateTime refDate)
    {
        return openDeals
            .GroupBy(d => d.Stage)
            .Select(g =>
            {
                var deals = g.ToList();
                var totalValue = deals.Sum(d => d.AmountGBP ?? 0);

                return new PromoterStageBreakdown
                {
                    Stage = g.Key,
                    DealCount = deals.Count,
                    TotalValue = totalValue,
                    WeightedValue = deals.Sum(d => d.WeightedAmountGBP ?? 0),
                    PotentialCommission = totalValue * commissionRate / 100,
                    AverageAgeDays = deals.Any(d => d.CreatedDate.HasValue)
                        ? (int)deals.Where(d => d.CreatedDate.HasValue)
                            .Average(d => (refDate - d.CreatedDate!.Value).TotalDays)
                        : 0
                };
            })
            .OrderBy(s => s.Stage)
            .ToList();
    }

    private List<PromoterAction> GenerateRecommendedActions(
        List<Deal> openDeals,
        decimal commissionRate,
        DateTime refDate)
    {
        var actions = new List<PromoterAction>();

        foreach (var deal in openDeals)
        {
            var dealActions = AnalyzeDealForActions(deal, commissionRate, refDate);
            actions.AddRange(dealActions);
        }

        // Sort by priority (highest first), then by potential commission (highest first)
        return actions
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.PotentialCommission ?? 0)
            .ToList();
    }

    private List<PromoterAction> AnalyzeDealForActions(Deal deal, decimal commissionRate, DateTime refDate)
    {
        var actions = new List<PromoterAction>();
        var daysInPipeline = deal.CreatedDate.HasValue
            ? (int)(refDate - deal.CreatedDate.Value).TotalDays
            : 0;
        var daysSinceContact = deal.LastContactedDate.HasValue
            ? (int)(refDate - deal.LastContactedDate.Value).TotalDays
            : int.MaxValue;
        var potentialCommission = deal.AmountGBP.HasValue
            ? deal.AmountGBP.Value * commissionRate / 100
            : (decimal?)null;

        // Check for stalled deals - promoter should check in with sales rep
        if (daysSinceContact >= NoProgressCriticalDays)
        {
            actions.Add(CreateAction(deal, potentialCommission,
                PromoterActionType.CheckIn,
                ActionPriority.High,
                $"Check in with {deal.Owner ?? "sales rep"} on this deal - no activity for {daysSinceContact} days",
                "Deal may need additional support or context from you",
                null,
                daysSinceContact));
        }
        else if (daysSinceContact >= NoProgressWarningDays)
        {
            actions.Add(CreateAction(deal, potentialCommission,
                PromoterActionType.CheckIn,
                ActionPriority.Medium,
                $"Touch base with {deal.Owner ?? "sales rep"} to see if they need any support",
                $"No activity for {daysSinceContact} days",
                null,
                daysSinceContact));
        }

        // Stage-specific recommendations
        switch (deal.Stage)
        {
            case DealStage.Lead:
                if (daysInPipeline > StaleLeadDays)
                {
                    actions.Add(CreateAction(deal, potentialCommission,
                        PromoterActionType.ProvideContext,
                        ActionPriority.Medium,
                        "Provide additional context about this lead to help qualification",
                        $"Lead has been in pipeline for {daysInPipeline} days without progressing",
                        null,
                        daysInPipeline));
                }
                break;

            case DealStage.Qualified:
                if (daysInPipeline > StaleQualifiedDays)
                {
                    actions.Add(CreateAction(deal, potentialCommission,
                        PromoterActionType.ShareContent,
                        ActionPriority.Medium,
                        "Share relevant case studies or content to help move to discovery",
                        $"Qualified lead stuck for {daysInPipeline} days",
                        null,
                        daysInPipeline));
                }
                break;

            case DealStage.Discovery:
                if (daysInPipeline > StaleDiscoveryDays)
                {
                    actions.Add(CreateAction(deal, potentialCommission,
                        PromoterActionType.MakeIntroduction,
                        ActionPriority.High,
                        "Consider facilitating an introduction to a key decision maker",
                        $"Discovery taking longer than usual ({daysInPipeline} days)",
                        null,
                        daysInPipeline));
                }
                break;

            case DealStage.Proposal:
                if (daysInPipeline > StaleProposalDays)
                {
                    actions.Add(CreateAction(deal, potentialCommission,
                        PromoterActionType.ProvideReference,
                        ActionPriority.High,
                        "Offer to provide a reference or introduction to an existing customer",
                        "Reference can help validate the proposal and accelerate decision",
                        null,
                        daysInPipeline));
                }
                break;

            case DealStage.Negotiation:
                if (daysInPipeline > StaleNegotiationDays)
                {
                    actions.Add(CreateAction(deal, potentialCommission,
                        PromoterActionType.EscalateInternal,
                        deal.AmountGBP >= _settings.HighValueThreshold
                            ? ActionPriority.Urgent
                            : ActionPriority.High,
                        "Flag this deal for priority attention - close is imminent",
                        $"High-value deal in negotiation for {daysInPipeline} days",
                        null,
                        daysInPipeline));
                }

                // High-value deals in negotiation
                if (deal.AmountGBP >= _settings.HighValueThreshold)
                {
                    actions.Add(CreateAction(deal, potentialCommission,
                        PromoterActionType.MakeIntroduction,
                        ActionPriority.High,
                        "Ensure all key stakeholders are engaged on both sides",
                        $"High-value deal (£{deal.AmountGBP:N0}) - verify decision-maker alignment",
                        null,
                        null));
                }
                break;
        }

        // Close date passed - need status update
        if (deal.CloseDate.HasValue && deal.CloseDate.Value.Date < refDate.Date)
        {
            var daysPast = (int)(refDate.Date - deal.CloseDate.Value.Date).TotalDays;
            actions.Add(CreateAction(deal, potentialCommission,
                PromoterActionType.CheckIn,
                ActionPriority.Urgent,
                $"Get status update from {deal.Owner ?? "sales rep"} - expected close date passed",
                $"Deal was expected to close {daysPast} days ago",
                deal.CloseDate,
                daysPast));
        }

        return actions;
    }

    private PromoterAction CreateAction(
        Deal deal,
        decimal? potentialCommission,
        PromoterActionType actionType,
        ActionPriority priority,
        string recommendation,
        string reason,
        DateTime? dueDate,
        int? daysStuck)
    {
        return new PromoterAction
        {
            DealId = deal.DealId,
            DealName = deal.DealName,
            AccountName = deal.AccountName,
            Owner = deal.Owner,
            Stage = deal.Stage,
            Amount = deal.AmountGBP,
            PotentialCommission = potentialCommission,
            ActionType = actionType,
            Priority = priority,
            Recommendation = recommendation,
            Reason = reason,
            DueDate = dueDate,
            DaysStuck = daysStuck
        };
    }

    private List<PromoterDealStatus> GetDealsNeedingAttention(
        List<Deal> openDeals,
        decimal commissionRate,
        DateTime refDate)
    {
        return openDeals
            .Select(d => CreateDealStatus(d, commissionRate, refDate))
            .Where(d => d.HealthStatus != DealHealthStatus.Healthy)
            .OrderByDescending(d => d.HealthStatus)
            .ThenByDescending(d => d.Amount ?? 0)
            .Take(10)
            .ToList();
    }

    private PromoterDealStatus CreateDealStatus(Deal deal, decimal commissionRate, DateTime refDate)
    {
        var daysInPipeline = deal.CreatedDate.HasValue
            ? (int)(refDate - deal.CreatedDate.Value).TotalDays
            : 0;

        var daysSinceContact = deal.LastContactedDate.HasValue
            ? (int)(refDate - deal.LastContactedDate.Value).TotalDays
            : (int?)null;

        var (healthStatus, statusReason) = DetermineHealthStatus(deal, refDate);

        return new PromoterDealStatus
        {
            DealId = deal.DealId,
            DealName = deal.DealName,
            AccountName = deal.AccountName,
            Stage = deal.Stage,
            Amount = deal.AmountGBP,
            PotentialCommission = deal.AmountGBP.HasValue
                ? deal.AmountGBP.Value * commissionRate / 100
                : null,
            Owner = deal.Owner,
            CloseDate = deal.CloseDate,
            LastContactedDate = deal.LastContactedDate,
            NextStep = deal.NextStep,
            NextStepDueDate = deal.NextStepDueDate,
            DaysInPipeline = daysInPipeline,
            DaysInCurrentStage = daysSinceContact,
            HealthStatus = healthStatus,
            StatusReason = statusReason
        };
    }

    private (DealHealthStatus Status, string Reason) DetermineHealthStatus(Deal deal, DateTime refDate)
    {
        if (deal.Stage == DealStage.ClosedWon)
            return (DealHealthStatus.Won, "Deal successfully closed");

        if (deal.Stage == DealStage.ClosedLost)
            return (DealHealthStatus.Lost, "Deal was lost");

        var issues = new List<string>();

        // Check for no activity
        if (!deal.LastContactedDate.HasValue)
        {
            issues.Add("No contact date recorded");
        }
        else
        {
            var daysSinceContact = (int)(refDate - deal.LastContactedDate.Value).TotalDays;
            if (daysSinceContact >= NoProgressCriticalDays)
                return (DealHealthStatus.Critical, $"No activity for {daysSinceContact} days");
            if (daysSinceContact >= NoProgressWarningDays)
                issues.Add($"No activity for {daysSinceContact} days");
        }

        // Check for overdue next step
        if (deal.NextStepDueDate.HasValue && deal.NextStepDueDate.Value.Date < refDate.Date)
        {
            var daysOverdue = (int)(refDate.Date - deal.NextStepDueDate.Value.Date).TotalDays;
            if (daysOverdue > 7)
                return (DealHealthStatus.Critical, $"Next step overdue by {daysOverdue} days");
            issues.Add($"Next step overdue by {daysOverdue} days");
        }

        // Check for missed close date
        if (deal.CloseDate.HasValue && deal.CloseDate.Value.Date < refDate.Date)
        {
            var daysPastClose = (int)(refDate.Date - deal.CloseDate.Value.Date).TotalDays;
            if (daysPastClose > 14)
                return (DealHealthStatus.Critical, $"Close date passed {daysPastClose} days ago");
            issues.Add($"Close date passed {daysPastClose} days ago");
        }

        // Check stage staleness
        var staleDays = GetStaleDaysForStage(deal.Stage);
        if (deal.CreatedDate.HasValue)
        {
            var daysInPipeline = (int)(refDate - deal.CreatedDate.Value).TotalDays;
            if (daysInPipeline > staleDays * 2)
                return (DealHealthStatus.Stalled, $"Deal stuck in {deal.Stage.ToDisplayString()} for {daysInPipeline} days");
            if (daysInPipeline > staleDays)
                issues.Add($"Deal in {deal.Stage.ToDisplayString()} for {daysInPipeline} days");
        }

        if (issues.Count >= 2)
            return (DealHealthStatus.AtRisk, string.Join("; ", issues));

        if (issues.Count == 1)
            return (DealHealthStatus.AtRisk, issues[0]);

        return (DealHealthStatus.Healthy, "Deal progressing normally");
    }

    private int GetStaleDaysForStage(DealStage stage) => stage switch
    {
        DealStage.Lead => StaleLeadDays,
        DealStage.Qualified => StaleQualifiedDays,
        DealStage.Discovery => StaleDiscoveryDays,
        DealStage.Proposal => StaleProposalDays,
        DealStage.Negotiation => StaleNegotiationDays,
        _ => 14
    };

    private PromoterCommissionSummary GenerateCommissionSummary(
        List<Deal> allDeals,
        List<Deal> openDeals,
        List<Deal> wonDeals,
        decimal commissionRate,
        DateTime refDate)
    {
        var thisMonth = new DateTime(refDate.Year, refDate.Month, 1);
        var nextMonth = thisMonth.AddMonths(1);
        var thisQuarter = new DateTime(refDate.Year, ((refDate.Month - 1) / 3) * 3 + 1, 1);
        var nextQuarter = thisQuarter.AddMonths(3);

        var paidDeals = wonDeals.Where(d => d.CommissionPaid).ToList();
        var pendingDeals = wonDeals.Where(d => !d.CommissionPaid).ToList();

        var dealsClosingThisMonth = openDeals
            .Where(d => d.CloseDate >= thisMonth && d.CloseDate < nextMonth)
            .ToList();

        var dealsClosingThisQuarter = openDeals
            .Where(d => d.CloseDate >= thisQuarter && d.CloseDate < nextQuarter)
            .ToList();

        return new PromoterCommissionSummary
        {
            TotalEarned = wonDeals.Sum(d => d.PromoterCommission ?? (d.AmountGBP ?? 0) * commissionRate / 100),
            TotalPaid = paidDeals.Sum(d => d.PromoterCommission ?? (d.AmountGBP ?? 0) * commissionRate / 100),
            PendingPayment = pendingDeals.Sum(d => d.PromoterCommission ?? (d.AmountGBP ?? 0) * commissionRate / 100),
            ProjectedFromPipeline = openDeals.Sum(d => (d.WeightedAmountGBP ?? 0) * commissionRate / 100),
            ProjectedThisMonth = dealsClosingThisMonth.Sum(d => (d.WeightedAmountGBP ?? 0) * commissionRate / 100),
            ProjectedThisQuarter = dealsClosingThisQuarter.Sum(d => (d.WeightedAmountGBP ?? 0) * commissionRate / 100),
            PendingCommissions = pendingDeals
                .Select(d => new CommissionDetail
                {
                    DealId = d.DealId,
                    DealName = d.DealName,
                    DealValue = d.AmountGBP ?? 0,
                    CommissionRate = commissionRate,
                    CommissionAmount = d.PromoterCommission ?? (d.AmountGBP ?? 0) * commissionRate / 100,
                    ClosedDate = d.CloseDate,
                    Status = CommissionStatus.Pending
                })
                .OrderByDescending(c => c.CommissionAmount)
                .ToList(),
            RecentPayments = paidDeals
                .OrderByDescending(d => d.CommissionPaidDate)
                .Take(5)
                .Select(d => new CommissionDetail
                {
                    DealId = d.DealId,
                    DealName = d.DealName,
                    DealValue = d.AmountGBP ?? 0,
                    CommissionRate = commissionRate,
                    CommissionAmount = d.PromoterCommission ?? (d.AmountGBP ?? 0) * commissionRate / 100,
                    ClosedDate = d.CloseDate,
                    PaidDate = d.CommissionPaidDate,
                    Status = CommissionStatus.Paid
                })
                .ToList()
        };
    }

    private List<PromoterDealStatus> GetRecentDeals(
        List<Deal> allDeals,
        decimal commissionRate,
        DateTime refDate,
        int count)
    {
        return allDeals
            .OrderByDescending(d => d.CreatedDate ?? DateTime.MinValue)
            .Take(count)
            .Select(d => CreateDealStatus(d, commissionRate, refDate))
            .ToList();
    }
}
