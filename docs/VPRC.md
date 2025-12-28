# VPRC.md — Verify Product Requirements Through Console

> **Purpose:** Unified step-by-step verification guide to validate all 64 stories using Console, Demo, and MCP.
> **Cross-Reference:** [UseCases.md](UseCases.md) for persona-specific use cases | [PRCD.md](PRCD.md) for requirements | [TestPlan.md](TestPlan.md) for test specifications
> **Last Updated:** 2025-12-28

---

## How to Use This Document

This document provides **executable verification recipes** for each of the 64 stories. Each recipe:

1. **Maps to a specific story** from PRCD.md
2. **Identifies the persona** who benefits (from UseCases.md)
3. **Specifies verification method** (Console, Demo, MCP, Code, or Test)
4. **Provides exact steps** to execute
5. **Describes expected results** to validate
6. **Includes a verification checkbox**

### Verification Methods

| Method | Tool | Description |
|--------|------|-------------|
| **Console** | `dotnet run --project src/Ox4D.Console` | Interactive menu verification |
| **Demo** | Console Menu Option 13 | Non-interactive full demo (Run Full Demo) |
| **MCP** | `echo '...' \| dotnet run --project src/Ox4D.Zarwin` | JSON-RPC command verification |
| **Code** | File inspection | Check source code implementation |
| **Test** | `dotnet test` | Run unit tests |
| **Inspect** | File/Excel | Check generated files or documentation |

> **Note:** The Demo project has been merged into Ox4D.Console. Use menu option **13. Run Full Demo** for non-interactive verification.

---

## Prerequisites

```bash
# 1. Build the solution
cd c:/Code/Ox4D
dotnet build
# Expected: 0 errors

# 2. Run all tests
dotnet test
# Expected: 393/393 tests pass

# 3. Verify .NET version
dotnet --version
# Expected: 10.0.x
```

---

## PART 1: Dataset Engineer Verification (Setup Phase)

**Persona:** Dataset Engineer (DE)
**Use Cases:** DE-UC1 through DE-UC6
**Stories Covered:** S5.2.1, S5.2.2, S5.2.3, S5.2.4, S2.2.1-S2.2.4, S2.3.1

Start here to create the test dataset for all other verifications.

---

### Recipe DE-1: Generate Synthetic Dataset via Full Demo Mode

**Stories:** S5.2.1, S5.2.2, S5.2.3, S5.2.4

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Run: `dotnet run --project src/Ox4D.Console` | Console menu appears | - |
| 2 | Select: **13. Run Full Demo (Non-Interactive)** | Demo starts, displays banner | - |
| 3 | Observe: "STEP 1: GENERATING 100 SYNTHETIC DEALS (seed=42)" | Shows "Generated 100 deals" | S5.2.1 |
| 4 | Observe: UK-realistic names, postcodes in output | Names like "James Wilson", postcodes like "SW1A" | S5.2.2 |
| 5 | Observe: "STEP 4: HYGIENE REPORT" section | Shows issues (~8% missing amounts) | S5.2.3 |
| 6 | Observe: Sample deal has PromoCode | PromoCode field populated | S5.2.4 |
| 7 | Observe: "STEP 7: SAVING TO EXCEL" | Shows "Saved 100 deals" | S2.2.1 |
| 8 | Verify file: `dir data\SalesPipelineV1.0.xlsx` | File exists with recent timestamp | S2.3.1 |

**Verification Checklist:**
- [ ] S5.2.1 — Simple generator created 100 deals with seed
- [ ] S5.2.2 — Bogus generator produced UK-realistic data
- [ ] S5.2.3 — Hygiene issues injected (visible in report)
- [ ] S5.2.4 — Promoter referrals generated (PromoCode present)

---

### Recipe DE-2: Verify Deterministic Seeding

**Stories:** S5.2.1 (deterministic seeding), S5.3.3

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Run Console, select **13. Run Full Demo** | Note first deal name and Total Pipeline value |
| 2 | Run Demo again (select option 13) | Same first deal name and Total Pipeline value |
| 3 | Compare owner names | Identical between runs |

**Verification Checklist:**
- [ ] Same seed (42) produces identical dataset each time
- [ ] S5.3.3 — Deterministic seeding validated

---

