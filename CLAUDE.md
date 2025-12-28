# CLAUDE.md — Ox4D Sales Pipeline Manager

This repo is a **Sales Manager Copilot** that operates on an **Excel-based sales pipeline dataset** using a sheet-per-table design, and exposes deterministic operations via **Ox4D.Zarwin** (MCP-style local tool server using stdio JSON-RPC).

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Ox4D Sales Pipeline                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   ┌──────────────────┐    ┌──────────────────┐                     │
│   │  Ox4D.Console    │    │  Ox4D.Zarwin     │                     │
│   │  (Interactive UI)│    │  (MCP Server)    │                     │
│   └────────┬─────────┘    └────────┬─────────┘                     │
│            │                        │                               │
│            └───────────┬────────────┘                               │
│                        │                                            │
│            ┌───────────▼───────────┐                               │
│            │    Ox4D.Core          │                               │
│            │  (Domain + Services)   │                               │
│            └───────────┬───────────┘                               │
│                        │                                            │
│            ┌───────────▼───────────┐                               │
│            │    Ox4D.Store         │                               │
│            │  (IDealRepository)    │                               │
│            └───────────┬───────────┘                               │
│                        │                                            │
│       ┌────────────────┼────────────────┐                          │
│       │                │                 │                          │
│   ┌───▼───┐    ┌───────▼───────┐   ┌────▼────┐                    │
│   │ Excel │    │   In-Memory   │   │Supabase │                    │
│   │(.xlsx)│    │   (Testing)   │   │(Future) │                    │
│   └───────┘    └───────────────┘   └─────────┘                    │
│                                                                      │
│   ┌─────────────────────────────────────────────────────────────┐   │
│   │                      Ox4D.Mutate                             │   │
│   │  (Bogus-powered synthetic data generation)                   │   │
│   └─────────────────────────────────────────────────────────────┘   │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 1) Project Structure

```
Ox4D.sln
src/
  Ox4D.Core/           # Domain models, services, reports (business logic)
  Ox4D.Store/          # Repository implementations (Excel, InMemory)
  Ox4D.Console/        # Unified gateway: interactive UI + full demo mode
  Ox4D.Zarwin/         # MCP server for LLM tool integration (stdio JSON-RPC)
  Ox4D.Mutate/         # Bogus-powered synthetic data generation
tests/
  Ox4D.Tests/          # Unit tests (xUnit + FluentAssertions)
data/
  SalesPipelineV1.0.xlsx   # Primary data storage (Excel workbook)
```

### Project Purposes

| Project | Purpose |
|---------|---------|
| **Ox4D.Core** | Domain entities (Deal, DealStage, Promoter), business logic (normalization, reports), service interfaces (IDealRepository, ISyntheticDataGenerator). Zero dependencies on storage or UI. |
| **Ox4D.Store** | Storage implementations. ExcelDealRepository uses sheet-per-table design for future Supabase migration. InMemoryDealRepository for testing. |
| **Ox4D.Console** | **Unified gateway** for all Ox4D operations. Rich terminal UI with Spectre.Console. Includes interactive menu (CRUD, reports, data management) plus a full non-interactive demo mode. |
| **Ox4D.Zarwin** | MCP (Model Context Protocol) server. Exposes pipeline and promoter tools via JSON-RPC over stdio. Allows LLMs like Claude to operate on pipeline data. |
| **Ox4D.Mutate** | Investor-grade synthetic data generation using Bogus. Produces realistic UK data (addresses, phone numbers, company names). |
| **Ox4D.Tests** | Unit tests for normalization, reports, and synthetic data generation. |

---

## 2) Data Storage: Excel with Sheet-Per-Table Design

**Primary data file:** `data/SalesPipelineV1.0.xlsx`

The Excel workbook uses a **sheet-per-table design** that directly maps to future Supabase tables:

| Sheet | Purpose | Future Supabase Table |
|-------|---------|----------------------|
| **Deals** | Main pipeline deals (includes promoter fields) | `deals` |
| **Lookups** | Postcode→Region, Stage→Probability | `postcode_regions`, `stage_probabilities` |
| **Metadata** | Version info, timestamps | `metadata` |

### Why Sheet-Per-Table?
- Each sheet = one database table
- Column headers = column names
- Rows = records
- Makes Supabase migration straightforward: swap `ExcelDealRepository` for `SupabaseDealRepository`

---

## 3) NuGet Packages

| Package | Purpose |
|---------|---------|
| `ClosedXML` | Excel read/write |
| `Spectre.Console` | Rich terminal UI |
| `System.Text.Json` | JSON serialization for MCP |
| `FluentAssertions` | Test assertions |
| `xUnit` | Unit testing |
| `Bogus` | Realistic synthetic data generation |

