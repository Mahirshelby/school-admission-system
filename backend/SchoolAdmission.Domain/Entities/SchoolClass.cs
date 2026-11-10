namespace SchoolAdmission.Domain.Entities;

// A class offered in one shift (e.g. Class 5 / Morning). It owns the seat count.
public class SchoolClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public int TotalSeats { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<Hold> Holds { get; set; } = new();
}
