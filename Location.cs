using MySql.Data.MySqlClient;
namespace TravelAgency

{

    class Locations

    {
        public record Location(int Id, string City, string Description, List<string> CulinaryExperiences);
        public static async Task<List<Location>> GetAll(Config config)
        {
            var locations = new List<Location>();
            string query = "SELECT id, countries_id, city, FROM locations"; // description 

            using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query);
            while (reader.Read())
            {
                locations.Add(new Location(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    new List<string>()
                ));
            }

            return locations;
        }
        public static async Task<Location?> Get(Config config, int id)
        {
            string query = "SELECT id, city, description FROM locations WHERE Id = @Id";
            var parameters = new MySqlParameter[] { new("@Id", id) };

            using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);
            if (reader.Read())
            {
                return new Location(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    new List<string>()
                );
            }

            return null;
        }
        //public static async Task ChooseArrival(Config config, int userId, int locationId)
        //{
        //  string query = "UPDATE users SET chosen_location_id = @LocationId WHERE Id = @UserId";
        //var parameters = new MySqlParameter[]
        //{
        //  new("@LocationId", locationId),
        //new("@UserId", userId)
        //};

        //await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
        //}
    }
}
