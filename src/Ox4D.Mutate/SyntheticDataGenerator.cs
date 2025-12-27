// =============================================================================
// SyntheticDataGenerator - Investor-Grade Demo Data with Bogus
// =============================================================================
// PURPOSE:
//   Generates realistic synthetic sales pipeline data for demos, testing, and
//   development. Uses the Bogus library for authentic UK addresses, phone
//   numbers, company names, and personal names.
//
// FEATURES:
//   - Realistic UK postcodes with proper region mapping
//   - Authentic British company and person names
//   - Proper UK phone number formats (+44 / 0xxx)
//   - Stage distribution matching real-world pipelines
//   - Intentional hygiene issues for testing data quality reports
//   - Deterministic output with configurable seed for reproducibility
//   - Promoter/referral data for commission tracking demos
// =============================================================================

using Bogus;
using Bogus.DataSets;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Services;

namespace Ox4D.Mutate;

/// <summary>
/// Generates realistic synthetic deal data using the Bogus library.
/// Produces investor-grade demo datasets with authentic UK data.
/// </summary>
public class SyntheticDataGenerator : ISyntheticDataGenerator
{
    private readonly LookupTables _lookups;
    private readonly PipelineSettings _settings;
    private readonly DealNormalizer _normalizer;

    // UK Postcode areas with realistic distribution
    private static readonly PostcodeArea[] UkPostcodeAreas =
    {
        // London
        new("EC", "Central London", "London"),
        new("WC", "West Central London", "London"),
        new("E", "East London", "London"),
        new("N", "North London", "London"),
        new("NW", "North West London", "London"),
        new("SE", "South East London", "London"),
        new("SW", "South West London", "London"),
        new("W", "West London", "London"),

        // South East
        new("BN", "Brighton", "South East"),
        new("CT", "Canterbury", "South East"),
        new("GU", "Guildford", "South East"),
        new("ME", "Medway", "South East"),
        new("OX", "Oxford", "South East"),
        new("RG", "Reading", "South East"),
        new("SL", "Slough", "South East"),
        new("TN", "Tonbridge", "South East"),

        // South West
        new("BA", "Bath", "South West"),
        new("BS", "Bristol", "South West"),
        new("EX", "Exeter", "South West"),
        new("GL", "Gloucester", "South West"),
        new("PL", "Plymouth", "South West"),
        new("SN", "Swindon", "South West"),
        new("TR", "Truro", "South West"),

        // Midlands
        new("B", "Birmingham", "Midlands"),
        new("CV", "Coventry", "Midlands"),
        new("DE", "Derby", "Midlands"),
        new("LE", "Leicester", "Midlands"),
        new("NG", "Nottingham", "Midlands"),
        new("ST", "Stoke-on-Trent", "Midlands"),
        new("WV", "Wolverhampton", "Midlands"),

        // North West
        new("CH", "Chester", "North West"),
        new("L", "Liverpool", "North West"),
        new("M", "Manchester", "North West"),
        new("PR", "Preston", "North West"),
        new("WA", "Warrington", "North West"),
        new("WN", "Wigan", "North West"),

        // North East & Yorkshire
        new("DH", "Durham", "North East"),
        new("HU", "Hull", "Yorkshire"),
        new("LS", "Leeds", "Yorkshire"),
        new("NE", "Newcastle", "North East"),
        new("S", "Sheffield", "Yorkshire"),
        new("SR", "Sunderland", "North East"),
        new("YO", "York", "Yorkshire"),

        // Scotland
        new("AB", "Aberdeen", "Scotland"),
        new("DD", "Dundee", "Scotland"),
        new("EH", "Edinburgh", "Scotland"),
        new("G", "Glasgow", "Scotland"),
        new("KY", "Kirkcaldy", "Scotland"),

        // Wales
        new("CF", "Cardiff", "Wales"),
        new("LL", "Llandudno", "Wales"),
        new("NP", "Newport", "Wales"),
        new("SA", "Swansea", "Wales"),

        // Northern Ireland
        new("BT", "Belfast", "Northern Ireland")
    };

    // British company name components for realistic business names
    private static readonly string[] CompanyPrefixes =
    {
        "Albion", "Britannia", "Crown", "Empire", "Heritage", "Imperial", "Majestic",
        "Noble", "Premier", "Regent", "Royal", "Sterling", "Thames", "Tudor", "Wellington",
        "Apex", "Cornerstone", "Elevate", "Frontier", "Horizon", "Meridian", "Nexus",
        "Pinnacle", "Summit", "Vanguard", "Atlantic", "Coastal", "Highland", "Northern",
        "Southern", "Eastern", "Western", "Pacific", "Global", "United", "Allied", "Metro"
    };

