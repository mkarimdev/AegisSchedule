# Contributing to AegisSchedule

Thank you for your interest in contributing! AegisSchedule is an open-source, .NET 10 university scheduling constraint-satisfaction engine. This guide covers everything you need to submit high-quality contributions.

---

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Development Prerequisites](#development-prerequisites)
3. [Repository Setup](#repository-setup)
4. [Project Architecture at a Glance](#project-architecture-at-a-glance)
5. [Branch Naming Conventions](#branch-naming-conventions)
6. [Coding Standards & Rules](#coding-standards--rules)
7. [Running Tests](#running-tests)
8. [Pull Request Guidelines](#pull-request-guidelines)
9. [Commit Message Format](#commit-message-format)

---

## Code of Conduct

All contributors are expected to be respectful, inclusive, and constructive. Harassment, discrimination, and derogatory language have no place in this project.

---

## Development Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0+** | Build, test, run |
| [PostgreSQL](https://www.postgresql.org/) | **17+** | Relational data store |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | latest | One-command stack via `docker compose` |
| [Git](https://git-scm.com/) | latest | Version control |

Optional but recommended:
- **Visual Studio 2022** (v17.12+) or **JetBrains Rider** for IDE support.
- **VS Code** with the [REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) to execute `Api.http`.

---

## Repository Setup

```bash
# 1. Fork → Clone your fork
git clone https://github.com/<your-username>/AegisSchedule.git
cd AegisSchedule

# 2. (Option A) Local — configure connection string, then run migrations
#    Edit src/AegisSchedule.Api/appsettings.Development.json:
#    "ConnectionStrings": { "DefaultConnection": "Host=localhost;Database=AegisScheduleDb;..." }
dotnet run --project src/AegisSchedule.Api  # auto-migrates on boot

# 2. (Option B) Docker — everything in one command
docker compose up -d --build
#   Web UI  → http://localhost:5046
#   API     → http://localhost:5221/scalar/v1
```

---

## Project Architecture at a Glance

```
AegisSchedule/
├── src/
│   ├── AegisSchedule.Api/      # .NET 10 REST API + EF Core + Constraint Solver
│   │   ├── Controllers/        # HTTP endpoints (public catalog + admin CRUD)
│   │   ├── Domain/             # Pure entities — ZERO external dependencies
│   │   ├── DTOs/               # API request/response contracts
│   │   ├── Migrations/         # EF Core migrations (source-controlled)
│   │   ├── Persistence/        # DbContext, configurations, DbInitializer
│   │   ├── Security/           # ApiKeyAuthFilter (timing-safe comparison)
│   │   └── Solver/             # Backtracking CSP engine + scoring
│   └── AegisSchedule.Web/      # Razor Pages server-side proxy
│       ├── Pages/              # Razor pages + page models
│       └── Services/           # ISchedulingApiClient, IAdminApiClient
└── tests/
    └── AegisSchedule.Tests/    # xUnit suite (48 test cases)
```

---

## Branch Naming Conventions

| Pattern | Use Case | Example |
|---------|----------|---------|
| `feature/<description>` | New functionality | `feature/sis-import-adapter` |
| `fix/<description>` | Bug fixes | `fix/overlap-detection-edge-case` |
| `docs/<description>` | Documentation only | `docs/update-api-http-collection` |
| `refactor/<description>` | Code improvements | `refactor/solver-scoring-weights` |
| `ci/<description>` | CI/CD changes | `ci/add-coverage-report` |

Branches must be cut from `main`. Do **not** commit directly to `main`.

---

## Coding Standards & Rules

### General
- Target **.NET 10** and **C# 14** language features.
- Enable **nullable reference types** — all code must be nullable-clean (`#nullable enable`).
- Use **implicit usings** (`<ImplicitUsings>enable</ImplicitUsings>`) — no redundant `using System;` statements.
- Prefer `record` and `record struct` for immutable data contracts (DTOs, solver snapshots).
- Use `sealed` on classes that are not designed for inheritance.

### Architecture Rules (Non-Negotiable)
- **`Domain/`** must have **zero** references to `Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`, or any database driver.
- **`Solver/`** must have **zero** references to databases, ORMs, HTTP contexts, or external I/O. It operates only on immutable snapshots.
- **No CQRS / MediatR** — use direct controller-to-service calls.
- **No premature multi-project Clean Architecture** — folder separation within a single `.csproj` is intentional.
- **No AI/ML in solver logic** — the solver must be deterministic and mathematically verifiable.

### Naming
- Controllers: `<Domain>Controller.cs` or `Admin<Domain>Controller.cs`
- DTOs: `<Entity>Dto.cs`, `Create<Entity>Request.cs`, `Update<Entity>Request.cs`
- Tests: `<SubjectUnderTest>Tests.cs`

### EF Core
- All entity configurations via `IEntityTypeConfiguration<T>` Fluent API.
- All primary keys are `Guid` (`uuid` in PostgreSQL).
- Add EF Core migrations with: `dotnet ef migrations add <MigrationName> --project src/AegisSchedule.Api`.

---

## Running Tests

```bash
# Run the full suite (48 tests) from the repository root
dotnet test AegisSchedule.sln

# With detailed output and code coverage
dotnet test AegisSchedule.sln -c Release --verbosity normal --collect:"XPlat Code Coverage"
```

All 48 tests must pass before submitting a PR. Regressions will fail the CI gate.

---

## Pull Request Guidelines

1. **Open an issue first** for non-trivial changes to align on scope and design.
2. Keep PRs **focused** — one logical change per PR.
3. Ensure `dotnet test AegisSchedule.sln` passes locally.
4. Write or update **unit tests** for any solver, domain, or service logic you touch.
5. Update `Api.http` if you add or modify API endpoints.
6. Provide a clear PR description:
   - **What** changed and **why**.
   - **How** to test it manually.
   - Screenshot / curl output for UI or endpoint changes.

### PR Review Checklist

- [ ] All 48 existing tests pass
- [ ] New tests added for new/modified logic
- [ ] No new compiler warnings
- [ ] No nullable reference type suppressions (`!`) without justification
- [ ] Domain layer has zero external dependencies
- [ ] Solver layer has zero I/O dependencies
- [ ] `Api.http` updated for any new endpoints

---

## Commit Message Format

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short description>

[optional body]
[optional footer(s)]
```

| Type | Use for |
|------|---------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code restructuring (no behavior change) |
| `test` | Adding or updating tests |
| `ci` | CI/CD changes |
| `chore` | Dependency bumps, build tooling |

**Examples:**
```
feat(solver): add evening-block preference scoring
fix(api): correct overlap detection for same-day activities
docs(readme): add docker quick-start guide
test(solver): add backtracking tests for 6-course scenarios
```

---

*Thank you for contributing to AegisSchedule! Every improvement helps students build better schedules.*
