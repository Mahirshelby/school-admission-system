namespace SchoolAdmission.API.Infrastructure;

// The one JSON shape every error uses: { "code": "...", "message": "..." }
public record ErrorResponse(string Code, string Message);
