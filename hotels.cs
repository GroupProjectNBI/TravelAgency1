namespace TravelAgency;

using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

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
    public static async Task
   DeleteHotel(int Id, Config config)
    {
        string query = "DELETE FROM hotels WHERE Id = @Id";
        var parameters = new MySqlParameter[] { new("@Id", Id) };
        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
    }
  
    record Post_Args
        
    (
    int Id,
    int LocationId,
    string Name,
    bool HasBreakfast,
    string Address
    );
    static async Task<int> Post(Post_Args hotels, Config config)

    {
        string query = @"
        INSERT INTO hotels (location_id, name, has_breakfast, address)
        VALUES (@LocationId, @Name, @HasBreakfast, @Address);";

        var parameters = new MySqlParameter[]
        {
        new("@LocationId", hotels.LocationId),
        new("@Name", hotels.Name),
        new("@HasBreakfast", hotels.HasBreakfast),
        new("@Address", hotels.Address)
        };

        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);

        // Hämta ID för den nya raden
        string idQuery = "SELECT LAST_INSERT_ID()";
        var idObj = await MySqlHelper.ExecuteScalarAsync(config.db, idQuery);

        return Convert.ToInt32(idObj);
    }
}





