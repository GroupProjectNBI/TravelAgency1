using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TravelAgency;

public static class AuthExtensions
{
    public static void AddTravelAgencyAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication("SessionAuth")
            .AddCookie("SessionAuth", options =>
            {
                // Konfigurera 401/403 svar istället för redirect till login-sida
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });
    }
}