namespace Ox4D.Core.Models.Config;

public class LookupTables
{
    public Dictionary<string, string> PostcodeToRegion { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<DealStage, int> StageProbabilities { get; set; } = new();

    public static LookupTables CreateDefault()
    {
        var tables = new LookupTables();

        // Default UK postcode area to region mappings
        var regionMappings = new Dictionary<string, string[]>
        {
            ["London"] = new[] { "E", "EC", "N", "NW", "SE", "SW", "W", "WC" },
            ["South East"] = new[] { "BN", "CT", "GU", "ME", "OX", "PO", "RG", "RH", "SL", "SO", "TN" },
            ["South West"] = new[] { "BA", "BH", "BS", "DT", "EX", "GL", "PL", "SN", "SP", "TA", "TQ", "TR" },
            ["East of England"] = new[] { "AL", "CB", "CM", "CO", "EN", "HP", "IP", "LU", "NR", "PE", "SG", "SS", "WD" },
            ["West Midlands"] = new[] { "B", "CV", "DY", "HR", "ST", "TF", "WR", "WS", "WV" },
            ["East Midlands"] = new[] { "DE", "DN", "LE", "LN", "NG", "NN" },
            ["Yorkshire"] = new[] { "BD", "HD", "HG", "HU", "HX", "LS", "S", "WF", "YO" },
            ["North West"] = new[] { "BB", "BL", "CA", "CH", "CW", "FY", "L", "LA", "M", "OL", "PR", "SK", "WA", "WN" },
            ["North East"] = new[] { "DH", "DL", "NE", "SR", "TS" },
            ["Wales"] = new[] { "CF", "LD", "LL", "NP", "SA", "SY" },
            ["Scotland"] = new[] { "AB", "DD", "DG", "EH", "FK", "G", "HS", "IV", "KA", "KW", "KY", "ML", "PA", "PH", "TD", "ZE" },
            ["Northern Ireland"] = new[] { "BT" }
        };

        foreach (var (region, postcodes) in regionMappings)
        {
            foreach (var postcode in postcodes)
            {
                tables.PostcodeToRegion[postcode] = region;
            }
        }

        // Default stage probabilities
        foreach (DealStage stage in Enum.GetValues<DealStage>())
        {
            tables.StageProbabilities[stage] = stage.GetDefaultProbability();
        }

        return tables;
    }

    public string? GetRegionForPostcode(string? postcode)
    {
        if (string.IsNullOrWhiteSpace(postcode))
            return null;

        var area = ExtractPostcodeArea(postcode);
        return PostcodeToRegion.TryGetValue(area, out var region) ? region : null;
    }

    public static string ExtractPostcodeArea(string postcode)
    {
        var clean = postcode.Trim().ToUpperInvariant().Replace(" ", "");
        var area = "";
        foreach (var c in clean)
        {
            if (char.IsLetter(c))
                area += c;
            else
                break;
        }
        return area;
    }

    public int GetProbabilityForStage(DealStage stage) =>
        StageProbabilities.TryGetValue(stage, out var prob) ? prob : stage.GetDefaultProbability();
}
