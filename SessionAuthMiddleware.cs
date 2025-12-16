using System.Security.Claims;

public class SessionAuthMiddleware
{
    private readonly RequestDelegate _next;

    public SessionAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // DEBUG: Kolla vad som finns i sessionen
        var userId = context.Session.GetInt32("user_id");
        var userRole = context.Session.GetString("role");
        if (userId.HasValue && !string.IsNullOrEmpty(userRole))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Role, userRole)
            };
            // VIKTIGT: "SessionAuth" måste matcha det du skrev i AuthExtensions/Program.cs
            var identity = new ClaimsIdentity(claims, "SessionAuth");
            var principal = new ClaimsPrincipal(identity);

            context.User = principal;

        }
        await _next(context);
    }
}