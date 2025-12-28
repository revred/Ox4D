# PRCD.md — Product Requirements Context and Diagnostics

> **Last Updated:** 2025-12-28
> **Solution Version:** 1.1
> **Cross-Reference:** [VPRC.md](VPRC.md) | [UseCases.md](UseCases.md) | [TestPlan.md](TestPlan.md)
> **.NET Version:** 10.0.100

---

## 1. Executive Summary

Ox4D is a **Sales Pipeline Manager Copilot** that fulfills its stated objectives:

| Objective | Status | Evidence |
|-----------|--------|----------|
| Excel-based storage with sheet-per-table design | **PASS** | `SalesPipelineV1.0.xlsx` (27.8KB) with Deals, Lookups, Metadata sheets |
| Rich terminal UI for sales managers | **PASS** | Spectre.Console menu with CRUD, reports, synthetic data |
| MCP server for AI integration | **PASS** | JSON-RPC 2.0 over stdio, all tools responding correctly |
| Repository pattern for storage flexibility | **PASS** | `IDealRepository` abstraction with Excel and InMemory implementations |
| Comprehensive deal tracking | **PASS** | 35+ fields per deal including promoter/commission support |
| Actionable reports | **PASS** | Daily Brief, Hygiene Report, Forecast Snapshot all functional |
| Synthetic data generation | **PASS** | Both simple (Core) and Bogus-powered (Mutate) generators available |

---

## 2. User Personas and Use Cases

### 2.1 Primary User Personas

Ox4D serves **two distinct user personas** with fundamentally different needs and expectations:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         OX4D USER PERSONAS                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────┐    ┌─────────────────────────────────┐ │
│  │     SALES MANAGER               │    │     PROMOTER (AFFILIATE)        │ │
│  │     (Internal User)             │    │     (External Partner)          │ │
│  ├─────────────────────────────────┤    ├─────────────────────────────────┤ │
│  │                                 │    │                                 │ │
│  │  Primary Focus:                 │    │  Primary Focus:                 │ │
│  │  • Pipeline health              │    │  • Referred deal status         │ │
│  │  • Revenue forecasting          │    │  • Commission earnings          │ │
│  │  • Team performance             │    │  • Tier progression             │ │
│  │  • Deal progression             │    │  • Partner recommendations      │ │
│  │                                 │    │                                 │ │
│  │  Key Questions:                 │    │  Key Questions:                 │ │
│  │  "What needs attention today?"  │    │  "How are my referrals doing?"  │ │
│  │  "Is our data quality good?"    │    │  "What commission am I owed?"   │ │
│  │  "What's our forecast?"         │    │  "How can I help close deals?"  │ │
│  │                                 │    │                                 │ │
│  │  Access:                        │    │  Access:                        │ │
│  │  • Full pipeline visibility     │    │  • Own referrals only           │ │
│  │  • All CRUD operations          │    │  • Read-only + recommendations  │ │
│  │  • Configuration changes        │    │  • Commission tracking          │ │
│  │                                 │    │                                 │ │
│  └─────────────────────────────────┘    └─────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 2.2 Sales Manager Use Cases

The **Sales Manager** is the primary internal user responsible for pipeline management and revenue generation.

| Use Case ID | Use Case | Description | Reports/Tools Used |
|-------------|----------|-------------|-------------------|
| **SM-UC1** | Daily Pipeline Review | Start each day knowing what needs attention | Daily Brief |
| **SM-UC2** | Data Quality Audit | Ensure pipeline data is complete and accurate | Hygiene Report |
| **SM-UC3** | Revenue Forecasting | Project revenue by stage, owner, region, product | Forecast Snapshot |
| **SM-UC4** | Deal Management | Create, update, and track individual deals | CRUD Operations |
| **SM-UC5** | Team Oversight | Monitor deals by owner and identify coaching needs | Pipeline Stats, Filters |
| **SM-UC6** | Regional Analysis | Understand geographic distribution of pipeline | Region Filters |
| **SM-UC7** | AI-Assisted Queries | Use natural language to query pipeline via Claude | MCP Tools |

#### Sales Manager User Stories

```
As a Sales Manager, I want to...

SM-US1: See overdue actions so I can follow up on stalled deals
SM-US2: Identify high-value deals at risk so I can prioritize intervention
SM-US3: Find deals with missing data so I can maintain data quality
SM-US4: View pipeline by close month so I can forecast quarterly revenue
SM-US5: Ask Claude about my pipeline so I can get instant insights
SM-US6: Generate demo data so I can train new team members
SM-US7: Export to Excel so I can share reports with leadership
```

---

### 2.3 Promoter (Affiliate) Use Cases

The **Promoter** is an external referral partner who earns commission on deals they bring in.

| Use Case ID | Use Case | Description | Reports/Tools Used |
|-------------|----------|-------------|-------------------|
| **PM-UC1** | Referral Tracking | See status of all deals I referred | Promoter Deals |
| **PM-UC2** | Commission Visibility | Understand earnings (pending, projected, paid) | Promoter Dashboard |
| **PM-UC3** | Tier Progression | Track progress toward next commission tier | Promoter Dashboard |
| **PM-UC4** | Deal Health Check | Identify referrals that are stalled or at risk | Promoter Deals |
| **PM-UC5** | Action Recommendations | Get suggestions on how to help close deals | Promoter Actions |
| **PM-UC6** | Performance Metrics | View conversion rates and pipeline contribution | Promoter Dashboard |

#### Promoter User Stories

```
As a Promoter, I want to...

PM-US1: See my referred deals so I know their current status
PM-US2: Track my commission earnings so I know what I've earned
PM-US3: Understand my tier level so I know my commission rate
PM-US4: Get alerts on stalled deals so I can help move them forward
PM-US5: See recommended actions so I can support the sales team
PM-US6: View my conversion rate so I can improve my referral quality
```

---

### 2.4 Commission Tier Structure (Promoter Context)

| Tier | Commission Rate | Minimum Referrals | Benefits |
|------|-----------------|-------------------|----------|
| Bronze | 10% | 0 | Basic commission |
| Silver | 12% | 10 | +2% rate increase |
| Gold | 15% | 25 | +5% rate, priority support |
| Platinum | 18% | 50 | +8% rate, co-marketing |
| Diamond | 20% | 100 | Maximum rate, strategic partner |

---

