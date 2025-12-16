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

    public static async Task<bool> Post(Post_Args credentials, Config config, HttpContext ctx)
    {
        string query = "SELECT users.id, roles.role FROM users INNER JOIN roles ON users.role_id = roles.id WHERE users.email = @email AND users.password = @password";

        var parameters = new MySqlParameter[]
        {
            new("@email", credentials.Email),
            new("@password", credentials.Password),
        };

        // using is important here to cleanly close the connection
        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);

        // If we find a row (ReadAsync returns true)
        if (await reader.ReadAsync())
        {
            int id = reader.GetInt32("id");
            string role = reader.GetString("role");

            if (ctx.Session.IsAvailable)
            {
                ctx.Session.SetInt32("user_id", id);
                ctx.Session.SetString("role", role);

                // IMPORTANT: Return true here since we succeeded!
                return true;
            }
        }

        // If we get here, the password was wrong or the user did not exist
        return false;
    }
}