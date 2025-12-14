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
DateOnly check_in,
DateOnly check_out,
int guests,
int rooms,
string status, //enum i DB string i c#
DateOnly created_at,
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
        check_in: DateOnly.FromDateTime(reader.GetDateTime(5)),
        check_out: DateOnly.FromDateTime(reader.GetDateTime(6)),
        guests: reader.GetInt32(7),
        rooms: reader.GetInt32(8),
        status: reader.GetString(9), // Read ENUM as string
        created_at: DateOnly.FromDateTime(reader.GetDateTime(10)),
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
  public record Booked_Meal_Data(
string restaurant_name,
string meal_type
  //int day_offset
  );
  public static async Task<List<Booked_Meal_Data>> Get_Meals(int booking_id, Config config)
  {
    List<Booked_Meal_Data> meals = new();
    string query = @"
    SELECT 
    r.name,
    pm.meal_type
    FROM bookings AS b
    JOIN packages AS p on b.package_id = p.id
    JOIN packages_meals AS pm ON p.id = pm.package_id
    JOIN restaurants AS r ON pm.restaurant_id = r.id
    WHERE b.id = @booking_id
    ORDER BY FIELD(pm.meal_type, 'Breakfast', 'Lunch', 'Dinner')";

    var parameters = new MySqlParameter[] { new("@booking_id", booking_id) };

    using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);

    while (reader.Read())
    {
      meals.Add(new Booked_Meal_Data(
      restaurant_name: reader.GetString(0),
      meal_type: reader.GetString(1)
      ));
    }
    if (meals.Count == 0)
    {
      return new List<Booked_Meal_Data>();
    }
    return meals;
  }
  public record Post_Args(
    int location_id,
    int hotel_id,
    int package_id,
    DateTime check_in,
    DateTime check_out,
    int guests,
    int rooms,
    decimal total_price
);
  public static async Task<IResult> Post(Post_Args args, int userId, Config config)
  {
    var checks = new (string table, int id, string error)[]
    {
        //("users", args.user_id, "user_id"),
        ("locations", args.location_id, "location_id"),
        ("hotels", args.hotel_id, "hotel_id"),
        ("packages", args.package_id, "package_id")
    };
    foreach (var check in checks)
    {
      var exists = await MySqlHelper.ExecuteScalarAsync(
          config.db,
          $"SELECT COUNT(1) FROM {check.table} WHERE id = @id",
          new MySqlParameter[] { new("@id", check.id) }
      );
      if (Convert.ToInt32(exists) == 0)
      {
        return Results.BadRequest(new { message = $"{check.error} does not exist." });
      }
    }
    if (args.check_in >= args.check_out)
      return Results.BadRequest(new { message = "Check-in date must be before check-out date." });

    if (args.guests <= 0 || args.rooms <= 0 || args.total_price <= 0)
      return Results.BadRequest(new { message = "Guests, rooms, and total price must be positive." });
    try
    {
      string query = """
            INSERT INTO bookings (
                user_id, location_id, hotel_id, package_id, 
                check_in, check_out, guests, rooms, status, total_price
            )
            VALUES (
                @user_id, @location_id, @hotel_id, @package_id, 
                @check_in, @check_out, @guests, @rooms, 'confirmed', @total_price
            );
        """;

      var parameters = new MySqlParameter[]
      {
            new("@user_id", userId),
            new("@location_id", args.location_id),
            new("@hotel_id", args.hotel_id),
            new("@package_id", args.package_id),
            new("@check_in", args.check_in),
            new("@check_out", args.check_out),
            new("@guests", args.guests),
            new("@rooms", args.rooms),
            new("@total_price", args.total_price)
      };

      await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);

      var newIdObj = await MySqlHelper.ExecuteScalarAsync(config.db, "SELECT LAST_INSERT_ID();");
      int newId = Convert.ToInt32(newIdObj);

      return Results.Created($"/bookings/{newId}", new { id = newId, message = "Booking created successfully." });
    }
    catch (Exception)
    {
      return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
  }
}
