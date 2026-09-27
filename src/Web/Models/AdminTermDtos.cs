namespace Web.Models;

public enum Semester
{
    Fall = 0,
    Spring = 1,
    Summer = 2
}

public class AdminTermDto
{
    public Guid Id { get; set; }
    public Semester Semester { get; set; }
    public int Year { get; set; }
    public bool IsCurrent { get; set; }
    public string DisplayName => $"{Semester} {Year}";
}

public class CreateTermRequest
{
    public Semester Semester { get; set; }
    public int Year { get; set; }
    public bool IsCurrent { get; set; }
}