### Recipe DE-3: Verify Excel Sheet-Per-Table Design

**Stories:** S2.2.1, S2.2.2, S2.2.3, S2.2.4, S2.3.1

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Open: `data/SalesPipelineV1.0.xlsx` in Excel | File opens successfully | - |
| 2 | Check: "Deals" worksheet exists | 35+ columns visible | S2.2.1 |
| 3 | Check: Deals has promoter columns | PromoCode, PromoterCommission, CommissionPaid | S2.2.4 |
| 4 | Check: "Lookups" worksheet exists | PostcodeRegions, StageProbabilities data | S2.2.2 |
| 5 | Check: "Metadata" worksheet exists | Version, LastModified fields | S2.2.3 |
| 6 | Count: Exactly 3 worksheets | Deals, Lookups, Metadata only | S2.3.1 |

**Verification Checklist:**
- [ ] S2.2.1 — Deals sheet with 35+ fields
- [ ] S2.2.2 — Lookups sheet with mappings
- [ ] S2.2.3 — Metadata sheet with version info
- [ ] S2.2.4 — Promoter fields in Deals sheet
- [ ] S2.3.1 — Sheet-per-table design (3 sheets = 3 future tables)

---

### Recipe DE-4: Generate Custom Dataset via Console

**Stories:** S5.2.1, S5.2.2

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Run: `dotnet run --project src/Ox4D.Console` | Console menu appears |
| 2 | Select: "3. Generate Synthetic Data" | Prompted for count |
| 3 | Enter: `50` for count | Prompted for seed |
| 4 | Enter: `12345` for seed | Prompted to clear existing |
| 5 | Enter: `y` to clear | Shows "Generated 50 deals with seed 12345" |
| 6 | Select: "7. Pipeline Statistics" | Shows Total Deals: 50 |

**Verification Checklist:**
- [ ] Custom count works (50 instead of 100)
- [ ] Custom seed produces reproducible data

---

## PART 2: Sales Manager Verification (Daily Workflow)

**Persona:** Sales Manager (SM)
**Use Cases:** SM-UC1 through SM-UC6
**Stories Covered:** S3.1.x, S3.2.x, S3.3.x, S3.4.x, S3.5.x

---

### Recipe SM-1: Daily Pipeline Review (Daily Brief)

**Stories:** S3.1.1, S3.1.2, S3.1.3, S3.1.4

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Run Console | Menu appears | - |
| 2 | Select: "1. Load Excel Workbook" | Press Enter for default | - |
| 3 | Wait for: "Loaded X deals" | Confirms data loaded | - |
| 4 | Select: "4. Daily Brief" | Daily Brief panel displays | - |
| 5 | Check: "Overdue" section | Deals with NextStepDueDate < today, shows days overdue | S3.1.1 |
| 6 | Check: "Due Today" section | Deals with NextStepDueDate = today | S3.1.2 |
| 7 | Check: "No Contact" section | Deals with no contact in 10+ days | S3.1.3 |
| 8 | Check: "High Value at Risk" section | Large deals that are overdue or no contact | S3.1.4 |

**Verification Checklist:**
- [ ] S3.1.1 — Overdue actions identified with days count
- [ ] S3.1.2 — Actions due today listed
- [ ] S3.1.3 — No-contact deals identified (10+ day threshold)
- [ ] S3.1.4 — High-value at-risk deals highlighted

---

### Recipe SM-2: Data Quality Audit (Hygiene Report)

**Stories:** S3.2.1, S3.2.2, S3.2.3, S3.2.4

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Select: "5. Hygiene Report" | Hygiene panel displays | - |
| 2 | Check: Health Score | Percentage with color coding | - |
| 3 | Check: "Issues by Type" chart | Visual breakdown | - |
| 4 | Find: MissingAmount issues | Deals with null/zero AmountGBP | S3.2.1 |
| 5 | Find: MissingCloseDate issues | Late-stage deals without CloseDate | S3.2.2 |
| 6 | Find: ProbabilityStageMismatch | Manually overridden probabilities | S3.2.3 |
| 7 | Find: MissingPostcode/Region | Deals without location data | S3.2.4 |

