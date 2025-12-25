// =============================================================================
// PromoterDashboard - Promoter/Referral Partner Reports
// =============================================================================
// PURPOSE:
//   Provides comprehensive reporting for promoters/referral partners including:
//   - Performance metrics and KPIs
//   - Pipeline breakdown by stage
//   - Actionable recommendations to help move deals forward
//   - Commission tracking and projections
//
// DISTINCTION:
//   Promoters refer deals using promo codes and earn commissions.
//   Sales Managers own and work deals directly through the pipeline.
// =============================================================================

namespace Ox4D.Core.Models.Reports;

/// <summary>
/// Comprehensive dashboard for promoters/referral partners.
/// </summary>
public class PromoterDashboard
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string PromoterId { get; set; } = string.Empty;
    public string PromoterName { get; set; } = string.Empty;
    public string PromoCode { get; set; } = string.Empty;
    public PromoterTier Tier { get; set; }
    public decimal CommissionRate { get; set; }

    // Performance Summary
    public PromoterSummary Summary { get; set; } = new();

    // Pipeline Breakdown
    public List<PromoterStageBreakdown> ByStage { get; set; } = new();

    // Actionable Items
    public List<PromoterAction> RecommendedActions { get; set; } = new();
    public List<PromoterDealStatus> DealsNeedingAttention { get; set; } = new();

    // Commission Tracking
    public PromoterCommissionSummary CommissionSummary { get; set; } = new();

    // Recent Activity
    public List<PromoterDealStatus> RecentDeals { get; set; } = new();
}

/// <summary>
/// Summary metrics for promoter performance.
/// </summary>
public class PromoterSummary
{
    public int TotalReferrals { get; set; }
    public int ActiveDeals { get; set; }
    public int ClosedWon { get; set; }
    public int ClosedLost { get; set; }
    public decimal ConversionRate { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public decimal TotalWonValue { get; set; }
    public decimal AverageDealsValue { get; set; }
    public int DealsClosingThisMonth { get; set; }
    public decimal ValueClosingThisMonth { get; set; }
}

/// <summary>
/// Pipeline breakdown by stage for promoter-referred deals.
/// </summary>
public class PromoterStageBreakdown
{
    public DealStage Stage { get; set; }
    public int DealCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal WeightedValue { get; set; }
    public decimal PotentialCommission { get; set; }
    public int AverageAgeDays { get; set; }
}

/// <summary>
/// Actionable recommendation for promoters to help move deals forward.
/// </summary>
public class PromoterAction
{
    public string DealId { get; set; } = string.Empty;
    public string DealName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public DealStage Stage { get; set; }
    public decimal? Amount { get; set; }
    public decimal? PotentialCommission { get; set; }
    public PromoterActionType ActionType { get; set; }
    public ActionPriority Priority { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int? DaysStuck { get; set; }
}

/// <summary>
/// Types of recommended actions for promoters.
/// </summary>
public enum PromoterActionType
{
    CheckIn,            // Check in with sales rep on progress
    ProvideContext,     // Provide additional context about the referral
    MakeIntroduction,   // Facilitate introduction to decision maker
    ProvideReference,   // Offer to provide a reference or testimonial
    ShareContent,       // Share relevant content with the prospect
    FollowUpWithLead,   // Follow up with the original lead
    EscalateInternal,   // Flag internally for priority attention
    CelebrateWin,       // Celebrate and potentially get testimonial
    ReviewLoss          // Review lost deal for learnings
}

/// <summary>
/// Priority level for recommended actions.
/// </summary>
public enum ActionPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

/// <summary>
/// Status summary for a specific deal from promoter perspective.
/// </summary>
public class PromoterDealStatus
{
    public string DealId { get; set; } = string.Empty;
    public string DealName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DealStage Stage { get; set; }
    public decimal? Amount { get; set; }
    public decimal? PotentialCommission { get; set; }
    public string? Owner { get; set; }
    public DateTime? CloseDate { get; set; }
    public DateTime? LastContactedDate { get; set; }
    public string? NextStep { get; set; }
    public DateTime? NextStepDueDate { get; set; }
    public int DaysInPipeline { get; set; }
    public int? DaysInCurrentStage { get; set; }
    public DealHealthStatus HealthStatus { get; set; }
    public string StatusReason { get; set; } = string.Empty;
}

/// <summary>
/// Health status of a deal from promoter perspective.
/// </summary>
public enum DealHealthStatus
{
    Healthy,        // On track, progressing well
    AtRisk,         // Some warning signs
    Stalled,        // No movement for extended period
    Critical,       // Urgent attention needed
    Won,            // Successfully closed
    Lost            // Deal lost
}

/// <summary>
/// Commission summary for promoter.
/// </summary>
public class PromoterCommissionSummary
{
    public decimal TotalEarned { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PendingPayment { get; set; }
    public decimal ProjectedFromPipeline { get; set; }
    public decimal ProjectedThisMonth { get; set; }
    public decimal ProjectedThisQuarter { get; set; }
    public List<CommissionDetail> PendingCommissions { get; set; } = new();
    public List<CommissionDetail> RecentPayments { get; set; } = new();
}

/// <summary>
/// Individual commission detail.
/// </summary>
public class CommissionDetail
{
    public string DealId { get; set; } = string.Empty;
    public string DealName { get; set; } = string.Empty;
    public decimal DealValue { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public DateTime? ClosedDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public CommissionStatus Status { get; set; }
}

/// <summary>
/// Status of commission payment.
/// </summary>
public enum CommissionStatus
{
    Projected,      // Deal not yet closed
    Pending,        // Deal closed, commission pending
    Processing,     // Commission being processed
    Paid            // Commission paid out
}

public static class PromoterActionTypeExtensions
{
    public static string ToDisplayString(this PromoterActionType type) => type switch
    {
        PromoterActionType.CheckIn => "Check In",
        PromoterActionType.ProvideContext => "Provide Context",
        PromoterActionType.MakeIntroduction => "Make Introduction",
        PromoterActionType.ProvideReference => "Provide Reference",
        PromoterActionType.ShareContent => "Share Content",
        PromoterActionType.FollowUpWithLead => "Follow Up",
        PromoterActionType.EscalateInternal => "Escalate",
        PromoterActionType.CelebrateWin => "Celebrate Win",
        PromoterActionType.ReviewLoss => "Review Loss",
        _ => "Action Required"
    };
}

public static class ActionPriorityExtensions
{
    public static string ToDisplayString(this ActionPriority priority) => priority switch
    {
        ActionPriority.Low => "Low",
        ActionPriority.Medium => "Medium",
        ActionPriority.High => "High",
        ActionPriority.Urgent => "Urgent",
        _ => "Unknown"
    };
}

public static class DealHealthStatusExtensions
{
    public static string ToDisplayString(this DealHealthStatus status) => status switch
    {
        DealHealthStatus.Healthy => "Healthy",
        DealHealthStatus.AtRisk => "At Risk",
        DealHealthStatus.Stalled => "Stalled",
        DealHealthStatus.Critical => "Critical",
        DealHealthStatus.Won => "Won",
        DealHealthStatus.Lost => "Lost",
        _ => "Unknown"
    };
}