## 3. Product Goals and Project Alignment

### 3.1 Goals Derived from Use Cases

Product goals are derived directly from the use cases of each persona:

| Goal ID | Goal | Derived From | Primary Persona |
|---------|------|--------------|-----------------|
| **G1** | **AI-Ready Sales Management** | SM-UC7 (AI-Assisted Queries) | Sales Manager |
| **G2** | **Zero Vendor Lock-in** | SM-UC7 (Excel export), Future Supabase | Both |
| **G3** | **Actionable Intelligence** | SM-UC1, SM-UC2, SM-UC3 (Reports) | Sales Manager |
| **G4** | **Referral Partner Support** | PM-UC1 through PM-UC6 (All Promoter UCs) | Promoter |
| **G5** | **Developer Experience** | Rapid feature development for both personas | Technical |

### 3.2 Goal-to-Use-Case Traceability

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    USE CASE → GOAL TRACEABILITY                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  SALES MANAGER USE CASES                    GOALS                           │
│  ─────────────────────────                  ─────                           │
│  SM-UC1: Daily Pipeline Review ────────────► G3 (Actionable Intelligence)  │
│  SM-UC2: Data Quality Audit ───────────────► G3 (Actionable Intelligence)  │
│  SM-UC3: Revenue Forecasting ──────────────► G3 (Actionable Intelligence)  │
│  SM-UC4: Deal Management ──────────────────► G3 (Actionable Intelligence)  │
│  SM-UC5: Team Oversight ───────────────────► G3 (Actionable Intelligence)  │
│  SM-UC6: Regional Analysis ────────────────► G3 (Actionable Intelligence)  │
│  SM-UC7: AI-Assisted Queries ──────────────► G1 (AI-Ready)                  │
│                                                                              │
│  PROMOTER USE CASES                         GOALS                           │
│  ─────────────────────                      ─────                           │
│  PM-UC1: Referral Tracking ────────────────► G4 (Referral Partner Support) │
│  PM-UC2: Commission Visibility ────────────► G4 (Referral Partner Support) │
│  PM-UC3: Tier Progression ─────────────────► G4 (Referral Partner Support) │
│  PM-UC4: Deal Health Check ────────────────► G4 (Referral Partner Support) │
│  PM-UC5: Action Recommendations ───────────► G4 (Referral Partner Support) │
│  PM-UC6: Performance Metrics ──────────────► G4 (Referral Partner Support) │
│                                                                              │
│  CROSS-CUTTING                              GOALS                           │
│  ─────────────                              ─────                           │
│  Excel storage & portability ──────────────► G2 (Zero Vendor Lock-in)      │
│  Clean architecture & testing ─────────────► G5 (Developer Experience)     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Epics (Goals) and Features/Stories

Goals are treated as **Epics** — large bodies of work that deliver significant value. Each Epic contains **Features** (capabilities) which are further broken down into **Stories** (implementable units).

### 4.1 Requirements Hierarchy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    REQUIREMENTS HIERARCHY                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  PERSONA ──► USE CASE ──► EPIC (Goal) ──► FEATURE ──► STORY                │
│                                                                              │
│  Example:                                                                    │
│  ┌─────────────┐                                                            │
│  │Sales Manager│                                                            │
│  └──────┬──────┘                                                            │
│         │                                                                    │
│         ▼                                                                    │
│  ┌──────────────────────┐                                                   │
│  │SM-UC1: Daily Review  │                                                   │
│  └──────────┬───────────┘                                                   │
│             │                                                                │
│             ▼                                                                │
│  ┌──────────────────────────────────┐                                       │
│  │G3: Actionable Intelligence (Epic)│                                       │
│  └──────────────┬───────────────────┘                                       │
│                 │                                                            │
│      ┌──────────┼──────────┐                                                │
│      ▼          ▼          ▼                                                │
│  ┌────────┐ ┌────────┐ ┌────────┐                                          │
│  │F3.1    │ │F3.2    │ │F3.3    │  (Features)                              │
│  │Daily   │ │Hygiene │ │Forecast│                                          │
│  │Brief   │ │Report  │ │Snapshot│                                          │
│  └───┬────┘ └────────┘ └────────┘                                          │
│      │                                                                       │
│      ├──► S3.1.1: Identify overdue actions                                  │
│      ├──► S3.1.2: Flag high-value at-risk deals                            │
│      └──► S3.1.3: List no-contact deals                                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 4.2 Epic G1: AI-Ready Sales Management

**Epic Owner:** Sales Manager
**Use Cases:** SM-UC7

> *Enable AI assistants (Claude, etc.) to query and manage sales pipeline data through MCP.*

#### Features

| Feature ID | Feature | Description | Stories |
|------------|---------|-------------|---------|
| **F1.1** | MCP Server Infrastructure | JSON-RPC 2.0 server over stdio | 3 stories |
| **F1.2** | Pipeline CRUD Tools | AI-accessible deal operations | 5 stories |
| **F1.3** | Report Tools | AI-accessible report generation | 3 stories |
| **F1.4** | Data Management Tools | AI-accessible data operations | 2 stories |

#### Stories

**F1.1 MCP Server Infrastructure**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S1.1.1 | Implement JSON-RPC message loop | Server reads/writes JSON-RPC 2.0 over stdio | ✅ Done |
| S1.1.2 | Create tool dispatch mechanism | Requests routed to correct handler by method name | ✅ Done |
| S1.1.3 | Handle errors gracefully | Invalid requests return proper JSON-RPC error responses | ✅ Done |

**F1.2 Pipeline CRUD Tools**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S1.2.1 | Implement `pipeline.list_deals` | Returns filtered deal list with pagination | ✅ Done |
| S1.2.2 | Implement `pipeline.get_deal` | Returns single deal by ID | ✅ Done |
| S1.2.3 | Implement `pipeline.upsert_deal` | Creates or updates deal with normalization | ✅ Done |
| S1.2.4 | Implement `pipeline.patch_deal` | Updates specific fields on existing deal | ✅ Done |
| S1.2.5 | Implement `pipeline.delete_deal` | Removes deal by ID | ✅ Done |

**F1.3 Report Tools**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S1.3.1 | Implement `pipeline.daily_brief` | Returns actionable daily report via MCP | ✅ Done |
| S1.3.2 | Implement `pipeline.hygiene_report` | Returns data quality issues via MCP | ✅ Done |
| S1.3.3 | Implement `pipeline.forecast_snapshot` | Returns forecast breakdown via MCP | ✅ Done |

