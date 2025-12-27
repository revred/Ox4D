using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;

namespace Ox4D.Core.Services;

/// <summary>
/// Simple synthetic data generator using basic Random.
/// For more realistic UK data with proper addresses, phone numbers, and company names,
/// use the Bogus-powered generator from Ox4D.Mutate.
/// </summary>
public class SyntheticDataGenerator : ISyntheticDataGenerator
{
    private readonly LookupTables _lookups;
    private readonly PipelineSettings _settings;
    private readonly DealNormalizer _normalizer;

    public SyntheticDataGenerator(LookupTables lookups, PipelineSettings settings)
    {
        _lookups = lookups;
        _settings = settings;
        _normalizer = new DealNormalizer(lookups);
    }

    public List<Deal> Generate(int count, int seed)
    {
        var random = new Random(seed);
        var deals = new List<Deal>();
        var baseDate = DateTime.Today;

        for (int i = 0; i < count; i++)
        {
            var deal = GenerateDeal(random, baseDate, i);
            deals.Add(_normalizer.Normalize(deal));
        }

        return deals;
    }

    private Deal GenerateDeal(Random random, DateTime baseDate, int index)
    {
        var stage = PickStage(random);
        var owner = PickOwner(random);
        var productLine = PickRandom(random, _settings.ProductLines);
        var leadSource = PickRandom(random, _settings.LeadSources);
        var postcode = GeneratePostcode(random);
        var createdDate = baseDate.AddDays(-random.Next(1, 180));

        var deal = new Deal
        {
            DealId = $"D-{baseDate:yyyyMMdd}-{index:D4}",
            AccountName = GenerateCompanyName(random),
            ContactName = GeneratePersonName(random),
            Email = null, // Will be set below
            Phone = GeneratePhone(random),
            Postcode = postcode,
            LeadSource = leadSource,
            ProductLine = productLine,
            DealName = GenerateDealName(random, productLine),
            Stage = stage,
            Probability = 0, // Will be normalized
            Owner = owner,
            CreatedDate = createdDate,
            ServicePlan = stage == DealStage.ClosedWon ? PickRandom(random, _settings.ServicePlans) : null
        };

        // Set email based on contact name
        if (!string.IsNullOrEmpty(deal.ContactName))
        {
            var emailName = deal.ContactName.ToLowerInvariant().Replace(" ", ".").Replace("'", "");
            var domain = GenerateEmailDomain(deal.AccountName);
            deal.Email = $"{emailName}@{domain}";
        }

        // Set amount based on product line and stage
        if (!ShouldHaveHygieneIssue(random, 0.08)) // 8% missing amount
        {
            deal.AmountGBP = GenerateAmount(random, productLine, stage);
        }

        // Set dates
        deal.LastContactedDate = ShouldHaveHygieneIssue(random, 0.12) ? null : // 12% missing last contact
            createdDate.AddDays(random.Next(0, (int)(baseDate - createdDate).TotalDays));

        if (!stage.IsClosed())
        {
            deal.CloseDate = baseDate.AddDays(random.Next(-30, 120));

            if (!ShouldHaveHygieneIssue(random, 0.10)) // 10% missing next step due date
            {
                deal.NextStep = GenerateNextStep(random, stage);
                deal.NextStepDueDate = baseDate.AddDays(random.Next(-14, 21));
            }
        }
        else if (stage == DealStage.ClosedWon)
        {
            deal.CloseDate = createdDate.AddDays(random.Next(14, 90));
            deal.LastServiceDate = deal.CloseDate;
            deal.NextServiceDueDate = deal.LastServiceDate?.AddYears(1);
        }
        else
        {
            deal.CloseDate = createdDate.AddDays(random.Next(7, 60));
        }

        // Add some tags
        deal.Tags = GenerateTags(random, stage, deal.AmountGBP);

        return deal;
    }

    private DealStage PickStage(Random random)
    {
        var roll = random.NextDouble();
        // ~45% early, ~35% mid, ~20% closed
        return roll switch
        {
            < 0.20 => DealStage.Lead,
            < 0.35 => DealStage.Qualified,
            < 0.45 => DealStage.Discovery,
            < 0.60 => DealStage.Proposal,
            < 0.75 => DealStage.Negotiation,
            < 0.85 => DealStage.ClosedWon,
            < 0.95 => DealStage.ClosedLost,
            _ => DealStage.OnHold
        };
    }

    private static readonly string[] Owners = { "James Wilson", "Sarah Chen", "Michael Brown", "Emma Taylor", "David Lee" };
    private string PickOwner(Random random) => Owners[random.Next(Owners.Length)];

    private static T PickRandom<T>(Random random, IList<T> items) => items[random.Next(items.Count)];

    private static bool ShouldHaveHygieneIssue(Random random, double probability) => random.NextDouble() < probability;

