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
  // and handles the HTTP response
  public record UpdateBookingArgs(
      int user_id,
      int location_id,
      int hotel_id,
      int package_id,
      DateOnly check_in,
      DateOnly check_out,
      int guests,
      int rooms,
      string status,
      decimal total_price
  );
  public static async Task Update(int id, UpdateBookingArgs args, Config config)
  {
    string query = """
        UPDATE bookings
        SET user_id = @user_id,
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

    var parameters = new MySqlParameter[]
    {
        new("@id", id),
        new("@user_id", args.user_id),
        new("@location_id", args.location_id),
        new("@hotel_id", args.hotel_id),
        new("@package_id", args.package_id),
        new("@check_in", args.check_in),
        new("@check_out", args.check_out),
        new("@guests", args.guests),
        new("@rooms", args.rooms),
        new("@status", args.status),
        new("@total_price", args.total_price)
    };

    await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
  }



}

