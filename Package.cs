namespace TravelAgency;

using MySql.Data.MySqlClient;


class Package
{
    public record GetAll_Data(int id, int location_id, string name, string description, string package_type);
    public static async Task<List<GetAll_Data>>
    GetAll(Config config)
    {
        List<GetAll_Data> restult = new();
        string get_all_query = "SELECT id, location_id, name, description, package_type FROM packages";
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


    public record UpdatePackage_package( // expected fiels for update

      int location_id,
      string name,
      string description,
      int package_type
     );



    public static async Task UpdatePackage(int Id, UpdatePackage_package package, Config config)

    {
        string updateSql = """
    UPDATE packages
    SET 
    location_id = @location_id,
    name = @name,
    description = @description,
    package_type = @package_type
    WHERE Id=@Id;
    """;

        var parameters = new MySqlParameter[]

      {
    new("@Id", Id),
    new("@location_id", package.location_id),
    new("@name", package.name),
    new("@description", package.description),
    new("@package_type", package.package_type),

      };
        await MySqlHelper.ExecuteNonQueryAsync(config.db, updateSql, parameters); // Used to update database
    }

    public static async Task<IResult> Put(int id, UpdatePackage_package package, Config config)
    {
        try
        {
            await UpdatePackage(id, package, config);
            return Results.Ok(new { message = "Package updated successfully", id = id });
        }
        catch (Exception)
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    public static async Task
     DeletePackage(int Id, Config config)
    {
        string query = "DELETE FROM packages WHERE Id = @Id";
        var parameters = new MySqlParameter[] { new("@Id", Id) };

        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
    }
    public record CheckAvailability_Args(int package_id, string check_in, string check_out);

    public static async Task<bool> IsPackageAvailable(int packageId, DateOnly checkIn, DateOnly checkOut, Config config)
    {
        var result = await MySqlHelper.ExecuteScalarAsync(config.db,
            "CALL sp_check_package_availability(@package_id, @check_in, @check_out);",
            new[]
            {
                new MySqlParameter("@package_id", packageId),
                new MySqlParameter("@check_in", checkIn.ToDateTime(TimeOnly.MinValue)),
                new MySqlParameter("@check_out", checkOut.ToDateTime(TimeOnly.MinValue))
            });

        if (result == null || result == DBNull.Value)
            return false;

        return Convert.ToInt32(result) == 1;
    }

    public static async Task<IResult> CheckAvailability(int package_id, DateOnly check_in, DateOnly check_out, Config config)
    {
        bool available = await IsPackageAvailable(package_id, check_in, check_out, config);
        return Results.Ok(new
        {
            package_id,
            check_in,
            check_out,
            available
        });
    }

}


