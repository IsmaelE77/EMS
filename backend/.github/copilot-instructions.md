# Copilot Instructions for EMS

## Build, test, and lint commands

### Prerequisites
- .NET SDK `10.0+`
- Node.js `20.11+`
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
