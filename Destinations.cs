namespace TravelAgency;

using System.Net.Mail;
using MySql.Data.MySqlClient;

class Destinations
{
  public record GetAll_Data(int Id, int country, string city);

  public static async Task<List<GetAll_Data>> Search(string UserInput, Config config)

  {

    if (string.IsNullOrWhiteSpace(UserInput))
      throw new ArgumentException("You must enter a city name."); // Prevents user from invalid (empty) input


    List<GetAll_Data> result = new();
    string searchValue = $"%{UserInput}%"; // Flexible search, exakt match not needed


    string query = """
    SELECT Id, country_id, city 
    FROM locations
    WHERE city LIKE @UserInput
    """;

    // Handles input as data not code, prevents harmful code.
    var parameters = new MySqlParameter[]
   {
    new MySqlParameter (@"UserInput", searchValue)

   };

    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
    {
      while (reader.Read())
      {

        result.Add(new(reader.GetInt32(0), // Id                                      
        reader.GetInt32(1), // Country
        reader.GetString(2)));// City

      }
    }


    if (result.Count == 0) // Forces user to type in a city that exists in the database
      throw new ArgumentException("No city found matching your input. Please enter a valid city.");

    return result;
  }


  public record Get_Data(int Id, string Country, string City);
  public static async Task<Get_Data?>
  Get(int Id, Config config)
  {
    Get_Data? result = null;
    string query = "SELECT Id, country, city FROM locations WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
    {
      if (reader.Read())
      {

        result = new(reader.GetInt32(0), // ID
        reader.GetString(1), // Country
        reader.GetString(2)); // City


      }
      return result;
    }
  }


  public record Post_Args(string Country, string City);
  public static async Task
  Post(Post_Args user, Config config)
  {
    string query = "INSERT INTO locations(country, city) VALUES (@country, @city)";
    var parameters = new MySqlParameter[]
    {
      new("@country", user.Country),
      new("@city", user.City),
    };
    await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
  }
  public static async Task
  Delete(int Id, Config config)
  {
    string query = "DELETE FROM locations WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };

    await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
  }
}
