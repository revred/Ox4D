using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;

namespace Ox4D.Core.Services;

public class DealNormalizer
{
    private readonly LookupTables _lookups;

    public DealNormalizer(LookupTables lookups)
    {
        _lookups = lookups;
    }

    public Deal Normalize(Deal deal)
    {
        var normalized = deal.Clone();

        // Normalize stage
        if (string.IsNullOrEmpty(normalized.DealId))
        {
            normalized.DealId = GenerateDealId();
        }

        // Set probability from stage if not set or zero
        if (normalized.Probability <= 0)
        {
            normalized.Probability = _lookups.GetProbabilityForStage(normalized.Stage);
        }

        // Extract postcode area and region
        if (!string.IsNullOrWhiteSpace(normalized.Postcode))
        {
            normalized.PostcodeArea = LookupTables.ExtractPostcodeArea(normalized.Postcode);
            normalized.Region ??= _lookups.GetRegionForPostcode(normalized.Postcode);
        }

        // Generate map link
        if (!string.IsNullOrWhiteSpace(normalized.Postcode) && string.IsNullOrWhiteSpace(normalized.MapLink))
        {
            var address = !string.IsNullOrWhiteSpace(normalized.InstallationLocation)
                ? $"{normalized.InstallationLocation}, {normalized.Postcode}"
                : normalized.Postcode;
            normalized.MapLink = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(address)}";
        }

        // Set created date if missing
        normalized.CreatedDate ??= DateTime.Today;

        // Normalize tags
        normalized.Tags = normalized.Tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalized;
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
