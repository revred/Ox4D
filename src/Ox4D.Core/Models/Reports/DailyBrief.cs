namespace Ox4D.Core.Models.Reports;

public class DailyBrief
{
    public DateTime ReferenceDate { get; set; }
    public List<DealAction> DueToday { get; set; } = new();
    public List<DealAction> Overdue { get; set; } = new();
    public List<DealAction> NoContactDeals { get; set; } = new();
    public List<DealAction> HighValueAtRisk { get; set; } = new();

    public int TotalActionItems => DueToday.Count + Overdue.Count;
    public decimal TotalAtRiskValue => HighValueAtRisk.Sum(d => d.Amount ?? 0);
}

public class DealAction
{
    public string DealId { get; set; } = string.Empty;
    public string DealName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Owner { get; set; }
    public DealStage Stage { get; set; }
    public decimal? Amount { get; set; }
    public string? NextStep { get; set; }
    public DateTime? NextStepDueDate { get; set; }
    public DateTime? LastContactedDate { get; set; }
    public int? DaysOverdue { get; set; }
    public int? DaysSinceContact { get; set; }
    public string? RiskReason { get; set; }

    public static DealAction FromDeal(Deal deal, DateTime referenceDate)
    {
        int? daysOverdue = null;
        if (deal.NextStepDueDate.HasValue && deal.NextStepDueDate < referenceDate.Date)
            daysOverdue = (int)(referenceDate.Date - deal.NextStepDueDate.Value.Date).TotalDays;

        int? daysSinceContact = null;
        if (deal.LastContactedDate.HasValue)
            daysSinceContact = (int)(referenceDate.Date - deal.LastContactedDate.Value.Date).TotalDays;

        return new DealAction
        {
            DealId = deal.DealId,
            DealName = deal.DealName,
            AccountName = deal.AccountName,
            Owner = deal.Owner,
            Stage = deal.Stage,
            Amount = deal.AmountGBP,
            NextStep = deal.NextStep,
            NextStepDueDate = deal.NextStepDueDate,
            LastContactedDate = deal.LastContactedDate,
            DaysOverdue = daysOverdue,
            DaysSinceContact = daysSinceContact
        };
    }
}
