namespace TravelAgency;

static class Profile
{
    public record Get_Data(string email, string? first_name, string? last_name);
    public static async Task<Get_Data?>

    Get(Config config, HttpContext ctx)
    {
        Get_Data? result = null;

        if (ctx.Session.IsAvailable)
        {
            if (ctx.Session.GetInt32("user_id") is int user_id)
            {
                string query = "SELECT email, first_name, last_name FROM users WHERE id =@id";
                var parameters = new MySqlParameter[] { new("@id", user_id) };
                using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
                {
                    if (reader.Read())
                    {
                        result = new(reader.GetString(0), reader[1] as string, reader[2] as string);
                    }
                }
            }
        }

        return result;
    }
}