
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using SolidarityConnection.Donors.Identity.Api.Extensions;

namespace SolidarityConnection.Donors.Identity.Api.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing request. {@RequestPath}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int status = (int)HttpStatusCode.InternalServerError;
        string title = "An unexpected error occurred.";

        switch (exception)
        {
            case KeyNotFoundException:
                status = (int)HttpStatusCode.NotFound;
                title = "Resource not found.";
                break;
            case UnauthorizedAccessException:
                status = (int)HttpStatusCode.Unauthorized;
                title = "Unauthorized.";
                break;
            case ArgumentNullException:
            case ArgumentException:
                status = (int)HttpStatusCode.BadRequest;
                title = "Invalid request.";
                break;
        }

        var problem = context.CreateProblemDetails(status, title, exception.Message);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var result = JsonSerializer.Serialize(problem, options);
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problem.Status ?? status;
        return context.Response.WriteAsync(result);
    }
}
