namespace DyDashboard.Api.Common.Errors;

/// <summary>
/// Base class for all expected, operational errors. Carries an HTTP status and a
/// stable machine-readable code so the exception handler can translate any
/// thrown <see cref="AppException"/> into a consistent JSON envelope.
/// </summary>
public class AppException(int statusCode, string code, string message, object? details = null)
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public object? Details { get; } = details;
}

public sealed class NotFoundException(string message = "Resource not found")
    : AppException(404, "NOT_FOUND", message);

public sealed class ValidationException(object? details = null, string message = "Validation failed")
    : AppException(422, "VALIDATION_ERROR", message, details);

public sealed class BadRequestException(string message = "Bad request", object? details = null)
    : AppException(400, "BAD_REQUEST", message, details);
