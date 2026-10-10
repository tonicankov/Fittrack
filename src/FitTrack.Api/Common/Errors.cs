namespace FitTrack.Api.Common;

/// <summary>One or more request fields are invalid (400).</summary>
public sealed class RequestValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more fields are invalid.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public static RequestValidationException For(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

/// <summary>
/// The entry does not exist, or the requester may not see it (404).
/// Both cases look the same on purpose, so private entries cannot be detected.
/// </summary>
public sealed class NotFoundException(string message = "Not found.") : Exception(message);

/// <summary>The request conflicts with the current state (409).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Wrong e-mail address or password (401).</summary>
public sealed class AuthenticationFailedException(string message = "Invalid e-mail address or password.")
    : Exception(message);
