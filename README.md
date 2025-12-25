# Ox4D — Sales Pipeline Manager

<div align="center">

**An AI-Ready Sales Pipeline Copilot with MCP Integration**

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![MCP Compatible](https://img.shields.io/badge/MCP-Compatible-blue)](https://modelcontextprotocol.io/)

*Manage your sales pipeline with an intelligent terminal UI and expose operations to AI assistants via the Model Context Protocol.*

</div>

---

## Overview

Ox4D is a **Sales Manager Copilot** designed for teams that want:

- **Excel-based storage** with a database-ready architecture (sheet-per-table design)
- **Rich terminal interface** for managing deals, generating reports, and maintaining data hygiene
- **AI integration** via MCP (Model Context Protocol), allowing LLMs like Claude to query and update your pipeline

The architecture is intentionally simple: a clean separation between domain logic, storage, and presentation layers makes it easy to swap Excel for Supabase (or any database) when you're ready to scale.

---

## Key Features

### Pipeline Management
- Full CRUD operations on deals
- Automatic field normalization (postcode extraction, region mapping, Google Maps links)
- Stage-based probability defaults
- Multi-field search and filtering

### Actionable Reports

| Report | Purpose |
|--------|---------|
| **Daily Brief** | What needs attention today? Due actions, overdue items, stale deals, high-value risks |
| **Hygiene Report** | Data quality issues: missing amounts, dates, owners, stage-probability mismatches |
| **Forecast Snapshot** | Pipeline breakdown by stage, owner, region, product, and close month |

### AI-Ready MCP Server
The **Zarwin** server exposes all pipeline operations as MCP tools, enabling AI assistants to:
- Query and search deals
- Create, update, and delete pipeline entries
- Generate reports and forecasts
- Save changes back to Excel

### Promoter/Referral Partner Support

Track deals referred by promoters using promo codes:

- **Commission Tracking**: Automatic commission calculation based on tier (Bronze to Diamond)
- **Promoter Dashboard**: Performance metrics, pipeline breakdown, and earnings projections
- **Actionable Recommendations**: AI-powered suggestions for promoters to help move deals forward
- **Deal Health Monitoring**: Identify stalled, at-risk, or critical deals needing attention

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Ox4D Sales Pipeline                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   ┌──────────────────┐    ┌──────────────────┐                      │
│   │  Ox4D.Console    │    │  Ox4D.Zarwin     │                      │
│   │  (Terminal UI)   │    │  (MCP Server)    │                      │
│   └────────┬─────────┘    └────────┬─────────┘                      │
│            │                        │                                │
│            └───────────┬────────────┘                                │
│                        │                                             │
│            ┌───────────▼───────────┐                                │
│            │      Ox4D.Core        │                                │
│            │  Domain + Services    │                                │
│            └───────────┬───────────┘                                │
│                        │                                             │
│            ┌───────────▼───────────┐                                │
│            │    Ox4D.Storage       │                                │
│            │   IDealRepository     │                                │
│            └───────────┬───────────┘                                │
│                        │                                             │
│       ┌────────────────┼────────────────┐                           │
│       │                │                │                            │
│   ┌───▼───┐    ┌───────▼───────┐   ┌────▼────┐                      │
│   │ Excel │    │   In-Memory   │   │Supabase │                      │
│   │(.xlsx)│    │   (Testing)   │   │(Planned)│                      │
│   └───────┘    └───────────────┘   └─────────┘                      │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Design Principles

1. **Domain-Driven Core**: All business logic lives in `Ox4D.Core` with zero dependencies on storage or UI
2. **Repository Pattern**: `IDealRepository` abstraction enables seamless storage backend swaps
3. **Sheet-Per-Table Excel**: Each worksheet maps to a database table for easy cloud migration
4. **Deterministic MCP Tools**: No LLM logic inside tools—Claude operates on structured, predictable operations

---

## Project Structure

```
Ox4D/
├── src/
│   ├── Ox4D.Core/           # Domain models, services, reports
│   │   ├── Models/          # Deal, DealStage, DealFilter, Promoter
│   │   │   ├── Config/      # LookupTables, PipelineSettings
│   │   │   └── Reports/     # DailyBrief, HygieneReport, ForecastSnapshot, PromoterDashboard
│   │   └── Services/        # PipelineService, ReportService, PromoterService
│   │
│   ├── Ox4D.Storage/        # Repository implementations
│   │   ├── ExcelDealRepository.cs    # Excel with sheet-per-table design
│   │   └── InMemoryDealRepository.cs # For testing
│   │
│   ├── Ox4D.Console/        # Interactive terminal UI (Spectre.Console)
│   │   └── CopilotMenu.cs   # Menu-driven interface
│   │
│   └── Ox4D.Zarwin/         # MCP server for AI integration
│       ├── ZarwinServer.cs  # JSON-RPC message loop
│       ├── Handlers/        # Tool implementations
│       └── Protocol/        # JSON-RPC types and tool definitions
│
├── tests/
│   └── Ox4D.Tests/          # xUnit + FluentAssertions
│
├── tools/
│   └── Demo/                # Non-interactive feature showcase
│
├── data/
│   └── SalesPipelineV1.0.xlsx  # Primary data storage
│
└── Ox4D.sln
```

---

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

### Installation

```bash
git clone https://github.com/revred/Ox4D.git
cd Ox4D
dotnet restore
dotnet build
```

### Run the Interactive Console

```bash
dotnet run --project src/Ox4D.Console
```

This launches a rich terminal interface where you can:
- Load and save Excel workbooks
- Generate synthetic demo data
- View reports (Daily Brief, Hygiene, Forecast)
- Create, update, search, and delete deals

### Run the MCP Server

```bash
# Use default data file (data/SalesPipelineV1.0.xlsx)
dotnet run --project src/Ox4D.Zarwin

# Use a custom Excel file
dotnet run --project src/Ox4D.Zarwin -- "path/to/your/pipeline.xlsx"
```

The server communicates via JSON-RPC 2.0 over stdio, making it compatible with any MCP-aware client.

### Run Tests

```bash
dotnet test
```

---

## MCP Integration

### Available Tools

The Zarwin server exposes these tools for AI assistants:

| Tool | Description |
|------|-------------|
| `pipeline.list_deals` | List/search deals with filters (owner, region, stage, amount range) |
| `pipeline.get_deal` | Retrieve a single deal by ID |
| `pipeline.upsert_deal` | Create or update a deal |
| `pipeline.patch_deal` | Update specific fields on a deal |
| `pipeline.delete_deal` | Remove a deal |
| `pipeline.daily_brief` | Generate daily action items report |
| `pipeline.hygiene_report` | Generate data quality report |
| `pipeline.forecast_snapshot` | Generate pipeline forecast breakdown |
| `pipeline.get_stats` | Get high-level pipeline statistics |
| `pipeline.generate_synthetic` | Generate test data with configurable seed |
| `pipeline.save` | Persist changes to Excel |
| `pipeline.reload` | Reload data from Excel |

#### Promoter Tools

| Tool | Description |
|------|-------------|
| `promoter.dashboard` | Get comprehensive promoter dashboard with metrics, pipeline breakdown, and commission tracking |
| `promoter.deals` | Get all deals referred by a promoter with status and health information |
| `promoter.actions` | Get recommended actions for a promoter to help move their referred deals forward |

### Example Request

```json
{"jsonrpc":"2.0","id":1,"method":"pipeline.list_deals","params":{"owner":"Alice","minAmount":10000}}
```

### Example Response

```json
{"jsonrpc":"2.0","id":1,"result":{"deals":[{"dealId":"D-12345678","accountName":"Acme Corp","dealName":"Enterprise License","stage":"Proposal","amountGBP":50000}]}}
```

---

## Domain Model

### Deal Entity

The `Deal` class represents a sales opportunity with comprehensive tracking:

| Category | Fields |
|----------|--------|
| **Identity** | DealId, OrderNo, UserId |
| **Account** | AccountName, ContactName, Email, Phone |
| **Location** | Postcode, PostcodeArea, Region, InstallationLocation, MapLink |
| **Details** | DealName, ProductLine, LeadSource |
| **Value** | Stage, Probability, AmountGBP, WeightedAmountGBP (computed) |
| **Ownership** | Owner, CreatedDate, LastContactedDate |
| **Actions** | NextStep, NextStepDueDate, CloseDate |
| **Service** | ServicePlan, LastServiceDate, NextServiceDueDate |
| **Metadata** | Comments, Tags |
| **Promoter** | PromoterId, PromoCode, PromoterCommission, CommissionPaid |

### Deal Stages

```
Lead → Qualified → Discovery → Proposal → Negotiation → Closed Won / Closed Lost / On Hold
```

Each stage has a default probability that's applied during normalization.

### Automatic Normalization

When deals are created or updated, the system automatically:
- Generates missing `DealId` values (format: `D-XXXXXXXX`)
- Extracts `PostcodeArea` from UK postcodes (e.g., "SW1A 1AA" → "SW")
- Derives `Region` using configurable postcode-to-region mappings
- Generates `MapLink` as a Google Maps URL
- Applies default probability based on stage

### Promoter Tiers

Promoters earn commissions based on their tier level:

| Tier | Commission Rate | Min Referrals |
|------|-----------------|---------------|
| Bronze | 10% | 0 |
| Silver | 12% | 10 |
| Gold | 15% | 25 |
| Platinum | 18% | 50 |
| Diamond | 20% | 100 |

---

## Data Storage

### Excel Sheet-Per-Table Design

The workbook `data/SalesPipelineV1.0.xlsx` uses a structure that maps directly to database tables:

| Sheet | Purpose | Migration Target |
|-------|---------|------------------|
| **Deals** | All pipeline deals | `deals` table |
| **Lookups** | Postcode→Region, Stage→Probability | `lookups` table |
| **Metadata** | Version info, timestamps | `metadata` table |

This design ensures that migrating to Supabase (or PostgreSQL) requires only implementing a new `IDealRepository`—no changes to business logic or UI.

---

## Roadmap

### Phase 1: Foundation (Complete)

- [x] Core domain model with comprehensive deal tracking
- [x] Excel storage with sheet-per-table architecture
- [x] Interactive console with Spectre.Console
- [x] MCP server with full tool coverage
- [x] Synthetic data generation for testing
- [x] Daily Brief, Hygiene, and Forecast reports
- [x] Promoter/referral partner support with commission tracking
- [x] Promoter dashboard with actionable recommendations
- [x] .NET 10.0 upgrade

### Phase 2: Cloud Migration
- [ ] **Supabase Integration**: Implement `SupabaseDealRepository` for cloud-native storage
- [ ] **Real-time Sync**: Enable live updates across multiple clients
- [ ] **User Authentication**: Role-based access control for team environments
- [ ] **Audit Trail**: Track all changes with timestamps and user attribution

### Phase 3: Enhanced Analytics
- [ ] **Historical Tracking**: Store pipeline snapshots for trend analysis
- [ ] **Win/Loss Analysis**: Detailed breakdown of closed deals by reason
- [ ] **Velocity Metrics**: Track stage-to-stage conversion times
- [ ] **Custom Dashboards**: Configurable views for different roles

### Phase 4: Advanced AI Integration
- [ ] **Natural Language Queries**: "Show me all stale deals over £50k"
- [ ] **Intelligent Suggestions**: AI-powered next-step recommendations
- [ ] **Anomaly Detection**: Flag unusual patterns in pipeline data
- [ ] **Forecasting Models**: ML-based revenue predictions

### Phase 5: Integrations
- [ ] **CRM Sync**: Bi-directional sync with Salesforce, HubSpot
- [ ] **Email Integration**: Auto-log communications from Outlook/Gmail
- [ ] **Calendar Sync**: Create follow-up reminders from NextStepDueDate
- [ ] **Slack/Teams**: Report delivery and deal alerts

---

## Technology Stack

| Component | Technology |
|-----------|------------|
| **Runtime** | .NET 10.0 |
| **Storage** | ClosedXML (Excel), Supabase (planned) |
| **Console UI** | Spectre.Console |
| **MCP Protocol** | JSON-RPC 2.0 over stdio |
| **Testing** | xUnit, FluentAssertions |
| **Serialization** | System.Text.Json |

---

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Keep MCP tools deterministic (no LLM logic inside tool handlers)
- All business logic belongs in `Ox4D.Core`
- Maintain the repository abstraction for storage flexibility
- Add tests for new functionality

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**Built for sales teams who want AI-powered pipeline management without vendor lock-in.**

</div>
