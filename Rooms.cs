namespace TravelAgency;

using MySql.Data.MySqlClient;

class Rooms
{
    public record GetAll_Data(int Id, int HotelId, int RoomNumber, string Name, int Capacity, decimal PricePerNight);
    public record Get_Data(int Id, int HotelId, int RoomNumber, string Name, int Capacity, decimal PricePerNight);
    public record Post_Args(int HotelId, string Name, int Capacity, decimal PricePerNight);
    public record Put_Args(int HotelId, string Name, int Capacity, decimal PricePerNight);

    //get all rooms
    public static async Task<List<GetAll_Data>> GetAll(Config config)
    {
        List<GetAll_Data> result = new();
        string query = """
        SELECT id, hotel_id, room_number, name, capacity, price_per_night
        FROM rooms
        """;

        using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query))
        {
            while (reader.Read())
            {
                result.Add(
                    new GetAll_Data(
                        reader.GetInt32(0), //id
                        reader.GetInt32(1), //hotel_id
                        reader.GetInt32(2), //room_number
                        reader.GetString(3), //name
                        reader.GetInt32(4), //capacity
                        reader.GetDecimal(5) //price_per_night
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
        SELECT id, hotel_id, room_number, name, capacity, price_per_night
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
                result = new Get_Data(
                        reader.GetInt32(0), //id
                        reader.GetInt32(1), //hotel_id
                        reader.GetInt32(2), //room_number
                        reader.GetString(3), //name
                        reader.GetInt32(4), //capacity
                        reader.GetDecimal(5) //price_per_night
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
        SELECT id, hotel_id, room_number, name, capacity, price_per_night
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
                        reader.GetInt32(0), //id
                        reader.GetInt32(1), //hotel_id
                        reader.GetInt32(2), //room_number
                        reader.GetString(3), //name
                        reader.GetInt32(4), //capacity
                        reader.GetDecimal(5) //price_per_night
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

        string[] allowedNames = { "Single", "Double", "Suite" };
        if (!allowedNames.Contains(room.Name))
        {
            return (RoomCreationStatus.InvalidFormat, null);
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

        string nextNumberQuery = """
            SELECT COALESCE(MAX(room_number), 100) +1
            FROM rooms
            WHERE hotel_id = @hotel_id
        """;

        var nextRoom = await MySqlHelper.ExecuteScalarAsync(
            config.db,
            nextNumberQuery,
            new MySqlParameter[] { new("@hotel_id", room.HotelId) }
        );

        int nextRoomNumber = Convert.ToInt32(nextRoom);
        string query = """
        INSERT INTO rooms(hotel_id, room_number, name, capacity, price_per_night)
        VALUES (@hotel_id, @room_number, @name, @capacity, @price_per_night);
        SELECT LAST_INSERT_ID();
        """;

        var parameters = new MySqlParameter[]
        {
            new("@hotel_id", room.HotelId),
            new("@room_number", nextRoomNumber),
            new("@name", room.Name),
            new("@capacity", room.Capacity),
            new("@price_per_night", room.PricePerNight)
        };

        var newId = await MySqlHelper.ExecuteScalarAsync(config.db, query, parameters);
        int roomId = Convert.ToInt32(newId);

        return (RoomCreationStatus.Success, roomId);


    }

    public enum RoomUpdateStatus
    {
        Success,
        InvalidFormat,
        NotFound,
        HotelNotFound
    }
    public static async Task<RoomUpdateStatus>
    Put(int id, Put_Args room, Config config)
    {
        //validation
        if (id <= 0 ||
        room.HotelId <= 0 ||
        string.IsNullOrWhiteSpace(room.Name) ||
        room.Capacity <= 0 ||
        room.PricePerNight <= 0)
        {
            return RoomUpdateStatus.InvalidFormat;
        }

        string[] allowedNames = { "Single", "Double", "Suite" };
        if (!allowedNames.Contains(room.Name))
        {
            return RoomUpdateStatus.InvalidFormat;
        }
        //validate that the hotel exists
        string checkHotelQuery = """
        SELECT COUNT(*) 
        FROM hotels 
        WHERE id = @hotel_id
        """;

        var hotelParams = new MySqlParameter[]
        {
            new("@hotel_id", room.HotelId)
        };

        var hotelCountExists = await MySqlHelper.ExecuteScalarAsync(config.db, checkHotelQuery, hotelParams);
        int hotelCount = Convert.ToInt32(hotelCountExists);

        if (hotelCount == 0)
        {
            return RoomUpdateStatus.HotelNotFound;
        }

        //update room
        string query = """
        UPDATE rooms
        SET hotel_id = @hotel_id,
        name = @name,
        capacity = @capacity,
        price_per_night = @price_per_night
        WHERE id = @id
        """;

        var parameters = new MySqlParameter[]
        {
            new("@id", id),
            new("@hotel_id", room.HotelId),
            new("@name", room.Name),
            new("@capacity", room.Capacity),
            new("@price_per_night", room.PricePerNight)
        };

        int affectedRows = await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);

        if (affectedRows == 0)
        {
            return RoomUpdateStatus.NotFound;
        }

        return RoomUpdateStatus.Success;
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