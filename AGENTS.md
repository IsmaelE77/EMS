# AGENTS.md

Compact guidance for OpenCode sessions working in this repo. Every line is something an agent would likely get wrong without it.

## What this project is

EMS (Distributed Examination System) is a digital exam platform for universities where exams run in **physical exam centers** with potentially unstable internet. It uses a **local-first distributed architecture**: each exam center has its own local server (UI, answer storage, session management, result sync), while a central server handles authoring, scheduling, and grading. Authoritative design docs live in `backend/wiki/plans/` (`Distributed Examination System.md`, `Domain_Definition.md`, `Application_Services.md`, `Project_Structure.md`).

The two bounded contexts map directly to this split:
- **`Ems.ExamManagement`** — the **central server**: authoring & versioning (`ExamDefinition` → immutable `ExamInstance`), question bank (`QuestionPool`/`Question`), scheduling & access control (`ExamSchedule` + `ExamCenter`), grading/appeals (`ExamResult`/`ExamReview`), and identity (`Student`/`Instructor`/`StudentGroup` linked 1:1 to ABP `IdentityUser`, mutually exclusive).
- **`Ems.ExamExecution`** — the **local server at each exam center**: delivery (`DeliveredExam`, a synced read-only package with `DataVersion`), the student attempt state machine (`ExamSession` with single-session + 30s grace-period invariants), room management (`ExamCenterSession` with dynamic unlock codes), and sync tracking (`SyncRecord`).

Key design intentions to keep in mind when adding features:
- **Zero data loss**: results are written to an Outbox table in the same transaction as the session status update, then pushed to the central server via RabbitMQ when online (ABP Inbox/Outbox + `Volo.Abp.EventBus.Distributed`).
- **Notify-then-Download sync**: central publishes a lightweight `ExamPublishedEto`; the local server downloads structured data via ABP Dynamic C# API Client Proxies and large media via `IHttpClientFactory` + Polly streaming (not the proxy, to avoid loading large blobs into RAM).
- **Server-to-server auth**: OAuth2 client credentials — each exam center is a registered OpenIddict client (`ExamCenter.LinkedClientId`), scope `ExamManagement.Sync`.
- **Student auth**: JWT issued by the central server, validated offline on the local server (public key cached during exam download); the local server does **not** sync the full Identity module.
- **Roles**: `Admin`/`Instructor`/`Student` live in ExamManagement; `Proctor` lives in ExamExecution.
- **Immutability rules**: `ExamDefinition` cannot be edited once `Published`; `ExamInstance` is immutable (new version = new instance with bumped `DataVersion`); `DeliveredExam` is immutable (updates arrive as a new higher-`DataVersion` package, old one marked `Deprecated` while active sessions finish on it).

> Note: `backend/wiki/plans/` is the design plan. Some cross-cutting pieces (RabbitMQ wiring, Outbox/Inbox, blob storage, the `SyncRecord`/`ExamCenterSession` aggregates) may not be fully implemented yet — verify against the actual `src/` code before assuming a feature exists. The DDD layering and the aggregates listed in `Domain_Definition.md` are the intended domain model.

## Repository layout

- The repo root holds only `azure.yaml` (azd config), `next-steps.md` (azd boilerplate), and `backend/`.
- **All .NET source, tests, and the solution live under `backend/`.** Run all dotnet commands from `backend/`.
- `backend/Ems.slnx` is the umbrella solution that includes every project across all three areas. Build it to build everything.
- There is no `README` at the root; per-context READMEs at `backend/Ems.ExamManagement/README.md` and `backend/Ems.ExamExecution/README.md` cover ABP setup steps.

## Prerequisites

- .NET SDK **10.0+** (solution targets `net10.0`; `.slnx` requires a recent SDK).
- Node.js **20.11+**, **Yarn** (each `HttpApi.Host` has `yarn.lock`, not `package-lock.json`).
- ABP CLI: `dotnet tool install -g Volo.Abp.Cli`. Run `abp install-libs` in each `HttpApi.Host` directory when client-side libs are missing (LeptonXLite theme).
- No repo-level lint/format target is checked in (no `dotnet format` config, no `npm run lint`).

