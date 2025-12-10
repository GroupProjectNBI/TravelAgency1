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
        string query = "SELECT name, address, price_class,rooms, breakfast FROM Hotels ;";
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
}
