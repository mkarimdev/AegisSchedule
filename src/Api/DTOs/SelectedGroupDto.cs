namespace Api.DTOs;

public sealed class SelectedGroupDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public List<MeetingDto> Meetings { get; set; } = [];
}