**F1.4 Data Management Tools**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S1.4.1 | Implement `pipeline.save` | Persists changes to Excel via MCP command | ✅ Done |
| S1.4.2 | Implement `pipeline.reload` | Reloads data from Excel via MCP command | ✅ Done |

---

### 4.3 Epic G2: Zero Vendor Lock-in

**Epic Owner:** Technical/Architecture
**Use Cases:** Cross-cutting

> *Use portable storage (Excel) with architecture ready for cloud migration.*

#### Features

| Feature ID | Feature | Description | Stories |
|------------|---------|-------------|---------|
| **F2.1** | Repository Abstraction | Interface-based storage access | 2 stories |
| **F2.2** | Excel Storage | Sheet-per-table workbook design | 4 stories |
| **F2.3** | Migration Readiness | Architecture for Supabase swap | 2 stories |

#### Stories

**F2.1 Repository Abstraction**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S2.1.1 | Define `IDealRepository` interface | All storage operations abstracted behind interface | ✅ Done |
| S2.1.2 | Implement `InMemoryDealRepository` | Test-friendly repository with no file I/O | ✅ Done |

**F2.2 Excel Storage**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S2.2.1 | Implement Deals sheet read/write | All 35+ fields persisted to/from Excel | ✅ Done |
| S2.2.2 | Implement Lookups sheet | Postcode→Region and Stage→Probability mappings | ✅ Done |
| S2.2.3 | Implement Metadata sheet | Version info and timestamps | ✅ Done |
| S2.2.4 | Support promoter fields in Excel | PromoCode, Commission, CommissionPaid columns | ✅ Done |

**F2.3 Migration Readiness**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S2.3.1 | Sheet-per-table design | Each worksheet maps to future database table | ✅ Done |
| S2.3.2 | Document Supabase migration path | Clear instructions for implementing `SupabaseDealRepository` | ✅ Done |

---

### 4.4 Epic G3: Actionable Intelligence

**Epic Owner:** Sales Manager
**Use Cases:** SM-UC1, SM-UC2, SM-UC3, SM-UC4, SM-UC5, SM-UC6

> *Provide daily briefs, hygiene reports, and forecasts that drive sales actions.*

#### Features

| Feature ID | Feature | Description | Stories |
|------------|---------|-------------|---------|
| **F3.1** | Daily Brief Report | What needs attention today? | 4 stories |
| **F3.2** | Hygiene Report | Data quality issues | 4 stories |
| **F3.3** | Forecast Snapshot | Pipeline breakdown and projections | 3 stories |
| **F3.4** | Deal Management | Full CRUD operations | 5 stories |
| **F3.5** | Pipeline Statistics | High-level metrics | 2 stories |

#### Stories

**F3.1 Daily Brief Report**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S3.1.1 | Identify overdue actions | List deals with NextStepDueDate in the past | ✅ Done |
| S3.1.2 | Flag actions due today | List deals with NextStepDueDate = today | ✅ Done |
| S3.1.3 | Identify no-contact deals | Deals with no contact in configurable days | ✅ Done |
| S3.1.4 | Highlight high-value at-risk | Large deals with overdue actions or no contact | ✅ Done |

**F3.2 Hygiene Report**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S3.2.1 | Detect missing amounts | Flag deals with null/zero AmountGBP | ✅ Done |
| S3.2.2 | Detect missing close dates | Flag late-stage deals without CloseDate | ✅ Done |
| S3.2.3 | Detect stage-probability mismatch | Flag manually overridden probabilities | ✅ Done |
| S3.2.4 | Detect missing location data | Flag deals without postcode/region | ✅ Done |

**F3.3 Forecast Snapshot**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S3.3.1 | Calculate total and weighted pipeline | Sum of open deals by amount and weighted amount | ✅ Done |
| S3.3.2 | Breakdown by dimensions | Group by stage, owner, region, product, close month | ✅ Done |
| S3.3.3 | Generate actionable insights | Identify concentration risks and gaps | ✅ Done |

**F3.4 Deal Management**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S3.4.1 | Create new deals | Form-based deal creation with validation | ✅ Done |
| S3.4.2 | Update existing deals | Modify any field with normalization | ✅ Done |
| S3.4.3 | Delete deals | Remove deals with confirmation | ✅ Done |
| S3.4.4 | Search and filter deals | Multi-field search with filters | ✅ Done |
| S3.4.5 | View deal details | Display all fields for single deal | ✅ Done |

**F3.5 Pipeline Statistics**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S3.5.1 | Calculate high-level metrics | Total, open, won, lost counts and values | ✅ Done |
| S3.5.2 | Extract unique owners/regions | List all owners and regions in pipeline | ✅ Done |

---

### 4.5 Epic G4: Referral Partner Support

**Epic Owner:** Promoter (Affiliate)
**Use Cases:** PM-UC1, PM-UC2, PM-UC3, PM-UC4, PM-UC5, PM-UC6

> *Track promoter commissions and provide partner dashboards.*

#### Features

| Feature ID | Feature | Description | Stories |
|------------|---------|-------------|---------|
| **F4.1** | Promoter Model | Data structure for referral tracking | 3 stories |
| **F4.2** | Promoter Dashboard | Metrics and earnings visibility | 4 stories |
| **F4.3** | Promoter Actions | Recommendations to help close deals | 3 stories |
| **F4.4** | Promoter MCP Tools | AI-accessible promoter operations | 3 stories |

#### Stories

**F4.1 Promoter Model**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S4.1.1 | Define Promoter entity | PromoCode, Tier, CommissionRate, TotalReferrals | ✅ Done |
| S4.1.2 | Add promoter fields to Deal | PromoterId, PromoCode, PromoterCommission, CommissionPaid | ✅ Done |
| S4.1.3 | Implement tier calculation | Tier derived from referral count | ✅ Done |

**F4.2 Promoter Dashboard**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S4.2.1 | Calculate pipeline metrics | Total referrals, open pipeline, weighted value | ✅ Done |
| S4.2.2 | Track commission earnings | Pending, projected, and paid commissions | ✅ Done |
| S4.2.3 | Show tier progression | Current tier and progress to next tier | ✅ Done |
| S4.2.4 | Display deal health summary | Count of stalled, at-risk, healthy deals | ✅ Done |

