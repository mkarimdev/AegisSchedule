# Domain Model Specification: UniSchedulingEngine

## 1. Ubiquitous Language & Core Domain Concepts

UniSchedulingEngine models academic organizational structures, curricula, course delivery timetables, and student enrollment contexts.

```
+---------------------------------------------------------------------------------+
|                                  University                                     |
|  - Id: Guid                                                                     |
|  - Name: string (e.g. "Minia National University")                               |
|  - Code: string (e.g. "MNU")                                                    |
+---------------------------------------+-----------------------------------------+
                                        | 1
                                        | has many
                                        v *
+---------------------------------------------------------------------------------+
|                                   Faculty                                       |
|  - Id: Guid, UniversityId: Guid                                                 |
|  - Name: string (e.g. "Faculty of Computers and Artificial Intelligence")        |
+---------------------------------------+-----------------------------------------+
                                        | 1
                                        | offers many
                                        v *
+---------------------------------------------------------------------------------+
|                                   Program                                       |
|  - Id: Guid, FacultyId: Guid                                                    |
|  - Name: string (e.g. "Software Engineering", "Artificial Intelligence")        |
|  - Code: string (e.g. "CS-SE", "CS-AI")                                         |
+---------------------------------------+-----------------------------------------+
                                        | 1
                                        | defines progression
                                        v *
+---------------------------------------------------------------------------------+
|                                AcademicLevel                                    |
|  - Id: Guid, ProgramId: Guid                                                    |
|  - LevelNumber: int (e.g. 1, 2, 3, 4)                                           |
|  - Name: string (e.g. "Year 1 / Freshman", "Year 3 / Junior")                   |
+---------------------------------------+-----------------------------------------+
                                        | 1
                                        | curriculum defines
                                        v *
+---------------------------------------------------------------------------------+
|                                    Course                                       |
|  - Id: Guid, ProgramId: Guid, AcademicLevelId: Guid                             |
|  - Code: string (e.g. "CS301", "MATH201")                                       |
|  - Name: string (e.g. "Operating Systems", "Discrete Mathematics")              |
|  - CreditHours: int (e.g. 3)                                                    |
+---------------------------------------+-----------------------------------------+
                                        | 1
                                        | scheduled in term
                                        v *
+---------------------------------------------------------------------------------+
|                                CourseOffering                                   |
|  - Id: Guid, CourseId: Guid                                                     |
|  - Semester: string (e.g. "2026-FALL", "2027-SPRING")                           |
|  - AcademicYear: string (e.g. "2026/2027")                                      |
+---------------------------------------+-----------------------------------------+
                                        | 1
                                        | contains
                                        v *
+---------------------------------------------------------------------------------+
|                                ActivityGroup                                    |
|  - Id: Guid, CourseOfferingId: Guid                                             |
|  - ActivityType: ActivityType (Lecture | Lab | Tutorial | Seminar)              |
|  - GroupCode: string (e.g. "G1", "G2", "SEC1", "SEC4")                          |
|  - Name: string (e.g. "Lecture Cohort 1", "Laboratory Section 4")               |
+---------------------------------------+-----------------------------------------+
                                        | 1
                                        | meets at
                                        v *
+---------------------------------------------------------------------------------+
|                                   Meeting                                       |
|  - Id: Guid, ActivityGroupId: Guid                                              |
|  - DayOfWeek: DayOfWeek (Saturday through Wednesday default)                    |
|  - StartTime: TimeOnly (e.g. 08:30)                                             |
|  - EndTime: TimeOnly (e.g. 10:30)                                               |
|  - Location: string (e.g. "Hall 101", "Computer Lab B")                         |
+---------------------------------------------------------------------------------+
```

---

## 2. Entity Definitions, Key Fields & Cardinalities

### 2.1 Institutional Hierarchy
1. **University**: The top-level administrative institution.
   - *Cardinality*: 1 University has many Faculties ($1 \rightarrow *$).
   - *Key Fields*: `Id` (Guid), `Name` (string), `Code` (string), `CreatedAtUtc` (DateTime).
