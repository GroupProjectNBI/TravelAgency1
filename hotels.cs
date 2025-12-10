namespace TravelAgency;

public class Hotels
{


  public static async Task
   Delete_Hotel(int Id, Config config)
  {
    string query = "DELETE FROM hotels WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };
    await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
  }
}