    private static readonly string[] CompanySuffixes =
    {
        "Holdings", "Group", "Partners", "Associates", "Consulting", "Solutions",
        "Industries", "Enterprises", "Corporation", "International", "Worldwide",
        "Services", "Systems", "Technologies", "Capital", "Investments", "Properties",
        "Ventures", "Management", "Development", "Resources", "Limited", "PLC"
    };

    // Product line value ranges
    private static readonly Dictionary<string, (int Min, int Max)> ProductAmountRanges = new()
    {
        ["Enterprise Software"] = (50_000, 500_000),
        ["Professional Services"] = (20_000, 150_000),
        ["SaaS Subscription"] = (12_000, 120_000),
        ["Hardware"] = (15_000, 200_000),
        ["Support & Maintenance"] = (8_000, 50_000),
        ["Training"] = (5_000, 40_000),
        ["Consulting"] = (30_000, 250_000)
    };

    // Promoter codes for demo referral tracking
    private static readonly string[] PromoCodes =
    {
        "PARTNER2024", "AFFILIATE01", "REF-GOLD", "PRO-VIP", "CHANNEL-UK",
        "RESELLER-A", "PARTNER-TOP", "INTRO-PREM", "NETWORK-B2B", "ALLIANCE2024"
    };

    public SyntheticDataGenerator(LookupTables lookups, PipelineSettings settings)
    {
        _lookups = lookups;
        _settings = settings;
        _normalizer = new DealNormalizer(lookups);
    }

    /// <summary>
    /// Generates a list of synthetic deals with realistic UK data.
    /// </summary>
    /// <param name="count">Number of deals to generate</param>
    /// <param name="seed">Random seed for reproducibility</param>
    /// <returns>List of normalized deals</returns>
    public List<Deal> Generate(int count, int seed)
    {
        Randomizer.Seed = new Random(seed);

        var personFaker = new Faker<PersonData>("en_GB")
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName())
            .RuleFor(p => p.Phone, f => GenerateUkPhone(f));

        var companyFaker = new Faker("en_GB");
        var deals = new List<Deal>();
        var baseDate = DateTime.Today;

        for (int i = 0; i < count; i++)
        {
            var deal = GenerateDeal(personFaker, companyFaker, baseDate, i, seed);
            deals.Add(_normalizer.Normalize(deal));
        }