2. **Faculty**: An academic college or division within the university.
   - *Cardinality*: 1 Faculty belongs to 1 University ($* \rightarrow 1$), has many Programs ($1 \rightarrow *$).
   - *Key Fields*: `Id` (Guid), `UniversityId` (Guid), `Name` (string), `Code` (string).
3. **Program**: A specific degree track (e.g., Software Engineering, Computer Science).
   - *Cardinality*: 1 Program belongs to 1 Faculty ($* \rightarrow 1$), defines many AcademicLevels ($1 \rightarrow *$).
   - *Key Fields*: `Id` (Guid), `FacultyId` (Guid), `Name` (string), `Code` (string).
4. **AcademicLevel**: An educational progression tier within a program (e.g., Level 1, Level 2, Level 3, Level 4).
   - *Cardinality*: 1 AcademicLevel belongs to 1 Program ($* \rightarrow 1$), groups curriculum Courses ($1 \rightarrow *$).
   - *Key Fields*: `Id` (Guid), `ProgramId` (Guid), `LevelNumber` (int), `Name` (string).

### 2.2 Academic Catalog & Delivery Hierarchy
1. **Course**: An abstract subject in the program curriculum.
   - *Cardinality*: Belongs to a Program and canonical AcademicLevel ($* \rightarrow 1$). Instantiated into many CourseOfferings ($1 \rightarrow *$).
   - *Key Fields*: `Id` (Guid), `ProgramId` (Guid), `AcademicLevelId` (Guid), `Code` (string, unique per program), `Name` (string), `CreditHours` (int).
2. **CourseOffering**: The concrete delivery of a Course during an academic term.
   - *Cardinality*: Belongs to 1 Course ($* \rightarrow 1$), has many ActivityGroups ($1 \rightarrow *$).
   - *Key Fields*: `Id` (Guid), `CourseId` (Guid), `Semester` (string, e.g., `"2026-FALL"`), `AcademicYear` (string).
3. **Activity & ActivityGroup**: Instructional contact delivery.
   - `ActivityType` (Value Object / Enum): `Lecture`, `Lab`, `Tutorial`, `Seminar`.
   - `ActivityGroup`: A scheduled group or section delivering a specific activity type for an offering.
   - *Cardinality*: Belongs to 1 CourseOffering ($* \rightarrow 1$), has many Meetings ($1 \rightarrow *$).
   - *Key Fields*: `Id` (Guid), `CourseOfferingId` (Guid), `ActivityType` (Enum), `GroupCode` (string, e.g., `"G1"`, `"SEC3"`), `Name` (string).
4. **Meeting**: A discrete scheduled weekly time slot and location for an ActivityGroup.
   - *Cardinality*: Belongs to 1 ActivityGroup ($* \rightarrow 1$).
   - *Key Fields*: `Id` (Guid), `ActivityGroupId` (Guid), `DayOfWeek` (DayOfWeek), `StartTime` (TimeOnly), `EndTime` (TimeOnly), `Location` (string).

### 2.3 Student Scheduling Context
1. **StudentSchedulingContext**: Runtime input encapsulating a student's standing and targets for a schedule generation run.
   - *Fields*:
     - `StudentId`: `Guid`
     - `PrimaryAcademicLevel`: `int` (e.g. `3` for a Junior)
     - `PrimaryLockedGroups`: `IReadOnlyDictionary<ActivityType, string>` (e.g., `{ Lecture: "G2", Lab: "SEC4" }`)
     - `TargetOfferingIds`: `IReadOnlyList<Guid>` (the set of course offerings the student plans to register for)
     - `Preferences`: `SchedulePreferences` (Scoring weights for gaps, off-days, and compact days)

---

## 3. Core Domain Rules & Invariants