## Build, test, run (from `backend/`)

- Build all: `dotnet build Ems.slnx -v minimal`
- Test all: `dotnet test Ems.slnx --nologo -v minimal`
- Single test project: `dotnet test Ems.ExamManagement/test/Ems.ExamManagement.Domain.Tests/Ems.ExamManagement.Domain.Tests.csproj --nologo -v minimal`
- Single class: `dotnet test <proj> --filter "FullyQualifiedName~EfCoreSampleDomainTests" --nologo -v minimal`
- Single method (xUnit display name): `dotnet test <proj> --filter "DisplayName~Should_Set_Email_Of_A_User" --nologo -v minimal`
- Run the distributed app (Aspire): `dotnet run --project Ems.Aspire/Ems.AppHost/Ems.AppHost.csproj`
- Run a migrator manually (first run, or after adding a migration): `dotnet run --project Ems.ExamManagement/src/Ems.ExamManagement.DbMigrator/Ems.ExamManagement.DbMigrator.csproj` (and the `Ems.ExamExecution` equivalent).

## Architecture

Two ABP bounded contexts with mirrored DDD layering, plus shared infrastructure:

- `backend/Ems.ExamManagement/` and `backend/Ems.ExamExecution/` — each has the standard ABP layers: `Domain.Shared`, `Domain`, `Application.Contracts`, `Application`, `EntityFrameworkCore`, `HttpApi`, `HttpApi.Host`, `HttpApi.Client`, `DbMigrator`, and `test/{Application,Domain,EntityFrameworkCore}.Tests` + `TestBase`.
- `backend/Ems.Shared/src/Ems.Shared.Domain.Shared/` — shared constants/localization depended on by both contexts.
- `backend/Ems.Aspire/Ems.AppHost/` — Aspire orchestration entrypoint. `Ems.Aspire/Ems.ServiceDefaults/` wires service discovery, health endpoints (`/health`, `/alive` mapped **only in Development**), HTTP resilience, and OpenTelemetry for both hosts.
- Host stack: Serilog (file + console), Autofac DI, OpenIddict auth, `Volo.Abp.AspNetCore.MultiTenancy`, LeptonXLite MVC theme. ABP **10.1.1** across all packages.

### Aspire topology

`AppHost.cs` branches on the `ExperimentalMode` appsetting (default `false`):
- **Production mode**: one Azure Postgres Flexible Server (`ServiceNames.DatabaseServer`) hosting two databases — `ExamManagementDatabase` and `ExamExecutionDatabase`. Each migrator runs to completion before its `HttpApi.Host` starts (`WaitForCompletion`).
- **Development mode (`ExperimentalMode: true`)**: the management node gets its own Postgres server, plus `localCentresCount` (default 2) exam-execution nodes, **each with its own dedicated Postgres server and database**. Postgres container lifetime defaults to `ContainerLifetime.Session`.

Service names are constants in `Ems.ServiceDefaults/ServiceNames.cs` (e.g. `ServiceNames.ExamManagementServer`). **Reference these constants; do not hardcode the string names** — recent commits migrated the codebase off magical strings.

## Data

- **Runtime DB: PostgreSQL** (`postgres:15.15-trixie` container locally, Azure Postgres Flexible Server via azd). Default dev connection string is in each `HttpApi.Host/appsettings.json` (`User ID=postgres;Password=123`); dev params also come from `Ems.AppHost/appsettings.Development.json`.
- **Tests: SQLite in-memory** (`UseSqlite("Data Source=:memory:")` in `*EntityFrameworkCoreTestModule.cs`). **You do not need Postgres to run tests.** EF tests use xUnit collection fixtures (`*EntityFrameworkCoreCollectionFixtureBase`).
- Secrets: hosts call `AddAppSettingsSecretsJson()` which loads `appsettings.secrets.json` (gitignored). The OpenIddict signing cert `openiddict.pfx` is gitignored (`*.pfx`); generate with `dotnet dev-certs https -v -ep openiddict.pfx -p <password>` — the password differs per context (see each README).

