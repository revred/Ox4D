# TestPlan.md — Comprehensive Test Specifications for Ox4D

> **Purpose:** Map all 64 stories to 200+ specific test cases organized by Epic
> **Cross-Reference:** [PRCD.md](PRCD.md) | [VPRC.md](VPRC.md) | [UseCases.md](UseCases.md)
> **Current Tests:** 393 (achieved) | **Original Target:** 200+
> **Last Updated:** 2025-12-28

---

## Test Organization

```
tests/Ox4D.Tests/
├── DealNormalizerTests.cs       # Existing - 7 tests
├── SyntheticDataGeneratorTests.cs # Existing - 6 tests
├── ReportServiceTests.cs        # Existing - 5 tests (expand to ~40)
├── PipelineServiceTests.cs      # NEW - ~35 tests
├── PromoterServiceTests.cs      # NEW - ~40 tests
├── InMemoryRepositoryTests.cs   # NEW - ~20 tests
├── DealFilterTests.cs           # NEW - ~15 tests
├── DealModelTests.cs            # NEW - ~15 tests
├── PromoterModelTests.cs        # NEW - ~15 tests
├── LookupTablesTests.cs         # NEW - ~10 tests
└── PipelineSettingsTests.cs     # NEW - ~5 tests
```

---

## Test Count Summary by Epic

| Epic | Stories | Tests Needed | Current | To Add |
|------|---------|--------------|---------|--------|
| G1: AI-Ready (MCP) | 13 | ~30 | 0 | 30 |
| G2: Zero Vendor Lock-in | 8 | ~25 | 0 | 25 |
| G3: Actionable Intelligence | 18 | ~75 | 5 | 70 |
| G4: Referral Partner Support | 13 | ~45 | 0 | 45 |
| G5: Developer Experience | 12 | ~35 | 17 | 18 |
| **TOTAL** | **64** | **210** | **22** | **188** |

---

## EPIC G1: AI-Ready Sales Management (13 Stories → 30 Tests)

### F1.1 MCP Server Infrastructure (3 stories)

#### S1.1.1 — JSON-RPC Message Loop
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-001 | ListDeals_ReturnsAllDeals_WhenNoFilter | Verify list returns all deals without filter | High |
| G1-002 | ListDeals_AppliesFilter_WhenFilterProvided | Verify filter is applied correctly | High |
| G1-003 | GetDeal_ReturnsNull_WhenDealNotFound | Handle missing deal gracefully | High |

#### S1.1.2 — Tool Dispatch Mechanism
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-004 | GetDailyBrief_ReturnsReport_WhenCalled | Daily brief dispatches correctly | High |
| G1-005 | GetHygieneReport_ReturnsReport_WhenCalled | Hygiene report dispatches correctly | High |
| G1-006 | GetForecastSnapshot_ReturnsReport_WhenCalled | Forecast dispatches correctly | High |

#### S1.1.3 — Error Handling
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-007 | UpsertDeal_HandlesNullDeal_Gracefully | Null input handled | Medium |
| G1-008 | PatchDeal_ReturnsNull_WhenDealNotFound | Patch on missing deal returns null | High |
| G1-009 | DeleteDeal_Succeeds_WhenDealNotFound | Delete on missing deal doesn't throw | Medium |

### F1.2 Pipeline CRUD Tools (5 stories)

#### S1.2.1 — List Deals
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-010 | ListDeals_FiltersByOwner_Correctly | Filter by owner works | High |
| G1-011 | ListDeals_FiltersByStage_Correctly | Filter by stage works | High |
| G1-012 | ListDeals_FiltersByRegion_Correctly | Filter by region works | High |
| G1-013 | ListDeals_FiltersByMinAmount_Correctly | Amount filter works | Medium |

#### S1.2.2 — Get Deal
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-014 | GetDeal_ReturnsCorrectDeal_ById | Returns exact deal by ID | High |
| G1-015 | GetDeal_IsCaseInsensitive_ForDealId | Case insensitive ID match | Medium |

#### S1.2.3 — Upsert Deal
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-016 | UpsertDeal_CreatesNewDeal_WhenNotExists | Insert works | High |
| G1-017 | UpsertDeal_UpdatesExisting_WhenExists | Update works | High |
| G1-018 | UpsertDeal_NormalizesData_BeforeSave | Normalization applied | High |
| G1-019 | UpsertDeal_GeneratesDealId_WhenMissing | Auto ID generation | High |

