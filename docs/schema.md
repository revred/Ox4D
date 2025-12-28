# Ox4D Data Schema Documentation

## Excel Workbook Structure

The primary data file (`data/SalesPipelineV1.0.xlsx`) uses a sheet-per-table design that maps directly to future Supabase tables.

### Sheets Overview

| Sheet | Purpose | Required |
|-------|---------|----------|
| **Deals** | Main pipeline deals table | Yes |
| **Lookups** | Configuration data (postcode→region, stage→probability) | No (warning if missing) |
| **Metadata** | Version info, timestamps | No (warning if missing) |

---

## Deals Sheet Schema

Primary pipeline data. Each row represents one deal/opportunity.

### Required Columns

| Column | Type | Description |
|--------|------|-------------|
| DealId | string | Unique identifier (format: `D-YYYYMMDD-XXXXXXXX`) |
| AccountName | string | Company/account name |
| DealName | string | Opportunity/deal name |
| Stage | string | Current pipeline stage |

### Identity Columns

| Column | Type | Description |
|--------|------|-------------|
| OrderNo | string | External order number |
| UserId | string | Customer user ID |

### Contact Columns

| Column | Type | Description |
|--------|------|-------------|
| ContactName | string | Primary contact name |
| Email | string | Contact email address |
| Phone | string | Contact phone number |

### Location Columns

| Column | Type | Description |
|--------|------|-------------|
| Postcode | string | UK postcode (e.g., "SW1A 1AA") |
| PostcodeArea | string | Extracted area code (e.g., "SW") |
| InstallationLocation | string | Full address |
| Region | string | Derived from postcode (e.g., "London") |
| MapLink | string | Google Maps URL (auto-generated) |

### Deal Details

| Column | Type | Description |
|--------|------|-------------|
| LeadSource | string | How the lead was acquired |
| ProductLine | string | Product/service category |
| Stage | DealStage | Pipeline stage (see enum below) |
| Probability | int | Win probability (0-100) |
| AmountGBP | decimal | Deal value in GBP |
| WeightedAmountGBP | decimal | Amount × Probability (computed) |

### Ownership & Dates

| Column | Type | Description |
|--------|------|-------------|
| Owner | string | Sales rep/deal owner |
| CreatedDate | DateTime | When deal was created |
| LastContactedDate | DateTime | Last customer contact |
| NextStep | string | Next action description |
| NextStepDueDate | DateTime | When next step is due |
| CloseDate | DateTime | Expected close date |

### Service Columns

| Column | Type | Description |
|--------|------|-------------|
| ServicePlan | string | Service plan type |
| LastServiceDate | DateTime | Last service date |
| NextServiceDueDate | DateTime | Next service due |

### Metadata

| Column | Type | Description |
|--------|------|-------------|
| Comments | string | Free-text notes |
| Tags | string | Comma-separated tags |

### Promoter Columns

| Column | Type | Description |
|--------|------|-------------|
| PromoterId | string | Referring promoter ID |
| PromoCode | string | Promo code used |
| PromoterCommission | decimal | Commission amount |
| CommissionPaid | bool | "Yes" or "No" |
| CommissionPaidDate | DateTime | When commission was paid |

---

## DealStage Enum

```
Lead → Qualified → Discovery → Proposal → Negotiation → Closed Won / Closed Lost / On Hold
```

| Stage | Default Probability | Description |
|-------|---------------------|-------------|
| Lead | 10% | Initial inquiry |
| Qualified | 30% | Confirmed fit and budget |
| Discovery | 40% | Requirements gathering |
| Proposal | 60% | Proposal submitted |
| Negotiation | 80% | Terms discussion |
| ClosedWon | 100% | Deal won |
| ClosedLost | 0% | Deal lost |
| OnHold | 0% | Temporarily paused |

---

## Lookups Sheet Schema

Configuration tables for normalization rules.

### Postcode to Region Mapping (Columns A:B)

| PostcodeArea | Region |
|--------------|--------|
| SW | London |
| M | North West |
| B | Midlands |
| EH | Scotland |
| ... | ... |

### Stage to Probability Mapping (Columns D:E)

| Stage | DefaultProbability |
|-------|-------------------|
| Lead | 10 |
| Qualified | 30 |
| ... | ... |

---

## Metadata Sheet Schema

Version and audit information.

| Property | Value |
|----------|-------|
| Version | 1.1 |
| LastModified | ISO timestamp |
| DealCount | Number of deals |
| GeneratedBy | "Ox4D Sales Pipeline Manager" |

---

## Normalization Rules

Applied automatically when deals are loaded or created:

1. **DealId**: Auto-generate if missing (`D-YYYYMMDD-XXXXXXXX`)
2. **Probability**: Set from stage lookup if zero or missing
3. **PostcodeArea**: Extract from Postcode (first letters before number)
4. **Region**: Derive from PostcodeArea using lookups
5. **MapLink**: Generate Google Maps URL from address/postcode
6. **CreatedDate**: Default to today if missing
7. **Tags**: Trim whitespace, remove duplicates

---

## Validation Rules

### On Load
- Deals sheet must exist
- Required columns must be present
- Invalid files trigger restore from backup

### Hygiene Report Checks
- Missing AmountGBP (Medium severity)
- Missing CloseDate for Proposal+ stage (High severity)
- Missing NextStep (Medium severity)
- Missing NextStepDueDate when NextStep exists (Low severity)
- Probability >30% off expected for stage (Medium/High severity)
- Missing Postcode (Low severity)
- Missing Email AND Phone (Medium severity)
- Missing Owner (High severity)

---

## File Integrity

### Atomic Save Strategy
1. Write to temp file (`~$filename.xlsx.tmp.xlsx`)
2. Validate temp file structure
3. Create timestamped backup of current file
4. Replace original with temp file (atomic move)

### Backup Rotation
- Format: `filename_YYYYMMDD_HHMMSS.xlsx.bak`
- Default retention: 5 most recent backups
- Configurable via `maxBackups` parameter

### Auto-Recovery
On load failure:
1. Attempt restore from most recent backup
2. Validate restored file
3. Throw if no valid backup available
