namespace Ox4D.Core.Models.Config;

public class PipelineSettings
{
    public int NoContactThresholdDays { get; set; } = 10;
    public int HighValueTopN { get; set; } = 10;
    public decimal HighValueThreshold { get; set; } = 50000;
    public int StaleContactWarningDays { get; set; } = 14;
    public List<string> ProductLines { get; set; } = new()
    {
        "Enterprise Software",
        "Professional Services",
        "SaaS Subscription",
        "Hardware",
        "Support & Maintenance",
        "Training",
        "Consulting"
    };
    public List<string> LeadSources { get; set; } = new()
    {
        "Inbound",
        "Outbound",
        "Referral",
        "Partner",
        "Event",
        "Website",
        "LinkedIn",
        "Cold Call"
    };
    public List<string> ServicePlans { get; set; } = new()
    {
        "Basic",
        "Standard",
        "Premium",
        "Enterprise"
    };
}
