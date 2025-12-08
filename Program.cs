using MySql.Data.MySqlClient;
using TravelAgency;

var builder = WebApplication.CreateBuilder(args);

Config config = new("server=127.0.0.1;uid=travelagency;pwd=travelagency;database=travelagency");
builder.Services.AddSingleton<Config>(config);
//builder.Services.AddDistributedMemoryCache();
var app = builder.Build();

app.MapGet("/register", Users.GetAll);
app.MapGet("/register/{Id}", Users.Get);
app.MapPost("/register", Users.Post);
app.MapDelete("/db", db_reset_to_default);
app.MapPatch("/newpassword/{temp_key}", Users.Patch);
//Lägg till så att man även kan ta bort användare och uppdatera, GHERKIN
app.Run();

//void
async Task db_reset_to_default(Config config)
{
  // string db = "server=127.0.0.1;uid=travelagency;pwd=travelagency;database=travelagency";

  string users_create = """ 
  // adding a new table to the database :
  CREATE TABLE password_request
  (
  user INT REFERENCES users(id),
  temp_key binary(16) PRIMARY KEY DEFAULT (UUID_TO_BIN(UUID()))
  );

  CREATE TABLE users
  (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  email VARCHAR(256) UNIQUE,
  first_name VARCHAR(50),
  last_name VARCHAR(100),
  date_of_birth DATE,
  password VARCHAR(256))
  """;

  await MySqlHelper.ExecuteNonQueryAsync(config.ConnectionString, "DROP TABLE IF EXISTS users");
  await MySqlHelper.ExecuteNonQueryAsync(config.ConnectionString, "DROP TABLE IF EXISTS password_request");
  await MySqlHelper.ExecuteNonQueryAsync(config.ConnectionString, users_create);
  await MySqlHelper.ExecuteNonQueryAsync(config.ConnectionString, "INSERT INTO users(email, first_name, last_name, date_of_birth, password) VALUES ('edvin@example.com', 'Edvin', 'Linconfig.ConnectionStringorg', '1997-08-20', 'travelagency')");
  await MySqlHelper.ExecuteNonQueryAsync(config.ConnectionString, "INSERT INTO password_request (user) VALUES (1)");
}
//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)