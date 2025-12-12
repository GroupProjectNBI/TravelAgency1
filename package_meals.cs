namespace TravelAgency;

using MySql.Data.MySqlClient;


class package_meals

{
    public record Post_Args(int package_id, int restaurant_id, string meal_type, int day_offset);
    public record Meal_Data(int id, int package_id, int restaurant_id, string meal_type, int day_offset);

    private static Meal_Data Read_Meal(MySqlDataReader reader)
    {
        return new Meal_Data(
            id: reader.GetInt32(0),
            package_id: reader.GetInt32(1),
            restaurant_id: reader.GetInt32(2),
            meal_type: reader.GetString(3),
            day_offset: reader.GetInt32(4)
        );
    }
    public static async Task<List<Meal_Data>> Get_All(Config config)
    {
        List<Meal_Data> meals = new();

        string query = "SELECT id, package_id, restaurant_id, meal_type FROM packages_meals";

        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query);

        while (reader.Read())
        {
            meals.Add(Read_Meal(reader));
        }

        return meals;
    }
    public static async Task<IResult> Post(Post_Args args, Config config)
    {
        // Validation: checks that the input is correct before inserting into the database, including:
        // 1) meal_type must be one of "Breakfast", "Lunch", or "Dinner" to prevent invalid meal entries,
        // 2) day_offset must be 0 or greater because a meal cannot occur on a negative day,
        // 3) package_id must exist in the packages table to avoid referencing a non-existing package,
        // 4) restaurant_id must exist in the restaurants table to avoid referencing a non-existing restaurant.

        string[] validMeals = { "Breakfast", "Lunch", "Dinner" };
        if (!validMeals.Contains(args.meal_type))
            return Results.BadRequest(new { message = "Invalid meal_type" });

        if (args.day_offset < 0)
            return Results.BadRequest(new { message = "day_offset must be 0 or greater" });

        // Kontrollera att package_id finns
        var packageExists = await MySqlHelper.ExecuteScalarAsync(config.db,
        "SELECT COUNT(1) FROM packages WHERE id = @id",
        new MySqlParameter[] { new("@id", args.package_id) });
        if (Convert.ToInt32(packageExists) == 0)
            return Results.BadRequest(new { message = "package_id does not exist" });

        // Kontrollera att restaurant_id finns
        var restaurantExists = await MySqlHelper.ExecuteScalarAsync(config.db,
        "SELECT COUNT(1) FROM restaurants WHERE id = @id",
        new MySqlParameter[] { new("@id", args.restaurant_id) });
        if (Convert.ToInt32(restaurantExists) == 0)
            return Results.BadRequest(new { message = "restaurant_id does not exist" });

        try
        {      /// write a SQL INSERT statement
            string query = """  
            INSERT INTO packages_meals (package_id, restaurant_id, meal_type, day_offset)
            VALUES (@package_id, @restaurant_id, @meal_type, @day_offset);
        """;

            var parameters = new MySqlParameter[]
            {
            new("@package_id", args.package_id),
            new("@restaurant_id", args.restaurant_id),
            new("@meal_type", args.meal_type),
            new("@day_offset", args.day_offset)
            };

            // /// write a SQL INSERT statement
            await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);

            // To know the unique ID that MySQL generated (auto-increment id column)
            var newIdObj = await MySqlHelper.ExecuteScalarAsync(config.db, "SELECT LAST_INSERT_ID();");
            int newId = Convert.ToInt32(newIdObj);

            return Results.Created($"/packages_meals/{newId}", new { id = newId, message = "Created successfully" });
        }
        catch (Exception)
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}

