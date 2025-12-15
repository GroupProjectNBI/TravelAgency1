namespace TravelAgency;

using MySql.Data.MySqlClient;


class bookings_meals
{
    public record Put_data(int bookings_id, DateOnly date, string meal_type);
    public static async Task<IResult>

  Put(int id, Put_data data, Config config)
    {

        var bok_mealExists = await MySqlHelper.ExecuteScalarAsync(config.db,
          "SELECT COUNT(1) FROM booking_meals WHERE id = @id",
          new MySqlParameter[] { new("@id", id) });

        if (Convert.ToInt32(bok_mealExists) == 0)
            return Results.BadRequest(new { message = "bookings_meals does not exist" });

        if (string.IsNullOrWhiteSpace(data.meal_type))
            return Results.BadRequest(new { message = "meal type is required" });

        var allowedMeals = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "Breakfast", "Lunch", "Dinner" };

        if (!allowedMeals.Contains(data.meal_type))
            return Results.BadRequest(new { message = "Invalid meal_type. Allowed: Breakfast, Lunch, Dinner." });

        var bookingsExist = await MySqlHelper.ExecuteScalarAsync(config.db,
            "SELECT COUNT(1) FROM bookings WHERE id = @bookings_id",
            new MySqlParameter[] { new("@bookings_id", data.bookings_id) });

        if (Convert.ToInt32(bookingsExist) == 0)
            return Results.BadRequest(new { message = "bookings_id does not exist" });

        if (data.bookings_id <= 0)
            return Results.BadRequest(new { message = "Invalid bookings_id." });

        try
        {
            string updateSql = """
            UPDATE booking_meals
            SET
                bookings_id = @bookings_id, 
                date = @date,
                meal_type = @meal_type
             WHERE id = @id;
            """;

            var parameters = new MySqlParameter[]
  {
            new("@id", id),
            new("@bookings_id", data.bookings_id),
                        new("@date", data.date),
            new("@meal_type", data.meal_type)
  };


            int affected = await MySqlHelper.ExecuteNonQueryAsync(config.db, updateSql, parameters);
            if (affected == 0)
            {
                var existsObj = await MySqlHelper.ExecuteScalarAsync(config.db,
                    "SELECT COUNT(1) FROM booking_meals WHERE id = @id",
                    new MySqlParameter[] { new MySqlParameter("@id", id) });

                if (existsObj == null || existsObj == DBNull.Value)
                {

                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                }

                long count = Convert.ToInt64(existsObj);
                if (count == 0)
                {
                    return Results.NotFound(new { message = "bookings meal not found" });
                }
                else
                {
                    return Results.NoContent();
                }
            }


            var body = new { id = id, message = "Updated successfully" };
            return Results.Ok(body);
        }
        catch (Exception mjau)
        {
            Console.WriteLine(mjau);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

    }
}