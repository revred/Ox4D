// =============================================================================
// ToolHandler - MCP Tool Implementation
// =============================================================================
// Maps MCP tool calls to PipelineService operations.
// Each tool method is deterministic - no "freeform LLM" logic here.
// =============================================================================

using System.Text.Json;
using Ox4D.Core.Models;
using Ox4D.Core.Models.Config;
using Ox4D.Core.Models.Reports;
using Ox4D.Core.Services;
using Ox4D.Zarwin.Protocol;
using Ox4D.Storage;

namespace Ox4D.Zarwin.Handlers;

/// <summary>
/// Handles MCP tool calls by dispatching to PipelineService and PromoterService.
/// </summary>
public class ToolHandler
{
    private readonly PipelineService _pipelineService;
    private readonly PromoterService _promoterService;
    private readonly IDealRepository _repository;
    private readonly LookupTables _lookups;
    private readonly string _excelPath;

    public ToolHandler(
        PipelineService pipelineService,
        PromoterService promoterService,
        IDealRepository repository,
        LookupTables lookups,
        string excelPath)
    {
        _pipelineService = pipelineService;
        _promoterService = promoterService;
        _repository = repository;
        _lookups = lookups;
        _excelPath = excelPath;
    }

    public async Task<object> HandleAsync(string method, JsonElement? parameters)
    {
        return method switch
        {
            // Pipeline operations
            "pipeline.list_deals" => await HandleListDeals(parameters),
            "pipeline.get_deal" => await HandleGetDeal(parameters),
            "pipeline.upsert_deal" => await HandleUpsertDeal(parameters),
            "pipeline.patch_deal" => await HandlePatchDeal(parameters),
            "pipeline.delete_deal" => await HandleDeleteDeal(parameters),
            "pipeline.hygiene_report" => await HandleHygieneReport(),
            "pipeline.daily_brief" => await HandleDailyBrief(parameters),
            "pipeline.forecast_snapshot" => await HandleForecastSnapshot(parameters),
            "pipeline.generate_synthetic" => await HandleGenerateSynthetic(parameters),
            "pipeline.get_stats" => await HandleGetStats(),
            "pipeline.save" => await HandleSave(),
            "pipeline.reload" => await HandleReload(),

            // Promoter operations
            "promoter.dashboard" => await HandlePromoterDashboard(parameters),
            "promoter.deals" => await HandlePromoterDeals(parameters),
            "promoter.actions" => await HandlePromoterActions(parameters),

            _ => throw new InvalidOperationException($"Unknown method: {method}")
        };
    }

    private async Task<object> HandleListDeals(JsonElement? parameters)
    {
        var filter = new DealFilter();

        if (parameters.HasValue)
        {
            var p = parameters.Value;
            filter.SearchText = GetString(p, "searchText");
            filter.Owner = GetString(p, "owner");
            filter.Region = GetString(p, "region");
            filter.ProductLine = GetString(p, "productLine");
            filter.MinAmount = GetDecimal(p, "minAmount");
            filter.MaxAmount = GetDecimal(p, "maxAmount");

            // Promoter filters
            filter.PromoterId = GetString(p, "promoterId");
            filter.PromoCode = GetString(p, "promoCode");
            filter.HasPromoter = GetBool(p, "hasPromoter");
            filter.CommissionPending = GetBool(p, "commissionPending");

            if (p.TryGetProperty("stages", out var stages) && stages.ValueKind == JsonValueKind.Array)
            {
                filter.Stages = stages.EnumerateArray()
                    .Select(s => DealStageExtensions.ParseStage(s.GetString()))
                    .ToList();
            }
        }

        var deals = await _pipelineService.ListDealsAsync(filter);
        return new { deals = deals.Select(DealToDto) };
    }

    private async Task<object> HandleGetDeal(JsonElement? parameters)
    {
        var dealId = GetRequiredString(parameters, "dealId");
        var deal = await _pipelineService.GetDealAsync(dealId);

        if (deal == null)
            throw new InvalidOperationException($"Deal not found: {dealId}");

        return new { deal = DealToDto(deal) };
    }

