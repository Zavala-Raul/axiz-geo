using System.Text.Json;
using AxizGeo.Application.Common;
using AxizGeo.Domain.Entities;
using AxizGeo.Domain.States;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AxizGeo.Api.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "Uno o más campos no son válidos.",
                validationEx.Errors
                    .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                argEx.Message,
                null),

            UnauthorizedAppException unauthorizedEx => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                unauthorizedEx.Message,
                null),

            ForbiddenException forbiddenEx => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbiddenEx.Message,
                null),

            NotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                "Not found",
                notFoundEx.Message,
                null),

            ConflictException conflictEx => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictEx.Message,
                null),

            InvalidAlertTransitionException alertStateEx => (
                StatusCodes.Status409Conflict,
                "Invalid state transition",
                alertStateEx.Message,
                null),

            InvalidTripTransitionException tripStateEx => (
                StatusCodes.Status409Conflict,
                "Invalid state transition",
                tripStateEx.Message,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                "Ocurrió un error interno al procesar la solicitud.",
                null)
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
