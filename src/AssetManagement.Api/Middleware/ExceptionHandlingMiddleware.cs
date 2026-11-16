using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Api.Middleware;

/// <summary>
/// Translates exceptions thrown anywhere in the pipeline into RFC 7807 <see cref="ProblemDetails"/>
/// responses with the appropriate status code. Unexpected exceptions are logged and returned as a
/// generic 500 so that no internal details leak to clients.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetailsService)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; nothing useful to send back.
            _logger.LogDebug("Request {Path} was cancelled by the client", context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "Unhandled exception after the response started");
                throw;
            }

            var problem = Map(exception);

            if (problem.Status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogInformation("Request {Method} {Path} failed with {StatusCode}: {Reason}",
                    context.Request.Method, context.Request.Path, problem.Status, exception.Message);
            }

            context.Response.Clear();
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem,
                Exception = exception,
            });
        }
    }

    internal static ProblemDetails Map(Exception exception) => exception switch
    {
        ValidationException validation => new ValidationProblemDetails(
            validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        },
        NotFoundException notFound => Problem(StatusCodes.Status404NotFound, "Resource not found", notFound.Message),
        ConflictException conflict => Problem(StatusCodes.Status409Conflict, "Conflict", conflict.Message, conflict.Code),
        DomainException domain => Problem(
            StatusCodes.Status409Conflict, "Business rule violation", domain.Message, domain.Code),
        AuthenticationFailedException auth => Problem(StatusCodes.Status401Unauthorized, "Unauthorized", auth.Message),
        ArgumentException argument => Problem(StatusCodes.Status400BadRequest, "Invalid argument", argument.Message),
        _ => Problem(
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            "The server encountered an unexpected condition. Use the traceId when reporting this problem."),
    };

    private static ProblemDetails Problem(int status, string title, string detail, string? code = null)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        return problem;
    }
}
