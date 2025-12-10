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
//app.MapPost("/register", Users.Post);
app.MapDelete("/db", db_reset_to_default);
app.MapPost("/register", Users_Post_Handler);

app.MapGet("/", () => "Hello world!");
app.MapGet("/profile", Profile.Get);
app.MapPost("/login", async (Login.Post_Args credentials, Config config, HttpContext ctx) =>
{
  bool success = await Login.Post(credentials, config, ctx);

  if (!success)
  {
    return Results.Json(
    new { message = "Unvalid credentials" },
    statusCode: StatusCodes.Status401Unauthorized);
  }

  return Results.Ok(new { message = "Login successful" });
});
app.MapDelete("/login", Login.Delete);
app.MapPatch("/newpassword/{temp_key}", Users.Patch);
app.MapGet("/reset/{email}", Users.Reset);
//Lägg till så att man även kan ta bort användare och uppdatera, GHERKIN
app.MapGet("/locations/{UserInput}", Destinations.Search);
app.MapPost("/location", Destinations.Post);
app.MapDelete("/location/{Id}", Destinations.Delete);
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
  CREATE TABLE countries
  (
  id INT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(100)
  );

  CREATE TABLE locations
  (
  id INT PRIMARY KEY AUTO_INCREMENT,
  contryId INT REFERENCES contries(id),
  city VARCHAR (100)
  );

  
  CREATE TABLE hotels
  (
  id INT PRIMARY KEY AUTO_INCREMENT,
  location_id INT NOT NULL,
  name varchar(100) NOT NULL, 
  address VARCHAR(255) NOT NULL,
  price_class INT NOT NULL,
  has_breakfast BOOL NOT NULL DEFAULT FALSE,
  FOREIGN KEY (location_id) REFERENCES location(id)
  );

  CREATE TABLE rooms
  (
  id INT PRIMARY KEY AUTO_INCREMENT,
  hotel_id INT NOT NULL,
  name ENUM ('Single, Double, Suit'),
  capacity INT,
  price_per_night DECIMAL,
  FOREIGN KEY (hotel_id) REFERENCES hotels(id)
  );   

  CREATE TABLE users
  (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  email VARCHAR(256) UNIQUE,
  first_name VARCHAR(50),
  last_name VARCHAR(100),
  date_of_birth DATE,
  password VARCHAR(256));




  """;

  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS users");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS password_request");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS hotels");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS rooms");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS countries");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS locations");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, users_create);
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO users(email, first_name, last_name, date_of_birth, password) VALUES ('edvin@example.com', 'Edvin', 'Linconfig.dborg', '1997-08-20', 'travelagency')");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO `countries` (id, name) VALUES (1,'Sweden'),(2,'Norway'),(3,'Denmark')");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO `locations` VALUES(1, 1, 'Stockholm'),(2, 1, 'Malmoe'),(3, 1, 'Gothenburg'),(4, 2, 'Copenhagen'),(5, 2, 'Aarhus'),(6, 2, 'Rodby'),(7, 3, 'Oslo'),(8, 3, 'Stavanger'),(9, 3, 'Bergen')");

  // await MySqlHelper.ExecuteNonQueryAsync(config.db, "CALL create_password_request('edvin@example.com')");
  //, NOW() + INTERVAL 1 DAY
}
static async Task<IResult> Users_Post_Handler(Users.Post_Args user, Config config)
{
  var (status, userId) = await Users.Post(user, config);
  return status switch

  {
    Users.RegistrationStatus.Success => Results.Created($"/register/{userId}", new { Message = "Account created." }),
    Users.RegistrationStatus.EmailConflict => Results.Conflict(new { Message = "Email already exists." }),
    Users.RegistrationStatus.InvalidFormat => Results.BadRequest(new { Message = "Unvalid format." }),
    Users.RegistrationStatus.WeakPassword => Results.BadRequest(new { Message = "Weak-password. Minimum 15 characters." }),
    _ => Results.StatusCode(500)
  };
}

// DELIMITER $$
//   CREATE PROCEDURE create_password_request(IN p_email VARCHAR(255))
//   BEGIN
//     START TRANSACTION;

// INSERT INTO password_request (`user`)
//     SELECT u.id
//     FROM users u
//     WHERE u.email = p_email;

// IF ROW_COUNT() = 0 THEN
//   ROLLBACK;
// SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No user with that email';
// END IF;

// COMMIT;
// END$$
//   DELIMITER ;

// ALTER TABLE Hotels
//   ADD FOREIGN KEY (rooms) REFERENCES Rooms(Id);

// floor INT NOT NULL, dddd
//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)