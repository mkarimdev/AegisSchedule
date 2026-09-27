# Technical Architecture Specification: UniSchedulingEngine

## 1. Architectural Philosophy: Pragmatic Modular Monolith

UniSchedulingEngine is intentionally designed as a **Pragmatic Modular Monolith**. Instead of fragmenting a focused domain across numerous micro-projects, it encapsulates all core backend capabilities within a single, highly structured backend project (`src/Api/`), accompanied by an isolated client frontend (`src/Web/`) and a comprehensive test suite (`tests/Api.Tests/`).

### Repository & Component Structure
```
UniSchedulingEngine/
├── docs/
│   ├── PROJECT_SPECIFICATION.md     # Vision, constraints, roadmap
│   ├── DOMAIN_MODEL.md              # Ubiquitous language, entities, snapshots
│   └── TECHNICAL_ARCHITECTURE.md    # Architecture, dependency rules, tech stack
├── src/
│   ├── Api/                         # Single C# Backend Executable (.NET 10)
│   │   ├── Domain/                  # Pure Entities, Value Objects, Invariant Rules
│   │   ├── Solver/                  # In-Memory Constraint Solver & Fitness Scorers
│   │   ├── Persistence/             # EF Core DbContext, Configurations, Migrations
│   │   ├── Controllers/             # RESTful API Endpoints & Auth
│   │   ├── DTOs/                    # Inbound/Outbound API Contracts
│   │   ├── Integrations/            # SIS Ingestion Adapters & Data Import Ports
│   │   └── Program.cs               # Host bootstrap, DI container, Middleware
│   └── Web/                         # Client Single Page Application (SPA)
├── tests/
│   └── Api.Tests/                   # Unit, Integration, and Solver Benchmark Tests
├── UniSchedulingEngine.sln          # Root solution file
└── README.md
```

---

## 2. Dependency Rules & Boundary Enforcement

The system enforces strict unidirectional dependencies across its logical modules:

```
[ Web Client (SPA) ]
        │
        │ HTTP / JSON
        ▼
[ Controllers / Endpoints ] ────────► [ DTOs ]
        │                                 ▲
        │                                 │
  ┌─────┴───────────────────────┐         │
  ▼                             ▼         │
[ Persistence (EF Core) ]   [ Solver Engine ]
  │                             │
  └───────────► [ Domain ] ◄────┘
```

### Dependency Rules:
1. **`Domain/`**:
   - Contains pure business models, value objects, and mathematical interval checks.
   - **Zero External Dependencies**: Must NOT import `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`, or database drivers.
2. **`Solver/`**:
   - Contains pure combinatorial search logic, constraint evaluation, and multi-objective scoring.
   - Depends **only** on `Domain/` and standard .NET runtime types.
   - Operates on immutable snapshot records (`ScheduleRequest`, `SolverActivityGroupSnapshot`).
   - Has **zero** references to databases, ORMs, or HTTP contexts.
3. **`Persistence/`**:
   - Manages relational mapping via EF Core 10.
   - Depends on `Domain/`.
   - Projects database entities into solver snapshots for calculation runs.
4. **`Controllers/` & `DTOs/`**:
   - Handles HTTP routing, model validation, and authorization.
   - Orchestrates requests between `Persistence/` (fetching data) and `Solver/` (computing schedules).
5. **`Web/`**:
   - Consumes the API via standard REST endpoints using JSON contracts. Completely decoupled from C# implementation details.

---

## 3. Technology Stack & Platform Standards

- **Runtime & Language**: .NET 10 (C# 14).
- **Web API Framework**: ASP.NET Core Web API with OpenAPI/Scalar documentation.
- **Relational Database**: PostgreSQL 17+.
- **Object-Relational Mapper**: Entity Framework Core 10 with `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **Identity & Security**: ASP.NET Core Identity with built-in token authentication endpoints (`MapIdentityApi<AppUser>()`).
- **Client Frontend**: Modern Single Page Application (TypeScript / HTML / CSS) in `src/Web/`.
- **Test Frameworks**: xUnit, FluentAssertions, Bogus (synthetic data generation), and Testcontainers for PostgreSQL integration testing.

---

## 4. Solver Database Independence

A paramount architectural invariant of UniSchedulingEngine is **absolute database independence for the solver engine**:

- **No I/O During Solving**: The solver never triggers SQL queries, network calls, or lazy loading inside combinatorial loops.
- **Pure Snapshot Consumption**: The API layer queries the database once, maps the course offerings and student context into pure immutable snapshots (`ScheduleRequest`), and passes them to the solver.
- **Deterministic & Benchmarkable**: The solver can be tested and benchmarked with thousands of permutations in pure unit tests with millisecond execution times, completely isolated from database state or Docker containers.

---

## 5. PostgreSQL & EF Core Persistence Standards

- **Entity Configuration**: All mappings must use EF Core Fluent API via separate `IEntityTypeConfiguration<T>` classes located in `Persistence/Configurations/`. No polluting domain models with EF attributes.
- **Identity & Primary Keys**: All relational tables use GUID primary keys (`uuid` in PostgreSQL).
- **Time Representation**: Standard .NET `TimeOnly` and `DayOfWeek` types mapped cleanly to PostgreSQL types.
- **Migrations**: Explicit, source-controlled EF Core migrations located in `Persistence/Migrations/`.

---

## 6. Explicit Anti-Patterns to Avoid

To maintain velocity, clarity, and performance, the following patterns are **strictly forbidden** in this codebase:

### 1. ❌ No CQRS / MediatR
- **Rationale**: Indirection layers like `IMediator`, command handlers, query handlers, and pipeline behaviors add substantial boilerplate, obfuscate call stacks, and complicate debugging without providing value for this service's scope.
- **Standard**: Use clean, focused domain services and controller actions directly.

### 2. ❌ No Premature Multi-Project Clean Architecture
- **Rationale**: Splitting a small service into 6-8 separate `.csproj` files (`Domain.csproj`, `Application.csproj`, `Infrastructure.csproj`, `Core.csproj`, `Contracts.csproj`, etc.) slows build times, introduces dependency versioning friction, and encourages empty pass-through interfaces.
- **Standard**: Enforce clean boundaries via folder organization and namespace separation inside a single `Api.csproj`.

### 3. ❌ No AI / Machine Learning in Solver Logic
- **Rationale**: Scheduling is a discrete combinatorial optimization problem with mathematical hard constraints. Machine learning models (LLMs or neural networks) hallucinate, are non-deterministic, and cannot guarantee zero time-overlap invariants.
- **Standard**: Use deterministic combinatorial search algorithms (branch-and-prune, backtracking) combined with explicit weighted multi-objective scoring functions.

### 4. ❌ No Distributed Messaging / Microservices Overhead
- **Rationale**: Introducing RabbitMQ, Kafka, or gRPC services for inter-component communication is an anti-pattern for this stage.
- **Standard**: Maintain an in-process, synchronous solver pipeline with sub-second execution times.

### 5. ❌ No Hard-Coded Institutional Assumptions
- **Rationale**: Hardcoding assumptions like "Saturday-Wednesday only" or specific period slot times into static engine logic limits reusability across universities.
- **Standard**: Provide sensible defaults (e.g. for Egyptian/regional universities), but ensure days, period slots, and cohort structures are parameterized domain models.
