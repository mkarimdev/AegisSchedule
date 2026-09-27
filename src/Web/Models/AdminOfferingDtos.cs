namespace Web.Models;

public class CreateOfferingRequest
{
    public Guid CourseId { get; set; }
    public Guid TermId { get; set; }
}

public class AdminOfferingDetailDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int AcademicLevelNumber { get; set; }
    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public List<AdminActivityDetailDto> Activities { get; set; } = [];
}

public class AdminActivityDetailDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<AdminActivityGroupDetailDto> Groups { get; set; } = [];
}

public class AdminActivityGroupDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<AdminMeetingDetailDto> Meetings { get; set; } = [];
}

public class AdminMeetingDetailDto
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? Room { get; set; }
}
