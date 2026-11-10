namespace SchoolAdmission.Domain.Entities;

public class Shift
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public List<SchoolClass> Classes { get; set; } = new();
}
