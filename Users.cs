namespace TravelAgency;


using MySql.Data.MySqlClient;

class Users
{
  public record GetAll_Data(int Id, string Email, string Password);
  public static async Task<List<GetAll_Data>>
  GetAll(Config config)
  {
    List<GetAll_Data> result = new();
    string query = "SELECT id, FirstName, LastName, Adress, DateOfBirth, Password";
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.ConnectionString, query))
    {
      while (reader.Read())
      {
        result.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
      }
    }
    return result;
  }
  public record Get_Data(string Email, string Password);
  public static async Task<Get_Data?>
  Get(int id, Config config)
  {
    Get_Data? result = null;
    string query = "SELECT Email, Password FROM users WEHRE id = @id";
    var parameters = new MySqlParameter[] { new("@id", id) };
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.ConnectionString, query, parameters))
    {
      if (reader.Read())
      {
        result = new(reader.GetString(0), reader.GetString(1));
      }
    }
    return result;
  }
  public record Post_Args(string Email, string Password);
  public static async Task
  Post(Post_Args user, Config config)
  {
    string query = """ INSERT INTO users(email, password) VALUES (@email, @password)""";
    var parameters = new MySqlParameter[]
    {
      new("@email", user.Email),
      new("@password", user.Password),
    };
    await MySqlHelper.ExecuteNonQueryAsync(config.ConnectionString, query, parameters);
  }
  public static async Task
  Delete(int id, Config config)
  {
    string query = "DELETE FROM users WHERE id = @id";
    var parameters = new MySqlParameter[] { new("@id", id) };

    await MySqlHelper.ExecuteNonQueryAsync(config.ConnectionString, query, parameters);
  }
}