### 3.1 Course vs. CourseOffering Distinction
- **Course**: An immutable catalog entity representing the theoretical subject (e.g., `CS204: Data Structures`, 3 Credit Hours). It exists independently of semesters and has no timetables, rooms, or meeting hours.
- **CourseOffering**: A temporal instantiation of a Course in a specific semester (e.g., `CS204` in Fall 2026). It owns the active `ActivityGroup` sections and their corresponding weekly `Meeting` schedules.

### 3.2 Canonical Meeting Overlap Mathematics
All collision detection in the domain obeys half-open time interval semantics $[Start, End)$:
1. A meeting interval $M$ on weekday $D$ is defined by:
   $$M = [T_{\text{start}}, T_{\text{end}}) \quad \text{where } T_{\text{start}} < T_{\text{end}}$$
2. Two meetings $M_1$ and $M_2$ collide if and only if they occur on the same day and satisfy:
   $$\text{Collides}(M_1, M_2) \iff (M_1.Day = M_2.Day) \land (M_1.StartTime < M_2.EndTime) \land (M_2.StartTime < M_1.EndTime)$$
3. **Adjacent Slot Transition**: Two back-to-back periods (e.g., Lecture $08:30 - 10:30$ and Lab $10:30 - 12:30$) evaluate to:
   $$08:30 < 12:30 \land 10:30 < 10:30 \implies \text{True} \land \text{False} \implies \text{False (No Collision)}$$
   This accurately represents real-world class changes without artificial buffer padding.

### 3.3 Cohort Lock vs. Cross-Level Freedom
- **Primary-Level Course Offerings**: If `CourseOffering.Course.AcademicLevel.LevelNumber == StudentContext.PrimaryAcademicLevel`, the student's assigned group codes are non-negotiable. If the student is assigned `Lecture: G1` and `Lab: SEC2`, the solver must solely evaluate options matching those codes.
- **Cross-Level Course Offerings**: If `CourseOffering.Course.AcademicLevel.LevelNumber != StudentContext.PrimaryAcademicLevel` (e.g., a Level 3 student repeating a Level 2 course), the solver evaluates all available `ActivityGroup` options for that course offering to discover conflict-free combinations.

---

## 4. Solver Snapshot Contracts vs. Persistence Entities

The scheduling solver never operates on entity tracking graphs or database contexts. Before execution, EF Core entities are projected into pure, immutable records:

```csharp
// High-performance immutable solver models (Domain & Solver Boundary)

public sealed record SolverMeetingSnapshot(
    Guid MeetingId,
    DayOfWeek Day,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Location
);

public sealed record SolverActivityGroupSnapshot(
    Guid ActivityGroupId,
    Guid OfferingId,
    string CourseCode,
    int AcademicLevel,
    ActivityType ActivityType,
    string GroupCode,
    IReadOnlyList<SolverMeetingSnapshot> Meetings
);

public sealed record SchedulePreferences(
    double WeightMinimizeGaps = 1.0,
    double WeightCompactDays = 1.0,
    IReadOnlyList<DayOfWeek>? PreferredOffDays = null,
    double WeightPreferredOffDays = 1.5
);

public sealed record ScheduleRequest(
    Guid StudentId,
    int PrimaryAcademicLevel,
    IReadOnlyDictionary<ActivityType, string> PrimaryLockedGroups,
    IReadOnlyList<SolverActivityGroupSnapshot> AvailableOptions,
    SchedulePreferences Preferences
);

public sealed record ScheduledItem(
    Guid OfferingId,
    string CourseCode,
    ActivityType ActivityType,
    Guid ActivityGroupId,
    string GroupCode,
    IReadOnlyList<SolverMeetingSnapshot> Meetings
);

public sealed record ValidSchedule(
    Guid ScheduleId,
    IReadOnlyList<ScheduledItem> Items,
    double FitnessScore,
    IReadOnlyDictionary<string, double> MetricBreakdown
);

public sealed record GenerationResult(
    bool IsSuccess,
    IReadOnlyList<ValidSchedule> Schedules,
    long PermutationsEvaluated,
    TimeSpan ElapsedDuration,
    string? ErrorMessage
);
```
