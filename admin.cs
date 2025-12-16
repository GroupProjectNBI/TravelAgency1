namespace TravelAgency;

using System.Text;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public class Admin
{
    // Exempel på enkel DTO
    public record UserDto(string Email, string FirstName);
    // Rätt metoddeklaration och HttpContext-typ
    public static async Task<IResult>
    GetAllUsers(Config config, HttpContext ctx)
    {
        if (ctx.Session.GetInt32("user_id") is int user_id)
        {
            List<UserDto> users = new();
            var is_admin = await MySqlHelper.ExecuteScalarAsync(config.db,
           "SELECT count(*) FROM users WHERE id= @id AND role_id = 1 ",
           new MySqlParameter[] { new("@id", user_id) });

            if (Convert.ToInt32(is_admin) < 1)
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            await using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, "SELECT email, first_name FROM users");


            while (await reader.ReadAsync())
            {
                // Hantera NULL-värden säkert
                var email = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var firstname = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                users.Add(new UserDto(email, firstname));
            }
            var body = new { userData = users, message = "Welcome admin here is all users" };
            return Results.Ok(body);
        }
        else
        {
            return Results.Unauthorized();
        }

    }
    public record HotelOccupancyDto(int HotelId, string HotelName, int TotalRoomsBooked);

    public static async Task<List<HotelOccupancyDto>> GetOccupancy(Config config)
    {
        List<HotelOccupancyDto> result = new();

        string query = @"
            SELECT 
                h.id AS hotel_id,
                h.name AS hotel_name,
                SUM(b.rooms) AS total_rooms_booked
            FROM hotels h
            LEFT JOIN bookings b ON h.id = b.hotel_id
            GROUP BY h.id, h.name;
        ";

        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query);

        while (reader.Read())
        {
            result.Add(new(
                reader.GetInt32("hotel_id"),
                reader.GetString("hotel_name"),
                reader.IsDBNull(reader.GetOrdinal("total_rooms_booked")) ? 0 : reader.GetInt32("total_rooms_booked")
            ));
        }

        return result;
    }


    public static async Task<IResult> AdminOccupancy_Handler(Config config)
    {
        try
        {
            var data = await GetOccupancy(config);
            return Results.Ok(data);
        }
        catch
        {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }


}
