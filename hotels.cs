namespace TravelAgency;

using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

public record UpdateHotel_hotel(

 int location_id,
 string name,
 bool has_breakfast,
 string address,
 int price_class



);

class Hotels
{
  public record GetAll_Data(string name, string address, int price_class, int rooms, bool breakfast);
  public static async Task<List<GetAll_Data>>

  GetAll(Config config)
  {
    List<GetAll_Data> result = new();
    string query = "SELECT name, address, price_class,rooms, has_breakfast FROM hotels ;";
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.db, query))
    {
      while (reader.Read())
      {
        result.Add(new(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetInt32(2),
        reader.GetInt32(3),
        reader.GetBoolean(4)
        ));
      }
    }
    return result;
  }
  public record Get_Data(string name, string address, int price_class, int rooms, bool breakfast);
  public static async Task<Get_Data?>
  Get(int Id, Config config)
  {
    Get_Data? result = null;
    string query = "SELECT name, address, price_class,rooms, has_breakfast FROM hotels WHERE Id = @Id";
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
        reader.GetInt32(3),
        reader.GetBoolean(4)
        );
      }
    }
    return result;
  }



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
    await MySqlHelper.ExecuteNonQueryAsync(config.db, updateSql, parameters);
  }


  public static async Task
   DeleteHotel(int Id, Config config)
  {
    string query = "DELETE FROM hotels WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };

    await MySqlHelper.ExecuteNonQueryAsync(config.db, updateSql, parameters);

  }
}

