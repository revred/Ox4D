namespace Ox4D.Core.Models;

public enum DealStage
{
    Lead = 0,
    Qualified = 1,
    Discovery = 2,
    Proposal = 3,
    Negotiation = 4,
    ClosedWon = 5,
    ClosedLost = 6,
    OnHold = 7,
    Other = 99
}

public static class DealStageExtensions
{
    public static string ToDisplayString(this DealStage stage) => stage switch
    {
        DealStage.Lead => "Lead",
        DealStage.Qualified => "Qualified",
        DealStage.Discovery => "Discovery",
        DealStage.Proposal => "Proposal",
        DealStage.Negotiation => "Negotiation",
        DealStage.ClosedWon => "Closed Won",
        DealStage.ClosedLost => "Closed Lost",
        DealStage.OnHold => "On Hold",
        _ => "Other"
    };

    public static DealStage ParseStage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DealStage.Lead;

        var normalized = value.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");

        return normalized switch
        {
            "lead" => DealStage.Lead,
            "qualified" => DealStage.Qualified,
            "discovery" => DealStage.Discovery,
            "proposal" => DealStage.Proposal,
            "negotiation" => DealStage.Negotiation,
            "closedwon" or "won" => DealStage.ClosedWon,
            "closedlost" or "lost" => DealStage.ClosedLost,
            "onhold" or "hold" => DealStage.OnHold,
            _ => DealStage.Other
        };
    }

    public static int GetDefaultProbability(this DealStage stage) => stage switch
    {
        DealStage.Lead => 10,
        DealStage.Qualified => 20,
        DealStage.Discovery => 40,
        DealStage.Proposal => 60,
        DealStage.Negotiation => 80,
        DealStage.ClosedWon => 100,
        DealStage.ClosedLost => 0,
        DealStage.OnHold => 10,
        _ => 10
    };

    public static bool IsClosed(this DealStage stage) =>
        stage == DealStage.ClosedWon || stage == DealStage.ClosedLost;
}
