namespace TravelAgency;

using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

class Users
{
  public record GetAll_Data(int Id, string email, string first_name, string last_name, DateOnly date_of_birth, string password);

  public enum RegistrationStatus { Success, EmailConflict, InvalidFormat, WeakPassword }

  public static async Task<List<GetAll_Data>>
  GetAll(Config config)
  {
    List<GetAll_Data> result = new();
    string query = "SELECT Id, email, first_name, last_name, date_of_birth, password FROM users";
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.db, query))
    {
      while (reader.Read())
      {
        DateOnly dob = DateOnly.FromDateTime(reader.GetDateTime(4));
        result.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), dob, reader.GetString(5)));
      }
    }
    return result;
  }

  public record Get_Data(string Email, string Password, string first_name, string last_name);
  public static async Task<Get_Data?>
  Get(int Id, Config config)
  {
    Get_Data? result = null;
    string query = "SELECT email, password, first_name, last_name FROM users WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };
    using (var reader = await
    MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
    {
      if (reader.Read())
      {

        result = new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
      }
    }
    return result;
  }

  public record No_GET_Data(string token);
  public static async Task<No_GET_Data?>
  Reset(string email, Config config)
  {
    No_GET_Data? result = null;
    string checkUserquery = """ SELECT COUNT(*) FROM users WHERE email = @email """;
    string queryPost = """  CALL create_password_request(@email)""";
    string query = "select BIN_TO_UUID(temp_key) from password_request where user = (select id from users  WHERE email = @email); ";
    var parameters = new MySqlParameter[] { new("@email", email) };
    var count = await MySqlHelper.ExecuteScalarAsync(config.db, checkUserquery, parameters);
    if (Convert.ToInt64(count) > 0)
    {
      await MySqlHelper.ExecuteNonQueryAsync(config.db, queryPost, parameters);
      using (var reader = await
      MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
      {
        if (reader.Read())
        {
          result = new(reader.GetString(0));
        }
      }

    }
    return result;

  }

  //Validation logic
  public static bool IsValidPassword(string password)
  {
    const int minLength = 15;
    const int maxLength = 64;

    if (string.IsNullOrWhiteSpace(password))
    {
      return false;
    }

    if (password.Length < minLength || password.Length > maxLength)
    {
      return false;
    }

    return true;
  }

  public static bool IsValidEmailFormat(string email)
  {
    if (string.IsNullOrWhiteSpace(email)) return false;

    //Regex for standard email-validate
    const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    //RegexOptions.IgnoreCase ignores if upper/lower_case
    try
    {
      return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
    }
    catch (RegexMatchTimeoutException)
    {
      return false;
    }
  }
  public static async Task<bool>
  IsEmailUnique(string email, Config config)
  {
    string query = "SELECT COUNT(*) FROM users WHERE email = @email";
    var parameters = new MySqlParameter[] { new("@email", email) };

    var count = await MySqlHelper.ExecuteScalarAsync(config.db, query, parameters);

    return Convert.ToInt64(count) == 0;
  }

  public record Post_Args(string Email, string first_name, string last_name, string date_of_birth, string Password);

  public static async Task<(RegistrationStatus Status, int? UserId)>
  Post(Post_Args user, Config config)
  {
    if (!IsValidPassword(user.Password))
    {
      return (RegistrationStatus.WeakPassword, null);
    }
    if (!IsValidEmailFormat(user.Email))
    {
      return (RegistrationStatus.InvalidFormat, null);
    }
    if (string.IsNullOrWhiteSpace(user.first_name) || string.IsNullOrWhiteSpace(user.last_name) || string.IsNullOrWhiteSpace(user.date_of_birth))
    {
      return (RegistrationStatus.InvalidFormat, null);
    }
    if (!await IsEmailUnique(user.Email, config))
    {
      return (RegistrationStatus.EmailConflict, null);
    }
    string query = """ 
            INSERT INTO users(email, first_name, last_name, date_of_birth, password) 
            VALUES (@email, @first_name, @last_name, @date_of_birth, @password);
            SELECT LAST_INSERT_ID(); 
            """;

    var parameters = new MySqlParameter[]
    {
            new("@email", user.Email),
            new("@first_name", user.first_name),
            new("@last_name", user.last_name),
            new("@date_of_birth", user.date_of_birth),
            new("@password", user.Password),
    };

    var newId = await MySqlHelper.ExecuteScalarAsync(config.db, query, parameters);
    int userId = Convert.ToInt32(newId);

    return (RegistrationStatus.Success, userId);
  }

  public record Patch_Args(string Email, string new_password, string confirm_password);
  public enum PatchStatus
  {
    Success,
    NotFound,
    WeakPassword,
    PasswordsDoNotMatch,
    InvalidFormat
  }


  public static async Task<PatchStatus>
  Patch(string temp_key, Patch_Args user, Config config)
  {

    if (user.new_password != user.confirm_password)
    {
      return PatchStatus.PasswordsDoNotMatch;
    }


    if (user.new_password.Length < 15)
    {
      return PatchStatus.WeakPassword;
    }

    string rawPassword = user.new_password;

    try
    {

      string query = """
    START TRANSACTION;
    
    UPDATE users 
    SET password = @password 
    WHERE id = (SELECT user from password_request where temp_key = UUID_TO_BIN(@temp_key)); 
    
   
    SELECT ROW_COUNT();
    
    DELETE FROM password_request WHERE temp_key = UUID_TO_BIN(@temp_key);

    COMMIT;
""";
      var parameters = new MySqlParameter[]
      {
            new("@temp_key", temp_key),
            new("@password", rawPassword)
      };

      var result = await MySqlHelper.ExecuteScalarAsync(config.db, query, parameters);
      int rowsUpdated = Convert.ToInt32(result);

      if (rowsUpdated == 0)
      {
        return PatchStatus.NotFound;
      }

      return PatchStatus.Success;
    }
    catch (Exception)
    {
      return PatchStatus.InvalidFormat;
    }
  }


  public static async Task
  Delete(int Id, Config config)
  {
    string query = "DELETE FROM users WHERE Id = @Id";
    var parameters = new MySqlParameter[] { new("@Id", Id) };

    await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
  }

}