**Verification Checklist:**
- [ ] S3.2.1 — Missing amounts flagged
- [ ] S3.2.2 — Missing close dates flagged (late-stage only)
- [ ] S3.2.3 — Stage-probability mismatches flagged
- [ ] S3.2.4 — Missing location data flagged

---

### Recipe SM-3: Revenue Forecasting (Forecast Snapshot)

**Stories:** S3.3.1, S3.3.2, S3.3.3

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Select: "6. Forecast Snapshot" | Forecast panel displays | - |
| 2 | Check: Total Pipeline value | Sum of all open deal amounts | S3.3.1 |
| 3 | Check: Weighted Pipeline value | Sum of (amount × probability) | S3.3.1 |
| 4 | Check: "By Stage" table | Stage breakdown with percentages | S3.3.2 |
| 5 | Check: "By Owner" table | Owner breakdown with win rate | S3.3.2 |
| 6 | Check: "By Close Month" chart | Expected revenue by month | S3.3.2 |
| 7 | Check: "By Region" table | Regional distribution | S3.3.2 |
| 8 | Identify: Concentration in one area | >50% in one stage/owner visible | S3.3.3 |

**Verification Checklist:**
- [ ] S3.3.1 — Total and weighted pipeline calculated
- [ ] S3.3.2 — Breakdown by stage, owner, region, close month
- [ ] S3.3.3 — Concentration risks visible

---

### Recipe SM-4: Pipeline Statistics

**Stories:** S3.5.1, S3.5.2

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Select: "7. Pipeline Statistics" | Stats table displays | - |
| 2 | Check: Total Deals, Open, Won, Lost | Counts visible | S3.5.1 |
| 3 | Check: Total Pipeline, Weighted, Won Value | Values visible | S3.5.1 |
| 4 | Check: Sales Reps list | Unique owner names | S3.5.2 |
| 5 | Check: Regions list | Unique regions | S3.5.2 |

**Verification Checklist:**
- [ ] S3.5.1 — High-level metrics present
- [ ] S3.5.2 — Unique owners and regions extracted

---

### Recipe SM-5: Search and View Deal Details

**Stories:** S3.4.4, S3.4.5

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Select: "8. Search / Deal Drilldown" | Prompted for search | - |
| 2 | Enter: partial name (e.g., "Tech") | Matching deals shown | S3.4.4 |
| 3 | Enter: `y` to view details | Prompted for Deal ID | - |
| 4 | Enter: Deal ID from results | Full details table | S3.4.5 |
| 5 | Verify: 20+ fields displayed | All deal fields visible | S3.4.5 |

**Verification Checklist:**
- [ ] S3.4.4 — Search across multiple fields works
- [ ] S3.4.5 — All deal fields displayed

---

### Recipe SM-6: Create New Deal

**Stories:** S3.4.1

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select: "10. Add New Deal" | Form prompts begin |
| 2 | Enter: Account Name = "Test Company" | Next prompt |
| 3 | Enter: Deal Name = "Widget Sale" | Next prompt |
| 4 | Complete all fields | Form completes |
| 5 | Note: "Deal created with ID: D-XXXXXXXX" | Auto-generated ID |
| 6 | Search for "Test Company" | New deal appears |

**Verification Checklist:**
- [ ] S3.4.1 — Deal created with form input
- [ ] Auto-generated Deal ID (D-XXXXXXXX format)
- [ ] Region derived from postcode
- [ ] Probability defaulted from stage

---

### Recipe SM-7: Update Existing Deal

**Stories:** S3.4.2

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select: "9. Update Deal" | Prompted for Deal ID |
| 2 | Enter: Deal ID | Deal details displayed |
| 3 | Select: "Stage" to update | Stage selection |
| 4 | Select: "Negotiation" | "Deal updated" message |
| 5 | Verify: Probability changed | ~70% (Negotiation default) |

**Verification Checklist:**
- [ ] S3.4.2 — Update works for all fields
- [ ] Stage change auto-updates probability

---

### Recipe SM-8: Delete Deal

**Stories:** S3.4.3

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Select: "11. Delete Deal" | Prompted for Deal ID |
| 2 | Enter: Deal ID | Deal details displayed |
| 3 | Confirm: `y` to delete | "Deal deleted" message |
| 4 | Search for deleted deal | "No deals found" |

