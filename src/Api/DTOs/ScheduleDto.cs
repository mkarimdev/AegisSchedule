namespace Api.DTOs;

public sealed class ScheduleDto
{
    public Guid ScheduleId { get; set; }
    public List<SelectedGroupDto> SelectedGroups { get; set; } = [];
}
