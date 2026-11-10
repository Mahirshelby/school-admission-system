using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SchoolAdmission.Application;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.Dtos;
using SchoolAdmission.Application.Exceptions;
using SchoolAdmission.Application.Options;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.Services;

public class AdmissionService : IAdmissionService
{
    private readonly AdmissionDbContext _db;
    private readonly TimeProvider _clock;
    private readonly AdmissionOptions _options;

    public AdmissionService(AdmissionDbContext db, TimeProvider clock, IOptions<AdmissionOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    // ---------- Classes ----------

    public async Task<ClassDto> CreateClassAsync(CreateClassRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        var shiftName = request.ShiftName?.Trim();

        if (string.IsNullOrEmpty(name) || name.Length > 100)
            throw new BadRequestException("invalid_name", "Class name is required (max 100 characters).");
        if (string.IsNullOrEmpty(shiftName) || shiftName.Length > 100)
            throw new BadRequestException("invalid_shift", "Shift name is required (max 100 characters).");
        if (request.TotalSeats <= 0)
            throw new BadRequestException("invalid_seats", "Total seats must be greater than zero.");

        var now = _clock.GetUtcNow();
        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Name == shiftName, ct);

        if (shift is null)
        {
            shift = new Shift { Name = shiftName, CreatedAt = now };
            _db.Shifts.Add(shift);
        }
        else if (await _db.Classes.AnyAsync(c => c.ShiftId == shift.Id && c.Name == name, ct))
        {
            throw new ConflictException("class_exists", "This class already exists in this shift.");
        }

        var cls = new SchoolClass { Name = name, Shift = shift, TotalSeats = request.TotalSeats, CreatedAt = now };
        _db.Classes.Add(cls);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("class_exists", "This class already exists in this shift.");
        }

