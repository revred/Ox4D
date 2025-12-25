namespace Ox4D.Core.Models;

public class DealFilter
{
    public string? SearchText { get; set; }
    public List<DealStage>? Stages { get; set; }
    public string? Owner { get; set; }
    public string? Region { get; set; }
    public string? ProductLine { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime? CloseDateFrom { get; set; }
    public DateTime? CloseDateTo { get; set; }
    public DateTime? NextStepDueBefore { get; set; }
    public bool? HasOverdueNextStep { get; set; }
    public int? NoContactDays { get; set; }
    public List<string>? Tags { get; set; }

    public bool Matches(Deal deal, DateTime referenceDate)
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            var matchesSearch =
                (deal.DealName?.ToLowerInvariant().Contains(search) ?? false) ||
                (deal.AccountName?.ToLowerInvariant().Contains(search) ?? false) ||
                (deal.ContactName?.ToLowerInvariant().Contains(search) ?? false) ||
                (deal.DealId?.ToLowerInvariant().Contains(search) ?? false) ||
                (deal.Owner?.ToLowerInvariant().Contains(search) ?? false);
            if (!matchesSearch) return false;
        }

        if (Stages?.Any() == true && !Stages.Contains(deal.Stage))
            return false;

        if (!string.IsNullOrWhiteSpace(Owner) &&
            !string.Equals(deal.Owner, Owner, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(Region) &&
            !string.Equals(deal.Region, Region, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(ProductLine) &&
            !string.Equals(deal.ProductLine, ProductLine, StringComparison.OrdinalIgnoreCase))
            return false;

        if (MinAmount.HasValue && (!deal.AmountGBP.HasValue || deal.AmountGBP < MinAmount))
            return false;

        if (MaxAmount.HasValue && (!deal.AmountGBP.HasValue || deal.AmountGBP > MaxAmount))
            return false;

        if (CloseDateFrom.HasValue && (!deal.CloseDate.HasValue || deal.CloseDate < CloseDateFrom))
            return false;

        if (CloseDateTo.HasValue && (!deal.CloseDate.HasValue || deal.CloseDate > CloseDateTo))
            return false;

        if (NextStepDueBefore.HasValue && deal.NextStepDueDate.HasValue && deal.NextStepDueDate > NextStepDueBefore)
            return false;

        if (HasOverdueNextStep == true)
        {
            if (!deal.NextStepDueDate.HasValue || deal.NextStepDueDate >= referenceDate)
                return false;
        }

        if (NoContactDays.HasValue)
        {
            if (!deal.LastContactedDate.HasValue)
                return true; // No contact date means it counts as "no contact"
            var daysSinceContact = (referenceDate - deal.LastContactedDate.Value).Days;
            if (daysSinceContact < NoContactDays)
                return false;
        }

        if (Tags?.Any() == true && !Tags.All(t => deal.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
            return false;

        return true;
    }
}
