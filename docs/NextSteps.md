# Ox4D Next Steps

## Completed in This Session (2025-12-28)

### From Recheck.docx Review

1. **Typed Patch DTO with Validation** (Priority #1)
   - Created `DealPatch.cs` with typed fields replacing reflection-based patching
   - Added `PatchResult` class with `AppliedFields`, `RejectedFields`, `NormalizationChanges`
   - Updated `PipelineService.PatchDealWithResultAsync()` for explicit validation
   - MCP `pipeline.patch_deal` now returns detailed results with rejected field reasons

2. **Deterministic ID Generation** (Priority #2A)
   - Created `IDealIdGenerator` interface in `IDealIdGenerator.cs`
   - Implementations: `DefaultDealIdGenerator`, `SeededDealIdGenerator`, `SequentialDealIdGenerator`
   - `DealNormalizer` now accepts injectable ID generator and clock

3. **Schema Versioning + Migration** (Priority #3)
   - Added `CurrentSchemaVersion = "1.2"` and `SupportedSchemaVersions` array
   - Schema version stored in Metadata sheet, read during validation
   - Migration scaffold: `MigrateFrom_1_0_To_1_1()`, `MigrateFrom_1_1_To_1_2()`
   - Rejects unsupported future versions with clear error messages

4. **IClock Injection for Timestamps** (Priority #2B)
   - `ExcelDealRepository` constructor now accepts `IClock` parameter
   - `WriteMetadataSheet()` uses `_clock.UtcNow` for LastModified
   - `CreateBackup()` uses `_clock.Now` for deterministic backup filenames

5. **MCP Tool Versioning** (Priority #4)
   - Server version updated to `1.2.0`
   - Added `ToolApiVersion = "v1"` constant
   - All 14 tools now include `Version = "1.0"` property

6. **Cross-Process Excel Locking** (From prior session)
   - Lock file with `FileShare.None` for exclusive access
   - Retry logic with exponential backoff
   - Unique temp file names with GUID suffix

7. **Test Coverage**
   - Added 22 patch semantics tests in `PatchSemanticsTests.cs`
   - Total: **415 tests passing**

---

## Remaining Items (Lower Priority)

### Console App Enhancements
- Extract command handlers into separate classes (`DealCommands`, `ReportCommands`, `PromoterCommands`)
- Add `--json` output mode for CI/scripting
- Align Console and MCP tool semantics

### Additional Test Coverage
- MCP contract tests (request validation, response schema, error stability)
- Schema version mismatch tests (older → migrate, newer → fail)
- Deterministic synthetic data tests (same seed → identical dataset)

### Future Schema Migrations
When adding new columns or changing enum values:
1. Increment `CurrentSchemaVersion`
2. Add to `SupportedSchemaVersions` array
3. Implement `MigrateFrom_X_To_Y()` method
4. Existing files auto-migrate on load

---

## Architecture Summary

```
Ox4D v1.2
├── Typed patch validation (no silent failures)
├── Deterministic ID generation (testable)
├── Schema versioning with migration chain
├── IClock injection throughout
├── MCP tools versioned (v1.0)
├── Cross-process Excel locking
└── 415 tests passing
```

---

## Files Changed

### New Files
- `src/Ox4D.Core/Models/DealPatch.cs` - Typed patch DTO + PatchResult
- `src/Ox4D.Core/Services/IDealIdGenerator.cs` - ID generator abstraction
- `tests/Ox4D.Tests/PatchSemanticsTests.cs` - 22 patch validation tests

### Modified Files
- `src/Ox4D.Core/Services/DealNormalizer.cs` - IDealIdGenerator + IClock injection
- `src/Ox4D.Core/Services/PipelineService.cs` - PatchDealWithResultAsync method
- `src/Ox4D.Store/ExcelDealRepository.cs` - Schema versioning, IClock, migrations
- `src/Ox4D.Zarwin/Handlers/ToolHandler.cs` - Detailed patch results
- `src/Ox4D.Zarwin/Protocol/ToolDefinitions.cs` - Tool versioning
- `src/Ox4D.Zarwin/ZarwinServer.cs` - Use centralized server info
