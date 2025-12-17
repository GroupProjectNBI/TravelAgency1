namespace TravelAgency;

using MySql.Data.MySqlClient;


class package_meals
{
    public enum DayKind
    {
        Arrival, Stay, Departure
    }
    public enum MealType
    {
        Breakfast, Lunch, Dinner
    }

    //POST
    public record Post_Args(int package_id, int restaurant_id, DayKind day_kind, MealType meal_type);

    // Post method to add a row in package_meals
    public static async Task<IResult> Post(Post_Args args, Config config)

    {
        //validation

        var ruleErr = ValidateRules(args.day_kind, args.meal_type);
        if (ruleErr is not null)
            return ruleErr;


        if (!await PackageExists(config, args.package_id))
            return Results.BadRequest(new { message = "package_id does not exist" });

        if (!await RestaurantExists(config, args.restaurant_id))
            return Results.BadRequest(new { message = "restaurant_id does not exist" });


        /// write a SQL INSERT statement
        string query = """  
            INSERT INTO packages_meals (package_id, restaurant_id, day_kind, meal_type)
            VALUES (@package_id, @restaurant_id, @day_kind, @meal_type);
            """;

        var parameters = new MySqlParameter[]
        {
        new("@package_id", args.package_id),
        new("@restaurant_id", args.restaurant_id),
        new("@day_kind", args.day_kind.ToString()),
        new("@meal_type", args.meal_type.ToString())
        };
        try
        {

            await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);


            var newIdObj = await MySqlHelper.ExecuteScalarAsync(config.db, "SELECT LAST_INSERT_ID();");
            int newId = Convert.ToInt32(newIdObj);

            return Results.Created("/packages_meals/" + newId, new { id = newId, message = "Created successfully" });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Results.Conflict(new { message = "Meal already exists for this package and day_kind" });
        }
        catch
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }


    }

    //SELECT
    private static Meal_Data Read_Meal(MySqlDataReader reader)
    {
        return new Meal_Data(
            id: reader.GetInt32(0),
            package_id: reader.GetInt32(1),
            restaurant_id: reader.GetInt32(2),
            day_kind: reader.GetString(3),
            meal_type: reader.GetString(4)
        );
    }

    public record Meal_Data(int id, int package_id, int restaurant_id, string day_kind, string meal_type);
    public static async Task<List<Meal_Data>> Get_All(Config config)
    {
        List<Meal_Data> meals = new();

        string query = "SELECT id, package_id, restaurant_id, day_kind, meal_type FROM packages_meals";

        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query);

        while (reader.Read())
        {
            meals.Add(Read_Meal(reader));
        }

        return meals;
    }

    // PUT
    public record Put_Args(int restaurant_id, DayKind day_kind, MealType meal_type);

    // Put method to edit an existing row in the package_meals
    public static async Task<IResult> Put(int id, Put_Args args, Config config)
    {
        if (id <= 0) return Results.BadRequest(new { message = "Invalid id" });

        //validate rules
        var ruleErr = ValidateRules(args.day_kind, args.meal_type);
        if (ruleErr is not null) return ruleErr;

        //does the row exist?
        var rowExists = await MySqlHelper.ExecuteScalarAsync(
        config.db, "SELECT COUNT(1) FROM packages_meals WHERE id = @id",
        [new("@id", id)]
        );

        if (Convert.ToInt32(rowExists) == 0)
            return Results.NotFound(new { message = "package_meals row not found" });

        //does the restaurant exist?
        if (!await RestaurantExists(config, args.restaurant_id))
            return Results.BadRequest(new { message = "The restaurant_id does not exist" });


        string query = """
            UPDATE packages_meals
            SET
            restaurant_id = @restaurant_id,
            day_kind = @day_kind,
            meal_type = @meal_type
            WHERE id = @id;
        """;

        var parameters = new MySqlParameter[]
        {
            new("@restaurant_id", args.restaurant_id),
            new("@day_kind", args.day_kind.ToString()),
            new("@meal_type", args.meal_type.ToString()),
            new("@id", id)
        };

        try
        {
            await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
            return Results.NoContent();
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Results.Conflict(new
            { message = "Meal already exist for this package and day_kind" });
        }
        catch
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }



    //DELETE
    //DELETE-method to delete a row in packages_meals
    public static async Task Delete(int Id, Config config)
    {
        string query = "DELETE FROM packages_meals WHERE id = @Id";
        var parameters = new MySqlParameter[] { new("@Id", Id) };

        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
    }

    //HELPERS
    static IResult? ValidateRules(DayKind dayKind, MealType mealType)
    {
        if (dayKind == DayKind.Arrival && mealType != MealType.Dinner)
            return Results.BadRequest(new { message = "Arrival day can only include dinner" });

        if (dayKind == DayKind.Departure && mealType != MealType.Breakfast)
            return Results.BadRequest(new { message = "Departure day can only include breakfast" });

        return null;
    }

    static async Task<bool> PackageExists(Config config, int packageId)
    {
        object obj = await MySqlHelper.ExecuteScalarAsync(
            config.db, "SELECT COUNT(1) FROM packages WHERE id = @id",
            [new("@id", packageId)]
            );
        return Convert.ToInt32(obj) > 0;
    }
    static async Task<bool> RestaurantExists(Config config, int restaurantId)
    {
        object obj = await MySqlHelper.ExecuteScalarAsync(
            config.db,
            "SELECT COUNT(1) FROM restaurants WHERE id = @id",
            [new("@id", restaurantId)]);

        return Convert.ToInt32(obj) > 0;
    }
}

