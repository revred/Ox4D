# DesignReview.md — Ox4D (Extensive Detail, Code-Referenced)

Snapshot: `Ox4D-main-2025-12-28-0028.zip`

This document is written as **work instructions**. It references concrete files and (where helpful) approximate line numbers from the current repo snapshot.

---

## 1) Architecture Reality Check (What the code *actually* implements)

### Solution layout (matches intended design)
- `src/Ox4D.Core` — domain models, config, reports, services
- `src/Ox4D.Store` — Excel + InMemory repositories
- `src/Ox4D.Console` — Spectre.Console UI (`CopilotMenu.cs`)
- `src/Ox4D.Zarwin` — JSON-RPC/MCP server (tools + handlers)
- `src/Ox4D.Mutate` — Bogus-based synthetic data generator
- `tests/Ox4D.Tests` — unit + contract + failure-mode tests
- `docs/*` — PRCD/VPRC, schema, architecture, test plan

**Design direction is solid and consistent.** Your recent commits added:
- Excel durability workflow (temp → validate → backup → atomic move)
- Contract tests for repos (Excel + in-memory)
- Failure mode tests for Excel corruption

---

## 2) Platform & Build Baseline

### Current setting
- `Directory.Build.props` sets `<TargetFramework>net10.0</TargetFramework>` (line 6)

### Why this matters
`net10.0` is cutting-edge. If this is intended to be:
- open-source friendly
- easy to build on standard CI
- simple for contractors to contribute

…then you’ll reduce friction by pinning to `net8.0` LTS (or documenting the exact SDK and CI images).

**Work instructions**
1. Decide “public baseline” TFM: `net8.0` recommended
2. Add `global.json` with required SDK version (or document it in README)
3. Add CI build for Windows + Linux

**DoD**
- Fresh clone builds on Windows and Linux using documented SDK.

---

## 3) The Data Contract: Excel Sheets as DB Tables

### What you have
- A workbook (`data/SalesPipelineV1.0.xlsx`) treated as a “human DB”.
- Documentation started in `docs/schema.md`.

### What’s missing for long-term safety
A **schema version** and migration story.

**Work instructions**
1. Add `SchemaVersion` to the workbook (hidden sheet or Lookups metadata)
2. On repository load:
   - validate schema version
   - if mismatch: run migrator or fail with explicit message
3. Add a `SchemaMigrator` scaffold:
   - `v1 -> v2` transformations

**DoD**
- The repo can detect schema drift and explain exactly what changed.

---

## 4) Core Domain Review (Deals, Promoters, Lookups)

### Deal identity and determinism
File: `src/Ox4D.Core/Models/Deal.cs` (+ `DealNormalizer.cs`)

- Current `DealId` type: `string`
- New deal IDs appear to be generated in `DealNormalizer.cs` using `DateTime.UtcNow` + `Guid` (see `DealNormalizer.cs` around line 160)

**Design concern**
- ID generation is non-deterministic and time-dependent.
- This is fine in production, but it complicates:
  - reproducible synthetic data generation
  - deterministic test snapshots
  - tool idempotency

**Work instructions**
1. Add `IDealIdGenerator` abstraction in Core.
2. Default generator uses time+guid.
3. Tests and Mutate can inject a deterministic generator.

**DoD**
- You can generate the same dataset twice with same seed and get identical IDs.

### Money and currency
File: `src/Ox4D.Core/Models/Deal.cs`

- Amount is `decimal? AmountGBP`
- Currency is “encoded” in the property name

**Work instructions**
- Add `Money` value object (`decimal Amount`, `string Currency`)
- Migrate `AmountGBP` → `Amount` + `Currency` (or keep `GBP` for now but explicitly document “GBP only”).

**DoD**
- Schema supports multi-region later without renaming fields everywhere.

### Promoter / affiliate tracking
Files:
- `src/Ox4D.Core/Models/Promoter.cs`
- `src/Ox4D.Core/Services/PromoterService.cs` (uses `DateTime.UtcNow` at line ~75)

**Work instructions**
- Write an explicit “Attribution Policy” doc:
  - first-touch vs last-touch
  - expiry window for attribution
- Ensure promoter dashboards are derived from immutable deal facts.

**DoD**
- Commission numbers are reproducible and explainable.

---

## 5) Normalization Contract (Now strong — finish the loop)

File: `src/Ox4D.Core/Services/DealNormalizer.cs`

You already introduced:
- `NormalizationChange` record (line 9)

**Next step**
Propagate normalization changes to:
- CLI upserts/patch output
- MCP tool responses

**Work instructions**
- Change service APIs to return:
  - normalized deal
  - list of normalization changes
- Log or display changes consistently.

**DoD**
- Users can see what the system auto-fixed and why.

---

## 6) Persistence Design (ExcelDealRepository)

File: `src/Ox4D.Store/ExcelDealRepository.cs`

### What you nailed
- Safe save strategy:
  - temp write
  - validate temp
  - create backup
  - atomic move (Step 4 around line 339)

### Remaining architecture risks
- **Cross-process concurrency**: `lock` only protects within one process.
- Temp-file naming collisions: if two instances run, both will write to `~$...tmp.xlsx`.
- Cleanup swallow: there are intentional “ignore cleanup errors” catches (e.g., around lines 348, 472). This is okay for cleanup, but should be paired with logging so issues don’t go silent.

**Work instructions**
1. Add cross-process lock:
   - Windows: named mutex
   - Cross-platform: lock file + retry
2. Make temp file unique per save:
   - suffix with `Guid.NewGuid()`
3. Add structured logging hooks (even if Console-only initially)

**DoD**
- Two concurrent instances cannot corrupt or race the workbook.

---

## 7) Zarwin MCP Tooling (Determinism vs Patch Risk)

Files:
- `src/Ox4D.Zarwin/Handlers/ToolHandler.cs`
- `src/Ox4D.Zarwin/Protocol/ToolDefinitions.cs`
- `src/Ox4D.Zarwin/Protocol/JsonRpcMessage.cs`

### What’s good
- Tool surface is clean and aligned to business needs:
  - `pipeline.list_deals`, `pipeline.patch_deal`, `pipeline.hygiene_report`, etc.
- Handler code reads JSON safely (`TryGetProperty`, etc.)

### What needs architectural tightening
- Tool versioning and schemas:
  - add `v1` tool namespace or include `toolVersion`
- Patch route currently inherits Core’s patch risk (reflection/silent skip).

**DoD**
- MCP tools are stable APIs (versioned + schema-validated).

---

## 8) Console UI Architecture (keep it thin)

File: `src/Ox4D.Console/CopilotMenu.cs`

**Work instructions**
- Extract command handlers into separate classes:
  - `DealCommands`, `ReportCommands`, `PromoterCommands`
- Add `--json` output for automation parity with MCP.

---

## 9) Priority PR Stack (fastest ROI)
1. Replace reflection patching with typed DTO + explicit patch result
2. Cross-process Excel lock + unique temp name
3. Normalize change logs returned to CLI + MCP
4. MCP tool versioning + DTO schemas + contract tests
5. Schema versioning + migration scaffold
