namespace AegisSchedule.Api.Domain;

public class Term
{
    public Guid Id { get; set; }
    public Semester Semester { get; set; }
    public int Year { get; set; }
    public bool IsCurrent { get; set; }
}
