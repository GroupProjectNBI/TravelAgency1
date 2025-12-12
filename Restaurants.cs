namespace TravelAgency;

using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

class Restaurants
{
    public record GetAll_Data(int location_id, string name, bool is_veggie_friendly, bool is_fine_dining, bool is_wine_focused);
    public static async Task<List<GetAll_Data>>

    GetAll(Config config)
    {
        List<GetAll_Data> result = new();
        string query = "SELECT location_id, name, is_veggie_friendly, is_fine_dining, is_wine_focused FROM restaurants ;";
        using (var reader = await
        MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (reader.Read())
            {
                result.Add(new(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3),
                reader.GetBoolean(4)
                ));
            }
        }
        return result;
    }
    public record Get_Data(int location_id, string name, bool is_veggie_friendly, bool is_fine_dining, bool is_wine_focused);
    public static async Task<Get_Data?>
    Get(int Id, Config config)
    {
        Get_Data? result = null;
        string query = "SELECT location_id, name, is_veggie_friendly, is_fine_dining, is_wine_focused FROM restaurants WHERE Id = @Id";
        var parameters = new MySqlParameter[] { new("@Id", Id) };
        using (var reader = await
        MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
        {
            if (reader.Read())
            {
                result = new(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3),
                reader.GetBoolean(4));
            }
        }
        return result;
    }
    public record Post_Args(int location_id, string name, bool is_veggie_friendly, bool is_fine_dining, bool is_wine_focused);

    public static async Task<IResult>
    Post(Post_Args restaurant, Config config)
    {
        // Simple validation of the object
        if (string.IsNullOrWhiteSpace(restaurant.name))
            return Results.BadRequest(new { message = "Name is required" });

        if (restaurant.location_id <= 0)
            return Results.BadRequest(new { message = "Invalid location_id" });

        // Check so the location id that is send in exist
        var exists = await MySqlHelper.ExecuteScalarAsync(config.db,
            "SELECT COUNT(1) FROM locations WHERE id = @id",
            new MySqlParameter[] { new("@id", restaurant.location_id) });

        if (Convert.ToInt32(exists) == 0)
            return Results.BadRequest(new { message = "location_id does not exist" });

        try
        {
            string insertSql = """
            INSERT INTO restaurants (location_id, name, is_veggie_friendly, is_fine_dining, is_wine_focused)
            VALUES (@location_id, @name, @veggie_friendly, @fine_dining, @wine_focused);
            """;

            var parameters = new MySqlParameter[]
            {
            new("@location_id", restaurant.location_id),
            new("@name", restaurant.name),
            new("@veggie_friendly", restaurant.is_veggie_friendly),
            new("@fine_dining", restaurant.is_fine_dining),
            new("@wine_focused", restaurant.is_wine_focused),
            };

            await MySqlHelper.ExecuteNonQueryAsync(config.db, insertSql, parameters);

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
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception)
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    public static async Task<IResult>
    Put(int id, Post_Args restaurant, Config config)
    {
        if (string.IsNullOrWhiteSpace(restaurant.name))
            return Results.BadRequest(new { message = "Name is required" });

        if (restaurant.location_id <= 0)
            return Results.BadRequest(new { message = "Invalid location_id" });

        // Kontrollera att location finns
        var locExists = await MySqlHelper.ExecuteScalarAsync(config.db,
            "SELECT COUNT(1) FROM locations WHERE id = @id",
            new MySqlParameter[] { new("@id", restaurant.location_id) });

        if (Convert.ToInt32(locExists) == 0)
            return Results.BadRequest(new { message = "location_id does not exist" });

        // Kontrollera att restaurangen som ska uppdateras finns
        var restExists = await MySqlHelper.ExecuteScalarAsync(config.db,
            "SELECT COUNT(1) FROM restaurants WHERE id = @id",
            new MySqlParameter[] { new("@id", id) });

        if (Convert.ToInt32(restExists) == 0)
            return Results.NotFound(new { message = "Restaurant not found" });

        try
        {
            string updateSql = """
            UPDATE restaurants
            SET
              location_id = @location_id,
              name = @name,
              is_veggie_friendly = @veggie_friendly,
              is_fine_dining = @fine_dining,
              is_wine_focused = @wine_focused
            WHERE id = @id;
            """;

            var parameters = new MySqlParameter[]
  {
            new("@location_id", restaurant.location_id),
            new("@name", restaurant.name),
            new("@veggie_friendly", restaurant.is_veggie_friendly),
            new("@fine_dining", restaurant.is_fine_dining),
            new("@wine_focused", restaurant.is_wine_focused),
            new("@id", id)
  };


            int affected = await MySqlHelper.ExecuteNonQueryAsync(config.db, updateSql, parameters);
            if (affected == 0)
            {
                var existsObj = await MySqlHelper.ExecuteScalarAsync(config.db,
                    "SELECT COUNT(1) FROM restaurants WHERE id = @id",
                    new MySqlParameter[] { new MySqlParameter("@id", id) });

                if (existsObj == null || existsObj == DBNull.Value)
                {

                    return Results.StatusCode(StatusCodes.Status500InternalServerError);
                }

                long count = Convert.ToInt64(existsObj);
                if (count == 0)
                {
                    return Results.NotFound(new { message = "Restaurant not found" });
                }
                else
                {
                    return Results.NoContent();
                }
            }


            var body = new { id = id, message = "Updated successfully" };
            return Results.Ok(body);
        }
        catch (Exception)
        {
            // 
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
    public static async Task<IResult>
Delete(int Id, Config config)
    {
        string query = "DELETE FROM restaurants WHERE Id = @Id";
        var parameters = new MySqlParameter[] { new("@Id", Id) };

        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
        var body = new { id = Id, message = "Delete successfully" };
        return Results.Ok(body);
    }



}
