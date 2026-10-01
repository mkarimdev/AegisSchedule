# AegisSchedule — Architecture Reference

> This document is the authoritative technical reference for the AegisSchedule system.
> For the domain model and ubiquitous language, see [DOMAIN_MODEL.md](DOMAIN_MODEL.md).

---

## 1. Architectural Philosophy: Pragmatic Modular Monolith

AegisSchedule is intentionally designed as a **Pragmatic Modular Monolith**.

Instead of fragmenting a focused scheduling domain across 6–8 separate `.csproj` files (`Domain.csproj`, `Application.csproj`, `Infrastructure.csproj`, etc.), it enforces clean separation via **folder structure and namespace isolation** within a single backend project, eliminating unnecessary indirection layers and build friction.

```
AegisSchedule/
├── src/
│   ├── AegisSchedule.Api/          # Single .NET 10 backend executable
│   │   ├── Domain/                 # Pure entities — ZERO external dependencies
│   │   ├── Solver/                 # In-memory constraint solver + scoring
│   │   ├── Persistence/            # EF Core 10 DbContext, configurations, migrations
│   │   ├── Controllers/            # RESTful HTTP endpoints + auth filters
│   │   ├── DTOs/                   # Inbound/outbound API contracts
│   │   ├── Security/               # Admin API key authentication
│   │   └── Program.cs              # Host bootstrap, DI, middleware
│   └── AegisSchedule.Web/          # Razor Pages server-side proxy
│       ├── Pages/                  # Razor page views + models
│       └── Services/               # Typed HTTP clients (ISchedulingApiClient, IAdminApiClient)
└── tests/
    └── AegisSchedule.Tests/        # xUnit, 48 tests, EF InMemory
```

---

## 2. Dependency Rules

Strict **unidirectional** dependencies are enforced:

```
[ Browser ]
    │
    │  HTTP
    ▼
[ AegisSchedule.Web (Razor Pages) ]
    │
    │  HTTP/JSON — internal container-to-container (http://api:8080)
    ▼
[ Controllers + DTOs ]
    │                  │
    ▼                  ▼
[ Persistence ]    [ Solver ]
    │                  │
    └────► [ Domain ] ◄┘
```

### Rules by Layer

| Layer | Allowed Dependencies | Forbidden |
|-------|---------------------|-----------|
| `Domain/` | Standard .NET (`System.*`) only | EF Core, ASP.NET Core, Npgsql |
| `Solver/` | `Domain/`, `System.*` | EF Core, DbContext, HTTP, I/O |
| `Persistence/` | `Domain/`, EF Core 10, Npgsql | ASP.NET Core HTTP types |
| `Controllers/` | `Persistence/`, `Solver/`, `DTOs/`, `Security/` | Direct DB access bypassing EF |
| `AegisSchedule.Web` | HTTP client contracts | C# domain models from Api project |

---

## 3. Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 10.0 |
| Language | C# | 13 |
| Web API | ASP.NET Core Web API | 10.0 |
| API Explorer | Scalar | 2.x |
| ORM | Entity Framework Core | 10.0 |
| Database | PostgreSQL | 17 |
| DB Driver | Npgsql | 10.x |
| Frontend | ASP.NET Core Razor Pages | 10.0 |
| Testing | xUnit + EF InMemory | 2.9.3 / 10.0 |
| Containers | Docker + Docker Compose | latest |
| CI | GitHub Actions | — |

---

## 4. Solver Architecture

### Absolute Database Independence

The constraint solver operates **entirely in memory** with **zero database access during solving**:

1. **Pre-solve**: The `SchedulesController` fetches all required entities from PostgreSQL via EF Core.
2. **Snapshot Projection**: Entities are projected into immutable `SchedulingInputSnapshot` records.
3. **Solving**: `ScheduleSolver.GenerateSchedules()` receives only the snapshot — no `DbContext`, no I/O, no lazy loading possible.
4. **Post-solve**: Results are returned as ranked `ScheduleDto` lists.

This makes the solver:
- **Fully unit-testable** without a database or Docker.
- **Deterministic** — same input always produces same output.
- **Benchmarkable** — thousands of permutations in milliseconds.