    private static readonly string[] CompanyPrefixes = { "Alpha", "Beta", "Global", "Premier", "Elite", "Apex", "Summit", "Prime", "Nova", "Vertex" };
    private static readonly string[] CompanySuffixes = { "Solutions", "Systems", "Technologies", "Group", "Industries", "Services", "Corp", "Ltd", "Holdings", "Partners" };

    private string GenerateCompanyName(Random random)
    {
        var prefix = CompanyPrefixes[random.Next(CompanyPrefixes.Length)];
        var suffix = CompanySuffixes[random.Next(CompanySuffixes.Length)];
        return $"{prefix} {suffix}";
    }

    private static readonly string[] FirstNames = { "John", "Jane", "Robert", "Emily", "William", "Sarah", "James", "Emma", "Michael", "Olivia", "David", "Sophie", "Thomas", "Charlotte", "Richard", "Amelia" };
    private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Wilson", "Anderson", "Taylor", "Thomas", "Moore", "Martin", "Jackson", "Thompson" };

    private string GeneratePersonName(Random random)
    {
        var first = FirstNames[random.Next(FirstNames.Length)];
        var last = LastNames[random.Next(LastNames.Length)];
        return $"{first} {last}";
    }

    private string GenerateEmailDomain(string companyName)
    {
        var clean = companyName.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("ltd", "")
            .Replace("corp", "")
            .Replace("holdings", "")
            .Replace("partners", "");
        return $"{clean}.co.uk";
    }

    private string GeneratePhone(Random random)
    {
        var areaCode = random.Next(100, 999);
        var part1 = random.Next(100, 999);
        var part2 = random.Next(1000, 9999);
        return $"0{areaCode} {part1} {part2}";
    }

    private static readonly string[] PostcodeAreas = { "SW1", "EC2", "W1", "NW1", "SE1", "M1", "B1", "LS1", "G1", "EH1", "CF1", "BS1", "BN1", "CB1", "OX1" };

    private string GeneratePostcode(Random random)
    {
        var area = PostcodeAreas[random.Next(PostcodeAreas.Length)];
        var num = random.Next(1, 9);
        var letters = $"{(char)('A' + random.Next(26))}{(char)('A' + random.Next(26))}";
        return $"{area} {num}{letters}";
    }

    private string GenerateDealName(Random random, string productLine)
    {
        var types = new[] { "Implementation", "Deployment", "Upgrade", "Migration", "Integration", "Expansion", "Renewal" };
        var type = types[random.Next(types.Length)];
        return $"{productLine} {type}";
    }

    private decimal GenerateAmount(Random random, string productLine, DealStage stage)
    {
        var baseAmount = productLine switch
        {
            "Enterprise Software" => random.Next(50000, 250000),
            "Professional Services" => random.Next(20000, 100000),
            "SaaS Subscription" => random.Next(10000, 60000),
            "Hardware" => random.Next(15000, 80000),
            "Support & Maintenance" => random.Next(5000, 30000),
            "Training" => random.Next(5000, 25000),
            "Consulting" => random.Next(25000, 150000),
            _ => random.Next(10000, 50000)
        };

        // Later stages tend to have more refined amounts
        if (stage >= DealStage.Proposal)
        {
            baseAmount = (int)Math.Round(baseAmount / 1000.0) * 1000;
        }

        return baseAmount;
    }

    private string GenerateNextStep(Random random, DealStage stage)
    {
        var steps = stage switch
        {
            DealStage.Lead => new[] { "Initial call", "Send introduction email", "Research company", "Schedule discovery call" },
            DealStage.Qualified => new[] { "Discovery meeting", "Needs assessment", "Demo scheduling", "Stakeholder mapping" },
            DealStage.Discovery => new[] { "Technical demo", "Solution workshop", "ROI analysis", "Reference call" },
            DealStage.Proposal => new[] { "Proposal review call", "Address objections", "Executive presentation", "Contract draft" },
            DealStage.Negotiation => new[] { "Final terms review", "Legal review", "Procurement meeting", "Sign-off meeting" },
            _ => new[] { "Follow up", "Check in", "Status update" }
        };
        return steps[random.Next(steps.Length)];
    }

    private List<string> GenerateTags(Random random, DealStage stage, decimal? amount)
    {
        var tags = new List<string>();

        if (amount >= 100000) tags.Add("high-value");
        if (amount >= 50000 && amount < 100000) tags.Add("mid-value");

        if (random.NextDouble() < 0.2) tags.Add("strategic");
        if (random.NextDouble() < 0.15) tags.Add("expansion");
        if (random.NextDouble() < 0.1) tags.Add("competitive");
        if (stage == DealStage.Negotiation && random.NextDouble() < 0.3) tags.Add("closing-soon");

        return tags;
    }
}