## Conventions (verified against this codebase)

- **DTOs**: use ABP `UserLookupDto` for any user reference (creator, assignee, etc.) — requires an `IdentityUser` navigation property on the entity + EF config + AutoMapper `MapFrom(src => src.Creator)`. Expose `ConcurrencyStamp` on read **and** update DTOs for optimistic concurrency.
- **Repositories**: define a custom `I*Repository` in Domain and an `EfCore*Repository` in EntityFrameworkCore; inject the custom interface from application services. Do not use `IRepository<T>` / `IQueryableRepository<T>` directly in application services.
- **Validation constants**: define max-length / length constants in `Domain.Shared` and reference them from EF configuration. Never hardcode numeric lengths in entity config.
- **EF configuration lives in `*DbContext.cs` `OnModelCreating`** (there is no separate `*DbContextModelCreatingExtensions.cs` in this repo — the file named in `backend/.github/copilot-instructions.md` does not exist here). Table naming uses `[Context]Consts.DbTablePrefix` (currently `"App"`) + pluralized entity name.
- **Unique indexes must filter soft-deletes**: `.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0")`. Without the filter, recreating soft-deleted rows violates the constraint.
- **FK delete behavior**: `.OnDelete(DeleteBehavior.Restrict)` by default; `Cascade` only for owned entities. Enums → `.HasConversion<int>()`. Money → `.HasPrecision(18, 4)`, rates → `.HasPrecision(18, 6)`. `DateOnly` → `.HasColumnType("date")`.

## Deployment

- `azd up` (or `azd provision` then `azd deploy`) provisions Azure Container Apps infra from `azure.yaml` (service `app` → `Ems.AppHost.csproj`).
- CI: `.github/workflows/azure-dev.yml` runs on push to the **`production`** branch (not `main`) plus `workflow_dispatch`. The workflow installs .NET 8/9/10, Node 24, Yarn, ABP CLI, runs `abp install-libs`, then `azd provision` + `azd deploy` using federated Azure credentials plus `AZURE_USERNAME`/`AZURE_PASSWORD` secrets.
- azd environments live under `.azure/` (e.g. `ems-dev-v01`, `ems-prod`); `.azure/config.json` sets the default environment.

## Gotchas

- **Stale instructions file**: `backend/.github/copilot-instructions.md` exists but was copied from another project. Its concrete examples reference symbols that do **not** exist here — `Itcp*`, `Warehouses`, `Accounting`, `ChartOfAccounts`, `CoreConsts`/`WarehouseConsts`, and an "Arabic and English only" localization rule. This repo ships ABP's full default localization language set (`Localization/<Context>/{en,ar,fr,...}.json`). Trust the conventions section above over that file's examples.
- **Missing `./frontend` dirs**: `ExamManagementServices.cs` and `ExamExecutionServices.cs` call `AddJavaScriptApp(..., "./frontend")` per node, but no `frontend/` directory exists under `Ems.AppHost`. Aspire dev mode may fail resolving these JS apps until they are added.
- **Per-context `.sln` files are Rider user-settings** (`*.sln.DotSettings`); the buildable solutions are the `.slnx` files. `Ems.Aspire.slnx` contains only the AppHost + ServiceDefaults; use `Ems.slnx` for the whole repo.
- **ABP CLI is required for CI**, not just local dev — `abp install-libs` runs in the deploy workflow.


Instructions for EMS

## Build, test, and lint commands

### Prerequisites
- .NET SDK `10.0+`
- ABP CLI (used for client libs and some migration workflows)

### Restore and build
- Restore + build whole repo:
  - `dotnet build Ems.slnx -v minimal`

### Test
- Run all tests:
  - `dotnet test Ems.slnx --nologo -v minimal`
