# AegisSchedule — Stage 2 SIS Integration Guide

> **Status**: Future scope (Stage 2). The AegisSchedule API is intentionally designed to receive catalog data from any source. This guide describes the anti-corruption adapter strategy for integrating with university Student Information Systems (SIS) and ERP platforms.

---

## Overview

Stage 1 (current) uses a manual admin API to build and maintain the course catalog:

```
Admin Panel (AegisSchedule.Web)
        │
        │  X-Admin-Api-Key
        ▼
POST /api/admin/offerings → activities → groups → meetings
```

Stage 2 introduces automated ingestion from institutional SIS/ERP platforms (e.g., Oracle Banner, SAP Campus Management, PeopleSoft Campus Solutions, in-house systems):

```
University SIS / ERP
        │
        │  Batch export / webhooks / REST feed
        ▼
[ Anti-Corruption Adapter Layer ]   ← new in Stage 2
        │
        │  AegisSchedule internal domain contracts
        ▼
[ AegisSchedule.Api Persistence ]
```

---

## 1. Anti-Corruption Layer (ACL) Architecture

The ACL ensures that upstream SIS data models never leak into the AegisSchedule domain. The adapter translates SIS-specific terminology and data structures into AegisSchedule's domain model.

### ACL Location

```
src/AegisSchedule.Api/
└── Integrations/
    ├── ISisImportPort.cs            # Import port interface
    ├── SisImportService.cs          # Orchestration: validate, map, persist
    ├── Models/
    │   └── SisCatalogSnapshot.cs    # Raw inbound data contract (SIS vocabulary)
    └── Adapters/
        ├── BannerSisAdapter.cs      # Oracle Banner-specific translation
        ├── GenericJsonSisAdapter.cs # Generic JSON feed (configurable field mapping)
        └── CsvSisAdapter.cs         # Flat-file CSV import
```

### Import Port Interface

```csharp
namespace AegisSchedule.Api.Integrations;

public interface ISisImportPort
{
    /// <summary>
    /// Ingest a raw SIS catalog snapshot, validate, and persist to AegisScheduleDb.
    /// Returns a summary of created/updated/skipped entities.
    /// </summary>
    Task<SisImportResult> ImportAsync(SisCatalogSnapshot snapshot, CancellationToken ct = default);
}
```

---

## 2. Domain Mapping Contract

The SIS adapter must translate to AegisSchedule's entity hierarchy:

```
SIS Concept               →  AegisSchedule Concept
─────────────────────────────────────────────────────
Academic Year / Semester  →  Term (Semester + Year, IsCurrent flag)
Study Level / Year        →  AcademicLevel (LevelNumber)
Course / Subject          →  Course (Code, Name, AcademicLevelId)
Course Section / CRN      →  CourseOffering (CourseId, TermId)
Session Type              →  Activity (ActivityType: Lecture | Lab | Tutorial)
Section Group / Cohort    →  ActivityGroup (Name e.g. "G1", "L2")
Meeting Pattern / MTWRF   →  Meeting (DayOfWeek, StartTime, EndTime, Room)
```

### Invariants the Adapter Must Guarantee

| Invariant | Description |
|-----------|-------------|
| **No duplicate offerings** | `(CourseId, TermId)` must be unique per term. |
| **No overlapping meetings within a group** | A single group may not have two meetings at the same time on the same day. |
| **Meeting times are non-zero duration** | `StartTime < EndTime`. |
| **Group names are unique within an activity** | Prevents duplicate group selection in the solver. |

---

## 3. Recommended Integration Patterns

### Pattern A — Scheduled Batch Import (Recommended for most SIS platforms)

```
┌─────────────────────────────────────────────────────────────┐
│ University SIS                                              │
│   - Exports catalog CSV / JSON nightly at 02:00            │
│   - Places file in SFTP drop zone                          │
└────────────────────────────────────────────────────────────┘
                │
                │  Secure file transfer
                ▼
┌─────────────────────────────────────────────────────────────┐
│ AegisSchedule Import Job (IHostedService)                   │
│   - Polls SFTP / S3 bucket at configurable interval        │
│   - Parses snapshot → validates → persists via ACL          │
│   - Sends import report (email / webhook)                  │
└─────────────────────────────────────────────────────────────┘
```

### Pattern B — Webhook / Event-Driven Import

For SIS platforms supporting real-time change events:

```
SIS Event: CourseAdded / SectionUpdated / MeetingChanged
        │
        │  HTTP POST to /api/integrations/sis/events
        ▼
AegisSchedule SIS Event Handler
        │  (validates event signature + API key)
        ▼
ACL Adapter → Persistence
```

### Pattern C — Read-Only SIS API Polling

For SIS platforms with a REST API but no webhook support:

```csharp
// Configurable interval via appsettings.json
// "SisIntegration": { "PollIntervalMinutes": 60, "BaseUrl": "https://sis.university.edu/api" }

public class SisPollingService(ISisImportPort importPort, IOptions<SisOptions> options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(options.Value.PollIntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var snapshot = await FetchFromSisApiAsync(stoppingToken);
            await importPort.ImportAsync(snapshot, stoppingToken);
        }
    }
}
```

---

## 4. Configuration Reference

Add to `appsettings.json` (or environment variables):

```json
{
  "SisIntegration": {
    "Enabled": false,
    "Adapter": "GenericJson",
    "PollIntervalMinutes": 60,
    "BaseUrl": "https://sis.university.edu/api/v1",
    "ApiKey": "",
    "FieldMapping": {
      "termSemesterField": "semester",
      "termYearField":     "academic_year",
      "courseCodeField":   "course_code",
      "courseNameField":   "course_title",
      "dayOfWeekField":    "day",
      "startTimeField":    "from_time",
      "endTimeField":      "to_time"
    }
  }
}
```

---

## 5. Idempotency & Conflict Resolution

The import service must be **idempotent** — running the same import twice must produce the same result without creating duplicates.

Recommended approach:

1. **Match by natural key**: Match `CourseOffering` by `(Course.Code, Term.Semester, Term.Year)`.
2. **Upsert strategy**: If a matching entity exists, update fields; if not, insert.
3. **Soft delete orphans**: Mark offerings/groups absent in the new import as inactive (do not hard-delete — historical schedule data may reference them).
4. **Conflict log**: Record any field-value conflicts to an `SisImportLog` table for admin review.

---

## 6. Security Considerations

- The SIS integration endpoint (`/api/integrations/sis/*`) must be protected by the same `X-Admin-Api-Key` pattern as admin endpoints, or a dedicated SIS integration API key.
- Validate inbound SIS data against a strict schema before persistence — never trust external data.
- Never expose the SIS platform credentials in `appsettings.json`; use environment variables or a secrets manager.

---

## 7. Testing the Integration

```bash
# Unit test the adapter with a mock SIS snapshot
dotnet test --filter "Category=SisIntegration"

# Manual integration test using Api.http
# (see request "37: Import SIS Snapshot" when the integration endpoint is implemented)
```

A reference mock SIS snapshot (`tests/fixtures/mock_sis_catalog.json`) should be provided alongside the adapter implementation.
