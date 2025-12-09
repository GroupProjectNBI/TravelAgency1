using MySql.Data.MySqlClient;

namespace TravelAgency
{
    // Record för att representera en destination
    public record Location(int Id, string City, string Description, List<string> CulinaryExperiences);

 // Klass som hanterar alla destinationsoperationer
    public class Locations
    {
        private readonly Config _config;

       // Konstruktor för Locations-klassen
        public Locations(Config config)
        {
            _config = config;
        }

        // Hämta alla destinationer
        public async Task<List<Location>> GetAll()
        {
            var locations = new List<Location>();
            string query = "SELECT Id, City, Description FROM locations";

            using var reader = await MySqlHelper.ExecuteReaderAsync(_config.db, query);
            while (reader.Read())
            {
                locations.Add(new Location(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    new List<string>() // Man kan hämta kulinariska upplevelser separat
                ));
            }
            return locations;
        }

        // Hämta en destination via ID
        public async Task<Location?> Get(int id)
        {
            string query = "SELECT Id, City, Description FROM locations WHERE Id = @Id";
            var parameters = new MySqlParameter[] { new("@Id", id) };

            using var reader = await MySqlHelper.ExecuteReaderAsync(_config.db, query, parameters);
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

        // Spara användarens val 
        public async Task ChooseArrival(int userId, int locationId)
        {
            string query = "UPDATE users SET chosen_location_id = @LocationId WHERE Id = @UserId";
            var parameters = new MySqlParameter[]
            {
                new("@LocationId", locationId),
                new("@UserId", userId)
            };
            await MySqlHelper.ExecuteNonQueryAsync(_config.db, query, parameters);
        }
    }
}
