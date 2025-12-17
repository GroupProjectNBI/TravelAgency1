namespace TravelAgency;

static class Profile
{
    public record Get_Data(string email, string? first_name, string? last_name);

    public record MyPackageOverview(
        int PackageId,
        string PackageName,
        string PackageType,
        DateTime CheckIn,
        DateTime CheckOut,
        string City,
        string Country
    );



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
    public static async Task<IResult> GetMyPackages(Config config, HttpContext ctx)
    {
        int? userId = ctx.Session.GetInt32("user_id");
        if (userId == null)
            return null;


        int user_id = userId.Value;


        List<MyPackageOverview> packages = new List<MyPackageOverview>();

        string sql = """
          SELECT 
          p.id,
          p.name,
          p.package_type,
          b.check_in,
          b.check_out,
          l.city,
          c.name AS country_name
          FROM bookings b
          JOIN packages p ON b.package_id = p.id
          JOIN locations l ON b.location_id = l.id
          JOIN countries c ON l.countries_id = c.id
          WHERE b.user_id = @user_id 
          """;


        var parameters = new MySqlParameter[]
        {
            new MySqlParameter("@user_id", user_id)
        };

        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, sql, parameters);

        while (reader.Read())
        {
            packages.Add(new MyPackageOverview(
            reader.GetInt32(0),       // p.id
            reader.GetString(1),      // p.name
            reader.GetString(2),      // p.package_type
            reader.GetDateTime(3),    // b.checkin
            reader.GetDateTime(4),    // b.checkout
            reader.GetString(5),      // l.city
            reader.GetString(6)   // country_name

            ));
        }

        return Results.Ok(packages);

    }
}


