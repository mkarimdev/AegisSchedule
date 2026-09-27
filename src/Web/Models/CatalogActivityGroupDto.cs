namespace Web.Models;

public sealed class CatalogActivityGroupDto
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