**Verification Checklist:**
- [ ] S3.4.3 — Deal deleted with confirmation
- [ ] Deal no longer appears in searches

---

### Recipe SM-9: Save Changes to Excel

**Stories:** S2.2.1, S1.4.1

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Make changes (add/update/delete) | Changes in memory |
| 2 | Select: "2. Save to Excel" | "Saved X deals" message |
| 3 | Exit and re-run Console | Load shows changes persisted |

**Verification Checklist:**
- [ ] S2.2.1 — Changes persisted to Excel
- [ ] S1.4.1 — Save operation completes

---

## PART 3: MCP Server Verification (AI Integration)

**Persona:** Sales Manager (SM-UC7), Promotion Manager (PM-UC1-6)
**Stories Covered:** S1.1.x, S1.2.x, S1.3.x, S1.4.x, S4.4.x

---

### Recipe MCP-1: Server Infrastructure

**Stories:** S1.1.1, S1.1.2, S1.1.3

```bash
# S1.1.1 — JSON-RPC message loop
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: JSON response with "jsonrpc":"2.0" and tool list

# S1.1.2 — Tool dispatch (try different methods)
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.get_stats","params":{}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Stats response

echo '{"jsonrpc":"2.0","id":2,"method":"pipeline.daily_brief","params":{}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Daily brief response (different structure)

# S1.1.3 — Error handling
echo '{"jsonrpc":"2.0","id":1,"method":"invalid.method","params":{}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: JSON-RPC error with code and message
```

**Verification Checklist:**
- [ ] S1.1.1 — Server reads/writes JSON-RPC 2.0
- [ ] S1.1.2 — Dispatch to correct handler by method
- [ ] S1.1.3 — Invalid requests return proper error

---

### Recipe MCP-2: Pipeline CRUD Tools

**Stories:** S1.2.1, S1.2.2, S1.2.3, S1.2.4, S1.2.5

```bash
# S1.2.1 — List deals with limit
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.list_deals","params":{"limit":5}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Array of 5 deals

# S1.2.1 — List deals with filter
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.list_deals","params":{"stage":"Lead"}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Only Lead stage deals

# S1.2.2 — Get single deal (use ID from list_deals)
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.get_deal","params":{"dealId":"D-XXXXXXXX"}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Single deal with all fields

# S1.2.3 — Create deal
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.upsert_deal","params":{"deal":{"accountName":"MCP Test","dealName":"API Deal","stage":"Lead","amountGBP":25000}}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Created deal with generated DealId

# S1.2.4 — Patch deal
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.patch_deal","params":{"dealId":"D-XXXXXXXX","updates":{"stage":"Qualified","amountGBP":30000}}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Updated deal

# S1.2.5 — Delete deal
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.delete_deal","params":{"dealId":"D-XXXXXXXX"}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Success confirmation
```

**Verification Checklist:**
- [ ] S1.2.1 — list_deals with filtering and pagination
- [ ] S1.2.2 — get_deal by ID
- [ ] S1.2.3 — upsert_deal creates with normalization
- [ ] S1.2.4 — patch_deal updates specific fields
- [ ] S1.2.5 — delete_deal removes deal

---

### Recipe MCP-3: Report Tools

**Stories:** S1.3.1, S1.3.2, S1.3.3

```bash
# S1.3.1 — Daily Brief
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.daily_brief","params":{}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Object with dueToday, overdue, noContactDeals, highValueAtRisk

# S1.3.2 — Hygiene Report
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.hygiene_report","params":{}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Object with healthScore, issuesByType, issues array

# S1.3.3 — Forecast Snapshot
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.forecast_snapshot","params":{}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Object with totalPipeline, weightedPipeline, byStage, byOwner, byRegion
```

**Verification Checklist:**
- [ ] S1.3.1 — Daily brief via MCP
- [ ] S1.3.2 — Hygiene report via MCP
- [ ] S1.3.3 — Forecast snapshot via MCP

---

### Recipe MCP-4: Data Management Tools

**Stories:** S1.4.1, S1.4.2

```bash
# S1.4.1 — Save to Excel
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.save","params":{}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Success, Excel file updated

# S1.4.2 — Reload from Excel
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.reload","params":{}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Success, data reloaded
```

