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
//Lägg till så att man även kan ta bort användare och uppdatera, GHERKIN
app.Run();

//void
async Task db_reset_to_default()
{
  string db = "server=127.0.0.1;uid=travelagency;pwd=travelagency;database=travelagency";

  string users_create = """ 
  CREATE TABLE users
  (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  email VARCHAR(256) UNIQUE,
  first_name VARCHAR(50),
  last_name VARCHAR(100),
  date_of_birth DATE,
  password VARCHAR(256) )
  """;

  await MySqlHelper.ExecuteNonQueryAsync(db, "DROP TABLE IF EXISTS users");
  await MySqlHelper.ExecuteNonQueryAsync(db, users_create);

  await MySqlHelper.ExecuteNonQueryAsync(db, "INSERT INTO users(email, first_name, last_name, date_of_birth, password) VALUES ('edvin@example.com', 'Edvin', 'Lindborg', '1997-08-20', 'travelagency')");
}
//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)