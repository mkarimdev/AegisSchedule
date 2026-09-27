# UniSchedulingEngine

**UniSchedulingEngine** is an independent, API-first university scheduling optimization engine. It consumes institutional course offering catalogs, timetable definitions, and individual student academic standing to synthesize, validate, and rank conflict-free personal weekly schedules.

---

## 🎯 Product Vision & Architecture

Universities publish complex master timetables consisting of lecture groups, laboratory sections, and tutorials across programs and academic levels. For students navigating cross-level prerequisites, carry-over subjects, or elective choices, finding a valid schedule without overlaps or grueling multi-hour gaps is a manual, error-prone challenge.

UniSchedulingEngine acts as a dedicated scheduling service:
- **API-First Architecture**: Exposes clean, high-performance RESTful endpoints for catalog ingestion, registration context setup, and schedule synthesis.
- **Two-Stage Integration Goal**:
  1. **Stage 1 (Current)**: Web Client / Reference UI $\rightarrow$ Headless API $\rightarrow$ In-Memory Constraint Solver.
  2. **Stage 2 (Future SIS Integration)**: University SIS/ERP (e.g., Banner, Campus Solutions) $\rightarrow$ Anti-Corruption Adapters $\rightarrow$ Headless API $\rightarrow$ In-Memory Constraint Solver.
- **Pragmatic Modular Monolith**: Avoids premature microservices or multi-project Clean Architecture overhead by organizing pure domain logic, constraint solving, persistence (PostgreSQL / EF Core), and presentation into clear directory boundaries inside `src/Api/`.

---

## 🏛️ Repository Layout

```
UniSchedulingEngine/
├── docs/
│   ├── PROJECT_SPECIFICATION.md    # Product vision, constraints, and phase roadmap
│   ├── DOMAIN_MODEL.md             # Core domain concepts, intervals, and snapshot contracts
│   └── TECHNICAL_ARCHITECTURE.md   # Architecture, dependency rules, and tech stack
├── src/
│   ├── Api/                        # ASP.NET Core (.NET 10) Modular Monolith
│   └── Web/                        # Client SPA / Reference Frontend
├── tests/
│   └── Api.Tests/                  # Unit, Integration, and Solver Benchmark Tests
├── UniSchedulingEngine.sln         # Root solution file
└── README.md
```

---

## 📋 Foundational Documentation

Comprehensive architecture and domain specifications are available in the [`docs/`](file:///e:/myProjects/UniSchedulingEngine/docs) directory:
- [Project Specification](file:///e:/myProjects/UniSchedulingEngine/docs/PROJECT_SPECIFICATION.md): Core goals, problem statement, hard vs. soft constraints, and phase roadmap.
- [Domain Model](file:///e:/myProjects/UniSchedulingEngine/docs/DOMAIN_MODEL.md): Ubiquitous language, entities (`University`, `Faculty`, `Program`, `AcademicLevel`, `Course`, `CourseOffering`, `Activity`, `ActivityGroup`, `Meeting`), interval mathematics, and solver snapshot models.
- [Technical Architecture](file:///e:/myProjects/UniSchedulingEngine/docs/TECHNICAL_ARCHITECTURE.md): Dependency rules (`Web -> API -> Solver -> Domain`), persistence layer guidelines, anti-corruption boundaries, and explicit anti-patterns to avoid.
