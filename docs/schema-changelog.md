# Schema Changelog

> **Purpose:** Track all schema changes for forward compatibility and migration support
> **Current Version:** 1.2
> **Supported Versions:** 1.0, 1.1, 1.2

---

## Schema 1.2 (Current)

**Released:** 2025-12-28

**Changes:**
- Added typed patch validation (`DealPatch` DTO + `PatchResult`)
- Added `IClock` injection for deterministic timestamps in Metadata sheet
- Added `IDealIdGenerator` abstraction for reproducible ID generation
- Added MCP tool versioning (`v1.0` for all tools)
- Added cross-process Excel locking with retry logic

**Migration from 1.1:**
- Automatic: Version property in Metadata sheet updated to "1.2"
- No column changes to Deals sheet
- No changes to Lookups sheet

**Behavior Changes:**
- `LastModified` in Metadata now uses injectable clock (testable)
- Backup filenames use injectable clock timestamps
- Deal ID generation can be seeded for reproducibility

---

## Schema 1.1

**Released:** 2025-12-27

**Changes:**
- Added explicit `Version` property to Metadata sheet
- Established Metadata sheet as required for version tracking

**Migration from 1.0:**
- Automatic: Metadata sheet created if missing
- Automatic: Version property added with value "1.1"

**New Metadata Properties:**
| Property | Type | Description |
|----------|------|-------------|
| Version | string | Schema version (e.g., "1.1") |

---

## Schema 1.0 (Legacy)

**Released:** Initial release

**Characteristics:**
- No explicit version in Metadata sheet
- Metadata sheet optional (warning if missing)
- Basic deal tracking with all core columns

**Sheets:**
| Sheet | Required | Purpose |
|-------|----------|---------|
| Deals | Yes | Pipeline deals (primary data) |
| Lookups | No | Configuration (postcode→region, stage→probability) |
| Metadata | No | Audit information |

**Deals Columns (unchanged since 1.0):**
- Identity: DealId, OrderNo, UserId
- Account: AccountName, ContactName, Email, Phone
- Location: Postcode, PostcodeArea, InstallationLocation, Region, MapLink
- Details: LeadSource, ProductLine, DealName
- Stage/Value: Stage, Probability, AmountGBP, WeightedAmountGBP
- Ownership: Owner, CreatedDate, LastContactedDate
- Actions: NextStep, NextStepDueDate, CloseDate
- Service: ServicePlan, LastServiceDate, NextServiceDueDate
- Metadata: Comments, Tags
- Promoter: PromoterId, PromoCode, PromoterCommission, CommissionPaid, CommissionPaidDate

---

## Migration Behavior

### Automatic Migration
Files with older supported versions are automatically migrated on load:
1. Migration chain: 1.0 → 1.1 → 1.2
2. Original file is backed up before migration
3. Migrated file is saved with updated schema version

### Unsupported Versions
Files with unsupported future versions (e.g., "2.0") are rejected with a clear error message instructing users to update Ox4D.

### Adding New Schema Versions
When adding new columns or changing behavior:

1. **Increment version** in `ExcelDealRepository.CurrentSchemaVersion`
2. **Add to supported array** in `ExcelDealRepository.SupportedSchemaVersions`
3. **Implement migration method** `MigrateFrom_X_To_Y()`
4. **Update this changelog** with changes and migration behavior
5. **Add tests** for migration correctness

---

## Third-Party Integration Notes

External systems integrating with Ox4D Excel files should:

1. **Check schema version** in Metadata sheet before processing
2. **Handle missing Metadata** as schema 1.0
3. **Validate required columns** exist in Deals sheet
4. **Preserve unknown columns** when writing back (forward compatibility)

---

*Cross-references: [schema.md](schema.md) | [architecture.md](architecture.md)*