**Verification Checklist:**
- [ ] S1.4.1 — Save persists to Excel
- [ ] S1.4.2 — Reload refreshes from Excel

---

### Recipe MCP-5: Promoter Tools

**Stories:** S4.4.1, S4.4.2, S4.4.3

```bash
# First, find a promoCode from the data
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.list_deals","params":{"limit":10}}' | dotnet run --project src/Ox4D.Zarwin
# Look for a deal with promoCode field

# S4.4.1 — Promoter Dashboard
echo '{"jsonrpc":"2.0","id":1,"method":"promoter.dashboard","params":{"promoCode":"PROMO-XXX"}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Dashboard with tier, commission, referral counts

# S4.4.2 — Promoter Deals
echo '{"jsonrpc":"2.0","id":1,"method":"promoter.deals","params":{"promoCode":"PROMO-XXX"}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Array of deals with matching promoCode

# S4.4.3 — Promoter Actions
echo '{"jsonrpc":"2.0","id":1,"method":"promoter.actions","params":{"promoCode":"PROMO-XXX"}}' | dotnet run --project src/Ox4D.Zarwin
# Expected: Array of recommended actions
```

**Verification Checklist:**
- [ ] S4.4.1 — promoter.dashboard returns full dashboard
- [ ] S4.4.2 — promoter.deals returns promoter's referrals
- [ ] S4.4.3 — promoter.actions returns recommendations

---

## PART 4: Architecture Verification (Developer Experience)

**Persona:** Dataset Engineer / Developer
**Stories Covered:** S5.1.x, S5.3.x, S5.4.x, S2.1.x

---

### Recipe ARCH-1: Clean Architecture

**Stories:** S5.1.1, S5.1.2, S5.1.3

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Open: `src/Ox4D.Core/Ox4D.Core.csproj` | No `<PackageReference>` elements | S5.1.1 |
| 2 | Verify: Core has no NuGet dependencies | Zero external packages | S5.1.1 |
| 3 | Open: `src/Ox4D.Store/*.cs` files | Only repository implementations | S5.1.2 |
| 4 | Verify: Store has no business logic | Only persistence code | S5.1.2 |
| 5 | Open: `src/Ox4D.Console/Program.cs` | Thin wrapper calling services | S5.1.3 |
| 6 | Open: `src/Ox4D.Zarwin/Program.cs` | Thin wrapper calling services | S5.1.3 |

**Verification Checklist:**
- [ ] S5.1.1 — Ox4D.Core has zero NuGet dependencies
- [ ] S5.1.2 — Ox4D.Store only handles persistence
- [ ] S5.1.3 — Console and Zarwin are thin wrappers

---

### Recipe ARCH-2: Interface Abstractions

**Stories:** S5.4.1, S5.4.2, S2.1.1, S2.1.2

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Find: `src/Ox4D.Core/Services/IDealRepository.cs` | Interface exists | S5.4.1, S2.1.1 |
| 2 | Verify: GetAllAsync, GetByIdAsync, UpsertAsync, DeleteAsync, SaveChangesAsync | All methods present | S2.1.1 |
| 3 | Find: `src/Ox4D.Core/Services/ISyntheticDataGenerator.cs` | Interface exists | S5.4.2 |
| 4 | Find: `src/Ox4D.Store/InMemoryDealRepository.cs` | Implements IDealRepository | S2.1.2 |

**Verification Checklist:**
- [ ] S5.4.1 — IDealRepository defined in Core
- [ ] S5.4.2 — ISyntheticDataGenerator defined in Core
- [ ] S2.1.1 — IDealRepository with full CRUD
- [ ] S2.1.2 — InMemoryDealRepository for testing

---

### Recipe ARCH-3: Unit Tests

**Stories:** S5.3.1, S5.3.2, S5.3.3

```bash
# Run all tests
dotnet test --verbosity normal
# Expected: 393/393 pass

# S5.3.1 — Normalization tests
dotnet test --filter "FullyQualifiedName~Normaliz"
# Expected: Tests pass for postcode, region, probability

# S5.3.2 — Report tests
dotnet test --filter "FullyQualifiedName~Report"
# Expected: Tests pass for Daily Brief, Hygiene, Forecast

# S5.3.3 — Synthetic data tests
dotnet test --filter "FullyQualifiedName~Synthetic"
# Expected: Tests pass, same seed = same data
```

