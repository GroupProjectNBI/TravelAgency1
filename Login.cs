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
        bool result = false;
        string query = "SELECT id, password, role FROM users WHERE email = @email";
        var parameters = new MySqlParameter[]
        {
            new("@email", credentials.Email),

        };

        var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);

        if (reader.Read())
        {
            int userId = reader.GetInt32("id");
            string passwordFromDb = reader.GetString("password");
            string role = reader.GetString("role");

            if (credentials.Password == passwordFromDb)
            {

                if (ctx.Session.IsAvailable)
                {
                    ctx.Session.SetInt32("user_id", userId);
                    result = true;

                }
            }
            else
            {

                result = false;

            }
        }

        return result;

    }
}


