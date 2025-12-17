namespace TravelAgency;

using MySql.Data.MySqlClient;


class Package
{
    private record HotelDto(
    int Id,
    string Name,
    string Address,
    string City,
    string Country,
    string Price,
    bool HasBreakfast,
    int Capacity
);

    private record PackageDto(
        int Id,
        string Name,
        string Description,
        string Type,
        string City,
        string Country,
        string IncludedMeals,
        string IncludedRestaurants
    );
    public record fullTell(string name, string address, int price, bool has_breakfast);
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
    public static async Task<IResult> GetDetails(int locationid, int packageid, int hotelid, Config config)
    {
        List<HotelDto> hotels = new();
        List<PackageDto> packages = new();

        // ---------------------------------------------------------
        // STEP 1: Get Hotel
        // ---------------------------------------------------------
        var hotelSql = @"
        SELECT 
            h.id, h.name, h.address, h.price_class, h.has_breakfast, h.capacity, h.max_cap,
            l.city,
            c.name as country
        FROM hotels h
        JOIN locations l ON h.location_id = l.id
        JOIN countries c ON l.countries_id = c.id
        WHERE h.id = @hotelid AND l.city = (SELECT city FROM locations WHERE id = @locationid)";

        var hotelParams = new MySqlParameter[]
        {
        new("@locationid", locationid),
        new("@hotelid", hotelid)
        };

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, hotelSql, hotelParams))
        {
            while (await reader.ReadAsync())
            {
                // Add to the list
                hotels.Add(new HotelDto(
                 Id: reader.GetInt32("id"),
                Name: reader.GetString("name"),
                Address: reader.GetString("address"),
                City: reader.GetString("city"),
                Country: reader.GetString("country"),
                Price: $"${reader.GetInt32("price_class")}",
                HasBreakfast: reader.GetBoolean("has_breakfast"),
                // Handle Capacity cab be NULL in the database
                Capacity: reader.IsDBNull(reader.GetOrdinal("capacity")) ? 0 : reader.GetInt32("capacity")
                ));
            }
        }

        // ---------------------------------------------------------
        // STEP 2: Get Package
        // ---------------------------------------------------------
        var packageSql = @"
       SELECT 
            p.id, p.name, p.description, p.package_type,
            l.city,
            c.name as country,
            -- Slå ihop alla måltider till en sträng (t.ex 'Breakfast, Dinner')
            GROUP_CONCAT(DISTINCT pm.meal_type SEPARATOR ', ') as meals,
            -- Slå ihop alla restaurangnamn till en sträng
            GROUP_CONCAT(DISTINCT r.name SEPARATOR ', ') as restaurants
        FROM packages p
        JOIN locations l ON p.location_id = l.id
        JOIN countries c ON l.countries_id = c.id
        LEFT JOIN packages_meals pm ON p.id = pm.package_id
        LEFT JOIN restaurants r ON pm.restaurant_id = r.id
        WHERE p.id = @packageid AND l.city = (SELECT city FROM locations WHERE id = @locationid)
        GROUP BY p.id";

        var packageParams = new MySqlParameter[]
        {
        new("@locationid", locationid),
        new("@packageid", packageid)
        };

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, packageSql, packageParams))
        {
            while (await reader.ReadAsync())
            {
                packages.Add(new PackageDto(
                Id: reader.GetInt32("id"),
                Name: reader.GetString("name"),
                Description: reader.GetString("description"),
                Type: reader.GetString("package_type"),
                City: reader.GetString("city"),
                Country: reader.GetString("country"),
                // check if null (no meals in the package)
                IncludedMeals: reader.IsDBNull(reader.GetOrdinal("meals")) ? "None" : reader.GetString("meals"),
                IncludedRestaurants: reader.IsDBNull(reader.GetOrdinal("restaurants")) ? "None" : reader.GetString("restaurants")
                ));
            }
        }

        // ---------------------------------------------------------
        // STEP 3: Done! Return the object
        // ---------------------------------------------------------

        // Create the response object containing both lists
        var response = new
        {
            hotels = hotels,
            package = packages
        };

        return Results.Ok(response);
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
            // Logg mex internally if you have logs
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception)
        {
            // Log ex internally
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }


    public record UpdatePackage_package( // expected fields for update

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
}