#### S1.2.4 — Patch Deal
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-020 | PatchDeal_UpdatesSingleField_Correctly | Single field patch works | High |
| G1-021 | PatchDeal_UpdatesMultipleFields_Correctly | Multi field patch works | High |
| G1-022 | PatchDeal_ParsesDateField_Correctly | Date parsing in patch | Medium |
| G1-023 | PatchDeal_ParsesAmountField_Correctly | Amount parsing in patch | Medium |
| G1-024 | PatchDeal_ParsesStageField_Correctly | Stage parsing in patch | Medium |

#### S1.2.5 — Delete Deal
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-025 | DeleteDeal_RemovesDeal_FromRepository | Deal removed | High |
| G1-026 | DeleteDeal_SavesChanges_AfterDelete | Changes persisted | High |

### F1.3 Report Tools (3 stories)

#### S1.3.1, S1.3.2, S1.3.3 — Report Tools
**File:** `ReportServiceTests.cs` (Expand existing)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-027 | DailyBrief_ViaService_ReturnsValidReport | Daily brief through PipelineService | High |
| G1-028 | HygieneReport_ViaService_ReturnsValidReport | Hygiene through PipelineService | High |
| G1-029 | ForecastSnapshot_ViaService_ReturnsValidReport | Forecast through PipelineService | High |

### F1.4 Data Management Tools (2 stories)

#### S1.4.1, S1.4.2 — Save/Reload
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G1-030 | GenerateSyntheticData_CreatesDeals_WithCount | Synthetic generation works | High |

---

## EPIC G2: Zero Vendor Lock-in (8 Stories → 25 Tests)

### F2.1 Repository Abstraction (2 stories)

#### S2.1.1 — IDealRepository Interface
**File:** `InMemoryRepositoryTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G2-001 | GetAllAsync_ReturnsEmptyList_WhenEmpty | Empty repo behavior | High |
| G2-002 | GetAllAsync_ReturnsAllDeals_WhenPopulated | Returns all deals | High |
| G2-003 | GetAllAsync_ReturnsClones_NotOriginals | Immutability check | High |
| G2-004 | GetByIdAsync_ReturnsNull_WhenNotFound | Handle missing | High |
| G2-005 | GetByIdAsync_ReturnsClone_WhenFound | Returns clone | High |
| G2-006 | QueryAsync_AppliesFilter_Correctly | Filter application | High |
| G2-007 | UpsertAsync_AddsNewDeal_WhenNotExists | Insert path | High |
| G2-008 | UpsertAsync_UpdatesDeal_WhenExists | Update path | High |
| G2-009 | UpsertManyAsync_AddsMultipleDeals | Bulk insert | High |
| G2-010 | DeleteAsync_RemovesDeal_WhenExists | Delete works | High |
| G2-011 | DeleteAsync_DoesNotThrow_WhenNotExists | Safe delete | Medium |
| G2-012 | SaveChangesAsync_CompletesSuccessfully | Save works | High |

#### S2.1.2 — InMemoryDealRepository
**File:** `InMemoryRepositoryTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G2-013 | Clear_RemovesAllDeals | Clear method works | High |
| G2-014 | LoadDeals_ReplacesExistingDeals | Load method works | High |
| G2-015 | Repository_IsThreadSafe_WithConcurrentAccess | Thread safety | Medium |

### F2.2 Excel Storage (4 stories)

#### S2.2.1-S2.2.4 — Excel Sheets
**Note:** These are integration tests requiring file I/O

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G2-016 | DealsSheet_Contains35PlusFields | All fields mapped | High |
| G2-017 | LookupsSheet_ContainsPostcodeRegions | Postcode mappings exist | High |
| G2-018 | LookupsSheet_ContainsStageProbabilities | Stage probabilities exist | High |
| G2-019 | MetadataSheet_ContainsVersionInfo | Version tracking | Medium |
| G2-020 | PromoterFields_PersistedToExcel | Promoter columns saved | High |

### F2.3 Migration Readiness (2 stories)

#### S2.3.1, S2.3.2 — Sheet-per-table Design
| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G2-021 | SheetPerTable_ThreeSheetsExist | Deals, Lookups, Metadata | High |
| G2-022 | ColumnNames_MatchPropertyNames | Consistent naming | Medium |
| G2-023 | DealId_IsPrimaryKey | Unique identifier | High |
| G2-024 | Repository_SupportsFiltering | Query capability | High |
| G2-025 | Repository_SupportsSorting | Order capability | Medium |

