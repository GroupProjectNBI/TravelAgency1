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

  // and handles the HTTP response
  public record UpdateBookingArgs(
      int user_id,
      int location_id,
      int hotel_id,
      int package_id,
      DateTime check_in,
      DateTime check_out,
      int guests,
      int rooms,
      string status,  // match ENUM in DB
      decimal total_price
  );
  
// Put handler method that the  endpoint will call:
  public static async Task<IResult> Put(int id, UpdateBookingArgs args, Config config)
  {
    try
    {
      await Update(id, args, config); // calls the actual SQL update method
      return Results.Ok(new { message = "Booking updated successfully", id = id });
    }
    catch (Exception)
    {
      return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
  }


  public static async Task Delete(int id, Config config)
{
    string query = "DELETE FROM bookings WHERE id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", id) };

    await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
}

}

