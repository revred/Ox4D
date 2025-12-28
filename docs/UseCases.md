# UseCases.md — User Personas and Detailed Use Cases

> **Purpose:** Define distinct user personas and their use cases for the Ox4D Sales Pipeline Manager.
> **Cross-Reference:** [VPRC.md](VPRC.md) for verification recipes | [PRCD.md](PRCD.md) for requirements | [TestPlan.md](TestPlan.md) for test specifications
> **Last Updated:** 2025-12-28

---

## 1. User Personas

Ox4D serves **three distinct user personas**, each with fundamentally different goals and workflows:

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              OX4D USER PERSONAS                                          │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                          │
│  ┌───────────────────────┐  ┌───────────────────────┐  ┌───────────────────────┐       │
│  │    SALES MANAGER      │  │   DATASET ENGINEER    │  │  PROMOTION MANAGER    │       │
│  │    (Internal User)    │  │   (Technical User)    │  │  (External Partner)   │       │
│  ├───────────────────────┤  ├───────────────────────┤  ├───────────────────────┤       │
│  │                       │  │                       │  │                       │       │
│  │ Primary Focus:        │  │ Primary Focus:        │  │ Primary Focus:        │       │
│  │ • Pipeline health     │  │ • Data quality        │  │ • Referral status     │       │
│  │ • Revenue forecasting │  │ • Master data setup   │  │ • Commission earnings │       │
│  │ • Team performance    │  │ • Synthetic datasets  │  │ • Tier progression    │       │
│  │ • Deal progression    │  │ • Testing scenarios   │  │ • Action guidance     │       │
│  │                       │  │                       │  │                       │       │
│  │ Key Questions:        │  │ Key Questions:        │  │ Key Questions:        │       │
│  │ "What needs my        │  │ "Is the data valid?"  │  │ "How are my deals?"   │       │
│  │  attention today?"    │  │ "Can I reproduce      │  │ "What's my payout?"   │       │
│  │ "What's our forecast?"│  │  this scenario?"      │  │ "How can I help?"     │       │
│  │                       │  │                       │  │                       │       │
│  │ Primary App:          │  │ Primary App:          │  │ Primary App:          │       │
│  │ • Ox4D.Console        │  │ • Ox4D.Console        │  │ • Ox4D.Console        │       │
│  │ • Ox4D.Zarwin (AI)    │  │ • (Demo mode: Menu 13)│  │ • Ox4D.Zarwin (AI)    │       │
│  │                       │  │                       │  │                       │       │
│  └───────────────────────┘  └───────────────────────┘  └───────────────────────┘       │
│                                                                                          │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Sales Manager Use Cases

The **Sales Manager** is the primary internal user responsible for pipeline management and revenue generation.

### 2.1 Persona Profile

| Attribute | Description |
|-----------|-------------|
| **Role** | Sales Manager / Sales Director |
| **Access Level** | Full pipeline visibility, all CRUD operations |
| **Primary Goals** | Maximize revenue, maintain pipeline health, coach team |
| **Key Metrics** | Pipeline value, win rate, forecast accuracy |
| **Frequency** | Daily use |

### 2.2 Use Cases

| Use Case ID | Use Case | Description | Primary Tool | Console Menu |
|-------------|----------|-------------|--------------|--------------|
| **SM-UC1** | Daily Pipeline Review | Start each day knowing what needs attention | Console | Menu 4: Daily Brief |
| **SM-UC2** | Data Quality Audit | Ensure pipeline data is complete and accurate | Console | Menu 5: Hygiene Report |
| **SM-UC3** | Revenue Forecasting | Project revenue by stage, owner, region, product | Console | Menu 6: Forecast Snapshot |
| **SM-UC4** | Deal Management | Create, update, and track individual deals | Console | Menus 9, 10, 11 |
| **SM-UC5** | Team Oversight | Monitor deals by owner and identify coaching needs | Console | Menu 7, 12 |
| **SM-UC6** | Regional Analysis | Understand geographic distribution of pipeline | Console | Menu 6: Forecast |
| **SM-UC7** | AI-Assisted Queries | Use natural language to query pipeline via Claude | Zarwin (MCP) | N/A |

### 2.3 User Stories

