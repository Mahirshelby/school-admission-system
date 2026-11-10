using System.Text.RegularExpressions;
using SchoolAdmission.Application.Exceptions;

namespace SchoolAdmission.Application;

public static class StudentIdValidator
{
    private static readonly Regex Pattern = new(@"^\d{17}$", RegexOptions.Compiled);

    public static void Validate(string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            throw new BadRequestException("student_id_required", "The X-Student-Id header is required.");
        if (!Pattern.IsMatch(studentId))
            throw new BadRequestException("student_id_invalid", "X-Student-Id must be a 17-digit birth registration number.");
    }
}
