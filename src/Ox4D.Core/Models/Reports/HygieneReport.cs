namespace Ox4D.Core.Models.Reports;

public class HygieneReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalDeals { get; set; }
    public int DealsWithIssues { get; set; }
    public List<HygieneIssue> Issues { get; set; } = new();

    public double HealthScore => TotalDeals > 0
        ? Math.Round((1 - (double)DealsWithIssues / TotalDeals) * 100, 1)
        : 100;

    public Dictionary<HygieneIssueType, int> IssuesByType =>
        Issues.GroupBy(i => i.IssueType).ToDictionary(g => g.Key, g => g.Count());
}

public class HygieneIssue
{
    public string DealId { get; set; } = string.Empty;
    public string DealName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public DealStage Stage { get; set; }
    public decimal? Amount { get; set; }
    public HygieneIssueType IssueType { get; set; }
    public string Description { get; set; } = string.Empty;
    public HygieneSeverity Severity { get; set; }
}

public enum HygieneIssueType
{
    MissingAmount,
    MissingCloseDate,
    MissingNextStep,
    MissingNextStepDueDate,
    ProbabilityStageMismatch,
    MissingPostcode,
    MissingContactInfo,
    StaleLastContact,
    MissingOwner
}

public enum HygieneSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public static class HygieneIssueTypeExtensions
{
    public static string ToDisplayString(this HygieneIssueType type) => type switch
    {
        HygieneIssueType.MissingAmount => "Missing Amount",
        HygieneIssueType.MissingCloseDate => "Missing Close Date",
        HygieneIssueType.MissingNextStep => "Missing Next Step",
        HygieneIssueType.MissingNextStepDueDate => "Missing Next Step Due Date",
        HygieneIssueType.ProbabilityStageMismatch => "Probability/Stage Mismatch",
        HygieneIssueType.MissingPostcode => "Missing Postcode",
        HygieneIssueType.MissingContactInfo => "Missing Contact Info",
        HygieneIssueType.StaleLastContact => "Stale Last Contact",
        HygieneIssueType.MissingOwner => "Missing Owner",
        _ => type.ToString()
    };
}
