using Ox4D.Core.Services;

namespace Ox4D.Core.Models;

/// <summary>
/// Typed DTO for patching Deal fields. All fields are nullable - only non-null fields are applied.
/// This replaces reflection-based patching to provide explicit validation and feedback.
/// </summary>
public class DealPatch
{
    // Identity (DealId is not patchable - it's the key)
    public string? OrderNo { get; set; }
    public string? UserId { get; set; }

    // Account & Contact
    public string? AccountName { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Location
    public string? Postcode { get; set; }
    public string? InstallationLocation { get; set; }
    // PostcodeArea, Region, MapLink are derived - not directly patchable

    // Deal Details
    public string? LeadSource { get; set; }
    public string? ProductLine { get; set; }
    public string? DealName { get; set; }

    // Stage & Value
    public string? Stage { get; set; }
    public int? Probability { get; set; }
    public decimal? AmountGBP { get; set; }

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
    public List<string>? Tags { get; set; }

    // Promoter/Referral
    public string? PromoterId { get; set; }
    public string? PromoCode { get; set; }
    public decimal? PromoterCommission { get; set; }
    public bool? CommissionPaid { get; set; }
    public DateTime? CommissionPaidDate { get; set; }

    /// <summary>
    /// Creates a DealPatch from a dictionary, validating field names.
    /// Returns the patch and any rejected fields.
    /// </summary>
    public static (DealPatch Patch, List<RejectedField> Rejected) FromDictionary(Dictionary<string, object?> dict)
    {
        var patch = new DealPatch();
        var rejected = new List<RejectedField>();

        foreach (var (key, value) in dict)
        {
            if (!TrySetField(patch, key, value, out var reason))
            {
                rejected.Add(new RejectedField(key, value?.ToString(), reason));
            }
        }

        return (patch, rejected);
    }

    private static bool TrySetField(DealPatch patch, string fieldName, object? value, out string reason)
    {
        reason = string.Empty;

        // Normalize field name (case-insensitive matching)
        var normalizedName = fieldName.ToLowerInvariant();

        try
        {
            switch (normalizedName)
            {
                // Identity
                case "orderno":
                    patch.OrderNo = value?.ToString();
                    return true;
                case "userid":
                    patch.UserId = value?.ToString();
                    return true;

                // Account & Contact
                case "accountname":
                    patch.AccountName = value?.ToString();
                    return true;
                case "contactname":
                    patch.ContactName = value?.ToString();
                    return true;
                case "email":
                    patch.Email = value?.ToString();
                    return true;
                case "phone":
                    patch.Phone = value?.ToString();
                    return true;

                // Location
                case "postcode":
                    patch.Postcode = value?.ToString();
                    return true;
                case "installationlocation":
                    patch.InstallationLocation = value?.ToString();
                    return true;

                // Deal Details
                case "leadsource":
                    patch.LeadSource = value?.ToString();
                    return true;
                case "productline":
                    patch.ProductLine = value?.ToString();
                    return true;
                case "dealname":
                    patch.DealName = value?.ToString();
                    return true;

                // Stage & Value
                case "stage":
                    patch.Stage = value?.ToString();
                    return true;
                case "probability":
                    if (value == null)
                    {
                        patch.Probability = null;
                        return true;
                    }
                    if (int.TryParse(value.ToString(), out var prob))
                    {
                        if (prob < 0 || prob > 100)
                        {
                            reason = "Probability must be between 0 and 100";
                            return false;
                        }
                        patch.Probability = prob;
                        return true;
                    }
                    reason = "Invalid probability value";
                    return false;
                case "amountgbp":
                case "amount":
                    if (value == null)
                    {
                        patch.AmountGBP = null;
                        return true;
                    }
                    // Use DealNormalizer.ParseAmount which handles currency symbols
                    var parsedAmount = DealNormalizer.ParseAmount(value.ToString());
                    if (parsedAmount.HasValue)
                    {
                        if (parsedAmount.Value < 0)
                        {
                            reason = "Amount cannot be negative";
                            return false;
                        }
                        patch.AmountGBP = parsedAmount.Value;
                        return true;
                    }
                    reason = "Invalid amount value";
                    return false;

                // Ownership & Dates
                case "owner":
                    patch.Owner = value?.ToString();
                    return true;
                case "createddate":
                    return TryParseDate(value, v => patch.CreatedDate = v, out reason);
                case "lastcontacteddate":
                    return TryParseDate(value, v => patch.LastContactedDate = v, out reason);

                // Next Steps
                case "nextstep":
                    patch.NextStep = value?.ToString();
                    return true;
                case "nextstepduedate":
                    return TryParseDate(value, v => patch.NextStepDueDate = v, out reason);
                case "closedate":
                    return TryParseDate(value, v => patch.CloseDate = v, out reason);

                // Service
                case "serviceplan":
                    patch.ServicePlan = value?.ToString();
                    return true;
                case "lastservicedate":
                    return TryParseDate(value, v => patch.LastServiceDate = v, out reason);
                case "nextserviceduedate":
                    return TryParseDate(value, v => patch.NextServiceDueDate = v, out reason);

                // Metadata
                case "comments":
                    patch.Comments = value?.ToString();
                    return true;
                case "tags":
                    if (value == null)
                    {
                        patch.Tags = null;
                        return true;
                    }
                    if (value is IEnumerable<string> list)
                    {
                        patch.Tags = list.ToList();
                        return true;
                    }
                    if (value is string str)
                    {
                        patch.Tags = str.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToList();
                        return true;
                    }
                    reason = "Tags must be a list or comma-separated string";
                    return false;

                // Promoter/Referral
                case "promoterid":
                    patch.PromoterId = value?.ToString();
                    return true;
                case "promocode":
                    patch.PromoCode = value?.ToString();
                    return true;
                case "promotercommission":
                    if (value == null)
                    {
                        patch.PromoterCommission = null;
                        return true;
                    }
                    var parsedCommission = DealNormalizer.ParseAmount(value.ToString());
                    if (parsedCommission.HasValue)
                    {
                        patch.PromoterCommission = parsedCommission.Value;
                        return true;
                    }
                    reason = "Invalid commission value";
                    return false;
                case "commissionpaid":
                    if (value == null)
                    {
                        patch.CommissionPaid = null;
                        return true;
                    }
                    if (bool.TryParse(value.ToString(), out var paid))
                    {
                        patch.CommissionPaid = paid;
                        return true;
                    }
                    if (value.ToString()?.ToLowerInvariant() is "yes" or "1")
                    {
                        patch.CommissionPaid = true;
                        return true;
                    }
                    if (value.ToString()?.ToLowerInvariant() is "no" or "0")
                    {
                        patch.CommissionPaid = false;
                        return true;
                    }
                    reason = "Invalid boolean value for CommissionPaid";
                    return false;
                case "commissionpaiddate":
                    return TryParseDate(value, v => patch.CommissionPaidDate = v, out reason);

                // Read-only/derived fields
                case "dealid":
                    reason = "DealId is the key and cannot be patched";
                    return false;
                case "postcodearea":
                case "region":
                case "maplink":
                case "weightedamountgbp":
                    reason = $"{fieldName} is a derived field and cannot be patched directly";
                    return false;

                default:
                    reason = $"Unknown field: {fieldName}";
                    return false;
            }
        }
        catch (Exception ex)
        {
            reason = $"Error processing {fieldName}: {ex.Message}";
            return false;
        }
    }

    private static bool TryParseDate(object? value, Action<DateTime?> setter, out string reason)
    {
        reason = string.Empty;
        if (value == null)
        {
            setter(null);
            return true;
        }

        var parsed = DealNormalizer.ParseDate(value.ToString());
        if (parsed.HasValue)
        {
            setter(parsed);
            return true;
        }

        reason = "Invalid date format";
        return false;
    }
}

/// <summary>
/// Represents a field that was rejected during patch parsing.
/// </summary>
public record RejectedField(string FieldName, string? AttemptedValue, string Reason);

/// <summary>
/// Result of applying a patch to a deal.
/// </summary>
public class PatchResult
{
    public bool Success { get; init; }
    public Deal? Deal { get; init; }
    public List<AppliedField> AppliedFields { get; init; } = new();
    public List<RejectedField> RejectedFields { get; init; } = new();
    public List<NormalizationChange> NormalizationChanges { get; init; } = new();
    public string? Error { get; init; }

    public static PatchResult NotFound(string dealId) => new()
    {
        Success = false,
        Error = $"Deal not found: {dealId}"
    };

    public static PatchResult ValidationFailed(List<RejectedField> rejected) => new()
    {
        Success = false,
        RejectedFields = rejected,
        Error = $"Validation failed for {rejected.Count} field(s)"
    };

    public static PatchResult Succeeded(Deal deal, List<AppliedField> applied, List<RejectedField> rejected, List<NormalizationChange> changes) => new()
    {
        Success = rejected.Count == 0,
        Deal = deal,
        AppliedFields = applied,
        RejectedFields = rejected,
        NormalizationChanges = changes,
        Error = rejected.Count > 0 ? $"Partial success: {rejected.Count} field(s) rejected" : null
    };
}

/// <summary>
/// Represents a field that was successfully applied during patching.
/// </summary>
public record AppliedField(string FieldName, string? OldValue, string? NewValue);