---

## EPIC G3: Actionable Intelligence (18 Stories → 75 Tests)

### F3.1 Daily Brief Report (4 stories)

#### S3.1.1 — Identify Overdue Actions
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-001 | DailyBrief_IdentifiesOverdue_WhenNextStepPast | Overdue detection | High |
| G3-002 | DailyBrief_CalculatesOverdueDays_Correctly | Days calculation | High |
| G3-003 | DailyBrief_SortsOverdue_ByDateThenAmount | Proper sorting | Medium |
| G3-004 | DailyBrief_ExcludesClosedDeals_FromOverdue | Closed excluded | High |

#### S3.1.2 — Actions Due Today
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-005 | DailyBrief_IdentifiesDueToday_WhenMatchDate | Due today detection | High |
| G3-006 | DailyBrief_SortsDueToday_ByAmount | Amount priority | Medium |
| G3-007 | DailyBrief_UsesReferenceDate_NotSystemDate | Reference date used | High |

#### S3.1.3 — No-Contact Deals
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-008 | DailyBrief_IdentifiesNoContact_WhenNeverContacted | Never contacted | High |
| G3-009 | DailyBrief_IdentifiesNoContact_WhenOverThreshold | Threshold exceeded | High |
| G3-010 | DailyBrief_UsesConfiguredThreshold_ForNoContact | Configurable days | High |

#### S3.1.4 — High-Value At-Risk
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-011 | DailyBrief_IdentifiesHighValue_AtRisk | High value detection | High |
| G3-012 | DailyBrief_UsesConfiguredThreshold_ForHighValue | Configurable amount | High |
| G3-013 | DailyBrief_DeterminesRiskReason_Correctly | Risk reason text | Medium |
| G3-014 | DailyBrief_LimitsHighValueAtRisk_ToTopN | TopN limit | Medium |

### F3.2 Hygiene Report (4 stories)

#### S3.2.1 — Missing Amounts
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-015 | HygieneReport_DetectsMissingAmount_WhenNull | Null amount | High |
| G3-016 | HygieneReport_DetectsMissingAmount_WhenZero | Zero amount | High |
| G3-017 | HygieneReport_SetsCorrectSeverity_ForMissingAmount | Medium severity | Medium |

#### S3.2.2 — Missing Close Dates
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-018 | HygieneReport_DetectsMissingCloseDate_ForProposal | Proposal stage | High |
| G3-019 | HygieneReport_DetectsMissingCloseDate_ForNegotiation | Negotiation stage | High |
| G3-020 | HygieneReport_IgnoresMissingCloseDate_ForEarlyStage | Lead/Qualified OK | High |
| G3-021 | HygieneReport_SetsHighSeverity_ForMissingCloseDate | High severity | Medium |

#### S3.2.3 — Stage-Probability Mismatch
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-022 | HygieneReport_DetectsMismatch_WhenProbabilityTooHigh | Over 30% diff | High |
| G3-023 | HygieneReport_DetectsMismatch_WhenProbabilityTooLow | Under expected | High |
| G3-024 | HygieneReport_AllowsReasonableVariance_InProbability | Within 30% OK | Medium |
| G3-025 | HygieneReport_SetsCorrectSeverity_ForMismatch | Based on diff | Medium |

#### S3.2.4 — Missing Location Data
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-026 | HygieneReport_DetectsMissingPostcode | No postcode | High |
| G3-027 | HygieneReport_DetectsMissingContactInfo | No email/phone | High |
| G3-028 | HygieneReport_DetectsMissingOwner | No owner | High |
| G3-029 | HygieneReport_DetectsMissingNextStep | No next step | Medium |

### F3.3 Forecast Snapshot (3 stories)

#### S3.3.1 — Total and Weighted Pipeline
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-030 | ForecastSnapshot_CalculatesTotalPipeline_Correctly | Sum of amounts | High |
| G3-031 | ForecastSnapshot_CalculatesWeightedPipeline_Correctly | Sum of weighted | High |
| G3-032 | ForecastSnapshot_ExcludesClosedDeals_FromPipeline | Open only | High |
| G3-033 | ForecastSnapshot_HandlesNullAmounts_Gracefully | Null handling | Medium |