- Run a single test project:
  - `dotnet test Ems.ExamManagement/test/Ems.ExamManagement.Domain.Tests/Ems.ExamManagement.Domain.Tests.csproj --nologo -v minimal`
- Run a single test class:
  - `dotnet test Ems.ExamManagement/test/Ems.ExamManagement.EntityFrameworkCore.Tests/Ems.ExamManagement.EntityFrameworkCore.Tests.csproj --filter "FullyQualifiedName~EfCoreSampleDomainTests" --nologo -v minimal`
- Run a single test method (xUnit display name filter):
  - `dotnet test Ems.ExamManagement/test/Ems.ExamManagement.EntityFrameworkCore.Tests/Ems.ExamManagement.EntityFrameworkCore.Tests.csproj --filter "DisplayName~Should_Set_Email_Of_A_User" --nologo -v minimal`

### Run services
- Run distributed local environment (Aspire):
  - `dotnet run --project Ems.Aspire/Ems.AppHost/Ems.AppHost.csproj`
- Run DB migrators manually (first run and after migration changes):
  - `dotnet run --project Ems.ExamManagement/src/Ems.ExamManagement.DbMigrator/Ems.ExamManagement.DbMigrator.csproj`
  - `dotnet run --project Ems.ExamExecution/src/Ems.ExamExecution.DbMigrator/Ems.ExamExecution.DbMigrator.csproj`
- Install ABP client-side libraries when needed:
  - `abp install-libs`

### Lint/format
- No repository-level lint script or formatter command is currently defined (no `npm run lint`/`dotnet format` target checked into this repo).

## High-level architecture

- This repository contains two ABP bounded contexts with mirrored layered structure:
  - `Ems.ExamManagement`
  - `Ems.ExamExecution`
- Each context follows ABP layering:
  - `Domain.Shared` (localization resources, shared constants, module extensions)
  - `Domain` (domain services, multi-tenancy setup)
  - `Application.Contracts` and `Application` (service contracts + implementations)
  - `EntityFrameworkCore` (DbContext + migrations, PostgreSQL provider)
  - `HttpApi` + `HttpApi.Host` (API modules and host startup)
  - `HttpApi.Client` (typed client proxies)
  - `DbMigrator` (schema migration + data seeding)
  - `test/*` (Application/Domain/EF test projects + shared test base)
- `Ems.Shared` provides shared domain-shared module dependencies used by both contexts.
- `Ems.Aspire/Ems.AppHost` orchestrates local infrastructure:
  - single PostgreSQL container
  - separate databases for management and execution
  - migrators are run before API hosts start
- HTTP hosts use `Ems.ServiceDefaults` for service discovery, health endpoints (`/health`, `/alive` in Development), resilience defaults, and OpenTelemetry wiring.



## Development Notes

### Backend Configuration
- Uses Serilog for logging with file and console outputs
- Configured with Autofac for dependency injection
- OpenIddict for authentication/authorization
- Connection strings configured in `appsettings.json` files

### DTOs and User Information
**Always use UserLookupDto for user details in DTOs:**
- When DTOs need to include user information (creator, modifier, assignee, etc.), use `UserLookupDto` from ABP
- This requires the entity to have navigation properties to `IdentityUser` with proper EF Core configuration
- UserLookupDto provides standardized user summary details (Id, UserName, Name, Surname, Email) sufficient for most UI scenarios
- This ensures consistency across the application for how user information is displayed

**Concurrency Stamp:**
- Always expose the `ConcurrencyStamp` property in DTOs for entities that have it
- This enables optimistic concurrency control and prevents lost updates
- Include it in both read DTOs and update DTOs

