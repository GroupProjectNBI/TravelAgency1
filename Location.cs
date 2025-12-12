using MySql.Data.MySqlClient;

namespace TravelAgency
{
    public record Location_Data(
    int id,
    int countries_id,
    string city,
    List<string> culinary_experiences
    );
    public record Post_Location_Args(int countries_id, string city);

    public class Locations
    {
        private static Location_Data Read_Location(MySqlDataReader reader)
        {
            return new Location_Data(
      id: reader.GetInt32(0),
      countries_id: reader.GetInt32(1),
      city: reader.GetString(2),
      culinary_experiences: new List<string>()
        );
        }
        public static async Task<List<Location_Data>> Get_All(Config config)
        {
            var locations = new List<Location_Data>();
            string query = "SELECT id, countries_Id, city FROM locations";
            using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query);

            while (reader.Read())
            {
                locations.Add(Read_Location(reader));
            }
            return locations;
        }
        public static async Task<Location_Data?> Get(Config config, int id)
        {
            string query = "SELECT id, countries_Id, city FROM locations WHERE id = @Id";
            var parameters = new MySqlParameter[] { new("@Id", id) };

            using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);

            if (reader.Read())
            {
                return Read_Location(reader);
            }
            return null;
        }
        public static async Task<List<Location_Data>> Search(string user_input, Config config)
        {
            if (string.IsNullOrWhiteSpace(user_input))
                throw new ArgumentException("You must enter a city name.");

            List<Location_Data> result = new();
            string search_value = $"%{user_input}%";

            string query = "SELECT id, countries_Id, city FROM locations WHERE city LIKE @UserInput";
            var parameters = new MySqlParameter[]
            {
        new MySqlParameter (@"UserInput", search_value)
            };
            using (var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters))
            {
                while (reader.Read())
                {
                    result.Add(Read_Location(reader));
                }
            }
            if (result.Count == 0)
                throw new ArgumentException("No city found matching your input. Please enter a valid city.");

            return result;
        }
        public static async Task<int> Post(Post_Location_Args args, Config config)
        {
            string query = "INSERT INTO locations(countries_id, city) VALUES (@countries_id, @city); SELECT LAST_INSERT_ID();";
            var parameters = new MySqlParameter[]
            {
        new("@countries_id", args.countries_id),
        new("@city", args.city),
            };

            var newId = await MySqlHelper.ExecuteScalarAsync(config.db, query, parameters);
            return Convert.ToInt32(newId);
        }
        public static async Task<int> Delete(int id, Config config)
        {
            string query = "DELETE FROM locations WHERE ID = @Id";
            var parameters = new MySqlParameter[] { new("@Id", id) };

            return await MySqlHelper.ExecuteNonQueryAsync(config.db, query, parameters);
        }
    }
}