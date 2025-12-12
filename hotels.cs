namespace TravelAgency;

using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

class Hotels
{
  public record GetAll_Data(string name, string address, int price_class, bool breakfast);
  public static async Task<List<GetAll_Data>>

  GetAll(Config config)
  {
    List<GetAll_Data> result = new();
    string query = "SELECT name, address, price_class, has_breakfast FROM hotels ;";
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.db, query))
    {
      while (reader.Read())
      {
        result.Add(new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetInt32(2),
        reader.GetBoolean(3)
        ));
      }
    }
    return result;
  }
  public record Get_Data(string name, string address, int price_class, bool breakfast);
  public static async Task<Get_Data?>
  Get(int Id, Config config)
  {
    Get_Data? result = null;
    string query = "SELECT name, address, price_class, has_breakfast FROM hotels WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
    {
      if (reader.Read())
      {
        result = new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetInt32(2),
        reader.GetBoolean(3)
        );
      }
    }
    return result;
  }

  public record Post_Args

    (
    int Id,
    int LocationId,
    string Name,
    bool HasBreakfast,
    string Address,
    int PriceClass
    );
  public static async Task<int> Post(Post_Args hotels, Config config)

  {
    string query = @"
        INSERT INTO hotels (location_id, name, address, price_class, has_breakfast)
        VALUES (@location_id, @name, @address, @price_class, @has_breakfast);";

    var parameters = new MySqlParameter[]
    {
        new("@location_id", hotels.LocationId),
        new("@name", hotels.Name),
        new("@address", hotels.Address),
        new("@price_class", hotels.PriceClass),
        new("@has_breakfast", hotels.HasBreakfast)
    };

    await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);

    // Hämta ID för den nya raden
    string idQuery = "SELECT last_insert_id()";
    var idObj = await MySqlHelper.ExecuteScalarAsync(config.db, idQuery);

    return Convert.ToInt32(idObj);
  }

  public record UpdateHotel_hotel( // expected fiels for update

   int location_id,
   string name,
   bool has_breakfast,
   string address,
   int price_class
  );
  public static async Task UpdateHotel(int Id, UpdateHotel_hotel hotel, Config config)

  {
    string updateSql = """
    UPDATE hotels
    SET 
    location_id = @location_id,
    name = @name,
    has_breakfast = @has_breakfast,
    address = @address,
    price_class = @price_class
    WHERE Id=@Id;
    """;

    var parameters = new MySqlParameter[]

  {
    new("@Id", Id),
    new("@location_id", hotel.location_id),
    new("@name", hotel.name),
    new("@has_breakfast", hotel.has_breakfast),
    new("@address", hotel.address),
    new("@price_class", hotel.price_class),

  };
    await MySqlHelper.ExecuteNonQueryAsync(config.db, updateSql, parameters); // Used to update database
  }

  public static async Task<IResult> Put(int id, UpdateHotel_hotel hotel, Config config)
  {
    try
    {
      await UpdateHotel(id, hotel, config);
      return Results.Ok(new { message = "Hotel updated successfully", id = id });
    }
    catch (Exception)
    {
      return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
  }

  public static async Task
   DeleteHotel(int Id, Config config)
  {
    string query = "DELETE FROM hotels WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };

    await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
  }
}