```
As a Sales Manager, I want to...

SM-US1: See overdue actions so I can follow up on stalled deals
        → Console Menu 4: Daily Brief → "Overdue" section

SM-US2: Identify high-value deals at risk so I can prioritize intervention
        → Console Menu 4: Daily Brief → "High Value at Risk" section

SM-US3: Find deals with missing data so I can maintain data quality
        → Console Menu 5: Hygiene Report → Issues table

SM-US4: View pipeline by close month so I can forecast quarterly revenue
        → Console Menu 6: Forecast Snapshot → "By Close Month" chart

SM-US5: Ask Claude about my pipeline so I can get instant insights
        → MCP: pipeline.daily_brief, pipeline.forecast_snapshot

SM-US6: Generate demo data so I can train new team members
        → Console Menu 3: Generate Synthetic Data

SM-US7: Export to Excel so I can share reports with leadership
        → Console Menu 2: Save to Excel → Open data/SalesPipelineV1.0.xlsx
```

### 2.4 Workflow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                   SALES MANAGER DAILY WORKFLOW                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Morning Routine:                                                │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │ Load Excel   │ -> │ Daily Brief  │ -> │ Work Actions │       │
│  │ (Menu 1)     │    │ (Menu 4)     │    │ Due Today    │       │
│  └──────────────┘    └──────────────┘    └──────────────┘       │
│                              │                                   │
│                              v                                   │
│  ┌──────────────┐    ┌──────────────┐                           │
│  │ Update Deals │ <- │ Review At-   │                           │
│  │ (Menu 9)     │    │ Risk Deals   │                           │
│  └──────────────┘    └──────────────┘                           │
│                                                                  │
│  Weekly Review:                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │ Hygiene      │ -> │ Forecast     │ -> │ Save & Share │       │
│  │ (Menu 5)     │    │ (Menu 6)     │    │ (Menu 2)     │       │
│  └──────────────┘    └──────────────┘    └──────────────┘       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Dataset Engineer Use Cases

The **Dataset Engineer** is a technical user responsible for creating and maintaining master data, synthetic datasets, and test scenarios.

### 3.1 Persona Profile

| Attribute | Description |
|-----------|-------------|
| **Role** | Data Engineer / QA Engineer / Developer |
| **Access Level** | Full system access including data generation |
| **Primary Goals** | Create reproducible datasets, validate data quality, test scenarios |
| **Key Metrics** | Data completeness, hygiene score, deterministic output |
| **Frequency** | Project-based (setup, testing, demos) |

### 3.2 Use Cases

| Use Case ID | Use Case | Description | Primary Tool | Console Menu |
|-------------|----------|-------------|--------------|--------------|
| **DE-UC1** | Generate Synthetic Dataset | Create realistic UK sales data for demos/testing | Console | Menu 3 or Menu 13 (Demo) |
| **DE-UC2** | Validate Data Quality | Check generated data meets hygiene standards | Console | Menu 5: Hygiene |
| **DE-UC3** | Reproducible Scenarios | Generate same data with deterministic seed | Console | Menu 13 (Demo) uses seed=42 |
| **DE-UC4** | Inject Hygiene Issues | Create data with known issues for testing | Console | Menu 13 (Demo) |
| **DE-UC5** | Master Data Setup | Create baseline Excel workbook for deployment | Console | Menu 2: Save |
| **DE-UC6** | Verify Statistics | Confirm deal distribution matches expectations | Console | Menu 7 or Menu 13 |

### 3.3 User Stories

```
As a Dataset Engineer, I want to...

DE-US1: Generate 100 realistic UK deals so I can demo the system to stakeholders
        → Console Menu 13: Run Full Demo (uses seed=42 by default)

DE-US2: Generate data with a specific seed so I can reproduce test scenarios
        → Console Menu 3: Enter seed value → Same seed = same data

DE-US3: Verify the data has expected hygiene issues so I can test reports
        → Console Menu 5: Hygiene Report → Check ~8% missing amounts, ~12% missing dates

DE-US4: Save the generated dataset as master data for deployment
        → Console Menu 2: Save to Excel → Creates data/SalesPipelineV1.0.xlsx

DE-US5: Validate pipeline statistics match generation parameters
        → Console Menu 7: Pipeline Statistics → Check totals, owners, regions

DE-US6: Clear existing data and regenerate fresh dataset
        → Console Menu 3: "Clear existing data first?" → Yes
```

