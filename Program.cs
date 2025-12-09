global using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
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


var locations = new Locations(config);
//skpar tabeller och data
await InitializeDatabase(config);

//POST välj destination
app.MapPost("/location/choose/{id}", async (int id, HttpContext http) =>
{
    var location = await locations.Get(id);
  if (location == null) return Results.NotFound("Destination not found"); // *uppdatera Gherkin
  
  http.Session.SetString("ChosenArrival", location.City);
  return Results.Ok(new
  {
    Message = "Destination chosen successfully",
    Location = location
  });
});
 // Get hämta vald destination
app.MapGet("/location/chosen", async (HttpContext http) =>
{
var chosen = http.Session.GetString("ChosenArrival");
  if (string.IsNullOrEmpty(chosen)) return Results.NotFound("No destination chosen .");

var allLocations = await locations.GetAll();
var location = allLocations.FirstOrDefault(l => l.City == chosen);
return Results.Ok(location);
});


app.Run();

async Task InitializeDatabase( Config config)
{
  string createLocations = @"
    CREATE TABLE IF NOT EXISTS locations
    (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        City VARCHAR(100) NOT NULL,
        Description TEXT
    );";

  string createCulinaryExpreriences = @"
    CREATE TABLE IF NOT EXISTS culinary_experiences
    (
        Id INT PRIMARY KEY AUTO_INCREMENT,
        LocationId INT NOT NULL,
        Name VARCHAR(255) NOT NULL,
        FOREIGN KEY (LocationId) REFERENCES locations(Id)
    );";
  await MySqlHelper.ExecuteNonQueryAsync(config.db, createLocations);
  await MySqlHelper.ExecuteNonQueryAsync(config.db, createCulinaryExpreriences);
}

// Lägg till City, INSERT ....
//Lägg till Culinary Experiences, INSERT ....

// TODO: Initialize database with sample locations like Malmö and Köpenhamn
// Example:
// INSERT INTO locations (City, Description) VALUES ('Malmö', 'Description...'), ('Köpenhamn', 'Description...');

// TODO: Add culinary experiences for each location
// Example:
// INSERT INTO culinary_experiences (LocationId, Name) VALUES (1, 'Smörrebröd provning'), (2, 'Noma-inspirerad matupplevelse');

// TODO: Implement method to fetch culinary experiences for each Location
// Currently, CulinaryExperiences is an empty list.
// Later: join with culinary_experiences table to populate it.









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

  DELIMITER $$
  CREATE PROCEDURE create_password_request(IN p_email VARCHAR(255))
  BEGIN
    START TRANSACTION;

    INSERT INTO password_request (`user`)
    SELECT u.id
    FROM users u
    WHERE u.email = p_email;

    IF ROW_COUNT() = 0 THEN
      ROLLBACK;
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No user with that email';
    END IF;

    COMMIT;
  END$$
  DELIMITER ;

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


//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)