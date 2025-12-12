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
  public record Booked_Meal_Data(
string restaurant_name,
string meal_type,
int day_offset
  );
  public static async Task<List<Booked_Meal_Data>> Get_Meals(int booking_id, Config config)
  {
    List<Booked_Meal_Data> meals = new();
    string query = @"
    SELECT 
    rest.name,
    pm.meal_type,
    pm.day_offset
    FROM bookings AS b
    JOIN packages AS p on b.package_id = p.id
    JOIN packages_meals AS pm ON p.id = pm.package_id
    JOIN restaurants AS r ON pm.restaurant_id = r.id
    WHERE b.id = @booking_id
    ORDER BY pm.day_offset, FIELD(pm.meal_type, 'Breakfast', 'Lunch', 'Dinner')";

    var parameters = new MySqlParameter[] { new("@booking_id", booking_id) };

    using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);

    while (reader.Read())
    {
      meals.Add(new Booked_Meal_Data(
restaurant_name: reader.GetString(0),
meal_type: reader.GetString(1),
day_offset: reader.GetInt32(2)
      ));
    }
    if (meals.Count == 0)
    {
      return new List<Booked_Meal_Data>();
    }
    return meals;
  }
}
