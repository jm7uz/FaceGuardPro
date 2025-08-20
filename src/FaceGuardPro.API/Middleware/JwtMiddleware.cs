using FaceGuardPro.Core.Interfaces;
using FaceGuardPro.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace FaceGuardPro.API.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractTokenFromRequest(context.Request);

        if (!string.IsNullOrEmpty(token))
        {
            await AttachUserToContext(context, token);
        }

        await _next(context);
    }

    private string? ExtractTokenFromRequest(HttpRequest request)
    {
        // Check Authorization header
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Check query parameter (for testing purposes)
        var tokenFromQuery = request.Query["token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tokenFromQuery))
        {
            return tokenFromQuery;
        }

        return null;
    }

    private async Task AttachUserToContext(HttpContext context, string token)
    {
        try
        {
            var jwtService = context.RequestServices.GetRequiredService<IJwtService>();

            if (jwtService.ValidateToken(token))
            {
                var principal = jwtService.GetPrincipalFromExpiredToken(token);
                if (principal != null)
                {
                    context.User = principal;
                    _logger.LogDebug("User attached to context: {UserId}",
                        principal.FindFirst("user_id")?.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching user to context");
            // Don't throw exception, just continue without user context
        }
    }
}

// JWT Middleware extension
public static class JwtMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtMiddleware>();
    }
}