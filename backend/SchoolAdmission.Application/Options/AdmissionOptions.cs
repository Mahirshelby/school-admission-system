namespace SchoolAdmission.Application.Options;

public class AdmissionOptions
{
    public const int DefaultHoldDurationSeconds = 600;
    public int HoldDurationSeconds { get; set; } = DefaultHoldDurationSeconds;
}
