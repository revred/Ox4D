# CodeReview.md — Extensive Findings (Concrete Files, Concrete Fixes)

Snapshot: `Ox4D-main-2025-12-28-0028.zip`

---

## A) The #1 correctness issue: reflection patching

File: `src/Ox4D.Core/Services/PipelineService.cs`

Evidence:
- Property reflection: `GetProperty` at line 62
- Conversion: `Convert.ChangeType` at line 98
- Silent ignore on failure: comment at line 103

**Failure modes**
- “Success” responses where nothing changed
- Type coercions (e.g., numeric parsing) that differ by culture/JSON serialization
- Hidden partial updates

**Fix (preferred)**
- Create `DealPatch` DTO:
  - nullable fields only for allowed patch targets
  - explicit parsing and validation
- Return a `PatchResult`:
  - `AppliedFields[]`
  - `RejectedFields[]` with reasons

**Minimum acceptable fix**
- Whitelist mapping dictionary: `fieldName -> setter`
- Reject unknown fields and return errors

---

## B) Excel persistence is strong — tighten the edges

File: `src/Ox4D.Store/ExcelDealRepository.cs`

Good:
- Atomic replacement Step 4 (line ~339)
- Validation function exists (line ~121)

Improve:
- Temp file uniqueness (avoid `~$...tmp.xlsx` collisions)
- Cross-process lock
- Log cleanup failures (currently swallowed in cleanup catches)

---

## C) Time usage & determinism

Findings (examples):
- `DealNormalizer.cs` uses `DateTime.UtcNow` in ID generation (around line 160)
- `ExcelDealRepository.cs` uses `DateTime.Now` for backup timestamp (line ~451)
- `PromoterService.cs` uses `DateTime.UtcNow` in dashboard generation (line ~75)

**Fix**
- Centralize time behind `IClock` (you already have this in `ReportService`)
- Add `IIdGenerator` / `IDealIdGenerator` for deterministic test runs

---

## D) Zarwin server: tool contract hardening

Files:
- `src/Ox4D.Zarwin/Handlers/ToolHandler.cs`
- `src/Ox4D.Zarwin/Protocol/*`

Improve:
- Add tool versioning (`v1`)
- Add strict DTO schemas for each tool
- Standardize error envelope (JSON-RPC error object + app-level code)

---

## E) Console UI maintainability

File: `src/Ox4D.Console/CopilotMenu.cs`

Improve:
- Split into command modules
- Add non-interactive JSON mode

---

## PR sequence (suggested)
1. Typed patch + PatchResult + tests
2. Cross-process Excel lock + unique temp + tests
3. Deterministic id generator injection + seed stability tests
4. Tool versioning + DTO schemas + MCP contract tests
5. Schema versioning + migration scaffold + tests
