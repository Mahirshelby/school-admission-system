using SchoolAdmission.Application.Dtos;

namespace SchoolAdmission.Application.Abstractions;

public interface IAdmissionService
{
    Task<ClassDto> CreateClassAsync(CreateClassRequest request, CancellationToken ct);
    Task<IReadOnlyList<ClassDto>> ListClassesAsync(CancellationToken ct);
    Task<ClassDto> GetClassAsync(int classId, CancellationToken ct);

    Task<HoldDto> CreateHoldAsync(int classId, string? studentId, CancellationToken ct);
    Task<HoldDto> GetHoldAsync(Guid holdId, string? studentId, CancellationToken ct);
    Task<HoldDto> ConfirmHoldAsync(Guid holdId, string? studentId, CancellationToken ct);
    Task<HoldDto> CancelHoldAsync(Guid holdId, string? studentId, CancellationToken ct);
    Task<IReadOnlyList<HoldDto>> GetMyHoldsAsync(string? studentId, CancellationToken ct);

    // Background sweep: marks expired HELD rows as EXPIRED. Returns how many rows changed.
    Task<int> ExpireStaleHoldsAsync(CancellationToken ct);
}
