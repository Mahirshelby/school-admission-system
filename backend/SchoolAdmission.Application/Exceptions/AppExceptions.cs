namespace SchoolAdmission.Application.Exceptions;

// Every expected failure derives from AppException. The API turns each one into a clean 4xx JSON response.
public abstract class AppException : Exception
{
    public string Code { get; }
    protected AppException(string code, string message) : base(message) { Code = code; }
}

public class BadRequestException : AppException
{
    public BadRequestException(string code, string message) : base(code, message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string code, string message) : base(code, message) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string code, string message) : base(code, message) { }
}

public class ConflictException : AppException
{
    public ConflictException(string code, string message) : base(code, message) { }
}
