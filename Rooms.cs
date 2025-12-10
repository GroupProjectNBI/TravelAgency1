namespace TravelAgency;

using MySql.Data.MySqlClient;

class Rooms
{
    public record GetAll_Data(int Id, int HotelId, string Name, int Capacity, decimal PricePerNight);
    public record Get_Data(int Id, int HotelId, string Name, int Capacity, decimal PricePerNight);
    public record Post_Args(int HotelId, string Name, int Capacity, decimal PricePerNight);

    //get all rooms
    public static async Task<List<GetAll_Data>> GetAll(Config config)
    {
        List<GetAll_Data> result = new();
        string query = """
        SELECT id, hotel_id, name, capacity, price_per_night
        FROM rooms
        """;

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (reader.Read())
            {
                result.Add(
                    new GetAll_Data(
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetString(2),
                        reader.GetInt32(3),
                        reader.GetDecimal(4)

                    )

                );
            }
        }
        return result;
    }

    //get a specific room on their id
    public static async Task<Get_Data?> Get(int id, Config config)
    {
        Get_Data? result = null;

        string query = """
        SELECT id, hotel_id, name, capacity, price_per_night
        FROM rooms
        WHERE id = @id
        """;

        var parameters = new MySqlParameter[]
        {
            new("@id", id)
        };

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
        {
            if (reader.Read())
            {
                result = new Get_Data(reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetDecimal(4)
                );
            }
        }

        return result;
    }

    //get all room for a specific hotel
    public static async Task<List<GetAll_Data>> GetByHotel(int hotelId, Config config)
    {
        List<GetAll_Data> result = new();

        string query = """
        SELECT id, hotel_id, name, capacity, price_per_night
        FROM rooms
        WHERE hotel_id = @hotel_id
        """;

        var parameters = new MySqlParameter[]
        {
            new("@hotel_id", hotelId)
        };

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
        {
            while (reader.Read())
            {
                result.Add(
                    new GetAll_Data(
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetString(2),
                        reader.GetInt32(3),
                        reader.GetDecimal(4)
                    )
                );
            }
        }

        return result;
    }

    public enum RoomCreationStatus
    {
        Success,
        InvalidFormat,
        HotelNotFound
    }

    public static async Task<(RoomCreationStatus, int? RoomId)>
    Post(Post_Args room, Config config)
    {
        //validation
        if (room.HotelId <= 0 ||
        string.IsNullOrWhiteSpace(room.Name) ||
        room.Capacity <= 0 ||
        room.PricePerNight <= 0)
        {
            return (RoomCreationStatus.InvalidFormat, null);
        }

        string[] allowedNames = { "Single", "Double", "Suit" };
        if (!allowedNames.Contains(room.Name))
        {
            return RoomCreationStatus.InvalidFormat;
        }

        string checkHotelQuery = """
            SELECT COUNT(*)
            FROM hotels
            WHERE id = @hotel_id
        """;

        var hotelParameter = new MySqlParameter[]
        {
            new("@hotel_id", room.HotelId)
        };

        var hotelCountExists = await MySqlHelper.ExecuteScalarAsync(config.db, checkHotelQuery, hotelParameter);
        int hotelCount = Convert.ToInt32(hotelCountExists);

        if (hotelCount == 0)
        {
            return (RoomCreationStatus.HotelNotFound, null);
        }


        string query = """
        INSERT INTO rooms(hotel_id, name, capacity, price_per_night)
        VALUES (@hotel_id, @name, @capacity, @price_per_night);
        SELECT LAST_INSERT_ID();
        """;

        var parameters = new MySqlParameter[]
        {
            new("@hotel_id", room.HotelId),
            new("@name", room.Name),
            new("@capacity", room.Capacity),
            new("@price_per_night", room.PricePerNight)
        };

        var newId = await MySqlHelper.ExecuteScalarAsync(config.db, query, parameters);
        return Convert.ToInt32(newId);


    }

    public static async Task Delete(int id, Config config)
    {
        string query = "DELETE FROM rooms WHERE id = @id";
        var parameters = new MySqlParameter[]
        {
            new("@id", id)
        };

        await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
    }


}