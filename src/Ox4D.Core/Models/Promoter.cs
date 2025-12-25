// =============================================================================
// Promoter - Referral Partner Entity for Sales Pipeline
// =============================================================================
// PURPOSE:
//   Represents a promoter/referral partner who refers deals to the pipeline.
//   Promoters use unique promo codes to track their referrals and earn
//   commissions on successful deals.
//
// KEY FEATURES:
//   - Unique promo code for deal attribution
//   - Commission rate tracking (percentage of deal value)
//   - Performance metrics and tier levels
//   - Referral tracking
//
// DISTINCTION FROM SALES MANAGER:
//   - Sales Managers own and work deals directly through the pipeline
//   - Promoters refer deals using promo codes and earn commissions
//   - A deal can have both an Owner (sales manager) and a Promoter
// =============================================================================

namespace Ox4D.Core.Models;

/// <summary>
/// Represents a promoter/referral partner who refers deals using promo codes.
/// </summary>
public class Promoter
{
    public string PromoterId { get; set; } = string.Empty;
    public string PromoCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }

    // Commission Settings
    public decimal CommissionRate { get; set; } = 10m; // Default 10%
    public PromoterTier Tier { get; set; } = PromoterTier.Bronze;

    // Status
    public PromoterStatus Status { get; set; } = PromoterStatus.Active;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityDate { get; set; }

    // Performance Tracking (computed from deals)
    public int TotalReferrals { get; set; }
    public int ConvertedReferrals { get; set; }
    public decimal TotalCommissionEarned { get; set; }
    public decimal PendingCommission { get; set; }

    // Metadata
    public string? Notes { get; set; }
    public List<string> Tags { get; set; } = new();

    public decimal ConversionRate => TotalReferrals > 0
        ? Math.Round((decimal)ConvertedReferrals / TotalReferrals * 100, 2)
        : 0;

    public Promoter Clone() => (Promoter)MemberwiseClone();
}

/// <summary>
/// Promoter tier levels determining commission rates and benefits.
/// </summary>
public enum PromoterTier
{
    Bronze = 0,    // 10% commission
    Silver = 1,    // 12% commission
    Gold = 2,      // 15% commission
    Platinum = 3,  // 18% commission
    Diamond = 4    // 20% commission
}

/// <summary>
/// Promoter account status.
/// </summary>
public enum PromoterStatus
{
    Pending = 0,
    Active = 1,
    Suspended = 2,
    Inactive = 3
}

public static class PromoterTierExtensions
{
    public static string ToDisplayString(this PromoterTier tier) => tier switch
    {
        PromoterTier.Bronze => "Bronze",
        PromoterTier.Silver => "Silver",
        PromoterTier.Gold => "Gold",
        PromoterTier.Platinum => "Platinum",
        PromoterTier.Diamond => "Diamond",
        _ => "Unknown"
    };

    public static decimal GetCommissionRate(this PromoterTier tier) => tier switch
    {
        PromoterTier.Bronze => 10m,
        PromoterTier.Silver => 12m,
        PromoterTier.Gold => 15m,
        PromoterTier.Platinum => 18m,
        PromoterTier.Diamond => 20m,
        _ => 10m
    };

    public static int GetMinReferralsForTier(this PromoterTier tier) => tier switch
    {
        PromoterTier.Bronze => 0,
        PromoterTier.Silver => 10,
        PromoterTier.Gold => 25,
        PromoterTier.Platinum => 50,
        PromoterTier.Diamond => 100,
        _ => 0
    };

    public static PromoterTier ParseTier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return PromoterTier.Bronze;

        return value.Trim().ToLowerInvariant() switch
        {
            "bronze" => PromoterTier.Bronze,
            "silver" => PromoterTier.Silver,
            "gold" => PromoterTier.Gold,
            "platinum" => PromoterTier.Platinum,
            "diamond" => PromoterTier.Diamond,
            _ => PromoterTier.Bronze
        };
    }
}

public static class PromoterStatusExtensions
{
    public static string ToDisplayString(this PromoterStatus status) => status switch
    {
        PromoterStatus.Pending => "Pending",
        PromoterStatus.Active => "Active",
        PromoterStatus.Suspended => "Suspended",
        PromoterStatus.Inactive => "Inactive",
        _ => "Unknown"
    };
}