**Verification Checklist:**
- [ ] S5.3.1 — Normalization tests pass
- [ ] S5.3.2 — Report tests pass
- [ ] S5.3.3 — Synthetic data tests pass
- [ ] All 393 tests pass

---

## PART 5: Promoter Model Verification

**Stories Covered:** S4.1.x, S4.2.x, S4.3.x

---

### Recipe PM-1: Promoter Entity

**Stories:** S4.1.1, S4.1.2, S4.1.3

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Open: `src/Ox4D.Core/Models/Promoter.cs` | Promoter class exists | S4.1.1 |
| 2 | Verify: PromoCode, Name, Tier, CommissionRate, TotalReferrals | All present | S4.1.1 |
| 3 | Open: `src/Ox4D.Core/Models/Deal.cs` | Promoter fields present | S4.1.2 |
| 4 | Verify: PromoCode, PromoterCommission, CommissionPaid | All present | S4.1.2 |
| 5 | Find: Tier calculation logic | Tier derived from referral count | S4.1.3 |

**Verification Checklist:**
- [ ] S4.1.1 — Promoter entity defined
- [ ] S4.1.2 — Promoter fields on Deal
- [ ] S4.1.3 — Tier calculation implemented

---

### Recipe PM-2: Promoter Dashboard Metrics

**Stories:** S4.2.1, S4.2.2, S4.2.3, S4.2.4

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Open: `src/Ox4D.Core/Models/Reports/PromoterDashboard.cs` | Dashboard class exists | - |
| 2 | Verify: TotalReferrals, OpenPipeline, WeightedPipeline | Present | S4.2.1 |
| 3 | Verify: PendingCommission, ProjectedCommission, PaidCommission | Present | S4.2.2 |
| 4 | Verify: CurrentTier, ReferralsToNextTier, NextTierName | Present | S4.2.3 |
| 5 | Verify: StalledDeals, AtRiskDeals, HealthyDeals | Present | S4.2.4 |

**Verification Checklist:**
- [ ] S4.2.1 — Pipeline metrics
- [ ] S4.2.2 — Commission earnings
- [ ] S4.2.3 — Tier progression
- [ ] S4.2.4 — Deal health summary

---

### Recipe PM-3: Promoter Actions

**Stories:** S4.3.1, S4.3.2, S4.3.3

| Step | Action | Expected Result | Story |
|------|--------|-----------------|-------|
| 1 | Open: `src/Ox4D.Core/Services/PromoterService.cs` | Service exists | - |
| 2 | Find: Method identifying stalled referrals | Logic for no recent activity | S4.3.1 |
| 3 | Find: Method generating recommendations | Specific action strings | S4.3.2 |
| 4 | Find: Prioritization logic | Sorted by value and/or urgency | S4.3.3 |

**Verification Checklist:**
- [ ] S4.3.1 — Stalled referrals identified
- [ ] S4.3.2 — Action recommendations generated
- [ ] S4.3.3 — Actions prioritized by impact

---

## PART 6: Documentation Verification

**Stories:** S2.3.2

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Open: README.md | Supabase migration section exists |
| 2 | Open: CLAUDE.md | "Future: Supabase Migration" section exists |
| 3 | Verify: 4-step migration path documented | Clear instructions |

**Verification Checklist:**
- [ ] S2.3.2 — Supabase migration documented

---

## Complete Verification Checklist

### Epic G1: AI-Ready Sales Management (13 Stories)
- [ ] S1.1.1 — JSON-RPC message loop
- [ ] S1.1.2 — Tool dispatch mechanism
- [ ] S1.1.3 — Error handling
- [ ] S1.2.1 — pipeline.list_deals
- [ ] S1.2.2 — pipeline.get_deal
- [ ] S1.2.3 — pipeline.upsert_deal
- [ ] S1.2.4 — pipeline.patch_deal
- [ ] S1.2.5 — pipeline.delete_deal
- [ ] S1.3.1 — pipeline.daily_brief
- [ ] S1.3.2 — pipeline.hygiene_report
- [ ] S1.3.3 — pipeline.forecast_snapshot
- [ ] S1.4.1 — pipeline.save
- [ ] S1.4.2 — pipeline.reload