#### S3.3.2 — Breakdown by Dimensions
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-034 | ForecastSnapshot_GroupsByStage_Correctly | Stage grouping | High |
| G3-035 | ForecastSnapshot_GroupsByOwner_Correctly | Owner grouping | High |
| G3-036 | ForecastSnapshot_GroupsByRegion_Correctly | Region grouping | High |
| G3-037 | ForecastSnapshot_GroupsByCloseMonth_Correctly | Month grouping | High |
| G3-038 | ForecastSnapshot_GroupsByProduct_Correctly | Product grouping | High |
| G3-039 | ForecastSnapshot_CalculatesOwnerWinRate_Correctly | Win rate calc | Medium |
| G3-040 | ForecastSnapshot_CalculatesStagePercentage_Correctly | Percentage calc | Medium |

#### S3.3.3 — Actionable Insights
**File:** `ReportServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-041 | ForecastSnapshot_IdentifiesConcentration_ByStage | Risk detection | Medium |
| G3-042 | ForecastSnapshot_IdentifiesConcentration_ByOwner | Risk detection | Medium |

### F3.4 Deal Management (5 stories)

#### S3.4.1-S3.4.5 — CRUD Operations
**File:** `DealModelTests.cs`, `DealFilterTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-043 | Deal_Clone_CreatesDeepCopy | Clone independence | High |
| G3-044 | Deal_WeightedAmount_CalculatesCorrectly | Computed property | High |
| G3-045 | Deal_WeightedAmount_ReturnsNull_WhenNoAmount | Null handling | High |
| G3-046 | DealFilter_MatchesByOwner_Correctly | Owner match | High |
| G3-047 | DealFilter_MatchesByStage_Correctly | Stage match | High |
| G3-048 | DealFilter_MatchesByRegion_Correctly | Region match | High |
| G3-049 | DealFilter_MatchesByMinAmount_Correctly | Min amount | High |
| G3-050 | DealFilter_MatchesByMaxAmount_Correctly | Max amount | High |
| G3-051 | DealFilter_MatchesBySearchTerm_InAccountName | Search account | High |
| G3-052 | DealFilter_MatchesBySearchTerm_InDealName | Search deal name | High |
| G3-053 | DealFilter_MatchesBySearchTerm_InContactName | Search contact | High |
| G3-054 | DealFilter_MatchesByDateRange_Correctly | Date range | Medium |
| G3-055 | DealFilter_CombinesMultipleFilters_WithAnd | Combined filters | High |

### F3.5 Pipeline Statistics (2 stories)

#### S3.5.1, S3.5.2 — High-Level Metrics
**File:** `PipelineServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G3-056 | GetStats_ReturnsTotalDeals_Count | Total count | High |
| G3-057 | GetStats_ReturnsOpenDeals_Count | Open count | High |
| G3-058 | GetStats_ReturnsClosedWonDeals_Count | Won count | High |
| G3-059 | GetStats_ReturnsClosedLostDeals_Count | Lost count | High |
| G3-060 | GetStats_ReturnsTotalPipeline_Value | Pipeline value | High |
| G3-061 | GetStats_ReturnsWeightedPipeline_Value | Weighted value | High |
| G3-062 | GetStats_ReturnsClosedWonValue | Won value | High |
| G3-063 | GetStats_ReturnsAverageDealsValue | Average calc | Medium |
| G3-064 | GetStats_ReturnsUniqueOwners | Owner list | High |
| G3-065 | GetStats_ReturnsUniqueRegions | Region list | High |

---

## EPIC G4: Referral Partner Support (13 Stories → 45 Tests)

### F4.1 Promoter Model (3 stories)

#### S4.1.1 — Promoter Entity
**File:** `PromoterModelTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G4-001 | Promoter_DefaultsToActiveBronze | Default state | High |
| G4-002 | Promoter_Clone_CreatesDeepCopy | Clone works | High |
| G4-003 | Promoter_ConversionRate_CalculatesCorrectly | Conversion calc | High |
| G4-004 | Promoter_ConversionRate_ReturnsZero_WhenNoReferrals | Zero handling | High |