**Example:**
```csharp
// Entity with navigation property
public class Request : FullAuditedAggregateRoot<Guid>
{
    public Guid CreatorId { get; set; }
    public IdentityUser Creator { get; set; } // Navigation property
}

// DTO with UserLookupDto and ConcurrencyStamp
public class RequestDto : EntityDto<Guid>
{
    public UserLookupDto Creator { get; set; } // ✓ Correct - unified user details
    // NOT: public string CreatorName { get; set; } // ✗ Wrong - custom user fields

    public string ConcurrencyStamp { get; set; } // ✓ Required for optimistic concurrency
}

public class UpdateRequestDto
{
    public string ConcurrencyStamp { get; set; } // ✓ Required - must be passed back for updates
    // ... other properties
}

// AutoMapper configuration
CreateMap<Request, RequestDto>()
    .ForMember(dest => dest.Creator, opt => opt.MapFrom(src => src.Creator));
```

### Entity Configuration (EF Core)
**Entity Framework Core configuration pattern:**
- All entity configurations are centralized in `ItcpDbContextModelCreatingExtensions.cs`
- Use inline configuration within static methods (not separate `IEntityTypeConfiguration<T>` classes)
- Group configurations by module with private static methods
- Apply all configurations via `ConfigureItcp()` method called from `DbContext.OnModelCreating()`

**Table Naming with Module Prefixes:**
- Use module-specific constants for table prefixes: `[Module]Consts.DbTablePrefix`
- Pattern: Module prefix + entity name pluralized
- Examples:
  - Core: `CoreConsts.DbTablePrefix + "GoOrganizationUnits"` → `AppGoOrganizationUnits`
  - Accounting: `AccountingConsts.DbTablePrefix + "ChartOfAccounts"` → `AppAccountingChartOfAccounts`
  - Warehouses: `WarehouseConsts.DbTablePrefix + "Materials"` → `AppWarehousesMaterials`
- This helps identify which module owns each table

**Critical: Unique Indexes with Soft Delete**
- **Always include soft delete filter on unique indexes**: `.HasFilter("[IsDeleted] = 0")`
- Without this filter, unique constraints will fail when trying to recreate soft-deleted records
- Example:
```csharp
b.HasIndex(x => x.Code)
    .IsUnique()
    .HasFilter("[IsDeleted] = 0"); // ✓ Required for soft delete support

// NOT: b.HasIndex(x => x.Code).IsUnique(); // ✗ Wrong - breaks soft delete
```

**Other Configuration Conventions:**
- Always call `.ConfigureByConvention()` after table configuration
- Mark required properties explicitly with `.IsRequired()`
- **Use domain validation constants from Domain.Shared for max lengths** (e.g., `WarehousesValidationConsts.Common.NameMaxLength`)
  - Never hardcode max length values in entity configuration
  - Define validation constants in Domain.Shared so they're shared across domain, validation, and EF configuration
- Use enum conversion: `.HasConversion<int>()` for enum properties
- Set decimal precision for monetary values: `.HasPrecision(18, 4)`
- Set high precision for rates/percentages: `.HasPrecision(18, 6)`
- Use `.HasColumnType("date")` for DateOnly properties
- Configure foreign keys with `.OnDelete(DeleteBehavior.Restrict)` by default (use Cascade only for owned entities)
- Use `.Ignore()` for heavy navigation collections that should be loaded explicitly

**Configuration Structure Example:**
```csharp
// Domain.Shared - define validation constants
public static class WarehousesValidationConsts
{
    public static class Common
    {
        public const int NameMaxLength = 256;
        public const int DescriptionMaxLength = 500;
    }

    public static class Material
    {
        public const int CodeLength = 20;
    }
}

// EntityFrameworkCore - use the constants
private static void ConfigureMaterial(ModelBuilder builder)
{
    builder.Entity<Material>(b =>
    {
        // 1. Table name with module prefix
        b.ToTable(WarehouseConsts.DbTablePrefix + "Materials", WarehouseConsts.DbSchema);

        // 2. Apply ABP conventions
        b.ConfigureByConvention();

        // 3. Property configurations - use domain constants
        b.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(WarehousesValidationConsts.Common.NameMaxLength); // ✓ Correct

        b.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(WarehousesValidationConsts.Material.CodeLength); // ✓ Correct

        // NOT: .HasMaxLength(256) // ✗ Wrong - hardcoded value

        // 4. Indexes - ALWAYS with soft delete filter for unique indexes
        b.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // 5. Relationships
        b.HasOne(x => x.BaseUnit)
            .WithMany()
            .HasForeignKey(x => x.BaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    });
}
```