---

## 4) Domain Model (Ox4D.Core)

### Deal Entity
Core entity with fields matching the Deals sheet columns:

```
Identity:        DealId, OrderNo, UserId
Account:         AccountName, ContactName, Email, Phone
Location:        Postcode, PostcodeArea, InstallationLocation, Region, MapLink
Deal Details:    LeadSource, ProductLine, DealName
Stage & Value:   Stage, Probability, AmountGBP, WeightedAmountGBP (computed)
Ownership:       Owner, CreatedDate, LastContactedDate
Actions:         NextStep, NextStepDueDate, CloseDate
Service:         ServicePlan, LastServiceDate, NextServiceDueDate
Metadata:        Comments, Tags
Promoter:        PromoterId, PromoCode, PromoterCommission, CommissionPaid, CommissionPaidDate
```

### DealStage Enum
```
Lead → Qualified → Discovery → Proposal → Negotiation → Closed Won / Closed Lost / On Hold
```

### Promoter Model
Referral partners who earn commissions on deals:

| Tier | Commission Rate | Min Referrals |
|------|-----------------|---------------|
| Bronze | 10% | 0 |
| Silver | 12% | 10 |
| Gold | 15% | 25 |
| Platinum | 18% | 50 |
| Diamond | 20% | 100 |

### Normalization Rules

- Missing `DealId` → auto-generate via `IDealIdGenerator` (format: `D-YYYYMMDD-XXXXXXXX`)
- Missing `Probability` → default from Stage lookup
- Extract `PostcodeArea` from `Postcode` (e.g., "SW1A 1AA" → "SW")
- Derive `Region` from `PostcodeArea` using Lookups
- Generate `MapLink` as Google Maps URL
- Set `CreatedDate` to today if missing (via `IClock`)

---

## 5) Reports

### Daily Brief
Answers: "What needs my attention today?"
- Next steps due today
- Overdue actions
- No-contact deals (>10 days since last contact)
- High-value deals at risk

### Hygiene Report
Answers: "What data quality issues exist?"
- Missing amounts
- Missing close dates (for late-stage deals)
- Stage-probability mismatches
- Missing postcodes/regions

### Forecast Snapshot
Answers: "What does my pipeline look like?"
- Total pipeline value
- Weighted pipeline value
- Breakdown by: Stage, Owner, Close Month, Region, Product

### Promoter Dashboard
Answers: "How are my referral partners performing?"
- Referral metrics (total, active, won, lost)
- Pipeline breakdown by stage
- Commission tracking (earned, paid, pending)
- Actionable recommendations for promoters

---

## 6) Console Application (Ox4D.Console)

**Ox4D.Console** is the **unified gateway** for all pipeline operations. It provides both interactive menu access and a full non-interactive demo mode.

### Usage
```bash
# Run the unified console application
dotnet run --project src/Ox4D.Console
```

### Menu Options
1. Load Excel workbook
2. Save changes to Excel
3. Generate synthetic demo data
4. View Daily Brief
5. View Hygiene Report
6. View Forecast Snapshot
7. View Pipeline Statistics
8. Search / Deal Drilldown
9. Update Deal
10. Add New Deal
11. Delete Deal
12. List All Deals
13. **Run Full Demo** (Non-Interactive) — showcases all features
0. Exit

### Full Demo Mode (Option 13)
The demo mode runs through all pipeline features automatically:
- Generates 100 synthetic deals (seed=42)
- Displays pipeline statistics
- Shows daily brief with overdue items
- Runs hygiene report with health score
- Displays forecast snapshot (by stage, owner, month)
- Shows sample deal details
- Saves everything to Excel

This mode is ideal for:
- **Demonstrations** to stakeholders
- **Verification** of all system capabilities
- **Training** new users on available features

---

## 7) MCP Server (Ox4D.Zarwin)

Zarwin exposes pipeline operations as MCP tools via JSON-RPC over stdio.

### Pipeline Tools

| Tool | Description |
|------|-------------|
| `pipeline.list_deals` | List deals with optional filters |
| `pipeline.get_deal` | Get a single deal by ID |
| `pipeline.upsert_deal` | Create or update a deal |
| `pipeline.patch_deal` | Update specific fields |
| `pipeline.delete_deal` | Delete a deal |
| `pipeline.hygiene_report` | Generate hygiene report |
| `pipeline.daily_brief` | Generate daily brief |
| `pipeline.forecast_snapshot` | Generate forecast snapshot |
| `pipeline.generate_synthetic` | Generate test data |
| `pipeline.get_stats` | Get pipeline statistics |
| `pipeline.save` | Save changes to Excel |
| `pipeline.reload` | Reload data from Excel |