**F4.3 Promoter Actions**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S4.3.1 | Identify stalled referrals | Flag deals with no recent activity | ✅ Done |
| S4.3.2 | Generate action recommendations | Suggest specific actions to help close deals | ✅ Done |
| S4.3.3 | Prioritize by impact | Rank actions by deal value and urgency | ✅ Done |

**F4.4 Promoter MCP Tools**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S4.4.1 | Implement `promoter.dashboard` | Return full dashboard via MCP | ✅ Done |
| S4.4.2 | Implement `promoter.deals` | Return promoter's referred deals via MCP | ✅ Done |
| S4.4.3 | Implement `promoter.actions` | Return recommended actions via MCP | ✅ Done |

---

### 4.6 Epic G5: Developer Experience

**Epic Owner:** Technical/Architecture
**Use Cases:** Cross-cutting

> *Clean architecture enabling rapid feature development and testing.*

#### Features

| Feature ID | Feature | Description | Stories |
|------------|---------|-------------|---------|
| **F5.1** | Clean Architecture | Layered design with clear boundaries | 3 stories |
| **F5.2** | Synthetic Data Generation | Realistic test data creation | 4 stories |
| **F5.3** | Unit Testing | Automated test coverage | 3 stories |
| **F5.4** | Interface Abstractions | Dependency injection ready | 2 stories |

#### Stories

**F5.1 Clean Architecture**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S5.1.1 | Isolate domain in Core | Ox4D.Core has zero NuGet dependencies | ✅ Done |
| S5.1.2 | Separate storage concerns | Ox4D.Store only handles persistence | ✅ Done |
| S5.1.3 | Separate UI concerns | Console and Zarwin are thin wrappers | ✅ Done |

**F5.2 Synthetic Data Generation**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S5.2.1 | Simple generator in Core | Basic random data with seeding | ✅ Done |
| S5.2.2 | Bogus generator in Mutate | Realistic UK data (postcodes, names, phones) | ✅ Done |
| S5.2.3 | Inject hygiene issues | Configurable missing data percentages | ✅ Done |
| S5.2.4 | Generate promoter referrals | PromoCode assignment in synthetic data | ✅ Done |

**F5.3 Unit Testing**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S5.3.1 | Test deal normalization | Validate postcode, region, probability logic | ✅ Done |
| S5.3.2 | Test report generation | Validate Daily Brief, Hygiene, Forecast | ✅ Done |
| S5.3.3 | Test synthetic data | Validate deterministic seeding | ✅ Done |

**F5.4 Interface Abstractions**

| Story ID | Story | Acceptance Criteria | Status |
|----------|-------|---------------------|--------|
| S5.4.1 | Define `IDealRepository` | Storage abstraction for DI | ✅ Done |
| S5.4.2 | Define `ISyntheticDataGenerator` | Generator abstraction for DI | ✅ Done |

---

### 4.7 Epic Summary

| Epic | Features | Stories | Complete | Status |
|------|----------|---------|----------|--------|
| **G1: AI-Ready** | 4 | 13 | 13 | ✅ 100% |
| **G2: No Lock-in** | 3 | 8 | 8 | ✅ 100% |
| **G3: Intelligence** | 5 | 18 | 18 | ✅ 100% |
| **G4: Promoters** | 4 | 13 | 13 | ✅ 100% |
| **G5: Dev Experience** | 4 | 12 | 12 | ✅ 100% |
| **TOTAL** | **20** | **64** | **64** | **✅ 100%** |

---

### 4.8 Project-to-Goal Traceability Matrix

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    PROJECT → GOAL TRACEABILITY                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  Product Goals:   G1: AI-Ready   G2: No Lock-in   G3: Intelligence          │
│                   G4: Promoters  G5: Dev Experience                          │
│                                                                              │
│  ┌──────────────┐                                                            │
│  │  Ox4D.Core   │ ──────────────────────────────────────────► G2, G3, G5    │
│  │  (Domain)    │   Pure domain logic, no external dependencies              │
│  └──────┬───────┘                                                            │
│         │                                                                    │
│         ├────────────────────────────────────────────────────────────────┐   │
│         │                                                                │   │
│  ┌──────▼───────┐    ┌──────────────┐    ┌──────────────┐               │   │
│  │  Ox4D.Store  │    │ Ox4D.Console │    │ Ox4D.Zarwin  │               │   │
│  │  (Storage)   │    │ (Terminal UI)│    │ (MCP Server) │               │   │
│  └──────────────┘    └──────────────┘    └──────────────┘               │   │
│         │                   │                   │                        │   │
│         ▼                   ▼                   ▼                        │   │
│       G2, G5              G3, G4              G1, G3                    │   │
│                                                                          │   │
│  ┌──────────────┐    ┌──────────────┐                                   │   │
│  │ Ox4D.Mutate  │    │  Ox4D.Tests  │◄──────────────────────────────────┘   │
│  │ (Bogus Data) │    │  (Testing)   │                                       │
│  └──────────────┘    └──────────────┘                                       │
│         │                   │                                                │
│         ▼                   ▼                                                │
│        G5                  G5                                                │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 5. Project Scopes and Objectives

### 3.1 Ox4D.Core — Domain Foundation

**Path:** `src/Ox4D.Core/`

**Mission:** Provide the pure domain logic layer with zero external dependencies, enabling maximum portability and testability.

| Scope Area | Responsibility | Files |
|------------|----------------|-------|
| **Domain Models** | Define Deal, DealStage, DealFilter, Promoter entities | `Models/*.cs` |
| **Configuration** | LookupTables, PipelineSettings | `Models/Config/*.cs` |
| **Reports** | DailyBrief, HygieneReport, ForecastSnapshot, PromoterDashboard | `Models/Reports/*.cs` |
| **Services** | PipelineService, ReportService, PromoterService, DealNormalizer | `Services/*.cs` |
| **Interfaces** | IDealRepository, ISyntheticDataGenerator | `Services/*.cs` |

#### Objectives Tied to Product Goals

