// =============================================================================
// Deal - Core Domain Entity for Sales Pipeline
// =============================================================================
// PURPOSE:
//   Represents a single sales opportunity in the pipeline. This is the central
//   entity that flows through all operations: reports, forecasts, and storage.
//
// KEY COMPUTED PROPERTIES:
//   - WeightedAmountGBP = AmountGBP * Probability / 100
//   - PostcodeArea and Region are derived from Postcode during normalization
//   - MapLink is auto-generated from Postcode
//
// STORAGE MAPPING:
//   Maps directly to the "Deals" sheet in Excel or "deals" table in Supabase.
//   All properties are serializable for both storage backends.
// =============================================================================

namespace Ox4D.Core.Models;

/// <summary>
/// Represents a sales opportunity/deal in the pipeline.
/// </summary>
public class Deal
{
    public string DealId { get; set; } = string.Empty;
    public string? OrderNo { get; set; }
    public string? UserId { get; set; }

    // Account & Contact
    public string AccountName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Location
    public string? Postcode { get; set; }
    public string? PostcodeArea { get; set; }
    public string? InstallationLocation { get; set; }
    public string? Region { get; set; }
    public string? MapLink { get; set; }

    // Deal Details
    public string? LeadSource { get; set; }
    public string? ProductLine { get; set; }
    public string DealName { get; set; } = string.Empty;

    // Stage & Value
    public DealStage Stage { get; set; } = DealStage.Lead;
    public int Probability { get; set; }
    public decimal? AmountGBP { get; set; }
    public decimal? WeightedAmountGBP => AmountGBP.HasValue ? AmountGBP.Value * Probability / 100m : null;

    // Ownership & Dates
    public string? Owner { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastContactedDate { get; set; }

    // Next Steps
    public string? NextStep { get; set; }
    public DateTime? NextStepDueDate { get; set; }
    public DateTime? CloseDate { get; set; }

    // Service
    public string? ServicePlan { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime? NextServiceDueDate { get; set; }

    // Metadata
    public string? Comments { get; set; }
    public List<string> Tags { get; set; } = new();

    public Deal Clone() => (Deal)MemberwiseClone();
}
