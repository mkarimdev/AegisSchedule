using System.Text.Json.Serialization;
using AegisSchedule.Api.Domain;

namespace AegisSchedule.Api.DTOs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SisSyncMode
{
    Upsert = 1,
    ReplaceOfferings = 2,
    FullTermReconcile = 3
}

public class SisBatchSyncRequest
{
    public Guid? UniversityId { get; set; }
    public SisTermSyncDto Term { get; set; } = null!;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SisSyncMode Mode { get; set; } = SisSyncMode.Upsert;
    public List<SisCourseSyncDto> Courses { get; set; } = [];
}

public class SisTermSyncDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Semester Semester { get; set; }
    public int Year { get; set; }
    public bool? SetAsCurrent { get; set; }
}

public class SisCourseSyncDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AcademicLevelNumber { get; set; }
    public string? ExternalCourseId { get; set; }
    public List<SisActivitySyncDto> Activities { get; set; } = [];
}

public class SisActivitySyncDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActivityType Type { get; set; }
    public List<SisActivityGroupSyncDto> Groups { get; set; } = [];
}

public class SisActivityGroupSyncDto
{
    public string Name { get; set; } = string.Empty;
    public string? ExternalSectionId { get; set; }
    public List<SisMeetingSyncDto> Meetings { get; set; } = [];
}

public class SisMeetingSyncDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Room { get; set; }
}

public class SisBatchSyncResult
{
    public bool Success { get; set; }
    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public SisSyncMetrics Metrics { get; set; } = new();
    public List<SisSyncDiagnostic> Errors { get; set; } = [];
    public List<SisSyncDiagnostic> Warnings { get; set; } = [];
}

public class SisSyncMetrics
{
    public int CoursesCreated { get; set; }
    public int CoursesUpdated { get; set; }
    public int OfferingsCreated { get; set; }
    public int OfferingsUpdated { get; set; }
    public int ActivitiesCreated { get; set; }
    public int GroupsCreated { get; set; }
    public int MeetingsCreated { get; set; }
    public int EntitiesRemoved { get; set; }
    public long ElapsedMilliseconds { get; set; }
}

public class SisSyncDiagnostic
{
    public string Code { get; set; } = string.Empty;
    public string EntityPath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
