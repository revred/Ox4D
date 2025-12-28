using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;

namespace Ox4D.Core.Services;

/// <summary>
/// Represents a single change made during normalization
/// </summary>
public record NormalizationChange(
    string FieldName,
    string? OldValue,
    string? NewValue,
    string Reason
);

/// <summary>
/// Result of normalizing a deal, including the normalized deal and any changes made
/// </summary>
public class NormalizationResult
{
    public Deal Deal { get; init; } = null!;
    public List<NormalizationChange> Changes { get; init; } = new();
    public bool HasChanges => Changes.Count > 0;

    public static NormalizationResult NoChanges(Deal deal) => new() { Deal = deal };
}

public class DealNormalizer
{
    private readonly LookupTables _lookups;
    private readonly IDealIdGenerator _idGenerator;
    private readonly IClock _clock;

    public DealNormalizer(LookupTables lookups)
        : this(lookups, new DefaultDealIdGenerator(), SystemClock.Instance) { }

    public DealNormalizer(LookupTables lookups, IDealIdGenerator idGenerator, IClock? clock = null)
    {
        _lookups = lookups;
        _idGenerator = idGenerator;
        _clock = clock ?? SystemClock.Instance;
    }

    /// <summary>
    /// Normalizes a deal without tracking changes (original behavior)
    /// </summary>
    public Deal Normalize(Deal deal)
    {
        return NormalizeWithTracking(deal).Deal;
    }

    /// <summary>
    /// Normalizes a deal and returns detailed change tracking
    /// </summary>
    public NormalizationResult NormalizeWithTracking(Deal deal)
    {
        var normalized = deal.Clone();
        var changes = new List<NormalizationChange>();

        // Generate DealId if missing
        if (string.IsNullOrEmpty(normalized.DealId))
        {
            var newId = _idGenerator.Generate();
            changes.Add(new NormalizationChange(
                nameof(Deal.DealId),
                null,
                newId,
                "Auto-generated missing DealId"));
            normalized.DealId = newId;
        }

        // Set probability from stage if not set or zero
        if (normalized.Probability <= 0)
        {
            var oldProb = normalized.Probability;
            var newProb = _lookups.GetProbabilityForStage(normalized.Stage);
            changes.Add(new NormalizationChange(
                nameof(Deal.Probability),
                oldProb.ToString(),
                newProb.ToString(),
                $"Set default probability for stage {normalized.Stage}"));
            normalized.Probability = newProb;
        }

        // Extract postcode area and region
        if (!string.IsNullOrWhiteSpace(normalized.Postcode))
        {
            var oldArea = normalized.PostcodeArea;
            var newArea = LookupTables.ExtractPostcodeArea(normalized.Postcode);
            if (oldArea != newArea)
            {
                changes.Add(new NormalizationChange(
                    nameof(Deal.PostcodeArea),
                    oldArea,
                    newArea,
                    $"Extracted from postcode {normalized.Postcode}"));
                normalized.PostcodeArea = newArea;
            }

            if (normalized.Region == null)
            {
                var newRegion = _lookups.GetRegionForPostcode(normalized.Postcode);
                if (newRegion != null)
                {
                    changes.Add(new NormalizationChange(
                        nameof(Deal.Region),
                        null,
                        newRegion,
                        $"Derived from postcode area {newArea}"));
                    normalized.Region = newRegion;
                }
            }
        }

        // Generate map link
        if (!string.IsNullOrWhiteSpace(normalized.Postcode) && string.IsNullOrWhiteSpace(normalized.MapLink))
        {
            var address = !string.IsNullOrWhiteSpace(normalized.InstallationLocation)
                ? $"{normalized.InstallationLocation}, {normalized.Postcode}"
                : normalized.Postcode;
            var newMapLink = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(address)}";
            changes.Add(new NormalizationChange(
                nameof(Deal.MapLink),
                null,
                newMapLink,
                "Generated Google Maps link from address"));
            normalized.MapLink = newMapLink;
        }

        // Set created date if missing
        if (!normalized.CreatedDate.HasValue)
        {
            var newDate = _clock.Today;
            changes.Add(new NormalizationChange(
                nameof(Deal.CreatedDate),
                null,
                newDate.ToString("yyyy-MM-dd"),
                "Set default creation date"));
            normalized.CreatedDate = newDate;
        }

        // Normalize tags
        var originalTags = string.Join(", ", normalized.Tags);
        normalized.Tags = normalized.Tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var normalizedTags = string.Join(", ", normalized.Tags);
        if (originalTags != normalizedTags)
        {
            changes.Add(new NormalizationChange(
                nameof(Deal.Tags),
                originalTags,
                normalizedTags,
                "Cleaned and deduplicated tags"));
        }

        return new NormalizationResult
        {
            Deal = normalized,
            Changes = changes
        };
    }

    public static string GenerateDealId() =>
        $"D-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    public static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Try ISO format first
        if (DateTime.TryParse(value, out var result))
            return result;

        // Try UK format (dd/MM/yyyy)
        if (DateTime.TryParseExact(value, new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy" },
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result))
            return result;

        return null;
    }

    public static decimal? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var clean = value.Replace("£", "").Replace(",", "").Replace(" ", "").Trim();
        return decimal.TryParse(clean, out var result) ? result : null;
    }

    public static int ParseProbability(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var clean = value.Replace("%", "").Trim();
        return int.TryParse(clean, out var result) ? Math.Clamp(result, 0, 100) : 0;
    }
}
