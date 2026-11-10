using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Application.Dtos;

public record CreateClassRequest(string? Name, string? ShiftName, int TotalSeats);

public record ClassDto(
    int Id,
    string Name,
    string ShiftName,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);

public record HoldDto(
    Guid Id,
    int ClassId,
    string ClassName,
    string ShiftName,
    string StudentId,
    HoldStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CancelledAt);
