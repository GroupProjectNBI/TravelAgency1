namespace TravelAgency;

using MySql.Data.MySqlClient;


class Package
{
    public record GetAll_Data(int id, int location_id, string name, string description, string package_type);
    public static async Task<List<GetAll_Data>>
    GetAll(Config config)
    {
        List<GetAll_Data> restult = new();
        string get_all_query = "SELECT id,location_id,name, description, package_type FROM packages";
        using (var reader = await
        MySqlHelper.ExecuteReaderAsync(config.db, get_all_query))
        {
            while (reader.Read())
            {
                restult.Add(new(
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4)
                ));
            }
        }
        return restult;
    }

    public record Get_Data(int location_id, string name, string description, string package_type);
    public static async Task<Get_Data>
    Get(int Id, Config config)
    {
        Get_Data? result = null;
        string get_one_query = "SELECT location_id,name, description, package_type FROM packages WHERE id = @id ";
        var parameters = new MySqlParameter[] { new("@Id", Id) };

        using (var reader = await
        MySqlHelper.ExecuteReaderAsync(config.db, get_one_query, parameters))
        {
            if (reader.Read())
            {
                result = new(
                  reader.GetInt32(0),
                  reader.GetString(1),
                  reader.GetString(2),
                  reader.GetString(3)
                );
            }
        }
        return result;
    }

    public record Post_Args(int location_id, string name, string description, int package_type);
    public static async Task<IResult>
    Post(Post_Args args, Config config)
    {
        if (args.location_id <= 0)
            return Results.BadRequest(new { message = "Invalid location_id" });

        // Check so the location id that is send in exist
        var exists = await MySqlHelper.ExecuteScalarAsync(config.db,
            "SELECT COUNT(1) FROM locations WHERE id = @id",
            new MySqlParameter[] { new("@id", args.location_id) });

        if (Convert.ToInt32(exists) == 0)
            return Results.BadRequest(new { message = "location_id does not exist" });

        if (string.IsNullOrWhiteSpace(args.name))
            return Results.BadRequest(new { message = "Name is required" });


        string insert_to_package_query = """
        INSERT INTO packages (location_id, name, description, package_type)
        VALUES (@location_id, @name, @description, @package_type);
        """;

        var parameters = new MySqlParameter[]
        {
        new("@location_id", args.location_id),
        new("@name", args.name),
        new("@description", args.description),
        new("@package_type", args.package_type),
        };

        try
        {
            await MySqlHelper.ExecuteNonQueryAsync(config.db, insert_to_package_query, parameters);
            // get the id for the new post
            // Fetch the new id seperatly
            var newIdObj = await MySqlHelper.ExecuteScalarAsync(config.db, "SELECT LAST_INSERT_ID();");
            // verifiy that the insert has gone ok. 
            if (newIdObj == null || newIdObj == DBNull.Value)
                return Results.StatusCode(StatusCodes.Status500InternalServerError);

            int newId = Convert.ToInt32(newIdObj);

            var body = new { id = newId, message = "Created Successfully" };
            return Results.Json(body, statusCode: StatusCodes.Status201Created);
        }
        catch (MySql.Data.MySqlClient.MySqlException)
        {
            // Logga mex internt om du har logger
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception)
        {
            // Logga ex internt
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }



}