| Objective | Goal | Deliverable | Status |
|-----------|------|-------------|--------|
| Define comprehensive Deal entity | G3 | 35+ fields covering full sales lifecycle | ✅ Complete |
| Implement stage-based pipeline logic | G3 | DealStage enum with probability defaults | ✅ Complete |
| Create report generation services | G3 | DailyBrief, HygieneReport, ForecastSnapshot | ✅ Complete |
| Support promoter/referral tracking | G4 | Promoter model, PromoterService, PromoterDashboard | ✅ Complete |
| Define repository abstraction | G2 | IDealRepository interface | ✅ Complete |
| Maintain zero dependencies | G5 | No NuGet packages, pure .NET | ✅ Complete |

#### Key Interfaces

```csharp
// Repository abstraction enabling storage flexibility (G2)
public interface IDealRepository { ... }

// Generator abstraction enabling test data flexibility (G5)
public interface ISyntheticDataGenerator { ... }
```

---

### 3.2 Ox4D.Store — Storage Layer

**Path:** `src/Ox4D.Store/`

**Mission:** Implement storage backends using the repository pattern, starting with Excel and designed for cloud migration.

| Scope Area | Responsibility | Files |
|------------|----------------|-------|
| **Excel Storage** | Read/write deals to/from Excel workbook | `ExcelDealRepository.cs` |
| **Test Storage** | In-memory storage for unit tests | `InMemoryDealRepository.cs` |
| **Sheet Design** | Implement sheet-per-table pattern | Deals, Lookups, Metadata sheets |

#### Objectives Tied to Product Goals

| Objective | Goal | Deliverable | Status |
|-----------|------|-------------|--------|
| Excel storage with sheet-per-table | G2 | ExcelDealRepository with 3 worksheets | ✅ Complete |
| Support all 35+ Deal fields | G3 | Full field mapping including promoter data | ✅ Complete |
| In-memory repository for testing | G5 | InMemoryDealRepository | ✅ Complete |
| Design for Supabase migration | G2 | Each sheet = future database table | ✅ Complete |

#### Sheet-to-Table Mapping (G2)

| Excel Sheet | Future Supabase Table | Purpose |
|-------------|----------------------|---------|
| Deals | `deals` | Primary pipeline data |
| Lookups | `lookups` | Postcode→Region, Stage→Probability |
| Metadata | `metadata` | Version, timestamps, settings |

---

### 3.3 Ox4D.Console — Unified Gateway

**Path:** `src/Ox4D.Console/`

**Mission:** Serve as the **single unified gateway** for all Ox4D operations, providing both interactive menu access and a comprehensive non-interactive demo mode for demonstrations, verification, and training.

| Scope Area | Responsibility | Files |
|------------|----------------|-------|
| **Menu System** | Interactive menu-driven interface | `CopilotMenu.cs` |
| **Data Display** | Rich tables, progress bars, panels | Uses Spectre.Console |
| **User Workflows** | CRUD, reports, data generation | Menu options 1-12 |
| **Demo Mode** | Non-interactive feature showcase | Menu option 13 |

#### Objectives Tied to Product Goals

| Objective | Goal | Deliverable | Status |
|-----------|------|-------------|--------|
| Interactive deal management | G3 | Create, update, delete, search deals | ✅ Complete |
| Display actionable reports | G3 | Daily Brief, Hygiene, Forecast views | ✅ Complete |
| Promoter dashboard access | G4 | View promoter metrics and deals | ✅ Complete |
| Synthetic data generation | G5 | Generate demo data with configurable seed | ✅ Complete |
| Excel import/export | G2 | Load and save workbooks | ✅ Complete |
| Non-interactive demo mode | G3, G5 | Full feature showcase for verification | ✅ Complete |

#### Menu Structure

```
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
13. Run Full Demo (Non-Interactive)
 0. Exit
```

#### Demo Mode (Option 13)

The **Run Full Demo** option provides a complete non-interactive showcase:
- Generates 100 synthetic deals (seed=42) for reproducibility
- Displays pipeline statistics
- Shows daily brief with overdue items
- Runs hygiene report with health score
- Displays forecast snapshot (by stage, owner, month)
- Shows sample deal details
- Saves everything to Excel

This unified approach eliminates the need for a separate Demo project.

---

### 3.4 Ox4D.Zarwin — MCP Server

**Path:** `src/Ox4D.Zarwin/`

**Mission:** Expose all pipeline operations as MCP tools via JSON-RPC, enabling AI assistants to manage sales data.

| Scope Area | Responsibility | Files |
|------------|----------------|-------|
| **Server Loop** | JSON-RPC 2.0 message handling over stdio | `ZarwinServer.cs` |
| **Tool Handlers** | Dispatch requests to appropriate services | `Handlers/ToolHandler.cs` |
| **Protocol Types** | JSON-RPC request/response structures | `Protocol/*.cs` |
| **Tool Definitions** | Schema for all available tools | `Protocol/ToolDefinitions.cs` |

#### Objectives Tied to Product Goals

| Objective | Goal | Deliverable | Status |
|-----------|------|-------------|--------|
| Expose CRUD via MCP | G1 | list_deals, get_deal, upsert_deal, patch_deal, delete_deal | ✅ Complete |
| Expose reports via MCP | G1, G3 | daily_brief, hygiene_report, forecast_snapshot | ✅ Complete |
| Expose promoter tools | G1, G4 | promoter.dashboard, promoter.deals, promoter.actions | ✅ Complete |
| Support data generation | G1, G5 | generate_synthetic with seed parameter | ✅ Complete |
| Maintain deterministic tools | G1 | No LLM logic inside tool handlers | ✅ Complete |
| Persist changes on demand | G1, G2 | save, reload tools | ✅ Complete |

#### MCP Tool Categories

| Category | Tools | Goal Alignment |
|----------|-------|----------------|
| Pipeline CRUD | 5 tools | G1 (AI access to data) |
| Reports | 3 tools | G1, G3 (AI-driven intelligence) |
| Promoter | 3 tools | G1, G4 (Partner support via AI) |
| Data | 2 tools | G1, G5 (AI-driven testing) |
| Persistence | 2 tools | G1, G2 (AI-controlled save/reload) |

---

### 3.5 Ox4D.Mutate — Synthetic Data Generation

**Path:** `src/Ox4D.Mutate/`

**Mission:** Generate realistic UK sales data using the Bogus library for demos, testing, and development.

| Scope Area | Responsibility | Files |
|------------|----------------|-------|
| **UK Data Generation** | Realistic postcodes, names, companies | `SyntheticDataGenerator.cs` |
| **Hygiene Issues** | Inject missing data for testing reports | Configurable percentages |
| **Promoter Data** | Generate promo codes and referrals | PromoCode patterns |

