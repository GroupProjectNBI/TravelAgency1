namespace TravelAgency;

using MySql.Data.MySqlClient;

public class Bookings
{
  public record Booking_Data(
int id,
int user_id,
int location_id,
int hotel_id,
int package_id,
DateTime check_in,
DateTime check_out,
int guests,
int rooms,
string status, //enum i DB string i c#
DateTime created_at,
decimal total_price
  );
  private static Booking_Data Read_Booking(MySqlDataReader reader)
  {
    return new Booking_Data(
        id: reader.GetInt32(0),
        user_id: reader.GetInt32(1),
        location_id: reader.GetInt32(2),
        hotel_id: reader.GetInt32(3),
        package_id: reader.GetInt32(4),
        check_in: reader.GetDateTime(5),
        check_out: reader.GetDateTime(6),
        guests: reader.GetInt32(7),
        rooms: reader.GetInt32(8),
        status: reader.GetString(9), // Read ENUM as string
        created_at: reader.GetDateTime(10),
        total_price: reader.GetDecimal(11)
    );
  }
  public static async Task<List<Booking_Data>> Get_All(Config config)
  {
    List<Booking_Data> bookings = new();
    string query = @"
        SELECT id, user_id, location_id, hotel_id, package_id, 
               check_in, check_out, guests, rooms, status, created_at, total_price 
        FROM bookings";
    using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query);

    while (reader.Read())
    {
      bookings.Add(Read_Booking(reader));
    }
    return bookings;
  }

  public record Post_Args(int user_id, int location_id, int hotel_id, int package_id, DateOnly check_in, DateOnly check_out, int guests, int rooms, string status, decimal total_price);
  public static async Task<IResult>
  Post(Post_Args bookings, Config config)
  {
    // more validation that i am able to handle at the moment. 

    try
    {
      string insertSql = """
            INSERT INTO bookings (user_id, location_id, hotel_id, package_id, check_in, check_out,guests, rooms, status,total_price )
            VALUES (@user_id, @location_id, @hotel_id, @package_id, @check_in, @check_out, @guests, @rooms, @status, @total_price);
            """;

      var parameters = new MySqlParameter[]
      {
            new("@user_id", bookings.user_id),
            new("@location_id", bookings.location_id),
            new("@hotel_id", bookings.hotel_id),
            new("@package_id", bookings.package_id),
            new("@check_in", bookings.check_in),
            new("@check_out", bookings.check_out),
            new("@guests", bookings.guests),
            new("@rooms", bookings.rooms),
            new("@status", bookings.status),
            new("@total_price", bookings.total_price)
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
    catch (MySql.Data.MySqlClient.MySqlException mex)
    {
      Console.WriteLine($"error: {mex}");
      return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
  }
  public record Change_Args(
      int user_id,
      int location_id,
      int hotel_id,
      int package_id,
      DateOnly check_in,
      DateOnly check_out,
      int guests,
      int rooms,
      string status,
      DateOnly updated_at,
      decimal total_price
  );
  public static async Task<IResult>
  Put(int id, Change_Args bookings, Config config)
  {
    // --- 1) validate that FK exist and is correct
    var fkSql = """
        SELECT
          (SELECT COUNT(*) FROM users WHERE id = @user_id) AS users_count,
          (SELECT COUNT(*) FROM locations WHERE id = @location_id) AS locations_count,
          (SELECT COUNT(*) FROM hotels WHERE id = @hotel_id) AS hotels_count,
          (SELECT COUNT(*) FROM packages WHERE id = @package_id) AS packages_count,
          (SELECT COUNT(*) FROM hotels h WHERE h.id = @hotel_id AND h.location_id = @location_id) AS hotel_matches_location,
      (SELECT COUNT(*) FROM packages p WHERE p.id = @package_id AND p.location_id = @location_id) AS package_matches_location;
    """;

    var fkParams = new MySqlParameter[]
    {
        new MySqlParameter("@user_id", bookings.user_id),
        new MySqlParameter("@location_id", bookings.location_id),
        new MySqlParameter("@hotel_id", bookings.hotel_id),
        new MySqlParameter("@package_id", bookings.package_id)
    };

    using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, fkSql, fkParams);
    if (!await reader.ReadAsync())
      return Results.StatusCode(StatusCodes.Status500InternalServerError);

    if (reader.GetInt32("users_count") == 0)
      return Results.BadRequest(new { message = "user_id does not exist" });
    if (reader.GetInt32("locations_count") == 0)
      return Results.BadRequest(new { message = "location_id does not exist" });
    if (reader.GetInt32("hotels_count") == 0)
      return Results.BadRequest(new { message = "hotel_id does not exist" });
    if (reader.GetInt32("packages_count") == 0)
      return Results.BadRequest(new { message = "package_id does not exist" });
    if (reader.GetInt32("hotel_matches_location") == 0)
      return Results.BadRequest(new { message = "hotel_id does not belong to the provided location_id" });
    if (reader.GetInt32("package_matches_location") == 0)
      return Results.BadRequest(new { message = "package_id does not belong to the provided location_id" });

    // --- 2) Datum- och affärsregler
    DateTime checkInDt = bookings.check_in.ToDateTime(TimeOnly.MinValue);
    DateTime checkOutDt = bookings.check_out.ToDateTime(TimeOnly.MinValue);




    try
    {
      string updateSql = """
            UPDATE bookings
            SET
              user_id = @user_id,
              location_id = @location_id,
              hotel_id = @hotel_id,
              package_id = @package_id,
              check_in = @check_in,
              check_out = @check_out,
              guests = @guests,
              rooms = @rooms,
              status = @status,
              total_price = @total_price
            WHERE id = @id;
            """;

      var updateParams = new MySqlParameter[]
      {
            new MySqlParameter("@id", id),
            new MySqlParameter("@user_id", bookings.user_id),
            new MySqlParameter("@location_id", bookings.location_id),
            new MySqlParameter("@hotel_id", bookings.hotel_id),
            new MySqlParameter("@package_id", bookings.package_id),
            new MySqlParameter("@check_in", checkInDt),
            new MySqlParameter("@check_out", checkOutDt),
            new MySqlParameter("@guests", bookings.guests),
            new MySqlParameter("@rooms", bookings.rooms),
            new MySqlParameter("@status", bookings.status),
            new MySqlParameter("@total_price", bookings.total_price)
      };

      int affected = await MySqlHelper.ExecuteNonQueryAsync(config.db, updateSql, updateParams);
      if (affected == 0)
      {
        var existsObj = await MySqlHelper.ExecuteScalarAsync(config.db,
            "SELECT COUNT(1) FROM bookings WHERE id = @id",
            new MySqlParameter[] { new MySqlParameter("@id", id) });

        if (existsObj == null || existsObj == DBNull.Value)
          return Results.StatusCode(StatusCodes.Status500InternalServerError);

        long count = Convert.ToInt64(existsObj);
        if (count == 0)
          return Results.NotFound(new { message = "No bookings found" });
        return Results.NoContent();
      }

      var body = new { id = id, message = "Updated successfully" };
      return Results.Ok(body);
    }
    catch (Exception er)
    {
      Console.WriteLine(er);
      return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
  }
}
