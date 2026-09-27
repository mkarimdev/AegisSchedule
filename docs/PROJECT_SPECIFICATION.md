# Project Specification: UniSchedulingEngine

## 1. Product Vision & Executive Summary

In contemporary higher education, universities operate on complex master timetables consisting of fixed lecture cohorts, specialized laboratory sessions, and recitation/tutorial sections. While students who follow an idealized cohort progression can often enroll into pre-assigned blocks, a substantial fraction of the student population has non-standard scheduling requirements:
- **Cross-level enrollments**: Students retaking prerequisite courses from earlier academic levels or taking advanced electives ahead of time.
- **Disjoint section constraints**: Lectures and lab sections that occur across disparate days or have rigid cohort locks.
- **Individual quality-of-life preferences**: Minimizing dead hours (idle gaps) between classes, grouping classes into fewer days per week, or reserving specific days off for research, commuting, or work.

Manually constructing a conflict-free schedule from dense tabular PDF or Excel timetables is computationally difficult, highly frustrating, and prone to registration rejections during tight add/drop windows.

**UniSchedulingEngine** is an independent, API-first scheduling optimization service. It takes published course offerings, timetable structures, institutional cohort constraints, and student registration targets as inputs to systematically validate, synthesize, and rank optimal personal weekly schedules.

---

## 2. Two-Stage Strategic Vision

To achieve immediate utility while maintaining long-term institutional scalability, UniSchedulingEngine is structured around a two-stage delivery model:

```
[ Stage 1: Current Architecture (Independent Scheduling Service) ]
+------------------------+        +------------------------+        +------------------------+
|  Web Client / UI       |  --->  |  UniSchedulingEngine   |  --->  |  In-Memory Solver      |
|  (Interactive Planner) |  REST  |  ASP.NET Core API      |        |  (Pure Combinatorics)  |
+------------------------+        +------------------------+        +------------------------+

[ Stage 2: Future Enterprise Integration (Direct SIS Ingestion) ]
+------------------------+        +------------------------+        +------------------------+
|  University SIS / ERP  |  --->  |  Anti-Corruption       |  --->  |  UniSchedulingEngine   |
|  (Banner, Campus Sol.) |  Push  |  Ingestion Adapters    |  REST  |  Core Engine           |
+------------------------+        +------------------------+        +------------------------+
```

### Stage 1: Independent Scheduling Service (Current Focus)
- The engine functions as an autonomous, self-contained scheduling backend.
- Course offerings, programs, and timetables are ingested via deterministic database seeds or administrative REST endpoints.
- Students interact with a modern web client to select their desired courses, declare their cohort standing, define preferences, and receive ranked schedules.

### Stage 2: University SIS / ERP Ingestion (Future Evolution)
- Adapters in an anti-corruption layer connect to institutional Student Information Systems (SIS) such as Ellucian Banner, Oracle PeopleSoft Campus Solutions, or custom campus databases.
- The external payloads are translated into canonical domain snapshots without coupling internal solver logic to legacy database structures.

---

## 3. Scope Boundaries: What It Is vs. What It Is NOT

| In Scope (What It Is) | Explicitly Out of Scope (What It Is NOT) |
|---|---|
| **Student-Centric Personal Schedule Solver**: Evaluates combinatorial section combinations for individual students. | **Master University Timetabling Engine**: Does not allocate rooms, balance campus-wide professor teaching loads, or resolve physical hall contention. |
| **Cross-Level Enrollment Combinatorics**: Supports students taking courses simultaneously across different academic years/levels. | **Live Seat Inventory & Registration System**: Does not track real-time physical seat depletion or execute final SIS enrollments. |
| **Cohort & Group Lock Enforcement**: Enforces locked primary cohort assignments while exploring open options for cross-level courses. | **Gradebook & GPA Engine**: Does not manage transcripts, grades, or graduation audits. |
| **Multi-Objective Preference Scoring**: Ranks schedules by compact days, gap minimization, and day-off affinity. | **Tuition & Financial Accounting**: Does not calculate tuition, enforce financial holds, or process payments. |
| **API-First Modular Architecture**: Exposes stateless solver endpoints and REST management APIs. | **Attendance & Campus Security Tracking**: Does not handle RFID check-ins or lecture attendance. |

---

## 4. Constraint Classification: Hard Invariants vs. Soft Preferences