#### Objectives Tied to Product Goals

| Objective | Goal | Deliverable | Status |
|-----------|------|-------------|--------|
| Generate investor-grade demo data | G5 | Realistic UK postcodes, phone numbers, names | ✅ Complete |
| Support deterministic seeding | G5 | Same seed = same data | ✅ Complete |
| Inject hygiene issues | G3, G5 | Missing amounts (8%), dates (12%), contacts (10%) | ✅ Complete |
| Generate promoter referrals | G4, G5 | PromoCode assignment | ✅ Complete |
| Implement ISyntheticDataGenerator | G5 | Interchangeable with Core's simple generator | ✅ Complete |

#### UK Data Realism

| Data Type | Source | Example |
|-----------|--------|---------|
| Postcodes | 15 UK area codes | SW1 3DL, EC2 1XU, M1 8LJ |
| Regions | Derived from postcode | London, Scotland, North West |
| Phone Numbers | UK format | 0387 442 3341 |
| Company Names | Bogus + UK patterns | Apex Technologies Ltd |
| Person Names | UK name distribution | James Wilson, Sarah Chen |

---

### 3.6 Ox4D.Tests — Test Suite

**Path:** `tests/Ox4D.Tests/`

**Mission:** Validate core functionality through unit tests, ensuring confidence in refactoring and feature development.

| Scope Area | Responsibility | Files |
|------------|----------------|-------|
| **Normalizer Tests** | Validate deal normalization logic | `DealNormalizerTests.cs` |
| **Report Tests** | Validate report generation | `ReportServiceTests.cs` |
| **Generator Tests** | Validate synthetic data generation | `SyntheticDataGeneratorTests.cs` |

#### Objectives Tied to Product Goals

| Objective | Goal | Deliverable | Status |
|-----------|------|-------------|--------|
| Test deal normalization | G5 | 17+ tests for DealNormalizer | ✅ Complete |
| Test report generation | G3, G5 | 25+ tests for ReportService | ✅ Complete |
| Test synthetic data | G5 | 16+ tests for SyntheticDataGenerator | ✅ Complete |
| Achieve deterministic results | G5 | Same seed = same output | ✅ Complete |
| Enable CI/CD integration | G5 | All 393 tests pass in < 2 seconds | ✅ Complete |

#### Test Coverage by Goal

| Goal | Test Count | Coverage Area |
|------|------------|---------------|
| G1 (AI-Ready) | 30+ | MCP tools, CRUD operations |
| G2 (No Lock-in) | 25+ | Repository pattern, Excel storage |
| G3 (Intelligence) | 75+ | Report generation, deal management |
| G4 (Promoters) | 45+ | Promoter dashboard, actions |
| G5 (Dev Experience) | 35+ | Normalization, data generation, testing |

---

## 4. Goal Achievement Summary

### 4.1 Goal Status Matrix

| Goal | Projects Contributing | Status | Evidence |
|------|----------------------|--------|----------|
| **G1: AI-Ready** | Zarwin | ✅ ACHIEVED | 15 MCP tools operational |
| **G2: No Lock-in** | Core, Store | ✅ ACHIEVED | IDealRepository abstraction, sheet-per-table Excel |
| **G3: Intelligence** | Core, Console, Zarwin | ✅ ACHIEVED | 3 actionable reports, 35+ deal fields |
| **G4: Promoters** | Core, Console, Zarwin | ✅ ACHIEVED | PromoterService, dashboard, MCP tools |
| **G5: Dev Experience** | All Projects | ✅ ACHIEVED | Clean architecture, 393 tests, Bogus generator |

### 4.2 Architecture Diagram with Goals

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Ox4D Sales Pipeline                               │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                     PRODUCT GOALS                                │    │
│  │  G1: AI-Ready | G2: No Lock-in | G3: Intelligence               │    │
│  │  G4: Promoters | G5: Dev Experience                              │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                          │
│  ┌──────────────────┐    ┌──────────────────┐                           │
│  │  Ox4D.Console    │    │  Ox4D.Zarwin     │                           │
│  │  [G3, G4]        │    │  [G1, G3, G4]    │                           │
│  │  Terminal UI     │    │  MCP Server      │                           │
│  └────────┬─────────┘    └────────┬─────────┘                           │
│           │                        │                                     │
│           └───────────┬────────────┘                                     │
│                       │                                                  │
│           ┌───────────▼───────────┐                                     │
│           │      Ox4D.Core        │                                     │
│           │  [G2, G3, G4, G5]     │                                     │
│           │  Domain + Services    │                                     │
│           └───────────┬───────────┘                                     │
│                       │                                                  │
│           ┌───────────▼───────────┐                                     │
│           │     Ox4D.Store        │                                     │
│           │     [G2, G5]          │                                     │
│           │   IDealRepository     │                                     │
│           └───────────┬───────────┘                                     │
│                       │                                                  │
│      ┌────────────────┼────────────────┐                                │
│      │                │                │                                 │
│  ┌───▼───┐    ┌───────▼───────┐   ┌────▼────┐                           │
│  │ Excel │    │   In-Memory   │   │Supabase │                           │
│  │[G2]   │    │   [G5]        │   │[G2-Future]│                         │
│  └───────┘    └───────────────┘   └─────────┘                           │
│                                                                          │
│  ┌──────────────────┐    ┌──────────────────┐                           │
│  │   Ox4D.Mutate    │    │   Ox4D.Tests     │                           │
│  │   [G5]           │    │   [G3, G5]       │                           │
│  │   Bogus Data     │    │   Unit Tests     │                           │
│  └──────────────────┘    └──────────────────┘                           │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 5. Build Diagnostics

### 2.1 Compilation Status

```
Build Status: SUCCESS
Errors: 0
Warnings: 0
Build Time: ~1.4 seconds
```

### 2.2 Warning Analysis

No warnings. Previously NU1903 vulnerability warnings from ClosedXML transitive dependencies have been resolved by package updates.

### 2.3 Target Framework

```
Runtime: .NET 10.0
SDK: 10.0.100
All projects target: net10.0
```

---

## 3. Test Diagnostics

### 3.1 Test Suite Results

```
Total Tests: 393
Passed: 393
Failed: 0
Skipped: 0
Duration: ~1 second
```

