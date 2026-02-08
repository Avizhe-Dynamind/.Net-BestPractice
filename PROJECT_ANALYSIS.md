# Project Analysis: Equinox Project

This document provides a comprehensive analysis of the Equinox Project based on the structured framework for understanding software projects.

---

## 1. Architecture & Design

### System Overview
- **Primary Purpose and Domain**: The Equinox Project is an open-source .NET 9.0 application designed to demonstrate modern software development best practices. It serves as a reference architecture for building enterprise-grade applications with .NET.
- **Architectural Pattern**: **Layered Architecture** with **Domain-Driven Design (DDD)**, **CQRS**, and **Event Sourcing**.
- **Main Components and Interaction**:
  - **Presentation**: `Equinox.UI.Web` (MVC) and `Equinox.Services.Api` (Web API).
  - **Application**: `Equinox.Application` - Handles UI/API requests, translates between ViewModels and Commands.
  - **Domain**: `Equinox.Domain` - Contains business logic, entities, commands, and events.
  - **Infrastructure**: `Equinox.Infra.Data` (Persistence), `Equinox.Infra.CrossCutting` (IoC, Bus, Identity).
- **Architectural Decision Records (ADRs)**: No formal ADRs found in the codebase.
- **Key Design Patterns**: Repository, Unit of Work, Mediator, Command, Event, Specification.

### Technology Stack
- **Languages and Frameworks**: C# 13, .NET 9.0, ASP.NET Core, Entity Framework Core.
- **Key Libraries**:
  - **NetDevPack**: DDD and CQRS patterns.
  - **NetDevPack.SimpleMediator**: Mediator implementation.
  - **FluentValidator**: Validation.
  - **Swagger/OpenAPI**: Documentation.
  - **Newtonsoft.Json**: Used in Event Sourcing for polymorphic serialization.
- **Minimum Required Runtime**: .NET 9.0 SDK.
- **Deprecated Dependencies**: AutoMapper and MediatR were recently replaced (v1.10) with custom mapping and SimpleMediator respectively.

### Data Flow
- **Flow**: Controller -> AppService -> Mediator -> CommandHandler -> Domain Model -> Repository -> DbContext -> Database.
- **Main Data Models**: `Customer` is the primary aggregate.
- **Data Transformation**: Custom mapping extensions translate between `ViewModels` and `Commands/Entities`.

---

## 2. Database & Data Management

- **Technology**: **SQL Server** (Production/Staging) and **SQLite** (Development).
- **Schema & Migrations**: Managed via **EF Core Migrations**.
- **Database Logic**: No stored procedures or triggers; logic is kept in the Domain/Application layers.
- **Seeding**: Automatic seeding for Development/Docker environments via `DbMigrationHelpers.cs`.
- **Backup & Recovery**: Not found (handled by infrastructure providers).

---

## 3. Authentication & Authorization

- **Mechanism**: **ASP.NET Identity** with **JWT** for API and **Cookies** for Web.
- **Identification**: Users are authenticated via Email/Password.
- **Authorization Model**: **Claims-based** using custom attributes (`CustomAuthorize`).
- **Secrets Storage**: `appsettings.json` and environment variables.
- **Roles/Permissions**: Supports roles and claims (e.g., `Customers: Write`).

---

## 4. API & Integrations

- **Exposed APIs**: RESTful API.
- **Versioning**: Not explicitly versioned in URLs (uses `v1` in Swagger).
- **Documentation**: Swagger UI at `/swagger`.
- **External Integrations**: None implemented.
- **Rate Limiting**: Not implemented.
- **Error Handling**: Uses a `CustomResponse` pattern returning `ValidationProblemDetails` for errors.

---

## 5. Configuration & Environment Management

- **Environments**: Dev, Staging, Testing (via `appsettings.{Env}.json`).
- **Required Variables**: `DefaultConnection` string and JWT `SecretKey`.
- **Feature Flags**: Not found.

---

## 6. Deployment & Infrastructure

- **Deployment Process**: CI/CD scripts not found in the repo (except for GitHub Actions workflows if present in `.github/`).
- **Infrastructure**: `Dockerfile` provided for containerization.
- **Hosting**: Cloud-agnostic, but traditionally demonstrated on Azure.
- **Scaling**: Horizontal scaling supported (stateless API).

---

## 7. Testing

- **Test Types**: **Architecture Tests** (`Equinox.Tests.Architecture`).
- **Coverage**: Low for business logic and integration.
- **Frameworks**: xUnit, NetArchTest.Rules.
- **Critical Paths without coverage**: Customer registration/update logic, API endpoints.

---

## 8. Logging, Monitoring & Observability

- **Logging**: Standard .NET `ILogger`.
- **Monitoring**: No specific tools (Datadog/Prometheus) configured.
- **Tracing**: Not implemented.

---

## 9. Error Handling & Resilience

- **Strategy**: Domain Notifications for validation errors.
- **Tracking**: Standard logging; no Sentry/Bugsnag integration found.
- **Retries/Circuit Breakers**: Not implemented.

---

## 10. Security

- **Measures**: HTTPS Redirection, JWT validation, Identity security defaults.
- **Vulnerability Prevention**: EF Core prevents SQL Injection; standard ASP.NET Core protections against XSS/CSRF.
- **Dependency Scanning**: Not found in codebase.

---

## 11. Performance & Optimization

- **SLA/Benchmarks**: Not found.
- **Caching**: No Redis or in-memory caching implemented.
- **Bottlenecks**: Potential performance hit in Event Sourcing if the `StoredEvent` table grows very large without snapshots (not implemented).

---

## 12. Developer Experience & Workflow

- **Local Setup**: Run `dotnet run` on `Equinox.UI.Web` or `Equinox.Services.Api`. SQLite handles the DB automatically.
- **Code Standards**: Follows Clean Code and SOLID principles. Linters not explicitly configured in the repo.
- **Branching Strategy**: Not found.

---

## 13. Dependencies & Package Management

- **Management**: NuGet (`.csproj` files).
- **Lockfile**: None found (uses standard NuGet resolution).

---

## 14. Build & Release Process

- **Tools**: `dotnet build`.
- **Versioning**: Semantic versioning mentioned in README.
- **Artifacts**: Docker images.

---

## 15. Domain-Specific Questions

- **Business Rules**: Customer must have a valid email and be of age (handled by `FluentValidation`).
- **Scheduled Jobs**: None found.
- **Data Retention**: Not found.

---

## 16. Maintenance & Operations

- **On-call/Incident Response**: Not applicable (Open source project).
- **Technical Debt**:
  - Inconsistent SQLite support in API vs Web projects.
  - Redundant Identity endpoints (`AccountController` vs `MapIdentityApi`).
  - Sync-over-async blocking calls in `JwtBuilder.cs` (`.Result`).
  - Outdated documentation in `docs/index.md`.

---

## 17. Potential Improvements & Recommendations

1. **Unify Database Configuration**: Ensure the API project supports SQLite in Development just like the Web project does.
2. **Fix Sync-over-Async**: Refactor `JwtBuilder` to use `async/await` properly to avoid potential deadlocks.
3. **Increase Test Coverage**: Add unit tests for domain logic and integration tests for API endpoints.
4. **Clean up Identity**: Choose one way to expose Identity endpoints in the API (either custom or built-in).
5. **Update Documentation**: Refresh `docs/` to reflect .NET 9 and the removal of AutoMapper/MediatR.
