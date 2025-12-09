namespace TravelAgency;

using System.Net.Mail;
using MySql.Data.MySqlClient;

class Users
{
  public record GetAll_Data(int Id, string email, string first_name, string last_name, DateOnly date_of_birth, string password);

  //Enum for status 
  public enum RegistrationStatus { Success, EmailConflict, InvalidFormat }

  public static async Task<List<GetAll_Data>>
  GetAll(Config config)
  {
    List<GetAll_Data> result = new();
    string query = "SELECT Id, email, first_name, last_name, date_of_birth, password FROM users";
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.ConnectionString, query))
    {
      while (reader.Read())
      {
        DateOnly dob = DateOnly.FromDateTime(reader.GetDateTime(4));
        result.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), dob, reader.GetString(5)));
      }
    }
    return result;
  } // New |
  //        v
  public record Get_Data(string Email, string Password, string first_name, string last_name);
  public static async Task<Get_Data?>
  Get(int Id, Config config)
  {
    Get_Data? result = null;
    string query = "SELECT email, password, first_name, last_name FROM users WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.ConnectionString, query, parameters))
    {
      if (reader.Read())
      {

        result = new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)); //Vad vill att den hämtar för något, Id? Email, Lösenord? 
      }
    }
    return result;
  }

  //Validation logic
  public static bool IsValidEmailFormat(string email)
  {
    if (string.IsNullOrWhiteSpace(email)) return false;
    try
    {
      var addr = new MailAddress(email);
      return addr.Address == email;
    }
    catch (FormatException)
    {
      return false;
    }
  }
  public static async Task<bool>
  IsEmailUnique(string email, Config config)
  {
    string query = "SELECT COUNT(*) FROM users WHERE email = @email";
    var parameters = new MySqlParameter[] { new("@email", email) };

    var count = await MySqlHelper.ExecuteScalarAsync(config.ConnectionString, query, parameters);

    return Convert.ToInt64(count) == 0;
  }



  public record Post_Args(string Email, string first_name, string last_name, string date_of_birth, string Password);
  public static async Task
  Post(Post_Args user, Config config)
  {
    string query = """ INSERT INTO users(email, first_name, last_name, date_of_birth, password) VALUES (@email, @first_name, @last_name, @date_of_birth, @password)""";
    var parameters = new MySqlParameter[]
    {
      new("@email", user.Email),
      new("@first_name", user.first_name),
      new("@last_name", user.last_name),
      new("@date_of_birth", user.date_of_birth),
      new("@password", user.Password),
    };
    await MySqlHelper.ExecuteNonQueryAsync(config.ConnectionString, query, parameters);
  }
  public static async Task
  Delete(int Id, Config config)
  {
    string query = "DELETE FROM users WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };

    await MySqlHelper.ExecuteNonQueryAsync(config.ConnectionString, query, parameters);
  }
}