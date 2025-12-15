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
        // 1. Kolla om vi har ett ID i sessionen
        var userId = context.Session.GetInt32("user_id");
        var userRole = context.Session.GetString("role");

        if (userId.HasValue && !string.IsNullOrEmpty(userRole))
        {
            // 2. Skapa Claims (identitetsbrickor) baserat på sessionen
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Role, userRole)
            };

            // 3. Skapa en identitet och sätt den på context.User
            // "SessionAuth" är namnet på vår autentiseringstyp (viktigt!)
            var identity = new ClaimsIdentity(claims, "SessionAuth");
            var principal = new ClaimsPrincipal(identity);

            context.User = principal;
        }

        // Gå vidare
        await _next(context);
    }
}