### 3.4 Workflow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                 DATASET ENGINEER WORKFLOW                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Initial Setup (Console Menu 13: Run Full Demo):                 │
│  ┌────────────────────────────────────────────────────────┐     │
│  │ dotnet run --project src/Ox4D.Console                  │     │
│  │ Select: 13. Run Full Demo (Non-Interactive)            │     │
│  │                                                         │     │
│  │ Automatically:                                          │     │
│  │ 1. Generates 100 deals (seed=42)                       │     │
│  │ 2. Shows statistics                                     │     │
│  │ 3. Shows daily brief                                    │     │
│  │ 4. Shows hygiene report                                 │     │
│  │ 5. Shows forecast snapshot                              │     │
│  │ 6. Saves to Excel                                       │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                  │
│  Custom Dataset (Console Menu 3):                                │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │ Clear Data   │ -> │ Generate     │ -> │ Validate     │       │
│  │ (Menu 3)     │    │ (Menu 3)     │    │ (Menu 5,7)   │       │
│  │ Yes to clear │    │ Set count+   │    │ Check hygiene│       │
│  └──────────────┘    │ seed         │    │ and stats    │       │
│                      └──────────────┘    └──────────────┘       │
│                              │                                   │
│                              v                                   │
│                      ┌──────────────┐                           │
│                      │ Save Master  │                           │
│                      │ (Menu 2)     │                           │
│                      └──────────────┘                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Promotion Manager Use Cases

The **Promotion Manager** (Affiliate/Referral Partner) is an external partner who earns commission on deals they refer.

### 4.1 Persona Profile

| Attribute | Description |
|-----------|-------------|
| **Role** | Affiliate Partner / Referral Agent / Promoter |
| **Access Level** | Read-only access to own referrals + commission tracking |
| **Primary Goals** | Track referral status, maximize commission, progress tiers |
| **Key Metrics** | Referral count, conversion rate, commission earned |
| **Frequency** | Weekly check-ins |

### 4.2 Use Cases

| Use Case ID | Use Case | Description | Primary Tool | MCP Method |
|-------------|----------|-------------|--------------|------------|
| **PM-UC1** | Referral Tracking | See status of all deals I referred | Zarwin (MCP) | promoter.deals |
| **PM-UC2** | Commission Visibility | Understand earnings (pending, projected, paid) | Zarwin (MCP) | promoter.dashboard |
| **PM-UC3** | Tier Progression | Track progress toward next commission tier | Zarwin (MCP) | promoter.dashboard |
| **PM-UC4** | Deal Health Check | Identify referrals that are stalled or at risk | Zarwin (MCP) | promoter.deals |
| **PM-UC5** | Action Recommendations | Get suggestions on how to help close deals | Zarwin (MCP) | promoter.actions |
| **PM-UC6** | Performance Metrics | View conversion rates and pipeline contribution | Zarwin (MCP) | promoter.dashboard |

### 4.3 User Stories

```
As a Promotion Manager, I want to...

PM-US1: See my referred deals so I know their current status
        → MCP: promoter.deals with my promoCode

PM-US2: Track my commission earnings so I know what I've earned
        → MCP: promoter.dashboard → PendingCommission, PaidCommission

PM-US3: Understand my tier level so I know my commission rate
        → MCP: promoter.dashboard → CurrentTier, CommissionRate

PM-US4: Get alerts on stalled deals so I can help move them forward
        → MCP: promoter.deals → Filter for stalled/no-contact

PM-US5: See recommended actions so I can support the sales team
        → MCP: promoter.actions → Prioritized list of actions

PM-US6: View my conversion rate so I can improve my referral quality
        → MCP: promoter.dashboard → ConversionRate, TotalWon/TotalReferrals
```

### 4.4 Commission Tier Structure

| Tier | Commission Rate | Minimum Referrals | Benefits |
|------|-----------------|-------------------|----------|
| **Bronze** | 10% | 0 | Basic commission |
| **Silver** | 12% | 10 | +2% rate increase |
| **Gold** | 15% | 25 | +5% rate, priority support |
| **Platinum** | 18% | 50 | +8% rate, co-marketing |
| **Diamond** | 20% | 100 | Maximum rate, strategic partner |

