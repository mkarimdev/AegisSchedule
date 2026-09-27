namespace Web.Models;

public sealed class ScheduleDto
{
    public Guid ScheduleId { get; set; }
    public List<SelectedGroupDto> SelectedGroups { get; set; } = [];
}