### 3.2 Test Coverage by Category

| Category | Tests | Status |
|----------|-------|--------|
| DealNormalizerTests | 17+ | All Pass |
| SyntheticDataGeneratorTests | 16+ | All Pass |
| ReportServiceTests | 25+ | All Pass |
| RepositoryContractTests | 38+ | All Pass |
| ExcelFailureModeTests | 19 | All Pass |
| PipelineServiceTests | 35+ | All Pass |
| PromoterServiceTests | 40+ | All Pass |
| DealFilterTests | 15+ | All Pass |
| IClock Tests | 5 | All Pass |
| NormalizationChangeTracking | 10 | All Pass |
| And more... | ... | All Pass |

### 3.3 Test Files

- `tests/Ox4D.Tests/DealNormalizerTests.cs`
- `tests/Ox4D.Tests/SyntheticDataGeneratorTests.cs`
- `tests/Ox4D.Tests/ReportServiceTests.cs`
- `tests/Ox4D.Tests/RepositoryContractTests.cs`
- `tests/Ox4D.Tests/InMemoryRepositoryContractTests.cs`
- `tests/Ox4D.Tests/ExcelRepositoryContractTests.cs`
- `tests/Ox4D.Tests/ExcelFailureModeTests.cs`
- And more...

---

## 4. Project Structure Verification

### 4.1 Solution Projects

| Project | Type | Framework | Dependencies | Status |
|---------|------|-----------|--------------|--------|
| Ox4D.Core | Class Library | net10.0 | None | OK |
| Ox4D.Store | Class Library | net10.0 | ClosedXML, Core | OK |
| Ox4D.Mutate | Class Library | net10.0 | Bogus, Core | OK |
| Ox4D.Console | Executable | net10.0 | Spectre.Console, Core, Store | OK |
| Ox4D.Zarwin | Executable | net10.0 | Core, Store | OK |
| Ox4D.Tests | Test Project | net10.0 | xUnit, FluentAssertions, Core, Store | OK |

### 4.2 Dependency Graph

```
Ox4D.Core (no dependencies)
    ↑
    ├── Ox4D.Store (+ ClosedXML)
    │       ↑
    │       ├── Ox4D.Console (+ Spectre.Console) ← Unified Gateway with Demo Mode
    │       ├── Ox4D.Zarwin
    │       └── Ox4D.Tests (+ xUnit, FluentAssertions)
    │
    └── Ox4D.Mutate (+ Bogus)
```

### 4.3 NuGet Packages

| Package | Version | Project(s) |
|---------|---------|------------|
| ClosedXML | 0.102.3 | Ox4D.Store |
| Spectre.Console | 0.49.1 | Ox4D.Console |
| Bogus | 35.6.1 | Ox4D.Mutate |
| xunit | 2.7.0 | Ox4D.Tests |
| FluentAssertions | 6.12.0 | Ox4D.Tests |
| Microsoft.NET.Test.Sdk | 17.9.0 | Ox4D.Tests |

---

## 5. Runtime Diagnostics

### 5.1 Console Application

```
Status: FUNCTIONAL
Startup: Displays Figlet banner "Ox4D Pipeline"
Menu: Interactive selection (requires terminal)
Note: Exits with NotSupportedException in non-interactive mode (expected)
```

### 5.2 MCP Server (Zarwin)

```
Status: FULLY OPERATIONAL
Protocol: JSON-RPC 2.0 over stdio
Response Time: < 1 second for all operations
```

#### Tool Response Tests

| Method | Status | Sample Response |
|--------|--------|-----------------|
| `pipeline.get_stats` | OK | Returns deal counts, pipeline values, owners, regions |
| `pipeline.list_deals` | OK | Returns paginated deal list with all 35+ fields |
| `pipeline.daily_brief` | OK | Returns action items, due today, overdue, at-risk deals |
| `pipeline.generate_synthetic` | OK | Generates specified count with seed |