### 4.5 Workflow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                 PROMOTION MANAGER WORKFLOW                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Via AI Assistant (Claude with MCP):                             │
│                                                                  │
│  "How are my referrals doing?"                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Claude calls: promoter.dashboard(promoCode: "PROMO123")  │   │
│  │                                                           │   │
│  │ Returns:                                                  │   │
│  │ • TotalReferrals: 15                                      │   │
│  │ • OpenPipeline: £125,000                                  │   │
│  │ • CurrentTier: Silver (12%)                               │   │
│  │ • PendingCommission: £8,500                               │   │
│  │ • ReferralsToNextTier: 10 (to Gold)                       │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  "What should I do to help close deals?"                         │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Claude calls: promoter.actions(promoCode: "PROMO123")    │   │
│  │                                                           │   │
│  │ Returns prioritized actions:                              │   │
│  │ 1. Follow up on "Apex Deal" (stalled 15 days, £45,000)    │   │
│  │ 2. Check in on "Beta Project" (decision pending, £30,000)│   │
│  │ 3. Provide reference for "Gamma Corp" (in negotiation)   │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Use Case to Story Traceability

### 5.1 Sales Manager Traceability

| Use Case | Stories | Verification |
|----------|---------|--------------|
| SM-UC1 | S3.1.1, S3.1.2, S3.1.3, S3.1.4 | Console Menu 4 |
| SM-UC2 | S3.2.1, S3.2.2, S3.2.3, S3.2.4 | Console Menu 5 |
| SM-UC3 | S3.3.1, S3.3.2, S3.3.3 | Console Menu 6 |
| SM-UC4 | S3.4.1, S3.4.2, S3.4.3, S3.4.4, S3.4.5 | Console Menus 8-11 |
| SM-UC5 | S3.5.1, S3.5.2 | Console Menus 7, 12 |
| SM-UC6 | S3.3.2 (byRegion) | Console Menu 6 |
| SM-UC7 | S1.2.1-S1.4.2 | MCP via Claude |

### 5.2 Dataset Engineer Traceability

| Use Case | Stories | Verification |
|----------|---------|--------------|
| DE-UC1 | S5.2.1, S5.2.2 | Console Menu 3 or Menu 13 |
| DE-UC2 | S3.2.1-S3.2.4 | Console Menu 5 |
| DE-UC3 | S5.2.1 (seeding) | Console Menu 13 (uses seed=42) |
| DE-UC4 | S5.2.3 | Console Menu 5 after Menu 13 |
| DE-UC5 | S2.2.1-S2.2.4 | Console Menu 2 |
| DE-UC6 | S3.5.1, S3.5.2 | Console Menu 7 |

### 5.3 Promotion Manager Traceability

| Use Case | Stories | Verification |
|----------|---------|--------------|
| PM-UC1 | S4.4.2 | MCP: promoter.deals |
| PM-UC2 | S4.2.2 | MCP: promoter.dashboard |
| PM-UC3 | S4.2.3 | MCP: promoter.dashboard |
| PM-UC4 | S4.3.1 | MCP: promoter.deals (stalled filter) |
| PM-UC5 | S4.3.2, S4.3.3 | MCP: promoter.actions |
| PM-UC6 | S4.2.1 | MCP: promoter.dashboard |

---

## 6. Access Control Matrix

| Feature | Sales Manager | Dataset Engineer | Promotion Manager |
|---------|---------------|------------------|-------------------|
| View All Deals | ✅ | ✅ | ❌ (own referrals only) |
| Create Deals | ✅ | ✅ | ❌ |
| Update Deals | ✅ | ✅ | ❌ |
| Delete Deals | ✅ | ✅ | ❌ |
| Generate Synthetic Data | ✅ | ✅ | ❌ |
| View Daily Brief | ✅ | ✅ | ❌ |
| View Hygiene Report | ✅ | ✅ | ❌ |
| View Forecast | ✅ | ✅ | ❌ |
| Save to Excel | ✅ | ✅ | ❌ |
| View Promoter Dashboard | ❌ | ❌ | ✅ (own data only) |
| View Promoter Actions | ❌ | ❌ | ✅ (own data only) |

---

## 7. Application Mapping

| Persona | Primary Application | Secondary Application | MCP Access |
|---------|--------------------|-----------------------|------------|
| **Sales Manager** | Ox4D.Console | Ox4D.Zarwin via Claude | Full pipeline tools |
| **Dataset Engineer** | Ox4D.Console (Menu 13: Demo) | Ox4D.Zarwin | Full pipeline + data tools |
| **Promotion Manager** | Ox4D.Zarwin via Claude | - | promoter.* tools only |

> **Note:** Ox4D.Console is the **unified gateway** for all operations. The Demo project has been merged into Console as Menu option 13 (Run Full Demo).

---

*Cross-referenced by VPRC.md for step-by-step verification*
