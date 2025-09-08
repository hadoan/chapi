You are generating a new ShipMvp module named: {ModuleName} (singular) / {ModuleNamePlural} (plural).
Follow the existing Projects module conventions (folder: modules/Projects) for structure, naming, style, nullability, and EF Core patterns.

High-level goals

- Create a full vertical slice: Domain, Application, Infrastructure, Controllers, Module.cs.
- Use clean architecture boundaries: Domain (pure), Application (use cases), Infrastructure (EF + external), Controllers (API endpoints).
- Use async, cancellation tokens, and immutable-ish patterns where practical.
- Respect existing base types (ShipMvp.Core.*). Follow the Entity<TId>, IRepository<T,TId> patterns.
- Use [Module] attribute and IModule interface for module registration.

Root folder: modules/{ModuleNamePlural}

1. Domain (modules/{ModuleNamePlural}/Domain)
   - Entities: Main aggregate root {ModuleName} : Entity<Guid>
   - Child/value entities if needed (e.g. {ModuleName}Item) similar to ProjectTask pattern
   - Enums for statuses/states (e.g. {ModuleName}Status)
   - Factory/static creation methods where appropriate
   - Methods enforcing invariants (e.g. state transitions)
   - Private parameterless constructors for EF Core
   - Repository interface: I{ModuleName}Repository : IRepository<{ModuleName}, Guid>
   - Avoid direct infrastructure concerns

2. Application (modules/{ModuleNamePlural}/Application)
   - Dtos/ folder: {ModuleName}Dto.cs, {ModuleName}Dtos.cs (can be single file with multiple DTOs)
   - Requests/ folder: Create{ModuleName}Request.cs, Update{ModuleName}Request.cs
   - Services/ folder:
       - I{ModuleName}Service.cs (write operations: Create, Update, Delete, status transitions)
       - I{ModuleName}ReadService.cs (read operations: GetById, List with paging/filter)
       - {ModuleName}Service.cs (implementation with [AutoController] attribute for API generation)
   - Mappings/ folder: {ModuleName}Mappings.cs (static extension methods Domain → DTO)
   - Query records for list operations (e.g. Get{ModuleNamePlural}Query with paging/filter params)
   - Validation via guard clauses in service methods or TODO comments for FluentValidation

3. Infrastructure (modules/{ModuleNamePlural}/Infrastructure)
   - EF Core configurations:
       - {ModuleName}Config.cs : IEntityTypeConfiguration<{ModuleName}>
       - {ModuleName}ItemConfig.cs : IEntityTypeConfiguration<{ModuleName}Item> (if child entities exist)
       - Table names: "{ModuleNamePlural}" with schema (e.g. "Project")
       - Relationships, owned types, property constraints
   - Data/ folder: Repository implementations
       - {ModuleName}Repository.cs : I{ModuleName}Repository with [UnitOfWork] attribute
   - ModelBuilderExtensions.cs for entity registration
   - Optional: DependencyInjection.cs or extension methods for service registration
   - Do not leak EF types into Domain or Application layers

4. Controllers (modules/{ModuleNamePlural}/Controllers)
   - {ModuleNamePlural}Controller.cs (e.g. route: [Route("api/{module-name-plural}")])
   - Use [ApiController] and [Authorize] attributes
   - Endpoints:
       POST / → create (returns 201 Created with Location)
       GET /{id} → fetch by id (returns 404 if not found)
       GET / → list (query params via GetProjectsQuery pattern)
       PUT /{id} → update
       DELETE /{id} → delete (follow soft delete pattern if used)
       POST /{id}/activate, /{id}/complete etc. for status transitions
   - Inject IProjectService (not split read/write interfaces in controller)
   - Use minimal DTOs in request/response
   - Accept CancellationToken parameter

5. Module bootstrap (modules/{ModuleNamePlural}/{ModuleNamePlural}Module.cs)
   - Class: {ModuleNamePlural}Module : IModule with [Module] attribute
   - Method: ConfigureServices(IServiceCollection services)
       - services.AddControllers().AddApplicationPart(typeof({ModuleNamePlural}Module).Assembly)
       - Register repositories: AddScoped<I{ModuleName}Repository, {ModuleName}Repository>()
       - Register services: AddTransient<I{ModuleName}Service, {ModuleName}Service>()
       - Call module-specific extension: services.Add{ModuleNamePlural}Module()
   - Method: Configure(IApplicationBuilder app, IHostEnvironment env) (placeholder)
   - Optional: separate Module.cs with static extension methods for DI registration

6. Repositories
   - Interface: I{ModuleName}Repository : IRepository<{ModuleName}, Guid> with additional methods:
       Task<IEnumerable<{ModuleName}>> GetByStatusAsync({ModuleName}Status status, CancellationToken ct)
       Task<(IEnumerable<{ModuleName}> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, {ModuleName}Status? status, string? search, CancellationToken ct)
   - Base interface provides: GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync
   - Implementation uses IDbContext and DbSet<{ModuleName}> with [UnitOfWork] attribute
   - Include related entities (.Include()) for navigation properties
   - Add guarded null checks & proper SaveChangesAsync calls

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
       {ModuleName}Item.cs (if nested entities)
       I{ModuleName}Repository.cs
     Application/
       Dtos/{ModuleName}Dto.cs (or {ModuleName}Dtos.cs with multiple DTOs)
       Requests/Create{ModuleName}Request.cs
       Requests/Update{ModuleName}Request.cs
       Services/I{ModuleName}Service.cs
       Services/I{ModuleName}ReadService.cs (optional)
       Services/{ModuleName}Service.cs
       Mappings/{ModuleName}Mappings.cs
     Infrastructure/
       {ModuleName}Config.cs
       {ModuleName}ItemConfig.cs (if nested)
       ModelBuilderExtensions.cs
       Data/{ModuleName}Repository.cs
     Controllers/
       {ModuleNamePlural}Controller.cs
     {ModuleNamePlural}Module.cs
     Module.cs (optional DI extensions)

14. Coding style

- Use expression-bodied members where concise
- Prefer readonly for collections exposed via AsReadOnly if mutation control required
- Initialize navigation collections
- Use cancellation tokens in all async public methods
- Use explicit access modifiers

15. Provide complete compilable C# code for every file (no placeholders except TODO comments).

- If unsure about a dependency (e.g. ShipMvp.Core.*), add a TODO with expected interface.

Generate now all described files for the {ModuleNamePlural} module in one response, grouped by file path, each in a separate C# code block with filepath comment.