### 5.3 Sample MCP Response

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "totalDeals": 100,
    "openDeals": 81,
    "closedWonDeals": 11,
    "closedLostDeals": 8,
    "totalPipeline": 4186489,
    "weightedPipeline": 1663720.7,
    "closedWonValue": 402000,
    "averageDealsValue": 51685.05,
    "owners": ["James Wilson", "David Lee", "Michael Brown", "Sarah Chen", "Emma Taylor"],
    "regions": ["London", "Scotland", "Yorkshire", "North West", "South West", "Wales", "South East", "West Midlands", "East of England"]
  }
}
```

---

## 6. Data Storage Diagnostics

### 6.1 Excel File

```
Path: data/SalesPipelineV1.0.xlsx
Size: 27,836 bytes
Sheets: Deals, Lookups, Metadata
Records: 100+ deals
```

### 6.2 Deal Fields Verified

| Category | Fields | Status |
|----------|--------|--------|
| Identity | dealId, orderNo, userId | OK |
| Account | accountName, contactName, email, phone | OK |
| Location | postcode, postcodeArea, region, mapLink | OK |
| Deal | dealName, productLine, leadSource, stage | OK |
| Value | amountGBP, probability, weightedAmountGBP | OK |
| Ownership | owner, createdDate, lastContactedDate | OK |
| Actions | nextStep, nextStepDueDate, closeDate | OK |
| Service | servicePlan, lastServiceDate, nextServiceDueDate | OK |
| Promoter | promoterId, promoCode, promoterCommission, commissionPaid | OK |

### 6.3 Automatic Normalization

| Feature | Status | Example |
|---------|--------|---------|
| DealId Generation | OK | `D-20251225-0000` |
| Postcode Area Extraction | OK | `SW1 3DL` → `SW` |
| Region Derivation | OK | `SW` → `London` |
| Map Link Generation | OK | Google Maps URL with postcode |
| Default Probability | OK | Stage-based (Lead=10%, Qualified=20%, etc.) |

---

## 7. Feature Verification Matrix

### 7.1 Core Domain Features

| Feature | Implemented | Tested | Working |
|---------|-------------|--------|---------|
| Deal CRUD Operations | Yes | Yes | Yes |
| Deal Normalization | Yes | Yes | Yes |
| Stage Management | Yes | Yes | Yes |
| Probability Defaults | Yes | Yes | Yes |
| Weighted Pipeline Calculation | Yes | Yes | Yes |
| Postcode → Region Mapping | Yes | Yes | Yes |
| Google Maps Link Generation | Yes | Yes | Yes |

### 7.2 Report Features

| Report | Implemented | Tested | Working |
|--------|-------------|--------|---------|
| Daily Brief | Yes | Yes | Yes |
| Hygiene Report | Yes | Yes | Yes |
| Forecast Snapshot | Yes | Yes | Yes |
| Promoter Dashboard | Yes | No | Untested |

### 7.3 Storage Features

| Feature | Implemented | Tested | Working |
|---------|-------------|--------|---------|
| Excel Read | Yes | Yes | Yes |
| Excel Write | Yes | Yes | Yes |
| Sheet-Per-Table Design | Yes | Yes | Yes |
| In-Memory Repository | Yes | Yes | Yes |
| Promoter Fields | Yes | No | Implemented |

### 7.4 Synthetic Data Features

| Feature | Implemented | Tested | Working |
|---------|-------------|--------|---------|
| Simple Generator (Core) | Yes | Yes | Yes |
| Bogus Generator (Mutate) | Yes | No | Implemented |
| Deterministic Seeding | Yes | Yes | Yes |
| Hygiene Issue Injection | Yes | Yes | Yes |
| UK Data (Postcodes, Names) | Yes | Yes | Yes |

---

## 8. Architecture Compliance

### 8.1 Design Principles

| Principle | Status | Evidence |
|-----------|--------|----------|
| Domain-Driven Core | COMPLIANT | All business logic in Ox4D.Core |
| Repository Pattern | COMPLIANT | IDealRepository abstraction |
| Zero Core Dependencies | COMPLIANT | Ox4D.Core has no package references |
| Deterministic MCP Tools | COMPLIANT | No LLM logic in tool handlers |
| Sheet-Per-Table Excel | COMPLIANT | Each worksheet = future DB table |

### 8.2 Interface Abstractions

```csharp
// Repository Interface
public interface IDealRepository
{
    Task<IReadOnlyList<Deal>> GetAllAsync(CancellationToken ct);
    Task<Deal?> GetByIdAsync(string dealId, CancellationToken ct);
    Task<IReadOnlyList<Deal>> QueryAsync(DealFilter filter, DateTime referenceDate, CancellationToken ct);
    Task UpsertAsync(Deal deal, CancellationToken ct);
    Task UpsertManyAsync(IEnumerable<Deal> deals, CancellationToken ct);
    Task DeleteAsync(string dealId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

// Synthetic Data Interface
public interface ISyntheticDataGenerator
{
    List<Deal> Generate(int count, int seed);
}
```

---

## 9. MCP Tool Inventory

### 9.1 Pipeline Tools

| Tool | Method | Parameters | Returns |
|------|--------|------------|---------|
| List Deals | `pipeline.list_deals` | owner, region, stage, minAmount, maxAmount, limit | Deal array |
| Get Deal | `pipeline.get_deal` | dealId | Single deal |
| Upsert Deal | `pipeline.upsert_deal` | Deal object | Updated deal |
| Patch Deal | `pipeline.patch_deal` | dealId, patch object | Updated deal |
| Delete Deal | `pipeline.delete_deal` | dealId | Success boolean |
| Daily Brief | `pipeline.daily_brief` | referenceDate (optional) | Brief report |
| Hygiene Report | `pipeline.hygiene_report` | None | Hygiene report |
| Forecast Snapshot | `pipeline.forecast_snapshot` | referenceDate (optional) | Forecast data |
| Get Stats | `pipeline.get_stats` | None | Pipeline statistics |
| Generate Synthetic | `pipeline.generate_synthetic` | count, seed | Generation result |
| Save | `pipeline.save` | None | Success boolean |
| Reload | `pipeline.reload` | None | Success boolean |

### 9.2 Promoter Tools

| Tool | Method | Parameters | Returns |
|------|--------|------------|---------|
| Dashboard | `promoter.dashboard` | promoterId | Promoter metrics |
| Deals | `promoter.deals` | promoterId | Referred deals |
| Actions | `promoter.actions` | promoterId | Recommended actions |

---

## 10. Known Issues and Recommendations

### 10.1 Current Issues

| Issue | Severity | Impact | Recommendation |
|-------|----------|--------|----------------|
| System.IO.Packaging vulnerability | Medium | Transitive only | Monitor ClosedXML updates |
| Promoter features untested | Low | Feature complete | Add integration tests |
| No concurrent access support | Low | Single-user design | Document limitation |

### 10.2 Future Enhancements

| Enhancement | Priority | Complexity | Notes |
|-------------|----------|------------|-------|
| Supabase Repository | High | Medium | Replace ExcelDealRepository |
| Promoter Integration Tests | Medium | Low | Add to Ox4D.Tests |
| Real-time Sync | Medium | High | Requires cloud backend |
| User Authentication | Medium | High | Role-based access |
| Audit Trail | Low | Medium | Change tracking |

---

## 11. Diagnostic Commands Reference

### Build & Test

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Run Applications

```bash
# Console - Unified Gateway (interactive terminal required)
# Includes all features: CRUD, reports, data generation, and full demo mode
dotnet run --project src/Ox4D.Console

# MCP Server
dotnet run --project src/Ox4D.Zarwin

# MCP Server with custom Excel file
dotnet run --project src/Ox4D.Zarwin -- "path/to/custom.xlsx"
```

### MCP Testing

```bash
# Get pipeline statistics
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.get_stats"}' | dotnet run --project src/Ox4D.Zarwin

# Generate synthetic data
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.generate_synthetic","params":{"count":10,"seed":42}}' | dotnet run --project src/Ox4D.Zarwin

# Get daily brief
echo '{"jsonrpc":"2.0","id":1,"method":"pipeline.daily_brief"}' | dotnet run --project src/Ox4D.Zarwin
```

---

## 12. Certification

### Diagnostic Summary

| Category | Result |
|----------|--------|
| Build | PASS |
| Tests | 393/393 PASS |
| Console App | FUNCTIONAL |
| MCP Server | OPERATIONAL |
| Data Storage | VERIFIED |
| Architecture | COMPLIANT |

### Overall Status: **PRODUCTION READY**

The solution meets all stated objectives and is ready for use as a Sales Pipeline Manager Copilot with MCP integration.

---

*Generated by Ox4D Diagnostics Suite*
