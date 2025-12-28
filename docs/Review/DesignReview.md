# DesignReview.md — Ox4D (Code‑Aligned Review)

> **Status:** Reviewed and largely implemented as of 2025-12-28
> **Cross-Reference:** [../architecture.md](../architecture.md) | [../schema.md](../schema.md) | [CodeReview.md](CodeReview.md) | [TestReview.md](TestReview.md)

This review is **explicitly aligned to the current Ox4D codebase**.

## Repository Structure (Actual)
- `Ox4D.sln`
- `src/`
  - `Ox4D.Core`
    - Models: `Deal`, `Promoter`, lookup tables
    - Services: `PipelineService`, `PromoterService`, `ReportService`
    - Normalization: `DealNormalizer`
  - `Ox4D.Store`
    - `InMemoryRepository`
    - `ExcelRepository` (SalesPipelineV1.0.xlsx)
  - `Ox4D.Console`
    - `CopilotMenu.cs` (primary CLI / TUI entry)
  - `Ox4D.Zarwin`
    - MCP server + JSON‑RPC protocol + tool definitions
  - `Ox4D.Mutate`
    - `SyntheticDataGenerator`
- `tests/Ox4D.Tests`
  - Core, Store, Mutate, and Service‑level tests

The architecture described in README is **accurately reflected** in the solution layout.

---

## 1. Boundary & Dependency Review (Actual)
### Observations
- `Ox4D.Core` does **not** reference Excel, Console, or MCP assemblies (good).
- `Ox4D.Store` references Core models only (correct).
- `Ox4D.Console` and `Ox4D.Zarwin` orchestrate via Core services.

### Gaps / Improvements
- No automated enforcement of boundaries.

### Actions
- ~~Add architecture tests asserting boundary constraints~~ (Future)
- ✅ **DONE**: Added `/docs/architecture.md` with architecture diagram

---

## 2. Domain Model Review
### Deal Model
Files:
- `Ox4D.Core/Models/Deal.cs`
- Tests: `DealModelTests.cs`

**Strengths**
- Clear separation of raw vs normalized fields.
- Stage and probability logic is centralized.

**Risks**
- `Id` generation rules should be explicit and immutable.
- Monetary fields are `decimal` but not wrapped.

### Actions
- Introduce `DealId` value object (Future enhancement)
- Introduce `Money` struct for amount + currency (Future enhancement)
- Lock `Deal.Id` after creation (Future enhancement)

---

## 3. Normalization Pipeline (DealNormalizer)
Files:
- `DealNormalizer.cs`
- Tests: `DealNormalizerTests.cs`

**Strengths**
- Deterministic normalization.
- Good postcode → region logic.

**Gaps**
- Normalization mutates silently.

### Actions
- ✅ **DONE**: `DealNormalizer.NormalizeWithTracking()` returns `NormalizationResult` with `Deal` and `List<NormalizationChange>`
- Surface changes in CLI output (Future)
- Surface changes in MCP tool responses (Future)

---

## 4. Repository Design (Excel & InMemory)
Files:
- `ExcelRepository.cs`
- `InMemoryRepository.cs`
- Tests: `InMemoryRepositoryTests.cs`

**Strengths**
- Same interface used for both repos.
- Tests already validate core behavior.

**Risks**
- Excel writes are not atomic.
- No integrity validation on load.

### Actions
- ✅ **DONE**: Atomic save implemented (temp file → validate → backup → replace)
- ✅ **DONE**: `ValidateExcelFile()` validates sheet presence and required columns
- ✅ **DONE**: Backup rotation with configurable `maxBackups` parameter
- ✅ **DONE**: Auto-restore from backup on load failure
- ✅ **DONE**: File locking during write operations

---

## 5. Reporting Architecture
Files:
- `ReportService.cs`
- Tests: `ReportServiceTests.cs`

**Strengths**
- Reports are centralized and tested.
- Forecast logic matches README.

**Improvements**
- Time is implicitly `DateTime.Now`.

### Actions
- ✅ **DONE**: `IClock` interface created with `SystemClock` and `FixedClock` implementations
- ✅ **DONE**: `ReportService` accepts optional `IClock` parameter for deterministic testing
- Reports are now pure functions over inputs when using `FixedClock`

---

## 6. MCP (Zarwin) Design Review
Files:
- `ToolDefinitions.cs`
- `JsonRpcMessage.cs`

**Strengths**
- Clean JSON‑RPC framing.
- Tools map cleanly to Core services.

**Gaps**
- No explicit versioning.
- Errors are not standardized.

### Actions
- ✅ **DONE**: `McpServerInfo` class with `Version = "1.1.0"` and protocol metadata
- ✅ **DONE**: Standardized `ErrorData` envelope with `errorType`, `description`, `identifier`, `details`, `timestamp`, `serverVersion`
- ✅ **DONE**: Extended error factory methods (`NotFound`, `ValidationError`, `DataIntegrityError`, `OperationFailed`)
- Add contract tests for each tool (Future)

---

## 7. Synthetic Data (Mutate)
Files:
- `SyntheticDataGenerator.cs`
- Tests: `SyntheticDataGeneratorTests.cs`

**Strengths**
- Uses realistic distributions.
- Already schema‑aware.

**Improvements**
- No scenario presets.

### Actions
- Add scenario presets (Future enhancement):
  - stalled pipeline
  - promoter‑heavy funnel
  - hygiene‑failure dataset
- ✅ **DONE**: Deterministic seed input already supported

---

## 8. Documentation Alignment
### Existing
- `PRCD.md`
- `VPRC.md`
- `UseCases.md`
- `TestPlan.md`

### Actions
- ✅ **DONE**: Cross-references added to all documentation files
- ✅ **DONE**: `/docs/schema.md` created with complete data schema documentation
- ✅ **DONE**: `/docs/architecture.md` created with architecture diagrams

---

## Priority Order (Status)
1. ✅ Atomic Excel persistence — **IMPLEMENTED**
2. ✅ Normalization change tracking — **IMPLEMENTED**
3. Architecture boundary tests — Future
4. ✅ MCP tool versioning — **IMPLEMENTED**
5. ✅ Clock injection into reports — **IMPLEMENTED**

---

## Implementation Summary

All high-priority items have been implemented. The codebase now includes:
- Atomic Excel persistence with backup rotation and auto-recovery
- Normalization change tracking via `NormalizeWithTracking()`
- MCP server versioning with structured error envelopes
- `IClock` abstraction for deterministic report testing
- Comprehensive documentation in `/docs/`
