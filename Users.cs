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
  public static async Task<IResult>
  Reset(string email, Config config)
  {
    string query = "CALL create_password_request(@email)";

    var parameters = new MySqlParameter[] {
        new("@email", email)
    };

    try
    {
      // 2. ExecuteScalarAsync gets the first value that is returned 
      object? result = await MySqlHelper.ExecuteScalarAsync(config.db, query, parameters);

      if (result != null && result != DBNull.Value)
      {
        string token = result.ToString() ?? String.Empty;
        var data = new No_GET_Data(token);

        // Return status code 200 OK
        return Results.Ok(new
        {
          message = "Reset link created successfully.",
          data = data
        });
      }
      else
      {

        return Results.Ok(new { message = "If the email exists, a reset link has been generated." });

      }
    }
    catch (Exception ex)
    {
      // Logga felet
      Console.WriteLine($"Database error: {ex.Message}");
      return Results.Problem("An internal error occurred.");
    }

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

  public record Patch_Args(string Email, string Password);

  public static async Task<IResult> Patch(string temp_key, Patch_Args user, Config config)
  {
    // query for checking the exp_date
    string queryCheck = "SELECT expire_date FROM password_request WHERE temp_key = UUID_TO_BIN(@temp_key);";

    // query for update password and delete the token in password_request
    string query = """
      START TRANSACTION;
      UPDATE users 
      SET password = @password 
      WHERE id = (SELECT user_id from password_request where temp_key = UUID_TO_BIN(@temp_key)); 

      DELETE FROM password_request WHERE temp_key = UUID_TO_BIN(@temp_key);

      COMMIT;
      """;

    var parameters = new MySqlParameter[]
    {
        new("@temp_key", temp_key),
        new("@password", user.Password) // Ensure you hash this password before sending it here!
    };

    try
    {
      // 1. Check if the token exists and get the expiration date
      object dateResult = await MySqlHelper.ExecuteScalarAsync(config.db, queryCheck, parameters);

      // 2. Check if we got a result (Token exists)
      if (dateResult != null && dateResult != DBNull.Value)
      {
        DateTime expireDate = Convert.ToDateTime(dateResult);

        // 3. Check if the token is still valid (Date is today or in the future)
        if (expireDate.Date >= DateTime.Now.Date)
        {
          // 4. Execute the update and delete transaction
          int rowsAffected = await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);

          if (rowsAffected > 0)
          {
            var body = new { message = "Password updated successfully." };
            return Results.Json(body, statusCode: StatusCodes.Status200OK);
          }
          else
          {
            // This happens if the token existed a millisecond ago but was deleted by another process
            var errorBody = new { error = "Could not update password. Please try again." };
            return Results.Json(errorBody, statusCode: StatusCodes.Status400BadRequest);
          }
        }
        else
        {
          // 5. Handle cases where the token exists but the date has passed
          var errorBody = new { error = "Token has expired." };
          return Results.Json(errorBody, statusCode: StatusCodes.Status400BadRequest);
        }
      }
      else
      {
        // 6. Token was not found in the database
        var errorBody = new { error = "Invalid token." };
        return Results.Json(errorBody, statusCode: StatusCodes.Status400BadRequest);
      }
    }
    catch (Exception ex)
    {
      // 7. catch potential database errors
      Console.WriteLine($"Error: {ex.Message}");
      return Results.Problem("An internal error occurred.");
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