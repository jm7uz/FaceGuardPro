using System.Net;
using System.Text.Json;
using FaceGuardPro.Shared.Models;
using FaceGuardPro.Shared.Enums;

namespace FaceGuardPro.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred. RequestId: {RequestId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>();

        switch (exception)
        {
            case UnauthorizedAccessException:
                response = ApiResponse<object>.UnauthorizedResult(exception.Message);
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case ArgumentNullException:
            case ArgumentException:
                response = ApiResponse<object>.BadRequestResult($"Invalid argument: {exception.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case KeyNotFoundException:
                response = ApiResponse<object>.NotFoundResult("The requested resource was not found");
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case InvalidOperationException:
                response = ApiResponse<object>.ErrorResult($"Invalid operation: {exception.Message}", ApiResponseStatus.Conflict);
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                break;

            case TimeoutException:
                response = ApiResponse<object>.ErrorResult("The operation timed out", ApiResponseStatus.InternalServerError);
                context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                break;

            case NotImplementedException:
                response = ApiResponse<object>.ErrorResult("This feature is not yet implemented", ApiResponseStatus.InternalServerError);
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                break;

            default:
                response = ApiResponse<object>.ErrorResult("An error occurred while processing your request", ApiResponseStatus.InternalServerError);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

// Extension method
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}