    private async Task<object> HandleUpsertDeal(JsonElement? parameters)
    {
        if (!parameters.HasValue)
            throw new InvalidOperationException("Deal data required");

        var p = parameters.Value;
        var deal = new Deal
        {
            DealId = GetString(p, "dealId") ?? DealNormalizer.GenerateDealId(),
            AccountName = GetRequiredString(parameters, "accountName"),
            DealName = GetRequiredString(parameters, "dealName"),
            ContactName = GetString(p, "contactName"),
            Email = GetString(p, "email"),
            Phone = GetString(p, "phone"),
            Postcode = GetString(p, "postcode"),
            Stage = DealStageExtensions.ParseStage(GetString(p, "stage")),
            Probability = GetInt(p, "probability"),
            AmountGBP = GetDecimal(p, "amountGBP"),
            Owner = GetString(p, "owner"),
            NextStep = GetString(p, "nextStep"),
            NextStepDueDate = GetDate(p, "nextStepDueDate"),
            CloseDate = GetDate(p, "closeDate"),
            ProductLine = GetString(p, "productLine"),
            LeadSource = GetString(p, "leadSource"),
            Comments = GetString(p, "comments"),
            // Promoter fields
            PromoterId = GetString(p, "promoterId"),
            PromoCode = GetString(p, "promoCode"),
            PromoterCommission = GetDecimal(p, "promoterCommission")
        };

        if (p.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
        {
            deal.Tags = tags.EnumerateArray()
                .Where(t => t.ValueKind == JsonValueKind.String)
                .Select(t => t.GetString()!)
                .ToList();
        }

        var result = await _pipelineService.UpsertDealAsync(deal);
        return new { success = true, deal = DealToDto(result) };
    }

    private async Task<object> HandlePatchDeal(JsonElement? parameters)
    {
        var dealId = GetRequiredString(parameters, "dealId");

        if (!parameters.HasValue || !parameters.Value.TryGetProperty("updates", out var updates))
            throw new InvalidOperationException("Updates required");

        var patch = new Dictionary<string, object?>();
        foreach (var prop in updates.EnumerateObject())
        {
            patch[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Number => prop.Value.TryGetDecimal(out var d) ? d : prop.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => prop.Value.GetRawText()
            };
        }

        var result = await _pipelineService.PatchDealAsync(dealId, patch);
        if (result == null)
            throw new InvalidOperationException($"Deal not found: {dealId}");

        return new { success = true, deal = DealToDto(result) };
    }

    private async Task<object> HandleDeleteDeal(JsonElement? parameters)
    {
        var dealId = GetRequiredString(parameters, "dealId");
        await _pipelineService.DeleteDealAsync(dealId);
        return new { success = true, dealId };
    }

    private async Task<object> HandleHygieneReport()
    {
        var report = await _pipelineService.GetHygieneReportAsync();
        return new
        {
            generatedAt = report.GeneratedAt,
            totalDeals = report.TotalDeals,
            dealsWithIssues = report.DealsWithIssues,
            healthScore = report.HealthScore,
            issuesByType = report.IssuesByType.ToDictionary(
                kvp => kvp.Key.ToDisplayString(),
                kvp => kvp.Value),
            issues = report.Issues.Select(i => new
            {
                dealId = i.DealId,
                dealName = i.DealName,
                accountName = i.AccountName,
                owner = i.Owner,
                stage = i.Stage.ToDisplayString(),
                amount = i.Amount,
                issueType = i.IssueType.ToDisplayString(),
                description = i.Description,
                severity = i.Severity.ToString()
            })
        };
    }

    private async Task<object> HandleDailyBrief(JsonElement? parameters)
    {
        var refDate = GetDate(parameters, "referenceDate") ?? DateTime.Today;
        var brief = await _pipelineService.GetDailyBriefAsync(refDate);

        return new
        {
            referenceDate = brief.ReferenceDate,
            totalActionItems = brief.TotalActionItems,
            totalAtRiskValue = brief.TotalAtRiskValue,
            dueToday = brief.DueToday.Select(ActionToDto),
            overdue = brief.Overdue.Select(ActionToDto),
            noContactDeals = brief.NoContactDeals.Select(ActionToDto),
            highValueAtRisk = brief.HighValueAtRisk.Select(ActionToDto)
        };
    }

    private async Task<object> HandleForecastSnapshot(JsonElement? parameters)
    {
        var refDate = GetDate(parameters, "referenceDate") ?? DateTime.Today;
        var snapshot = await _pipelineService.GetForecastSnapshotAsync(refDate);

        return new
        {
            referenceDate = snapshot.ReferenceDate,
            totalDeals = snapshot.TotalDeals,
            openDeals = snapshot.OpenDeals,
            totalPipeline = snapshot.TotalPipeline,
            weightedPipeline = snapshot.WeightedPipeline,
            byStage = snapshot.ByStage.Select(s => new
            {
                stage = s.Stage.ToDisplayString(),
                dealCount = s.DealCount,
                totalAmount = s.TotalAmount,
                weightedAmount = s.WeightedAmount,
                percentageOfPipeline = s.PercentageOfPipeline
            }),
            byOwner = snapshot.ByOwner.Select(o => new
            {
                owner = o.Owner,
                dealCount = o.DealCount,
                totalAmount = o.TotalAmount,
                weightedAmount = o.WeightedAmount,
                winRate = o.WinRate,
                closedWon = o.ClosedWon,
                closedLost = o.ClosedLost
            }),
            byCloseMonth = snapshot.ByCloseMonth.Select(m => new
            {
                monthName = m.MonthName,
                dealCount = m.DealCount,
                totalAmount = m.TotalAmount,
                weightedAmount = m.WeightedAmount
            }),
            byRegion = snapshot.ByRegion.Select(r => new
            {
                region = r.Region,
                dealCount = r.DealCount,
                totalAmount = r.TotalAmount,
                weightedAmount = r.WeightedAmount
            }),
            byProduct = snapshot.ByProduct.Select(p => new
            {
                productLine = p.ProductLine,
                dealCount = p.DealCount,
                totalAmount = p.TotalAmount,
                weightedAmount = p.WeightedAmount,
                percentageOfPipeline = p.PercentageOfPipeline
            })
        };
    }

    private async Task<object> HandleGenerateSynthetic(JsonElement? parameters)
    {
        var count = GetInt(parameters, "count");
        if (count <= 0) count = 100;

        var seed = GetInt(parameters, "seed");
        if (seed == 0) seed = Environment.TickCount;

        var generated = await _pipelineService.GenerateSyntheticDataAsync(count, seed);
        return new { success = true, dealsGenerated = generated, seed };
    }

    private async Task<object> HandleGetStats()
    {
        var stats = await _pipelineService.GetStatsAsync();
        return new
        {
            totalDeals = stats.TotalDeals,
            openDeals = stats.OpenDeals,
            closedWonDeals = stats.ClosedWonDeals,
            closedLostDeals = stats.ClosedLostDeals,
            totalPipeline = stats.TotalPipeline,
            weightedPipeline = stats.WeightedPipeline,
            closedWonValue = stats.ClosedWonValue,
            averageDealsValue = stats.AverageDealsValue,
            owners = stats.Owners,
            regions = stats.Regions
        };
    }

    private async Task<object> HandleSave()
    {
        await _repository.SaveChangesAsync();
        return new { success = true, message = "Changes saved to Excel" };
    }

    private async Task<object> HandleReload()
    {
        if (_repository is ExcelDealRepository excelRepo)
        {
            excelRepo.Reload();
        }
        return new { success = true, message = "Data reloaded from Excel" };
    }

    private static object DealToDto(Deal d) => new
    {
        dealId = d.DealId,
        orderNo = d.OrderNo,
        userId = d.UserId,
        accountName = d.AccountName,
        contactName = d.ContactName,
        email = d.Email,
        phone = d.Phone,
        postcode = d.Postcode,
        postcodeArea = d.PostcodeArea,
        installationLocation = d.InstallationLocation,
        region = d.Region,
        mapLink = d.MapLink,
        leadSource = d.LeadSource,
        productLine = d.ProductLine,
        dealName = d.DealName,
        stage = d.Stage.ToDisplayString(),
        probability = d.Probability,
        amountGBP = d.AmountGBP,
        weightedAmountGBP = d.WeightedAmountGBP,
        owner = d.Owner,
        createdDate = d.CreatedDate,
        lastContactedDate = d.LastContactedDate,
        nextStep = d.NextStep,
        nextStepDueDate = d.NextStepDueDate,
        closeDate = d.CloseDate,
        servicePlan = d.ServicePlan,
        lastServiceDate = d.LastServiceDate,
        nextServiceDueDate = d.NextServiceDueDate,
        comments = d.Comments,
        tags = d.Tags,
        // Promoter fields
        promoterId = d.PromoterId,
        promoCode = d.PromoCode,
        promoterCommission = d.PromoterCommission,
        commissionPaid = d.CommissionPaid,
        commissionPaidDate = d.CommissionPaidDate
    };

    private static object ActionToDto(DealAction a) => new
    {
        dealId = a.DealId,
        dealName = a.DealName,
        accountName = a.AccountName,
        owner = a.Owner,
        stage = a.Stage.ToDisplayString(),
        amount = a.Amount,
        nextStep = a.NextStep,
        nextStepDueDate = a.NextStepDueDate,
        lastContactedDate = a.LastContactedDate,
        daysOverdue = a.DaysOverdue,
        daysSinceContact = a.DaysSinceContact,
        riskReason = a.RiskReason
    };

    private static string? GetString(JsonElement? p, string name)
    {
        if (!p.HasValue) return null;
        return p.Value.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() : null;
    }

    private static string GetRequiredString(JsonElement? p, string name)
    {
        var value = GetString(p, name);
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Required parameter missing: {name}");
        return value;
    }

    private static int GetInt(JsonElement? p, string name)
    {
        if (!p.HasValue) return 0;
        if (!p.Value.TryGetProperty(name, out var prop)) return 0;
        return prop.ValueKind == JsonValueKind.Number ? prop.GetInt32() : 0;
    }

    private static decimal? GetDecimal(JsonElement? p, string name)
    {
        if (!p.HasValue) return null;
        if (!p.Value.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.Number ? prop.GetDecimal() : null;
    }

    private static DateTime? GetDate(JsonElement? p, string name)
    {
        var str = GetString(p, name);
        return DealNormalizer.ParseDate(str);
    }

    private static bool? GetBool(JsonElement? p, string name)
    {
        if (!p.HasValue) return null;
        if (!p.Value.TryGetProperty(name, out var prop)) return null;
        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    // =========================================================================
    // Promoter Handlers
    // =========================================================================

    private async Task<object> HandlePromoterDashboard(JsonElement? parameters)
    {
        var promoterId = GetRequiredString(parameters, "promoterId");
        var promoterName = GetString(parameters, "promoterName") ?? "Promoter";
        var promoCode = GetString(parameters, "promoCode") ?? promoterId;
        var tierStr = GetString(parameters, "tier");
        var tier = PromoterTierExtensions.ParseTier(tierStr);
        var refDate = GetDate(parameters, "referenceDate") ?? DateTime.Today;

        var dashboard = await _promoterService.GetPromoterDashboardAsync(
            promoterId, promoterName, promoCode, tier, refDate);

        return new
        {
            generatedAt = dashboard.GeneratedAt,
            promoterId = dashboard.PromoterId,
            promoterName = dashboard.PromoterName,
            promoCode = dashboard.PromoCode,
            tier = dashboard.Tier.ToDisplayString(),
            commissionRate = dashboard.CommissionRate,
            summary = new
            {
                totalReferrals = dashboard.Summary.TotalReferrals,
                activeDeals = dashboard.Summary.ActiveDeals,
                closedWon = dashboard.Summary.ClosedWon,
                closedLost = dashboard.Summary.ClosedLost,
                conversionRate = dashboard.Summary.ConversionRate,
                totalPipelineValue = dashboard.Summary.TotalPipelineValue,
                weightedPipelineValue = dashboard.Summary.WeightedPipelineValue,
                totalWonValue = dashboard.Summary.TotalWonValue,
                averageDealsValue = dashboard.Summary.AverageDealsValue,
                dealsClosingThisMonth = dashboard.Summary.DealsClosingThisMonth,
                valueClosingThisMonth = dashboard.Summary.ValueClosingThisMonth
            },
            byStage = dashboard.ByStage.Select(s => new
            {
                stage = s.Stage.ToDisplayString(),
                dealCount = s.DealCount,
                totalValue = s.TotalValue,
                weightedValue = s.WeightedValue,
                potentialCommission = s.PotentialCommission,
                averageAgeDays = s.AverageAgeDays
            }),
            recommendedActions = dashboard.RecommendedActions.Select(PromoterActionToDto),
            dealsNeedingAttention = dashboard.DealsNeedingAttention.Select(PromoterDealStatusToDto),
            commissionSummary = new
            {
                totalEarned = dashboard.CommissionSummary.TotalEarned,
                totalPaid = dashboard.CommissionSummary.TotalPaid,
                pendingPayment = dashboard.CommissionSummary.PendingPayment,
                projectedFromPipeline = dashboard.CommissionSummary.ProjectedFromPipeline,
                projectedThisMonth = dashboard.CommissionSummary.ProjectedThisMonth,
                projectedThisQuarter = dashboard.CommissionSummary.ProjectedThisQuarter
            },
            recentDeals = dashboard.RecentDeals.Select(PromoterDealStatusToDto)
        };
    }

    private async Task<object> HandlePromoterDeals(JsonElement? parameters)
    {
        var promoterId = GetRequiredString(parameters, "promoterId");
        var promoCode = GetString(parameters, "promoCode") ?? promoterId;
        var tierStr = GetString(parameters, "tier");
        var tier = PromoterTierExtensions.ParseTier(tierStr);
        var commissionRate = tier.GetCommissionRate();
        var refDate = GetDate(parameters, "referenceDate") ?? DateTime.Today;

        var deals = await _promoterService.GetPromoterDealsAsync(
            promoterId, promoCode, commissionRate, refDate);

        return new { deals = deals.Select(PromoterDealStatusToDto) };
    }

    private async Task<object> HandlePromoterActions(JsonElement? parameters)
    {
        var promoterId = GetRequiredString(parameters, "promoterId");
        var promoCode = GetString(parameters, "promoCode") ?? promoterId;
        var tierStr = GetString(parameters, "tier");
        var tier = PromoterTierExtensions.ParseTier(tierStr);
        var commissionRate = tier.GetCommissionRate();
        var refDate = GetDate(parameters, "referenceDate") ?? DateTime.Today;

        var actions = await _promoterService.GetRecommendedActionsAsync(
            promoterId, promoCode, commissionRate, refDate);

        return new { actions = actions.Select(PromoterActionToDto) };
    }

    private static object PromoterActionToDto(PromoterAction a) => new
    {
        dealId = a.DealId,
        dealName = a.DealName,
        accountName = a.AccountName,
        owner = a.Owner,
        stage = a.Stage.ToDisplayString(),
        amount = a.Amount,
        potentialCommission = a.PotentialCommission,
        actionType = a.ActionType.ToDisplayString(),
        priority = a.Priority.ToDisplayString(),
        recommendation = a.Recommendation,
        reason = a.Reason,
        dueDate = a.DueDate,
        daysStuck = a.DaysStuck
    };

    private static object PromoterDealStatusToDto(PromoterDealStatus d) => new
    {
        dealId = d.DealId,
        dealName = d.DealName,
        accountName = d.AccountName,
        stage = d.Stage.ToDisplayString(),
        amount = d.Amount,
        potentialCommission = d.PotentialCommission,
        owner = d.Owner,
        closeDate = d.CloseDate,
        lastContactedDate = d.LastContactedDate,
        nextStep = d.NextStep,
        nextStepDueDate = d.NextStepDueDate,
        daysInPipeline = d.DaysInPipeline,
        daysInCurrentStage = d.DaysInCurrentStage,
        healthStatus = d.HealthStatus.ToDisplayString(),
        statusReason = d.StatusReason
    };
}
