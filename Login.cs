namespace TravelAgency;

static class Login
{
    public static void Delete(HttpContext ctx)
    {
        if (ctx.Session.IsAvailable)
        {
            ctx.Session.Clear();
        }
    }

    public record Post_Args(string Email, string Password);
    public static async Task<bool>

    Post(Post_Args credentials, Config config, HttpContext ctx)
    {
        string query = "SELECT id, role FROM users WHERE email = @email AND password = @password";

        var parameters = new MySqlParameter[]
        {
            new("@email", credentials.Email),
            new("@password", credentials.Password),
        };

        // using är viktigt här för att stänga kopplingen snyggt
        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);

        // Om vi hittar en rad (ReadAsync returnerar true)
        if (await reader.ReadAsync())
        {
            int id = reader.GetInt32("id");
            string role = reader.GetString("role");

            if (ctx.Session.IsAvailable)
            {
                ctx.Session.SetInt32("user_id", id);
                ctx.Session.SetString("role", role);

                // VIKTIGT: Returnera true här eftersom vi lyckades!
                return true;
            }
        }

        // Om vi kommer hit var lösenordet fel eller användaren fanns inte
        return false;

    }
}