### Constraint Evaluation

```
SchedulingInputSnapshot
    │
    ├── StudentGroupConstraint     (primary lecture group, lab section lock)
    ├── SolverCourseSnapshot[]     (per-course activity/group metadata)
    │   └── SolverActivityGroupSnapshot[]
    │       └── SolverMeetingSnapshot[]  (DayOfWeek, StartTime, EndTime)
    └── SchedulePreferenceProfile  (MinimizeDaysWeight, MinimizeGapsWeight, TimeBlock)
```

**Hard constraint** (pruning): Any combination containing a meeting pair where `[A.Start, A.End)` overlaps `[B.Start, B.End)` on the same day is discarded immediately via interval arithmetic.

**Soft constraint** (scoring): Valid combinations are scored by a weighted sum:
- `MinimizeDaysWeight × (7 - distinctActiveDays)`
- `MinimizeGapsWeight × (-totalIdleMinutes)`
- `PreferredTimeBlockWeight × (meetingsInPreferredBlock / totalMeetings)`

---

## 5. Security Model

### Admin API Key Authentication

Admin endpoints (`/api/admin/*`) are protected by `ApiKeyAuthFilter`:

1. Extracts the `X-Admin-Api-Key` header value.
2. Reads `Security:AdminApiKey` from configuration.
3. Compares using `CryptographicOperations.FixedTimeEquals()` (constant-time, timing-attack resistant).
4. Returns `401 Unauthorized` on failure.

**Configuration** (via environment variable in Docker):
```
Security__AdminApiKey=AegisAdmin_SuperSecretKey_2026_ChangeMeInProd
```

In production, supply the key via a Docker secret or a secrets manager (e.g., Azure Key Vault, AWS Secrets Manager).

### CORS Policy

The API enforces an allowlist CORS policy (`AegisScheduleCors`). In Docker:
```
Cors__AllowedOrigins__0=http://localhost:5046
```

The `AegisSchedule.Web` server-side proxy is the only legitimate origin — the browser never calls the API directly.

### Security Response Headers

All responses include:
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: strict-origin-when-cross-origin
```

---

## 6. Database & EF Core Standards

- **Primary keys**: `Guid` (`uuid` in PostgreSQL) — never sequential `int`.
- **Entity configurations**: Fluent API via `IEntityTypeConfiguration<T>` in `Persistence/Configurations/`. Entities in `Domain/` are never annotated with EF attributes.
- **Migrations**: Source-controlled EF Core migrations in `Migrations/`. Applied automatically at startup via `Database.MigrateAsync()`.
- **Time types**: `TimeOnly` and `DayOfWeek` mapped via Npgsql type mappings.

```bash
# Adding a new migration
dotnet ef migrations add <MigrationName> \
  --project src/AegisSchedule.Api \
  --startup-project src/AegisSchedule.Api
```

---

## 7. Container Topology

```
Host
├── :5046  →  aegisschedule-web:8080   (AegisSchedule.Web)
├── :5221  →  aegisschedule-api:8080   (AegisSchedule.Api)
└── :5432  →  aegisschedule-postgres:5432 (PostgreSQL 17)

Docker bridge network: aegis-net
  ├── postgres    (health-checked: pg_isready)
  ├── api         (depends_on postgres healthy — auto-migrates on boot)
  └── web         (depends_on api — ApiSettings__BaseUrl=http://api:8080)
```

---

## 8. Explicit Anti-Patterns (Forbidden)

| Pattern | Reason |
|---------|--------|
| CQRS / MediatR | Adds indirection without value at this scale |
| Multi-project Clean Architecture | 6–8 `.csproj` files increase build friction with no benefit |
| AI/ML in solver | Non-deterministic; cannot guarantee zero overlap invariants |
| Lazy loading in EF Core navigation | Causes N+1 query storms; all queries use explicit `.Include()` |
| Distributed messaging (RabbitMQ, Kafka) | Out of scope; adds operational overhead |
| Hard-coded institutional assumptions | Days, periods, and cohort structures must be parameterized |