        return deals;
    }

    private Deal GenerateDeal(Faker<PersonData> personFaker, Faker companyFaker, DateTime baseDate, int index, int seed)
    {
        var faker = new Faker("en_GB");
        faker.Random = new Randomizer(seed + index);

        var stage = PickStage(faker);
        var productLine = faker.PickRandom(_settings.ProductLines);
        var leadSource = faker.PickRandom(_settings.LeadSources);
        var postcodeArea = faker.PickRandom(UkPostcodeAreas);
        var createdDate = baseDate.AddDays(-faker.Random.Int(1, 180));

        var person = personFaker.UseSeed(seed + index + 10000).Generate();
        var companyName = GenerateCompanyName(faker);
        var owner = faker.PickRandom(_settings.Owners.Count > 0 ? _settings.Owners : GetDefaultOwners());

        var deal = new Deal
        {
            DealId = $"D-{baseDate:yyyyMMdd}-{index:D4}",
            AccountName = companyName,
            ContactName = $"{person.FirstName} {person.LastName}",
            Email = GenerateEmail(person.FirstName, person.LastName, companyName),
            Phone = person.Phone,
            Postcode = GeneratePostcode(faker, postcodeArea),
            PostcodeArea = postcodeArea.Code,
            Region = postcodeArea.Region,
            InstallationLocation = $"{faker.Address.StreetAddress()}, {postcodeArea.City}",
            LeadSource = leadSource,
            ProductLine = productLine,
            DealName = GenerateDealName(faker, productLine),
            Stage = stage,
            Probability = 0, // Will be normalized
            Owner = owner,
            CreatedDate = createdDate,
            ServicePlan = stage == DealStage.ClosedWon ? faker.PickRandom(_settings.ServicePlans) : null
        };

        // Set amount based on product line (with hygiene issues)
        if (!ShouldHaveHygieneIssue(faker, 0.08)) // 8% missing amount
        {
            var range = ProductAmountRanges.GetValueOrDefault(productLine, (10_000, 100_000));
            deal.AmountGBP = faker.Random.Decimal(range.Item1, range.Item2);
            // Round to nearest 1000 for later stages
            if (stage >= DealStage.Proposal)
            {
                deal.AmountGBP = Math.Round(deal.AmountGBP.Value / 1000) * 1000;
            }
        }

        // Set contact dates (with hygiene issues)
        deal.LastContactedDate = ShouldHaveHygieneIssue(faker, 0.12) ? null : // 12% missing last contact
            createdDate.AddDays(faker.Random.Int(0, Math.Max(1, (int)(baseDate - createdDate).TotalDays)));

        // Set future dates for open deals
        if (!stage.IsClosed())
        {
            deal.CloseDate = baseDate.AddDays(faker.Random.Int(-30, 120));

            if (!ShouldHaveHygieneIssue(faker, 0.10)) // 10% missing next step
            {
                deal.NextStep = GenerateNextStep(faker, stage);
                deal.NextStepDueDate = baseDate.AddDays(faker.Random.Int(-14, 21));
            }
        }
        else if (stage == DealStage.ClosedWon)
        {
            deal.CloseDate = createdDate.AddDays(faker.Random.Int(14, 90));
            deal.LastServiceDate = deal.CloseDate;
            deal.NextServiceDueDate = deal.LastServiceDate?.AddYears(1);
        }
        else
        {
            deal.CloseDate = createdDate.AddDays(faker.Random.Int(7, 60));
        }

        // Add tags based on deal characteristics
        deal.Tags = GenerateTags(faker, stage, deal.AmountGBP);

        // Add promoter data for some deals (25% chance)
        if (faker.Random.Bool(0.25f))
        {
            deal.PromoCode = faker.PickRandom(PromoCodes);
            deal.PromoterId = $"PRM-{faker.Random.AlphaNumeric(6).ToUpper()}";

            if (stage == DealStage.ClosedWon && deal.AmountGBP.HasValue)
            {
                // Bronze tier (10%) commission for demo
                deal.PromoterCommission = deal.AmountGBP.Value * 0.10m;
                deal.CommissionPaid = faker.Random.Bool(0.7f); // 70% paid
                if (deal.CommissionPaid)
                {
                    deal.CommissionPaidDate = deal.CloseDate?.AddDays(faker.Random.Int(7, 30));
                }
            }
        }

        // Add comments for some deals
        if (faker.Random.Bool(0.3f))
        {
            deal.Comments = GenerateComment(faker, stage);
        }

        return deal;
    }

    private static string GenerateUkPhone(Faker faker)
    {
        // Generate realistic UK phone numbers
        var formats = new[]
        {
            "020 #### ####",   // London landline
            "0121 ### ####",   // Birmingham
            "0161 ### ####",   // Manchester
            "0131 ### ####",   // Edinburgh
            "0141 ### ####",   // Glasgow
            "07### ######",    // Mobile
            "+44 7### ######", // Mobile with country code
            "+44 20 #### ####" // London with country code
        };
        return faker.Phone.PhoneNumber(faker.PickRandom(formats));
    }

    private string GenerateCompanyName(Faker faker)
    {
        var style = faker.Random.Int(1, 4);
        return style switch
        {
            1 => $"{faker.PickRandom(CompanyPrefixes)} {faker.PickRandom(CompanySuffixes)}",
            2 => $"{faker.Name.LastName()} & {faker.Name.LastName()} {faker.PickRandom(CompanySuffixes)}",
            3 => $"{faker.Name.LastName()} {faker.PickRandom(CompanySuffixes)}",
            _ => $"{faker.PickRandom(CompanyPrefixes)} {faker.Name.LastName()} {faker.PickRandom(new[] { "Ltd", "PLC", "Group" })}"
        };
    }

    private static string GenerateEmail(string firstName, string lastName, string company)
    {
        var cleanCompany = company.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("&", "")
            .Replace(",", "")
            .Replace("'", "");

        // Remove common suffixes for cleaner domain
        foreach (var suffix in new[] { "ltd", "plc", "group", "holdings", "limited", "international", "worldwide" })
        {
            cleanCompany = cleanCompany.Replace(suffix, "");
        }

        cleanCompany = cleanCompany.TrimEnd('-', '_');
        if (cleanCompany.Length > 20) cleanCompany = cleanCompany[..20];

        return $"{firstName.ToLower()}.{lastName.ToLower()}@{cleanCompany}.co.uk";
    }

    private static string GeneratePostcode(Faker faker, PostcodeArea area)
    {
        // UK postcode format: AA9A 9AA or A9A 9AA or A9 9AA or A99 9AA etc.
        var district = faker.Random.Int(1, 99);
        var sector = faker.Random.Int(0, 9);
        var unit = $"{faker.Random.Char('A', 'Z')}{faker.Random.Char('A', 'Z')}";

        // Skip I and Q in the unit
        unit = unit.Replace('I', 'J').Replace('Q', 'R');

        return $"{area.Code}{district} {sector}{unit}";
    }

    private DealStage PickStage(Faker faker)
    {
        var roll = faker.Random.Double();
        // Realistic pipeline distribution: ~45% early, ~35% mid, ~20% closed
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

    private static bool ShouldHaveHygieneIssue(Faker faker, double probability) =>
        faker.Random.Double() < probability;

    private static string GenerateDealName(Faker faker, string productLine)
    {
        var types = new[]
        {
            "Implementation", "Deployment", "Upgrade", "Migration",
            "Integration", "Expansion", "Renewal", "Rollout",
            "Phase 2", "Enterprise", "Strategic", "Transformation"
        };
        return $"{productLine} {faker.PickRandom(types)}";
    }

    private static string GenerateNextStep(Faker faker, DealStage stage)
    {
        var steps = stage switch
        {
            DealStage.Lead => new[]
            {
                "Initial discovery call", "Send introduction email", "Research company background",
                "Schedule exploratory meeting", "Qualify budget and timeline"
            },
            DealStage.Qualified => new[]
            {
                "Technical discovery session", "Stakeholder mapping workshop",
                "Demo scheduling call", "Requirements gathering meeting", "ROI discussion"
            },
            DealStage.Discovery => new[]
            {
                "Product demonstration", "Technical deep-dive", "Solution architecture review",
                "Reference customer call", "Proof of concept planning"
            },
            DealStage.Proposal => new[]
            {
                "Proposal walkthrough meeting", "Executive presentation", "Commercial negotiation",
                "Legal review meeting", "Final pricing discussion"
            },
            DealStage.Negotiation => new[]
            {
                "Contract review call", "Final terms negotiation", "Procurement meeting",
                "Executive sign-off", "Implementation planning"
            },
            _ => new[] { "Follow up call", "Status check-in", "Relationship nurturing" }
        };
        return faker.PickRandom(steps);
    }

    private List<string> GenerateTags(Faker faker, DealStage stage, decimal? amount)
    {
        var tags = new List<string>();

        if (amount >= 100_000) tags.Add("high-value");
        else if (amount >= 50_000) tags.Add("mid-value");
        else if (amount.HasValue) tags.Add("standard");

        if (faker.Random.Bool(0.15f)) tags.Add("strategic");
        if (faker.Random.Bool(0.12f)) tags.Add("expansion");
        if (faker.Random.Bool(0.08f)) tags.Add("competitive");
        if (faker.Random.Bool(0.10f)) tags.Add("multi-year");
        if (stage == DealStage.Negotiation && faker.Random.Bool(0.40f)) tags.Add("closing-soon");
        if (faker.Random.Bool(0.05f)) tags.Add("urgent");

        return tags;
    }

    private static string GenerateComment(Faker faker, DealStage stage)
    {
        var comments = stage switch
        {
            DealStage.Lead => new[]
            {
                "Initial contact made via LinkedIn.",
                "Inbound enquiry from website form.",
                "Referred by existing customer.",
                "Met at industry conference."
            },
            DealStage.Qualified => new[]
            {
                "Budget confirmed for this financial year.",
                "Decision maker engaged and supportive.",
                "Competing with incumbent vendor.",
                "Strong business case identified."
            },
            DealStage.Discovery => new[]
            {
                "Technical team very impressed with demo.",
                "Some concerns about integration timeline.",
                "Requested additional references.",
                "Positive feedback from end users."
            },
            DealStage.Proposal => new[]
            {
                "Proposal well received by stakeholders.",
                "Negotiating on payment terms.",
                "Procurement involved, expect delays.",
                "Awaiting board approval."
            },
            DealStage.Negotiation => new[]
            {
                "Final legal review in progress.",
                "Verbal commitment received.",
                "Minor contract amendments requested.",
                "Target close this quarter."
            },
            DealStage.ClosedWon => new[]
            {
                "Successful implementation completed.",
                "Customer very satisfied with outcome.",
                "Expansion opportunity identified.",
                "Reference customer potential."
            },
            DealStage.ClosedLost => new[]
            {
                "Lost to competitor on price.",
                "Budget reallocated to other project.",
                "Decision postponed indefinitely.",
                "Key champion left the company."
            },
            _ => new[]
            {
                "On hold pending budget approval.",
                "Waiting for internal restructure.",
                "Will revisit next quarter."
            }
        };

        return faker.PickRandom(comments);
    }

    private static List<string> GetDefaultOwners() => new()
    {
        "James Wilson",
        "Sarah Chen",
        "Michael Brown",
        "Emma Taylor",
        "David Lee"
    };

    private record PostcodeArea(string Code, string City, string Region);
    private class PersonData
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}
