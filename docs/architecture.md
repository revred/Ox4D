# Ox4D Architecture Documentation

## Overview

Ox4D Sales Pipeline Manager is a .NET application designed for sales teams to manage their deal pipeline. It uses a layered architecture that separates concerns and enables future migration from Excel to cloud storage (Supabase).

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Ox4D Sales Pipeline                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   ┌──────────────────┐    ┌──────────────────┐                     │
│   │  Ox4D.Console    │    │  Ox4D.Zarwin     │                     │
│   │  (Interactive UI)│    │  (MCP Server)    │                     │
│   └────────┬─────────┘    └────────┬─────────┘                     │
│            │                        │                               │
│            └───────────┬────────────┘                               │
│                        │                                            │
│            ┌───────────▼───────────┐                               │
│            │    Ox4D.Core          │                               │
│            │  (Domain + Services)   │                               │
│            └───────────┬───────────┘                               │
│                        │                                            │
│            ┌───────────▼───────────┐                               │
│            │    Ox4D.Store         │                               │
│            │  (IDealRepository)    │                               │
│            └───────────┬───────────┘                               │
│                        │                                            │
│       ┌────────────────┼────────────────┐                          │
│       │                │                 │                          │
│   ┌───▼───┐    ┌───────▼───────┐   ┌────▼────┐                    │
│   │ Excel │    │   In-Memory   │   │Supabase │                    │
│   │(.xlsx)│    │   (Testing)   │   │(Future) │                    │
│   └───────┘    └───────────────┘   └─────────┘                    │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

## Project Structure

| Project | Purpose | Dependencies |
|---------|---------|--------------|
| **Ox4D.Core** | Domain models, business logic, service interfaces | None |
| **Ox4D.Store** | Repository implementations (Excel, InMemory) | Ox4D.Core, ClosedXML |
| **Ox4D.Mutate** | Synthetic data generation | Ox4D.Core, Bogus |
| **Ox4D.Console** | Interactive terminal UI | Ox4D.Core, Ox4D.Store, Spectre.Console |
| **Ox4D.Zarwin** | MCP server for LLM integration | Ox4D.Core, Ox4D.Store |
| **Ox4D.Tests** | Unit tests | All projects, xUnit, FluentAssertions |

## Layer Responsibilities

### Presentation Layer (Console, Zarwin)

- **Ox4D.Console**: Menu-driven terminal interface using Spectre.Console
- **Ox4D.Zarwin**: JSON-RPC over stdio for LLM tool integration (MCP protocol)

Both presentation layers are thin wrappers that delegate to Core services.

### Core Layer (Ox4D.Core)

Contains all business logic and domain models:

- **Models**: Deal, DealStage, DealFilter, Report DTOs
- **Services**: PipelineService, ReportService, DealNormalizer, PromoterService
- **Interfaces**: IDealRepository (storage abstraction)
- **Config**: LookupTables, PipelineSettings

Key design principle: **Zero storage dependencies**. Core knows nothing about how data is persisted.

### Store Layer (Ox4D.Store)

Implements IDealRepository for different backends:

- **ExcelDealRepository**: Production storage using sheet-per-table design
- **InMemoryDealRepository**: Fast in-memory storage for testing

Features:
- Atomic save with temp file validation
- Automatic backup rotation
- File integrity validation on load
- Thread-safe with file locking

## Key Design Patterns

### Repository Pattern
```csharp
public interface IDealRepository
{
    Task<IReadOnlyList<Deal>> GetAllAsync(CancellationToken ct = default);
    Task<Deal?> GetByIdAsync(string dealId, CancellationToken ct = default);
    Task<IReadOnlyList<Deal>> QueryAsync(DealFilter filter, DateTime referenceDate, CancellationToken ct = default);
    Task UpsertAsync(Deal deal, CancellationToken ct = default);
    Task DeleteAsync(string dealId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

### Defensive Copies
All repository methods return cloned objects to prevent external modification of internal state.

### Normalization with Change Tracking
DealNormalizer applies business rules and tracks all changes:
```csharp
var result = normalizer.NormalizeWithTracking(deal);
// result.Changes contains list of all modifications made
```

### Clock Abstraction
IClock interface enables deterministic testing:
```csharp
var report = new ReportService(repo, settings, FixedClock.AtDate(2025, 1, 15));
```

## Data Flow

### Read Path
1. Presentation layer calls Core service
2. Core service calls IDealRepository
3. Repository returns cloned domain objects
4. Core applies business logic
5. Results returned to presentation

### Write Path
1. Presentation layer calls Core service with input
2. Core normalizes and validates data
3. Core calls IDealRepository.UpsertAsync()
4. Repository stores (marks dirty for batch save)
5. SaveChangesAsync() commits to storage atomically

## Error Handling

### MCP Server Errors
Structured error envelope with:
- Standard JSON-RPC error codes
- Application-specific codes (-32001 to -32004)
- Machine-readable ErrorData with type, description, timestamp

### Excel Validation
On load and save:
- Required sheets: Deals
- Required columns: DealId, AccountName, DealName, Stage
- Auto-restore from backup on corruption

## Testing Strategy

### Contract Tests
Abstract `RepositoryContractTests<T>` base class ensures all IDealRepository implementations behave identically.

### Test Categories
- Unit tests: Pure business logic
- Integration tests: Repository with real files
- Failure mode tests: Corrupted files, missing data

## Future: Supabase Migration

When migrating to Supabase:

1. Implement `SupabaseDealRepository : IDealRepository`
2. Replace DI registration
3. No changes needed to Core, Console, or Zarwin

The sheet-per-table Excel design maps directly to PostgreSQL tables.