```
                     +---------------------------------------+
                     | Complete Course Offering Permutation  |
                     +-------------------+-------------------+
                                         |
                                         v
                     +---------------------------------------+
                     |         Hard Invariant Checks         |
                     |  - Zero time overlap [start, end)     |
                     |  - All required activities covered    |
                     |  - Primary cohort group locked        |
                     +-------------------+-------------------+
                                         |
                       +-----------------+-----------------+
                       |                                   |
                  Failed any                           Passed all
                       |                                   |
                       v                                   v
             [ Permutation Rejected ]            [ Valid Candidate ]
                                                           |
                                                           v
                                         +---------------------------------------+
                                         |      Soft Preference Evaluation       |
                                         |  - Minimize campus days (weight: W_d) |
                                         |  - Minimize idle gaps  (weight: W_g)  |
                                         |  - Preferred off-days  (weight: W_o)  |
                                         |  - Time-of-day affinity(weight: W_t)  |
                                         +-------------------+-------------------+
                                                           |
                                                           v
                                                 [ Ranked Candidate List ]
```

### 4.1 Hard Invariants (Zero Tolerance)
A candidate schedule is immediately discarded if it violates any of the following rules:
1. **Zero Time Overlap (Collision Invariant)**:
   Two instructional sessions cannot take place simultaneously. On any given day of the week, meeting intervals $[Start_A, End_A)$ and $[Start_B, End_B)$ must satisfy:
   $$\text{Overlap} \iff (Day_A = Day_B) \land (Start_A < End_B) \land (Start_B < End_A)$$
   If $\text{Overlap}$ is true, the permutation is invalid.
2. **Complete Activity Fulfillment**:
   For every enrolled course offering, the student must have exactly one scheduled `ActivityGroup` for each required activity type (e.g., exactly one lecture group and one laboratory section if both are required).
3. **Primary-Level Cohort Group Lock**:
   When a student registers for a course belonging to their declared primary academic level, their group assignment is locked to their designated cohort section.
4. **Cross-Level Section Freedom**:
   When a student registers for a course outside their primary academic level, any available, non-conflicting section or group may be assigned by the solver.

### 4.2 Soft Preferences (Multi-Objective Optimization)
Valid candidate schedules are evaluated and ranked based on a normalized fitness score $F \in [0.0, 1.0]$:
- **Campus Day Compression**: Minimizes the number of days per week the student is required to be on campus.
- **Idle Gap Minimization**: Penalizes dead time between consecutive periods on the same day.
- **Preferred Days Off**: Rewards schedules that keep user-specified days completely free of meetings.
- **Time-of-Day Affinity**: Rewards schedules with morning vs. afternoon class distributions matching student preference.

---

## 5. Phase Roadmap

```
+-----------------------------------------------------------------------------------+
| Phase 0: Specification, Repository Setup & Architecture Foundation                |
| - Repository initialization, solution structure, core documentation               |
+-----------------------------------------------------------------------------------+
                                         |
                                         v
+-----------------------------------------------------------------------------------+
| Phase 1: Pure Domain Modeling & In-Memory Constraint Solver Engine                |
| - Immutable snapshot models, interval math, combinatorial generator, fitness tests |
+-----------------------------------------------------------------------------------+
                                         |
                                         v
+-----------------------------------------------------------------------------------+
| Phase 2: Persistence, EF Core & Relational Data Model                             |
| - PostgreSQL schema, EF Core DbContext, Fluent API mappings, data seed migrations |
+-----------------------------------------------------------------------------------+
                                         |
                                         v
+-----------------------------------------------------------------------------------+
| Phase 3: RESTful API Endpoints & Authentication                                   |
| - ASP.NET Core controllers, DTO contracts, Identity token auth, OpenAPI/Scalar    |
+-----------------------------------------------------------------------------------+
                                         |
                                         v
+-----------------------------------------------------------------------------------+
| Phase 4: Web Client Reference Application                                         |
| - Interactive UI for course selection, cohort locks, and visual timetable view    |
+-----------------------------------------------------------------------------------+
                                         |
                                         v
+-----------------------------------------------------------------------------------+
| Phase 5: Anti-Corruption SIS Ingestion Adapters                                   |
| - External timetable parser, batch import pipeline, external sync verification    |
+-----------------------------------------------------------------------------------+
```