### Promoter Tools

| Tool | Description |
|------|-------------|
| `promoter.dashboard` | Get comprehensive promoter dashboard with metrics |
| `promoter.deals` | Get all deals referred by a promoter |
| `promoter.actions` | Get recommended actions for a promoter |

### Usage
```bash
# Run the MCP server (reads from data/SalesPipelineV1.0.xlsx by default)
dotnet run --project src/Ox4D.Zarwin

# Use custom Excel file
dotnet run --project src/Ox4D.Zarwin -- "path/to/custom.xlsx"
```

### Protocol
JSON-RPC 2.0 over stdio. Each request is a single line of JSON.

```json
{"jsonrpc":"2.0","id":1,"method":"pipeline.list_deals","params":{"owner":"Alice"}}
```

---

## 8) Synthetic Data Generation

Two implementations available via `ISyntheticDataGenerator` interface:

### Simple Generator (Ox4D.Core)
- Basic Random-based generation
- Suitable for quick testing
- No external dependencies

### Bogus Generator (Ox4D.Mutate)
- Uses Bogus library for realistic UK data
- Proper UK postcode formats with region mapping
- Authentic British company and person names
- Proper UK phone number formats (+44/0xxx)
- Promoter/referral data with commission tracking
- Investor-grade demo datasets

### Common Features
- Deterministic output with configurable seed
- Distribution: ~45% early stage, ~35% mid stage, ~20% closed
- Intentional hygiene issues for testing:
  - ~10% missing NextStepDueDate
  - ~8% missing AmountGBP
  - ~12% missing LastContactedDate

---

## 9) Definition of Done (V1.2)

- [x] Excel-only storage with sheet-per-table design
- [x] Console app with full CRUD + reports
- [x] MCP server with all pipeline tools (v1.0, versioned)
- [x] Promoter/referral partner support with commission tracking
- [x] Promoter MCP tools (dashboard, deals, actions)
- [x] Bogus-powered synthetic data generation (Ox4D.Mutate)
- [x] **Typed patch validation** (DealPatch DTO + PatchResult)
- [x] **Schema versioning** (v1.2) with migration scaffold
- [x] **Deterministic ID generation** (IDealIdGenerator abstraction)
- [x] **IClock injection** throughout for testable timestamps
- [x] **Cross-process Excel locking** with retry logic
- [x] Unit tests (415 passing)
- [x] Project documentation
- [x] .NET 10.0 upgrade

---

## 10) Future: Supabase Migration

When migrating to Supabase:

1. Create tables matching sheet structure:
   - `deals` table with all Deal columns (including promoter fields)
   - `promoters` table for promoter profiles
   - `lookups` table for configuration

2. Implement `SupabaseDealRepository : IDealRepository`

3. Replace DI registration:
   ```csharp
   // Before
   services.AddSingleton<IDealRepository, ExcelDealRepository>();

   // After
   services.AddSingleton<IDealRepository, SupabaseDealRepository>();
   ```

4. No changes needed to Ox4D.Core, Ox4D.Console, or Ox4D.Zarwin

---

## 11) Key Abstractions (V1.2)

### Time Abstraction (IClock)

All date/time operations use injectable `IClock` for testability:

```csharp
public interface IClock
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
    DateTime Today { get; }
}
// Production: SystemClock.Instance
// Testing: FixedClock.At(2025, 1, 15)
```

### ID Generation (IDealIdGenerator)

Deterministic ID generation for reproducible tests:

```csharp
public interface IDealIdGenerator
{
    string Generate();
}
// Production: DefaultDealIdGenerator (uses GUID)
// Testing: SequentialDealIdGenerator, SeededDealIdGenerator
```

### Typed Patch Validation (DealPatch + PatchResult)

No more silent failures from reflection-based patching:

```csharp
var result = await service.PatchDealWithResultAsync(dealId, updates);
// result.AppliedFields - successfully applied changes
// result.RejectedFields - invalid fields with reasons
// result.NormalizationChanges - auto-applied normalizations
```

### Schema Versioning

Excel files track schema version in Metadata sheet:

- `CurrentSchemaVersion = "1.2"`
- Auto-migration from older versions (1.0 → 1.1 → 1.2)
- Rejects unsupported future versions with clear error

---

## 12) Guardrails

- Keep MCP tools deterministic (no LLM logic inside)
- Treat Lookups as user-editable configuration
- All business logic in Ox4D.Core (not in storage or UI)
- Maintain repository abstraction for storage swap
- Use ISyntheticDataGenerator interface for data generation flexibility
- Use IClock for all date/time operations (testability)
- Use IDealIdGenerator for ID creation (reproducibility)
