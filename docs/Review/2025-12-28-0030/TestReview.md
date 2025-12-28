# TestReview.md — Extensive Test Alignment Review

Snapshot: `Ox4D-main-2025-12-28-0028.zip`

---

## 1) What you already have (excellent)
- Unit tests for models, filtering, normalization, services
- Repo contract tests for Excel and InMemory
- Excel failure mode tests
- Synthetic data generator tests

This is the correct test portfolio for an “Excel-as-DB” system.

---

## 2) Critical missing tests (because the code currently allows silent failure)

### A) Patch semantics tests (highest priority)
Add tests ensuring:
- Unknown patch keys are rejected
- Invalid types are rejected
- Patch returns applied vs rejected fields with reasons
- No silent skipping

These tests must cover both:
- Core service patch route
- MCP patch tool route

### B) Cross-process concurrency integration test
Because the repository uses in-process `lock`, add an integration test that:
- starts two repository instances writing concurrently
- verifies file integrity and expected final state

### C) Deterministic synthetic data test
Because IDs and time are currently time-based:
- Add a seeded generation test that asserts identical output for same seed
- This will require injecting an ID generator and clock

### D) MCP contract tests
For each tool method:
- validate request schema
- validate response schema
- validate error envelope stability

---

## 3) CI recommendations
- Test on Windows + Linux
- Upload corrupted workbook artifacts on failure
- Gate coverage:
  - Core high
  - Store medium
  - Zarwin medium

---

## Definition of Done
- Patch routes are fully validated and never silently ignore errors
- Excel save path is safe under concurrent usage
- Synthetic data generation is reproducible with a seed
- MCP contracts are versioned, documented, and tested
