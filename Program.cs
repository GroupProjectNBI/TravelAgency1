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

  
    CREATE TABLE Hotels
  (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  name varchar(100) NOT NULL, 
  address VARCHAR(255) NOT NULL,
  price_class INT NOT NULL,
  rooms INT NOT NULL, 
  floor INT NOT NULL,
  breakfast BOOL NOT NULL DEFAULT FALSE
  );     

  /*
  CREATE TABLE Floors 
  (
  FloorId NOT NULL, 

  */
  
  CREATE TABLE Rooms
  (
   Id INT PRIMARY KEY AUTO_INCREMENT,
   hotelsId INT NOT NULL,
   how_many_beds INT NOT NULL, /* typeofroom*/
   vacancy BOOL NOT NULL DEFAULT TRUE,
   floor INT NOT NULL UNIQUE
   );

  CREATE TABLE users
  (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  email VARCHAR(256) UNIQUE,
  first_name VARCHAR(50),
  last_name VARCHAR(100),
  date_of_birth DATE,
  password VARCHAR(256));


  ALTER TABLE Hotels
  ADD FOREIGN KEY (rooms) REFERENCES Rooms(Id);

  ALTER TABLE Rooms
  ADD FOREIGN KEY (hotelsId) REFERENCES Hotels(Id);

  """;

  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS users");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS password_request");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS Rooms");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS Hotels");
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


//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)