You are generating a new ShipMvp module named: {ModuleName} (singular) / {ModuleNamePlural} (plural).
Follow the existing Invoices module conventions (folder: modules/Invoices) for structure, naming, style, nullability, and EF Core patterns.

High-level goals
- Create a full vertical slice: Domain, Application, Infrastructure, Controllers, Module.cs.
- Use clean architecture boundaries: Domain (pure), Application (use cases), Infrastructure (EF + external), Presentation (Controllers).
- Use async, cancellation tokens, and immutable-ish patterns where practical.
- Respect existing base types (ShipMvp.Core.*). If a referenced abstraction does not exist, stub an interface with TODO.

Root folder: modules/{ModuleNamePlural}

1. Domain (modules/{ModuleNamePlural}/Domain)
   - Entities: Main aggregate root {ModuleName} : Entity<Guid>
   - Child/value entities if needed (e.g. {ModuleName}Item) similar to InvoiceItem pattern
   - Enums for statuses/states
   - Factory/static creation methods where appropriate
   - Methods enforcing invariants (e.g. state transitions)
   - Private parameterless constructors for EF Core
   - Avoid direct infrastructure concerns

2. Application (modules/{ModuleNamePlural}/Application)
   - DTOs / Records: {ModuleName}Dto, Create{ModuleName}Request, Update{ModuleName}Request
   - Commands & Queries (if MediatR is in use; otherwise simple services):
       Commands: Create{ModuleName}, Update{ModuleName}, Delete{ModuleName}, Transition/Status actions
       Queries: Get{ModuleName}ById, List{ModuleNamePlural} (with paging/filter params)
   - Handlers / Services implementing business workflows
   - Mapping helpers (extension methods Domain → DTO)
   - Validation (basic guard clauses or FluentValidation if present; otherwise TODO comments)
   - Interfaces: I{ModuleName}Service, I{ModuleName}ReadService (splitting read/write if helpful)

3. Infrastructure (modules/{ModuleNamePlural}/Infrastructure)
   - EF Core configurations:
       - IEntityTypeConfiguration<{ModuleName}>
       - Table names: "{ModuleNamePlural}"
       - Relationships, owned types, property constraints
   - Repository implementations: {ModuleName}Repository : I{ModuleName}Repository
   - Persistence bridging (e.g. add registrations in an extension method)
   - Optional: Outbox/events publisher placeholders (with TODO) if pattern is used elsewhere
   - Do not leak EF types into Domain or Application layers

4. Controllers (modules/{ModuleNamePlural}/Controllers)
   - {ModuleName}Controller (e.g. route: /api/{module-name-plural})
   - Endpoints:
       POST / → create
       GET /{id} → fetch by id
       GET / → list (query params: page, pageSize, filters)
       PUT /{id} → update
       DELETE /{id} → delete (soft or hard — follow Invoice precedent)
       PATCH /{id}/status (only if status transitions exist)
   - Use minimal DTOs in request/response
   - Return proper status codes (201 Created with Location on create, 404 when not found)
   - Accept CancellationToken

5. Module bootstrap (modules/{ModuleNamePlural}/Module.cs)
   - Public static class {ModuleName}Module (or {ModuleNamePlural}Module)
   - Method: Add{ModuleNamePlural}Module(this IServiceCollection services)
       - Registers DbContext mapping (if modularized), repositories, services, handlers
   - (Optional) Map{ModuleNamePlural}Endpoints(WebApplication app) if minimal APIs used
   - Follow existing Invoice module naming (inspect Module.cs there)

6. Repositories
   - Interface: I{ModuleName}Repository with methods:
       Task<{ModuleName}?> GetByIdAsync(Guid id, CancellationToken ct)
       Task AddAsync({ModuleName} entity, CancellationToken ct)
       Task UpdateAsync({ModuleName} entity, CancellationToken ct)
       Task DeleteAsync({ModuleName} entity, CancellationToken ct)
       IQueryable<{ModuleName}> Query()  // if pattern exists; otherwise omit
   - Implementation uses EF Core DbContext (e.g. AppDbContext or dedicated module context)
   - Add guarded null checks & tracking considerations

7. Naming & namespaces
   - Namespace roots: {ModuleNamePlural}.Domain, {ModuleNamePlural}.Application, {ModuleNamePlural}.Infrastructure, {ModuleNamePlural}.Controllers
   - Entities inside Domain only
   - DTOs end with Dto or Request
   - Keep files one type per file unless tiny enums

8. Concurrency & validation
   - Add basic guard clauses (throw ArgumentException / InvalidOperationException)
   - Add TODO markers where deeper validation or business rules are pending
   - Include optimistic concurrency token property (e.g. byte[] RowVersion) if used elsewhere (check Invoice example; if absent, add TODO)

9. Mapping
   - Provide static class {ModuleName}Mappings with methods:
       ToDto(this {ModuleName} entity)
       UpdateFrom(this {ModuleName} entity, Update{ModuleName}Request request)
   - Avoid direct AutoMapper unless already project standard (if yes, add Profile subclass)

10. Documentation & comments
   - XML summary comments on public entity classes, repositories, services, controller
   - TODO comments for security, validation, and transactional boundaries

11. Error handling
   - Return NotFound when entity missing
   - Basic validation returns 400 with problem details (stub if global middleware handles it)
   - Domain violations throw; assume global middleware translates to 400/422

12. Security (placeholder)
   - Add [Authorize] attribute if the rest of the API uses it; else add TODO: secure endpoint

13. Output structure (example)
   modules/{ModuleNamePlural}/
     Domain/
       {ModuleName}.cs
       {ModuleName}Status.cs (enum if needed)
       {ModuleName}Item.cs (if nested)
     Application/
       Dtos/{ModuleName}Dto.cs
       Requests/Create{ModuleName}Request.cs
       Requests/Update{ModuleName}Request.cs
       Services/I{ModuleName}Service.cs
       Services/{ModuleName}Service.cs
       Commands/Create{ModuleName}.cs (+ Handler)
       Queries/Get{ModuleName}ById.cs (+ Handler)
       Queries/List{ModuleNamePlural}.cs (+ Handler)
       Mappings/{ModuleName}Mappings.cs
     Infrastructure/
       Persistence/{ModuleName}EntityTypeConfiguration.cs
       Persistence/{ModuleName}Repository.cs
       Persistence/I{ModuleName}Repository.cs
       DependencyInjection.cs (Add{ModuleNamePlural}Infrastructure)
     Controllers/
       {ModuleName}Controller.cs
     Module.cs

14. Coding style
   - Use expression-bodied members where concise
   - Prefer readonly for collections exposed via AsReadOnly if mutation control required
   - Initialize navigation collections
   - Use cancellation tokens in all async public methods
   - Use explicit access modifiers

15. Provide complete compilable C# code for every file (no placeholders except TODO comments).
   - If unsure about a dependency (e.g. ShipMvp.Core.*), add a TODO with expected interface.

Generate now all described files for the {ModuleNamePlural} module in one response, grouped by file path, each in a separate C# code block with filepath comment.