### Repository Pattern
**Always define custom repository interfaces for aggregate roots:**
- Do define a repository interface (e.g., `IChartOfAccountsRepository`, `IMaterialRepository`) in the Domain layer
- Do create corresponding EF Core implementations (e.g., `EfCoreChartOfAccountsRepository`) in the EntityFrameworkCore layer
- Do always use the custom repository interface from application services

**Avoid generic repositories:**
- Don't use `IRepository<TEntity>` or `IQueryableRepository<TEntity>` directly in application services
- This prevents IQueryable from leaking into application/presentation layers
- Keeps data access concerns isolated to the infrastructure layer
- Exceptions must be explicitly discussed and justified during implementation

**Example structure:**
```csharp
// Domain layer
public interface IChartOfAccountsRepository : IRepository<ChartOfAccounts, Guid>
{
    Task<ChartOfAccounts> FindByCodeAsync(string code);
    Task<List<ChartOfAccounts>> GetActiveChartsAsync();
}

// EntityFrameworkCore layer
public class EfCoreChartOfAccountsRepository : EfCoreRepository<ItcpDbContext, ChartOfAccounts, Guid>, IChartOfAccountsRepository
{
    // Implementation with EF Core specific queries
}

// Application layer - Always inject the custom interface
public class ChartOfAccountsAppService : ItcpAppService
{
    private readonly IChartOfAccountsRepository _repository; // ✓ Correct
    // NOT: IRepository<ChartOfAccounts, Guid> _repository   // ✗ Wrong
}
```

### Error Handling and Localization
**Use domain error codes and business exceptions:**
- Define domain-specific error codes for business rule violations
- Throw `BusinessException` with appropriate error codes when business rules are violated
- **All error codes must be localized in Arabic and English only**
- **All permission names must be localized in Arabic and English only**

**Guidelines:**

- Define error code constants following the pattern: `[Module]:[Entity]:[ErrorType]`
- Add localization entries to the Arabic localization files (`.json` files in `Localization` folder)
- Use descriptive, user-friendly Arabic messages that clearly explain the error
- Error messages should be actionable and help users understand what went wrong

**Example:**
```csharp
// Domain error code constant
public static class ChartOfAccountsErrorCodes
{
    public const string CodeAlreadyExists = "Go.Itcp:ChartOfAccounts:001";
    public const string CannotDeleteActiveChart = "Go.Itcp:ChartOfAccounts:002";
}

// Throwing business exception
if (await _repository.AnyAsync(x => x.Code == code))
{
    throw new BusinessException(ChartOfAccountsErrorCodes.CodeAlreadyExists)
        .WithData("Code", code);
}

// Localization file (ar.json)
{
    "Go.Itcp:ChartOfAccounts:001": "الكود '{Code}' مستخدم مسبقاً",
    "Go.Itcp:ChartOfAccounts:002": "لا يمكن حذف دليل حسابات نشط",
    "Permission:ChartOfAccounts.Create": "إنشاء دليل حسابات",
    "Permission:ChartOfAccounts.Edit": "تعديل دليل حسابات"
}
```

### Demo Data Seeding
**Always provide demo data for new features:**
- New feature implementations should include demo data seeding to provide realistic data for testing and demonstration
- Example data should be **regionally friendly** (prefer Middle Eastern context: Arabic names, local companies, regional conventions)

**Guidelines:**
- Create demo data seeders for new entities following the existing ordered seeder pattern
- Provide realistic, meaningful data that represents actual use cases
- Use culturally appropriate examples (Arabic/Middle Eastern names, local business scenarios, regional date/number formats)
- Ensure seeded data respects entity relationships and business rules
- Demo data should showcase the feature's functionality effectively

