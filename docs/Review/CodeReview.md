# CodeReview.md — File‑Specific Findings

> **Status:** Reviewed with key items implemented as of 2025-12-28
> **Cross-Reference:** [../architecture.md](../architecture.md) | [../schema.md](../schema.md) | [DesignReview.md](DesignReview.md) | [TestReview.md](TestReview.md)

This review references **actual files and tests** in the current Ox4D repository.

---

## 1. Core Layer
### `PipelineService.cs`
- Well‑structured orchestration.
- Suggest extracting validation into a dedicated `PipelineValidator`.

### `DealNormalizer.cs`
- Deterministic logic (good).
- ✅ **DONE**: Change tracking implemented via `NormalizeWithTracking()` returning `NormalizationResult`

### `PromoterService.cs`
- Commission logic is clear.
- Add explicit rounding rules and currency handling.

---

## 2. Store Layer
### `ExcelRepository.cs`
- Reads are clear.
- ✅ Writes are now safe with atomic persistence.

**Implemented changes**
- ✅ **DONE**: Temp file save strategy (write to temp → validate → backup → replace)
- ✅ **DONE**: File lock during write operations
- ✅ **DONE**: Integrity check on load with auto-restore from backup

### `InMemoryRepository.cs`
- Good behavioral baseline.
- Use this as the reference for contract tests.

---

## 3. Console Layer
### `CopilotMenu.cs`
- Powerful but dense.
- Business logic is creeping into UI flows.

**Actions**
- Extract command handlers:
  - `DealCommands`
  - `ReportCommands`
- Add `--json` output mode for automation.

---

## 4. Zarwin (MCP)
### `ToolDefinitions.cs`
- Clear mapping to Core services.
- ✅ Schema validation and structured errors implemented.

**Implemented**
- ✅ **DONE**: `McpServerInfo` class with version metadata
- ✅ **DONE**: `ErrorData` structured envelope with errorType, description, timestamp, serverVersion
- Request/response DTOs (Future enhancement)
- Versioned tool names (Future enhancement)

---

## 5. Mutate
### `SyntheticDataGenerator.cs`
- Solid implementation.
- ✅ **DONE**: Seed control implemented for deterministic generation
- Scenario flags (Future enhancement)

---

## 6. Cross‑Cutting
- Enable nullable refs everywhere.
- Add `Directory.Build.props`.
- Centralize lookup tables (currently duplicated).

---

## Recommended Refactor PRs (Status)

1. ✅ Atomic Excel persistence + validation — **IMPLEMENTED**
2. ✅ Normalization change tracking — **IMPLEMENTED**
3. CLI command extraction — Future
4. ✅ MCP tool versioning — **IMPLEMENTED** (contract tests future)
5. Domain value objects (`DealId`, `Money`) — Future
