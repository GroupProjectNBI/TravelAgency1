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

}
