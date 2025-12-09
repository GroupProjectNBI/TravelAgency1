global using MySql.Data.MySqlClient;
using TravelAgency;

Config config = new("server=127.0.0.1;uid=travelagency;pwd=travelagency;database=travelagency");
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(config);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
  options.Cookie.HttpOnly = true;
  options.Cookie.IsEssential = true;
});

var app = builder.Build();
app.UseSession();

app.MapGet("/register", Users.GetAll);
app.MapGet("/register/{Id}", Users.Get);
app.MapPost("/register", Users.Post);
app.MapDelete("/db", db_reset_to_default);

app.MapGet("/", () => "Hello world!");
app.MapGet("/profile", Profile.Get);
app.MapPost("/login", Login.Post);
app.MapDelete("/login", Login.Delete);
app.MapPatch("/newpassword/{temp_key}", Users.Patch);
app.MapGet("/reset/{email}", Users.Reset);
//Lägg till så att man även kan ta bort användare och uppdatera, GHERKIN
app.Run();

//void
async Task db_reset_to_default(Config config)
{
  // string db = "server=127.0.0.1;uid=travelagency;pwd=travelagency;database=travelagency";

  string users_create = """ 
  /* adding a new table to the database : */
  CREATE TABLE password_request
  (
  user INT REFERENCES users(id),
  temp_key binary(16) PRIMARY KEY DEFAULT (UUID_TO_BIN(UUID()))
  /* expire_date  DATE NOT NULL */
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

  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS users");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS password_request");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, users_create);
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO users(email, first_name, last_name, date_of_birth, password) VALUES ('edvin@example.com', 'Edvin', 'Linconfig.dborg', '1997-08-20', 'travelagency')");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "CALL create_password_request('edvin@example.com')");
  //, NOW() + INTERVAL 1 DAY
  // DELIMITER $$
  // CREATE PROCEDURE create_password_request(IN p_email VARCHAR(255))
  // BEGIN
  //   START TRANSACTION;

  //   INSERT INTO password_request (`user`)
  //   SELECT u.id
  //   FROM users u
  //   WHERE u.email = p_email;

  //   IF ROW_COUNT() = 0 THEN
  //     ROLLBACK;
  //     SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No user with that email';
  //   END IF;

  //   COMMIT;
  // END$$
  // DELIMITER ;
}

//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)