#### S4.1.2 — Promoter Fields in Deal
**File:** `DealModelTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G4-005 | Deal_HasPromoterId_Property | Field exists | High |
| G4-006 | Deal_HasPromoCode_Property | Field exists | High |
| G4-007 | Deal_HasPromoterCommission_Property | Field exists | High |
| G4-008 | Deal_HasCommissionPaid_Property | Field exists | High |
| G4-009 | Deal_HasCommissionPaidDate_Property | Field exists | High |

#### S4.1.3 — Tier Calculation
**File:** `PromoterModelTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G4-010 | PromoterTier_Bronze_Returns10PercentRate | Bronze rate | High |
| G4-011 | PromoterTier_Silver_Returns12PercentRate | Silver rate | High |
| G4-012 | PromoterTier_Gold_Returns15PercentRate | Gold rate | High |
| G4-013 | PromoterTier_Platinum_Returns18PercentRate | Platinum rate | High |
| G4-014 | PromoterTier_Diamond_Returns20PercentRate | Diamond rate | High |
| G4-015 | PromoterTier_MinReferrals_Bronze | Bronze threshold | High |
| G4-016 | PromoterTier_MinReferrals_Silver | Silver threshold | High |
| G4-017 | PromoterTier_MinReferrals_Gold | Gold threshold | High |
| G4-018 | PromoterTier_ParseTier_HandlesValidInput | Parse valid | High |
| G4-019 | PromoterTier_ParseTier_DefaultsToBronze | Parse invalid | High |

### F4.2 Promoter Dashboard (4 stories)

#### S4.2.1-S4.2.4 — Dashboard Metrics
**File:** `PromoterServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G4-020 | Dashboard_CalculatesTotalReferrals | Count referrals | High |
| G4-021 | Dashboard_CalculatesActiveDeals | Open deals count | High |
| G4-022 | Dashboard_CalculatesClosedWon | Won count | High |
| G4-023 | Dashboard_CalculatesClosedLost | Lost count | High |
| G4-024 | Dashboard_CalculatesConversionRate | Win rate | High |
| G4-025 | Dashboard_CalculatesTotalPipelineValue | Pipeline sum | High |
| G4-026 | Dashboard_CalculatesWeightedPipelineValue | Weighted sum | High |
| G4-027 | Dashboard_CalculatesTotalWonValue | Won value | High |
| G4-028 | Dashboard_TracksCommissionEarned | Total commission | High |
| G4-029 | Dashboard_TracksPaidCommission | Paid amount | High |
| G4-030 | Dashboard_TracksPendingCommission | Pending amount | High |
| G4-031 | Dashboard_CalculatesProjectedCommission | Projected amount | High |
| G4-032 | Dashboard_CountsStalledDeals | Health metric | High |
| G4-033 | Dashboard_CountsAtRiskDeals | Health metric | High |
| G4-034 | Dashboard_CountsHealthyDeals | Health metric | High |

### F4.3 Promoter Actions (3 stories)

#### S4.3.1-S4.3.3 — Action Recommendations
**File:** `PromoterServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G4-035 | Actions_IdentifiesStalledReferrals | Stalled detection | High |
| G4-036 | Actions_GeneratesCheckIn_ForNoActivity | Check in action | High |
| G4-037 | Actions_GeneratesContextAction_ForStaleLead | Lead help | High |
| G4-038 | Actions_GeneratesIntroductionAction_ForDiscovery | Discovery help | High |
| G4-039 | Actions_GeneratesReferenceAction_ForProposal | Proposal help | High |
| G4-040 | Actions_GeneratesEscalation_ForNegotiation | Negotiation help | High |
| G4-041 | Actions_PrioritizesByPriority_ThenCommission | Priority sort | High |
| G4-042 | Actions_CalculatesPotentialCommission | Commission calc | High |

### F4.4 Promoter MCP Tools (3 stories)

