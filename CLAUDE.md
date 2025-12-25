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
│            │    Ox4D.Storage       │                               │
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
└─────────────────────────────────────────────────────────────────────┘
```

---

## 1) Project Structure

```
Ox4D.sln
src/
  Ox4D.Core/           # Domain models, services, reports (business logic)
  Ox4D.Storage/        # Repository implementations (Excel, InMemory)
  Ox4D.Console/        # Interactive menu-driven terminal UI (Spectre.Console)
  Ox4D.Zarwin/         # MCP server for LLM tool integration (stdio JSON-RPC)
tests/
  Ox4D.Tests/          # Unit tests (xUnit + FluentAssertions)
tools/
  Demo/                # Non-interactive demo showcasing all features
data/
  SalesPipelineV1.0.xlsx   # Primary data storage (Excel workbook)
```

### Project Purposes

| Project | Purpose |
|---------|---------|
| **Ox4D.Core** | Domain entities (Deal, DealStage), business logic (normalization, reports), service interfaces (IDealRepository). Zero dependencies on storage or UI. |
| **Ox4D.Storage** | Storage implementations. ExcelDealRepository uses sheet-per-table design for future Supabase migration. InMemoryDealRepository for testing. |
| **Ox4D.Console** | Interactive copilot for sales managers. Rich terminal UI with Spectre.Console. Imports/exports Excel, generates reports, manages deals. |
| **Ox4D.Zarwin** | MCP (Model Context Protocol) server. Exposes pipeline tools via JSON-RPC over stdio. Allows LLMs like Claude to operate on pipeline data. |
| **Ox4D.Tests** | Unit tests for normalization, reports, and synthetic data generation. |

---

## 2) Data Storage: Excel with Sheet-Per-Table Design

**Primary data file:** `data/SalesPipelineV1.0.xlsx`

The Excel workbook uses a **sheet-per-table design** that directly maps to future Supabase tables:

| Sheet | Purpose | Future Supabase Table |
|-------|---------|----------------------|
| **Deals** | Main pipeline deals | `deals` |
| **Lookups** | Postcode→Region, Stage→Probability | `postcode_regions`, `stage_probabilities` |
| **Metadata** | Version info, timestamps | `metadata` |

### Why Sheet-Per-Table?
- Each sheet = one database table
- Column headers = column names
- Rows = records
- Makes Supabase migration straightforward: swap `ExcelDealRepository` for `SupabaseDealRepository`

### No CSV Files
CSV storage was removed in V1.1. All data persists in Excel with the sheet-per-table structure.

---

## 3) NuGet Packages

| Package | Purpose |
|---------|---------|
| `ClosedXML` | Excel read/write |
| `Spectre.Console` | Rich terminal UI |
| `System.Text.Json` | JSON serialization for MCP |
| `FluentAssertions` | Test assertions |
| `xUnit` | Unit testing |

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
```

### DealStage Enum
```
Lead → Qualified → Discovery → Proposal → Negotiation → Closed Won / Closed Lost / On Hold
```

### Normalization Rules
- Missing `DealId` → auto-generate (D-XXXXXXXX)
- Missing `Probability` → default from Stage lookup
- Extract `PostcodeArea` from `Postcode` (e.g., "SW1A 1AA" → "SW")
- Derive `Region` from `PostcodeArea` using Lookups
- Generate `MapLink` as Google Maps URL

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

---

## 6) Console Menu (Ox4D.Console)

Interactive terminal menu:
1. Load Excel workbook
2. Save changes to Excel
3. Generate synthetic demo data
4. View Daily Brief
5. View Hygiene Report
6. View Forecast Snapshot
7. View Pipeline Statistics
8. Search deals
9. View deal details
10. Create new deal
11. Update existing deal
12. Delete deal

---

## 7) MCP Server (Ox4D.Zarwin)

Zarwin exposes pipeline operations as MCP tools via JSON-RPC over stdio.

### Available Tools

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

## 8) Synthetic Data Generator

Deterministic generation with configurable seed:
- Distribution: ~45% early stage, ~35% mid stage, ~20% closed
- Injects hygiene issues for testing:
  - ~10% missing NextStepDueDate
  - ~8% missing AmountGBP
  - ~12% missing LastContactedDate
- Realistic UK postcodes, company names, owner names

---

## 9) Definition of Done (V1.1)

- [x] Excel-only storage with sheet-per-table design
- [x] Console app with full CRUD + reports
- [x] MCP server with all pipeline tools
- [x] Synthetic data generation
- [x] Unit tests for core logic
- [x] Project documentation

---

## 10) Future: Supabase Migration

When migrating to Supabase:

1. Create tables matching sheet structure:
   - `deals` table with all Deal columns
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

## 11) Guardrails

- Keep MCP tools deterministic (no LLM logic inside)
- Treat Lookups as user-editable configuration
- All business logic in Ox4D.Core (not in storage or UI)
- Maintain repository abstraction for storage swap
