# Chapi — chat-first, open-source API testing

Chapi is the chat-first, open-source API testing tool you're building. Instead of writing tests by hand, you describe what you want in chat and Chapi generates runnable test packs for you.

---

## What it does

- Turn natural-language test requests (from chat) into runnable test packs.
- Accept API specs (OpenAPI preferred) or snippets (Postman, cURL) and normalize endpoints.
- Generate runnable artifacts: shell scripts, JSON IR, test packs that can run locally or in CI.

---

## How it works

1. Import your API spec (OpenAPI preferred, or Postman / cURL snippets).
2. Chapi normalizes endpoints into a searchable endpoint catalog.
3. In chat you ask for tests (e.g., “Generate smoke tests for our user service”). Chapi plans the tests and emits runnable files (`.sh`, JSON IR, test packs).
4. Run locally (download a zero-install run pack) or run in the cloud (CI checks, GitHub PR comments).

---

## Why it matters

- Fast: move from idea to runnable tests in minutes.
- Grounded: tests are generated from your real API spec, reducing hallucinations.
- Flexible: zero-install local run packs for quick verification; scales to cloud CI and PR-based quality gates.
- Open and modular: open-source frontend and modular backend so you can extend or host it yourself.

---

## Quick summary (TL;DR)

Chapi = Postman + CI, but chat-native. Rather than wiring YAML or clicking UIs, you describe the tests in plain language and get executable test packs.

---

## Technical reference

Below are the developer-focused sections you requested: project structure, ShipMVP notes, Docker configuration, and common workflows.

### Architecture

- Built on the ShipMVP modular framework (see `/shipmvp` submodule).
- Modular backend: features live in `modules/<ModuleName>/` with separate Domain, Application, Infrastructure, and Controllers folders.
- Single host API: `apps/backend/Chapi.Api` discovers modules and exposes the HTTP API and Swagger.
- CLI tooling: `apps/backend/Chapi.CLI` provides administrative and development commands.
- Frontend: `apps/frontend` is an open-source UI (Vite + TypeScript).

Key notes:

- The host API wires a single DbContext and scans module assemblies for entity configurations.
- Migrations are generated from the host project (so generated SQL reflects the composed model across modules).

### Folder structure

```text
apps/
 backend/
  Chapi.Api/            # Host API (runs the app)
   Program.cs          # Application startup and module discovery
   Data/                # DbContext and database wiring
   appsettings.json     # Connection strings and local config
  Chapi.CLI/            # Command-line interface tool
   Program.cs          # CLI host with commands (seed-data, run-sql, etc.)
 frontend/               # Frontend (Vite (build tool) + TypeScript)
  src/
modules/                  # Feature modules (one folder per feature)
 <Feature>/
  Domain/
  Application/
  Infrastructure/
  Controllers/
shipmvp/                  # Git submodule: ShipMVP framework (do not edit directly)
```

### Prerequisites

- .NET 9 SDK (or the SDK version the solution targets)
- Docker (for local Postgres)
- (Optional) `psql` CLI for DB debugging

### Quick start — Docker (local Postgres)

1. Start PostgreSQL (data persists in a Docker volume):

```bash
docker run --name chapi-postgres \
 -e POSTGRES_USER=postgres \
 -e POSTGRES_PASSWORD=ChapiLocalPass! \
 -e POSTGRES_DB=chapi_dev \
 -e PGDATA=/var/lib/postgresql/data/pgdata \
 -p 5432:5432 \
 -v chapi-postgres-data:/var/lib/postgresql/data \
 --restart unless-stopped \
 -d postgres:latest
```

2. Configure connection string in `apps/backend/Chapi.Api/appsettings.json` or via environment variable `ConnectionStrings__Default`.

3. Run the API locally:

```bash
dotnet run --project apps/backend/Chapi.Api
```

Swagger will be available at the host/port configured by the API (check the console output).

### Migrations

-- Generate migrations from the host API project so EF can see all module entity configurations:

```bash
dotnet ef migrations add Initial --project apps/backend/Chapi.Api --context AppDbContext
dotnet ef database update --project apps/backend/Chapi.Api --context AppDbContext
```

- Note: removing or renaming module entities can create drop operations in migrations. Review generated migrations before applying them.

### Modules (how to add features)

1. Create a module under `modules/<Name>/` with Domain, Infrastructure, Application, and Controllers folders.
2. Register module services and enable controller discovery from the module assembly in the module registration class.
3. Reference the module project from `Chapi.Api` so the host picks up entity configurations and controllers.

Minimal checklist for a module:

- Domain: entities and value objects
- Infrastructure: EF configs, repositories
- Application: DTOs and services
- Controllers: HTTP surface

### Command Line Interface (CLI)

The CLI is in `apps/backend/Chapi.CLI`. Useful commands:

```bash
cd apps/backend/Chapi.CLI
dotnet run                 # show available commands
dotnet run seed-data       # seed initial test data
dotnet run run-sql "SELECT 1"
```

### Requests & examples

Example: create a user (adapt to your APIs):

```bash
curl -X POST http://localhost:5000/api/users \
 -H "Content-Type: application/json" \
 -d '{"email":"user@example.com","name":"User"}'
```

### Troubleshooting

- DB not reachable:

```bash
docker ps | grep chapi-postgres
docker logs chapi-postgres
```

- Reset local DB (⚠ deletes data):

```bash
docker rm -f chapi-postgres
docker volume rm chapi-postgres-data
# then re-run docker run from Quick start
```

- No model changes detected:

- Ensure `Chapi.Api` references your module project.
- Make sure EF configs implement `IEntityTypeConfiguration<>` and are in a loaded assembly.
- Clean & rebuild: `dotnet clean && dotnet build`.

### Do not edit `/shipmvp`

`/shipmvp` is a Git submodule. Update it by bumping the submodule reference, not by editing files in-place.

Manual bump example:

```bash
git -C shipmvp fetch --tags origin
git -C shipmvp checkout stable     # or a specific tag, e.g., v0.3.0
git add shipmvp
git commit -m "chore(shipmvp): bump backend template"
```

CI guard may block PRs that change files under `/shipmvp/*`.