#### S4.4.1-S4.4.3 — MCP Tools
**File:** `PromoterServiceTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G4-043 | GetDeals_ReturnsOnlyPromoterDeals | Filter by promo | High |
| G4-044 | GetDeals_MatchesByPromoCode | Code match | High |
| G4-045 | GetDeals_MatchesByPromoterId | ID match | High |

---

## EPIC G5: Developer Experience (12 Stories → 35 Tests)

### F5.1 Clean Architecture (3 stories)

#### S5.1.1-S5.1.3 — Architecture Verification
**Note:** These are structural tests via reflection

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G5-001 | Core_HasNoNuGetDependencies | Zero deps | High |
| G5-002 | Store_OnlyHasStorageConcerns | Layer check | Medium |
| G5-003 | Services_UseInterfaceAbstraction | DI ready | High |

### F5.2 Synthetic Data Generation (4 stories)

#### S5.2.1-S5.2.4 — Data Generation
**File:** `SyntheticDataGeneratorTests.cs` (Existing + expand)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G5-004 | Generator_CreatesRequestedCount | Count accurate | High |
| G5-005 | Generator_IsDeterministic_WithSameSeed | Seeded repeat | High |
| G5-006 | Generator_ProducesDifferentData_WithDifferentSeeds | Seed variation | High |
| G5-007 | Generator_ProducesValidStages | Stage values | High |
| G5-008 | Generator_ProducesNormalizedDeals | Normalization | High |
| G5-009 | Generator_InjectsMissingAmounts | Hygiene issue | High |
| G5-010 | Generator_InjectsMissingDates | Hygiene issue | High |
| G5-011 | Generator_InjectsMissingContacts | Hygiene issue | High |
| G5-012 | Generator_CreatesUKPostcodes | UK format | High |
| G5-013 | Generator_CreatesRealisticNames | Name format | Medium |
| G5-014 | Generator_GeneratesPromoCodes | Promo codes | High |
| G5-015 | Generator_GeneratesValidOwners | Owner names | High |

### F5.3 Unit Testing (3 stories)

#### S5.3.1 — Test Deal Normalization
**File:** `DealNormalizerTests.cs` (Existing + expand)

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G5-016 | Normalize_GeneratesDealId_WhenEmpty | ID generation | High |
| G5-017 | Normalize_SetsProbability_FromStage | Prob default | High |
| G5-018 | Normalize_ExtractsPostcodeArea | Area extraction | High |
| G5-019 | Normalize_DerivesRegion_FromPostcode | Region lookup | High |
| G5-020 | Normalize_GeneratesMapLink | Map link | High |
| G5-021 | Normalize_SetsCreatedDate_WhenMissing | Date default | High |
| G5-022 | Normalize_NormalizesTags | Tag cleanup | Medium |
| G5-023 | ParseDate_ParsesISOFormat | ISO dates | High |
| G5-024 | ParseDate_ParsesUKFormat | UK dates | High |
| G5-025 | ParseDate_ReturnsNull_ForInvalid | Invalid handling | High |
| G5-026 | ParseAmount_ParsesCurrencySymbol | £ removal | High |
| G5-027 | ParseAmount_ParsesCommas | Comma removal | High |
| G5-028 | ParseAmount_ReturnsNull_ForInvalid | Invalid handling | High |
| G5-029 | ParseProbability_ParsesPercentSign | % removal | High |
| G5-030 | ParseProbability_Clamps0To100 | Range clamp | High |

### F5.4 Interface Abstractions (2 stories)

#### S5.4.1, S5.4.2 — Interface Verification
**File:** `LookupTablesTests.cs`

| Test ID | Test Name | Description | Priority |
|---------|-----------|-------------|----------|
| G5-031 | LookupTables_ReturnsDefaultProbability_ForStage | Stage prob | High |
| G5-032 | LookupTables_ReturnsRegion_ForPostcode | Region lookup | High |
| G5-033 | LookupTables_ExtractsPostcodeArea_Correctly | Area extraction | High |
| G5-034 | LookupTables_HandlesUnknownPostcode_Gracefully | Unknown code | High |
| G5-035 | PipelineSettings_HasConfigurableThresholds | Settings work | High |

---

## Implementation Order

### Phase 1: Core Model Tests (40 tests)
1. `DealModelTests.cs` - Deal entity tests
2. `PromoterModelTests.cs` - Promoter entity tests
3. `DealFilterTests.cs` - Filter logic tests
4. `LookupTablesTests.cs` - Lookup tests

### Phase 2: Repository Tests (20 tests)
1. `InMemoryRepositoryTests.cs` - Repository pattern tests

### Phase 3: Service Tests (75 tests)
1. Expand `ReportServiceTests.cs` - Report generation
2. Create `PipelineServiceTests.cs` - Pipeline operations
3. Create `PromoterServiceTests.cs` - Promoter operations

### Phase 4: Expand Existing (30 tests)
1. Expand `DealNormalizerTests.cs`
2. Expand `SyntheticDataGeneratorTests.cs`

---

## Test Execution

```bash
# Run all tests
dotnet test

# Run with verbosity
dotnet test --verbosity normal

# Run specific test file
dotnet test --filter "FullyQualifiedName~PipelineServiceTests"

# Run by Epic (using test naming convention)
dotnet test --filter "DisplayName~G1-"  # Epic G1 tests
dotnet test --filter "DisplayName~G2-"  # Epic G2 tests

# Coverage report
dotnet test --collect:"XPlat Code Coverage"
```

---

*Cross-references: [PRCD.md](PRCD.md) | [VPRC.md](VPRC.md) | [UseCases.md](UseCases.md)*