        return new ClassDto(cls.Id, cls.Name, shift.Name, cls.TotalSeats, 0, 0, cls.TotalSeats);
    }

    public Task<IReadOnlyList<ClassDto>> ListClassesAsync(CancellationToken ct) => LoadClassesAsync(null, ct);

    public async Task<ClassDto> GetClassAsync(int classId, CancellationToken ct)
    {
        var list = await LoadClassesAsync(classId, ct);
        return list.FirstOrDefault() ?? throw new NotFoundException("class_not_found", "Class not found.");
    }

    private async Task<IReadOnlyList<ClassDto>> LoadClassesAsync(int? classId, CancellationToken ct)
    {
        var now = _clock.GetUtcNow();

        var rows = await _db.Classes.AsNoTracking()
            .Where(c => classId == null || c.Id == classId)
            .OrderBy(c => c.Shift.Name).ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                ShiftName = c.Shift.Name,
                c.TotalSeats,
                // An expired HELD row no longer counts, even if the sweeper has not run yet.
                Held = c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                Confirmed = c.Holds.Count(h => h.Status == HoldStatus.Confirmed)
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new ClassDto(r.Id, r.Name, r.ShiftName, r.TotalSeats, r.Held, r.Confirmed,
                Math.Max(0, r.TotalSeats - r.Held - r.Confirmed)))
            .ToList();
    }

    // ---------- Holds ----------

    public async Task<HoldDto> CreateHoldAsync(int classId, string? studentId, CancellationToken ct)
    {
        StudentIdValidator.Validate(studentId);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1. Lock the class row. Other requests for this class now wait here, one at a time.
        var cls = await LockClassAsync(classId, ct);
        var now = _clock.GetUtcNow();

        // 2. Free seats from stale holds.
        await ExpireStaleAsync(classId, now, ct);

        // 3. One active application per student per class.
        var alreadyApplied = await _db.Holds.AnyAsync(h =>
            h.ClassId == classId && h.StudentId == studentId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed), ct);
        if (alreadyApplied)
            throw new ConflictException("already_applied", "You already have an active application for this class.");

        // 4. Count seats that are taken. Safe now because nobody else can insert for this class.
        var taken = await _db.Holds.CountAsync(h =>
            h.ClassId == classId && (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed), ct);
        if (taken >= cls.TotalSeats)
            throw new ConflictException("class_full", "No seats are available in this class.");

        // 5. Insert and commit (this releases the lock).
        var hold = new Hold
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            StudentId = studentId!,
            Status = HoldStatus.Held,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(_options.HoldDurationSeconds)
        };
        _db.Holds.Add(hold);

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("already_applied", "You already have an active application for this class.");
        }

        return await ToDtoAsync(hold, now, ct);
    }

    public async Task<HoldDto> GetHoldAsync(Guid holdId, string? studentId, CancellationToken ct)
    {
        StudentIdValidator.Validate(studentId);

        var hold = await _db.Holds.AsNoTracking().FirstOrDefaultAsync(h => h.Id == holdId, ct)
                   ?? throw new NotFoundException("hold_not_found", "Application not found.");
        if (hold.StudentId != studentId)
            throw new ForbiddenException("not_your_hold", "This application belongs to another student.");

        return await ToDtoAsync(hold, _clock.GetUtcNow(), ct);
    }

    public async Task<HoldDto> ConfirmHoldAsync(Guid holdId, string? studentId, CancellationToken ct)
    {
        StudentIdValidator.Validate(studentId);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var hold = await LockHoldAsync(holdId, studentId!, ct);
        var now = _clock.GetUtcNow();

        if (hold.Status == HoldStatus.Confirmed)
        {
            // Idempotent: already confirmed, change nothing.
            return await ToDtoAsync(hold, now, ct);
        }
        if (hold.Status == HoldStatus.Cancelled)
            throw new ConflictException("hold_cancelled", "This application was cancelled and cannot be confirmed.");
        if (hold.Status == HoldStatus.Expired)
            throw new ConflictException("hold_expired", "This hold has expired. Please apply again.");

        if (hold.ExpiresAt <= now)
        {
            hold.Status = HoldStatus.Expired;
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct); // keep the EXPIRED status, then report the error
            throw new ConflictException("hold_expired", "This hold has expired. Please apply again.");
        }

        hold.Status = HoldStatus.Confirmed;
        hold.ConfirmedAt = now;
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await ToDtoAsync(hold, now, ct);
    }

    public async Task<HoldDto> CancelHoldAsync(Guid holdId, string? studentId, CancellationToken ct)
    {
        StudentIdValidator.Validate(studentId);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var hold = await LockHoldAsync(holdId, studentId!, ct);
        var now = _clock.GetUtcNow();

        if (hold.Status == HoldStatus.Cancelled)
            return await ToDtoAsync(hold, now, ct); // repeating a cancel is harmless

        if (hold.Status == HoldStatus.Expired)
            throw new ConflictException("hold_expired", "This hold has already expired.");

        if (hold.Status == HoldStatus.Held && hold.ExpiresAt <= now)
        {
            hold.Status = HoldStatus.Expired;
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            throw new ConflictException("hold_expired", "This hold has already expired.");
        }

        hold.Status = HoldStatus.Cancelled;
        hold.CancelledAt = now;
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await ToDtoAsync(hold, now, ct);
    }

    public async Task<IReadOnlyList<HoldDto>> GetMyHoldsAsync(string? studentId, CancellationToken ct)
    {
        StudentIdValidator.Validate(studentId);
        var now = _clock.GetUtcNow();

        var holds = await _db.Holds.AsNoTracking()
            .Include(h => h.Class).ThenInclude(c => c.Shift)
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);

        return holds.Select(h => ToDto(h, h.Class.Name, h.Class.Shift.Name, now)).ToList();
    }

    public Task<int> ExpireStaleHoldsAsync(CancellationToken ct)
    {
        var now = _clock.GetUtcNow();
        return _db.Holds
            .Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), ct);
    }

    // ---------- Helpers ----------

    // SELECT ... FOR UPDATE: takes a row lock that lasts until the transaction ends.
    private async Task<SchoolClass> LockClassAsync(int classId, CancellationToken ct)
    {
        var rows = await _db.Classes
            .FromSqlInterpolated($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.FirstOrDefault() ?? throw new NotFoundException("class_not_found", "Class not found.");
    }

    // Finds the hold, checks ownership, locks its class row, then loads the hold for editing.
    private async Task<Hold> LockHoldAsync(Guid holdId, string studentId, CancellationToken ct)
    {
        var info = await _db.Holds.AsNoTracking()
            .Where(h => h.Id == holdId)
            .Select(h => new { h.ClassId, h.StudentId })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("hold_not_found", "Application not found.");

        if (info.StudentId != studentId)
            throw new ForbiddenException("not_your_hold", "This application belongs to another student.");

        await LockClassAsync(info.ClassId, ct);

        // Re-read after the lock so we see the latest status.
        return await _db.Holds.FirstAsync(h => h.Id == holdId, ct);
    }

    private Task<int> ExpireStaleAsync(int classId, DateTimeOffset now, CancellationToken ct) =>
        _db.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), ct);

    private async Task<HoldDto> ToDtoAsync(Hold hold, DateTimeOffset now, CancellationToken ct)
    {
        var names = await _db.Classes.AsNoTracking()
            .Where(c => c.Id == hold.ClassId)
            .Select(c => new { c.Name, ShiftName = c.Shift.Name })
            .FirstAsync(ct);
        return ToDto(hold, names.Name, names.ShiftName, now);
    }

    private static HoldDto ToDto(Hold h, string className, string shiftName, DateTimeOffset now)
    {
        // Show EXPIRED as soon as the time passes, even before the sweeper updates the row.
        var status = h.Status == HoldStatus.Held && h.ExpiresAt <= now ? HoldStatus.Expired : h.Status;
        return new HoldDto(h.Id, h.ClassId, className, shiftName, h.StudentId, status,
            h.CreatedAt, h.ExpiresAt, h.ConfirmedAt, h.CancelledAt);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