### Epic G2: Zero Vendor Lock-in (8 Stories)
- [ ] S2.1.1 — IDealRepository interface
- [ ] S2.1.2 — InMemoryDealRepository
- [ ] S2.2.1 — Deals sheet read/write
- [ ] S2.2.2 — Lookups sheet
- [ ] S2.2.3 — Metadata sheet
- [ ] S2.2.4 — Promoter fields in Excel
- [ ] S2.3.1 — Sheet-per-table design
- [ ] S2.3.2 — Migration documentation

### Epic G3: Actionable Intelligence (18 Stories)
- [ ] S3.1.1 — Overdue actions
- [ ] S3.1.2 — Actions due today
- [ ] S3.1.3 — No-contact deals
- [ ] S3.1.4 — High-value at-risk
- [ ] S3.2.1 — Missing amounts
- [ ] S3.2.2 — Missing close dates
- [ ] S3.2.3 — Stage-probability mismatch
- [ ] S3.2.4 — Missing location data
- [ ] S3.3.1 — Total and weighted pipeline
- [ ] S3.3.2 — Breakdown by dimensions
- [ ] S3.3.3 — Actionable insights
- [ ] S3.4.1 — Create deals
- [ ] S3.4.2 — Update deals
- [ ] S3.4.3 — Delete deals
- [ ] S3.4.4 — Search and filter
- [ ] S3.4.5 — View deal details
- [ ] S3.5.1 — High-level metrics
- [ ] S3.5.2 — Unique owners/regions

### Epic G4: Referral Partner Support (13 Stories)
- [ ] S4.1.1 — Promoter entity
- [ ] S4.1.2 — Promoter fields in Deal
- [ ] S4.1.3 — Tier calculation
- [ ] S4.2.1 — Pipeline metrics
- [ ] S4.2.2 — Commission earnings
- [ ] S4.2.3 — Tier progression
- [ ] S4.2.4 — Deal health summary
- [ ] S4.3.1 — Stalled referrals
- [ ] S4.3.2 — Action recommendations
- [ ] S4.3.3 — Prioritize by impact
- [ ] S4.4.1 — promoter.dashboard
- [ ] S4.4.2 — promoter.deals
- [ ] S4.4.3 — promoter.actions

### Epic G5: Developer Experience (12 Stories)
- [ ] S5.1.1 — Core isolation
- [ ] S5.1.2 — Storage separation
- [ ] S5.1.3 — UI separation
- [ ] S5.2.1 — Simple generator
- [ ] S5.2.2 — Bogus generator
- [ ] S5.2.3 — Hygiene issues
- [ ] S5.2.4 — Promoter referrals
- [ ] S5.3.1 — Normalization tests
- [ ] S5.3.2 — Report tests
- [ ] S5.3.3 — Synthetic data tests
- [ ] S5.4.1 — IDealRepository
- [ ] S5.4.2 — ISyntheticDataGenerator

---

## Quick Verification Sequence

Execute in order for rapid verification:

```bash
# 1. Build and test (G5 architecture)
cd c:/Code/Ox4D
dotnet build && dotnet test

# 2. Run Console - Unified Gateway
dotnet run --project src/Ox4D.Console

# Menu 13: Run Full Demo (generates data, DE-UC1 through DE-UC6)
# Menu 4: Daily Brief
# Menu 5: Hygiene Report
# Menu 6: Forecast Snapshot
# Menu 7: Statistics
# Menu 10, 9, 11: CRUD operations (SM-UC1 through SM-UC6)

# 3. Test MCP tools (G1 AI-Ready)
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.get_stats","params":{}}' | dotnet run --project src/Ox4D.Zarwin
echo '{"jsonrpc":"2.0","id":2,"method":"pipeline.daily_brief","params":{}}' | dotnet run --project src/Ox4D.Zarwin
echo '{"jsonrpc":"2.0","id":3,"method":"pipeline.forecast_snapshot","params":{}}' | dotnet run --project src/Ox4D.Zarwin

# 4. Check Excel file (G2 storage)
# Open data/SalesPipelineV1.0.xlsx - verify 3 worksheets
```

---

*Cross-references: [UseCases.md](UseCases.md) | [PRCD.md](PRCD.md)*
