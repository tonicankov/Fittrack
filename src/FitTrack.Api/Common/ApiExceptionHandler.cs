using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Common;

/// <summary>Turns the exceptions from Errors.cs into RFC 9457 problem details.</summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails? details = exception switch
        {
            RequestValidationException e => new HttpValidationProblemDetails(e.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = e.Message,
            },
            NotFoundException e => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = e.Message },
            ConflictException e => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = e.Message },
            AuthenticationFailedException e => new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = e.Message },
            _ => null,
        };

        if (details is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = details.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = details,
            Exception = exception,
        });
    }
}
