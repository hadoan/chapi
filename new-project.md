# Invoice Management System - Project Documentation

A modern invoice management system built with .NET 9, React (Vite), and PostgreSQL, following Clean Architecture and modular design principles.

## 🚀 Project Overview

This project implements a complete invoice management system with:
- **Backend**: .NET 9 Web API with modular architecture
- **Frontend**: React with TypeScript and Vite
- **Database**: PostgreSQL with Entity Framework Core
- **Architecture**: Clean Architecture with Repository pattern

## 📁 Project Structure

```
Invoice.sln
├── apps/
│   ├── backend/
│   │   ├── Chapi.Api/           # Main API application
│   │   └── Chapi.CLI/           # Command-line interface tool
│   └── frontend/                  # React frontend application
├── modules/
│   └── Invoices/                  # Invoice module (Clean Architecture)
│       ├── Domain/                # Domain entities and interfaces
│       ├── Application/           # Application services and DTOs
│       ├── Infrastructure/        # Data access and repositories
│       └── Controllers/           # API controllers
└── shipmvp/                       # Core framework (submodule)
    ├── ShipMvp.Api/              # API framework
    ├── ShipMvp.Application/       # Application infrastructure
    ├── ShipMvp.Core/             # Core abstractions
    └── ShipMvp.Domain/           # Domain framework
```

## 🛠️ Technology Stack

### Backend
- **.NET 9** - Latest framework version
- **ASP.NET Core** - Web API framework
- **Entity Framework Core 9** - ORM with PostgreSQL provider
- **Clean Architecture** - Domain-driven design
- **Repository Pattern** - Data access abstraction
- **Modular Design** - Feature-based modules

### Frontend
- **React 18** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool and dev server
- **Tailwind CSS** - Styling framework

### Database
- **PostgreSQL** - Primary database
- **Npgsql** - .NET PostgreSQL provider
- **EF Migrations** - Database versioning

## 🚀 Getting Started

### Prerequisites

- .NET 9 SDK
- Node.js 18+
- PostgreSQL 14+
- Git

### 1. Clone and Setup

```bash
# Clone the repository
git clone <repository-url>
cd Invoices

# Initialize submodules
git submodule update --init --recursive
```

### 2. Database Setup

Start PostgreSQL using Docker:

```bash
docker run --name shipmvp-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=ShipMVPPass123! \
  -e POSTGRES_DB=shipmvp \
  -e PGDATA=/var/lib/postgresql/data/pgdata \
  -p 5432:5432 \
  -v shipmvp-postgres-data:/var/lib/postgresql/data \
  --restart unless-stopped \
  -d postgres:latest
```

### 3. Backend Setup

```bash
# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update --project apps/backend/Chapi.Api

# Run the API
dotnet run --project apps/backend/Chapi.Api
```

The API will be available at `http://localhost:5066`
Swagger documentation at `http://localhost:5066/swagger`

### 4. Frontend Setup

```bash
cd apps/frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

The frontend will be available at `http://localhost:5173`

### 5. Command Line Interface (CLI)

The Chapi.CLI provides command-line tools for database management and administrative tasks:

```bash
cd apps/backend/Chapi.CLI

# Show available commands
dotnet run

# Seed initial application data
dotnet run seed-data

# Seed integration platforms
dotnet run seed-integrations

# Execute SQL queries
dotnet run run-sql "SELECT * FROM Invoices"
```

#### Available CLI Commands

| Command | Description |
|---------|-------------|
| `seed-data` | Seeds initial application data (users, subscription plans, etc.) |
| `seed-integrations` | Seeds integration platform configurations |
| `run-sql` | Executes custom SQL queries against the database |
| `help` | Shows detailed help information |

#### CLI Architecture

- **Framework**: Uses ShipMvp.CLI command framework
- **Dependency Injection**: Inherits all services from Chapi.Api
- **Database Access**: Uses same InvoiceDbContext as the main API
- **Modular Design**: Commands are discoverable and extensible


## 🏗️ Architecture

### Clean Architecture Implementation

#### Domain Layer (`modules/Invoices/Domain/`)
- **Entities**: `Invoice.cs`, `InvoiceItem.cs`
- **Enums**: `InvoiceStatus.cs`
- **Interfaces**: `IInvoiceRepository.cs`

#### Application Layer (`modules/Invoices/Application/`)
- **Services**: `InvoiceService.cs`
- **DTOs**: Request/Response models
- **Interfaces**: `IInvoiceService.cs`

#### Infrastructure Layer (`modules/Invoices/Infrastructure/`)
- **Repositories**: `InvoiceRepository.cs`
- **Data Configurations**: `InvoiceConfig.cs`, `InvoiceItemConfig.cs`

#### Presentation Layer (`modules/Invoices/Controllers/`)
- **Controllers**: `InvoicesController.cs`

### Key Features

#### Repository Pattern
- **Interface**: `IInvoiceRepository` in Domain layer
- **Implementation**: `InvoiceRepository` in Infrastructure layer
- **Benefits**: Testability, loose coupling, abstraction over data access

#### Modular Architecture
- **Module Registration**: Each module registers its own services
- **Controller Discovery**: Controllers discovered from module assemblies
- **Clean Separation**: Each module is self-contained

#### Entity Framework Configuration
- **Custom Base Entity**: Auditing, soft delete, versioning
- **Code-First Approach**: Domain-driven database design
- **Migration Support**: Database versioning and updates

## 📡 API Endpoints

### Invoice Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/invoices` | List all invoices with optional filtering |
| `GET` | `/api/invoices/{id}` | Get invoice by ID |
| `POST` | `/api/invoices` | Create new invoice |
| `PUT` | `/api/invoices/{id}` | Update existing invoice |
| `POST` | `/api/invoices/{id}/mark-as-paid` | Mark invoice as paid |
| `DELETE` | `/api/invoices/{id}` | Delete invoice |

### Sample Request Bodies

#### Create Invoice
```json
{
  "customerName": "Acme Corp",
  "totalAmount": 1500.00,
  "status": "Draft",
  "items": [
    {
      "description": "Consulting Services",
      "amount": 1500.00
    }
  ]
}
```

#### Update Invoice
```json
{
  "customerName": "Updated Customer Name",
  "totalAmount": 2000.00,
  "status": "Sent"
}
```

## 🛡️ Security Features

- **CORS Configuration**: Configurable allowed origins
- **Authentication Ready**: OpenIddict integration
- **Authorization**: Role-based access control
- **Data Protection**: Built-in data protection services

## 🔧 Development

### Adding New Features

1. **Domain First**: Add entities and interfaces to Domain layer
2. **Application Logic**: Implement services in Application layer  
3. **Data Access**: Create repositories in Infrastructure layer
4. **API Endpoints**: Add controllers for external access
5. **Module Registration**: Register services in module configuration

### Database Migrations

```bash
# Add new migration
dotnet ef migrations add <MigrationName> --project apps/backend/Chapi.Api

# Update database
dotnet ef database update --project apps/backend/Chapi.Api

# Remove last migration (if not applied)
dotnet ef migrations remove --project apps/backend/Chapi.Api
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test <TestProjectPath>
```

## 📦 Deployment

### Production Considerations

1. **Connection Strings**: Use secure configuration (Azure Key Vault, etc.)
2. **Environment Variables**: Configure for production settings
3. **HTTPS**: Enable HTTPS redirection
4. **Logging**: Configure structured logging
5. **Health Checks**: Add application health monitoring

### Docker Support

```dockerfile
# Example Dockerfile for API
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY published/ .
ENTRYPOINT ["dotnet", "Chapi.Api.dll"]
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Follow Clean Architecture principles
4. Add tests for new functionality
5. Submit a pull request

## 📝 Additional Notes

### Configuration Files

- **appsettings.json**: Base application configuration
- **appsettings.Development.json**: Development overrides
- **launchSettings.json**: Development server settings

### Key Dependencies

- **Microsoft.EntityFrameworkCore.Design**: EF tooling
- **Npgsql.EntityFrameworkCore.PostgreSQL**: PostgreSQL provider
- **Microsoft.AspNetCore.OpenApi**: Swagger/OpenAPI support
- **ShipMvp.*** packages: Core framework components

### Troubleshooting

#### Common Issues

1. **Controller not discovered**: Ensure `.AddApplicationPart()` is called in module
2. **Database connection**: Verify PostgreSQL is running and connection string is correct
3. **Migration errors**: Check entity configurations and relationships
4. **DI issues**: Verify service registrations in module configuration
5. **CLI command failures**: Ensure database is running and Chapi.Api dependencies are properly referenced

#### CLI Troubleshooting

- **IDbContext not found**: Verify InvoiceDbModule is registered in CLI Program.cs
- **Command not recognized**: Check that CLIModule is properly loaded
- **Database connection**: CLI uses same connection string as main API
- **Seed data errors**: May indicate database schema issues or missing migrations

#### Logs Location

- **Console**: Default for development
- **Debug**: Visual Studio output window
- **Structured**: Configure Serilog for production

---

*This documentation reflects the current state of the Invoice Management System as of August 2025.*
