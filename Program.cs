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
app.MapGet("/upcomingtrips", UpcomingTrips_Get_Handler); //NEW
app.MapPatch("/ratings", RateTrip_Handler); //NEW

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
  // shouldn't be string create_schema, to define whole schema in one string? 
  //Shouldn't Stored procedure be last or run seperated? Like string create_proc .... because it use DELIMITER
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

  CREATE TABLE trips
  (
  id INT PRIMARY KEY AUTO_INCREMENT,
  destination VARCHAR(100) NOT NULL,
  departure_date DATE NOT NULL,
  return_date DATE NOT NULL,
  price DECIMAL(10, 2)
  );

  CREATE TABLE bookings
  (
  id INT PRIMARY KEY AUTO_INCREMENT,
  user_id INT NOT NULL REFERENCES users(id),
  trip_id INT NOT NULL REFERENCES trips(id),
  booking_date DATETIME DEFAULT CURRENT_TIMESTAMP,
  rating TINYINT NULL,
  UNIQUE KEY user_trip_unique (user_id, trip_id)
  );
  """;
  await MySqlHelper.ExecuteNonQueryAsync(config.db, create_schema); //new 
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS users");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS password_request");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS bookings"); //NEW
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS trips");//NEW
  await MySqlHelper.ExecuteNonQueryAsync(config.db, users_create);
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO users(email, first_name, last_name, date_of_birth, password) VALUES ('edvin@example.com', 'Edvin', 'Linconfig.dborg', '1997-08-20', 'travelagency')");
  //example trips
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO trips(destination, departure_date, return_date, price) VALUES ('Stockholm', CURDATE() + INTERVAL 7 DAY, CURDATE() + INTERVAL 14 DAY, 5000.00)"); //NEW
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INFO trips(destination, departure_date, return_date, price) VALUES ('Göteborg (Completed)', CURDATE() - INTERVAL 14 DAY, CURDATE() - INTERVAL 7 DAY, 15000.00)"); //NEW

  //Example boking
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO bookings(user_id, trip_id) VALUES (1, 1)"); //NEW
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO bookings(user_id, trip_id) VALUES (1, 2)"); //NEW
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
static async Task<IResult> UpcomingTrips_Get_Handler(HttpContext context, Config config)
{
  //Controlls login session
  if (!context.Session.TryGetValue("UserId", out byte[]? userIdBytes))
  {
    return Results.Unauthorized();
  }
  if (userIdBytes == null)
  {
    return Results.Unauthorized();
  }
  int userId = BitConverter.ToInt32(userIdBytes, 0);

  var trips = await Users.GetUpcomingTrips(userId, config);

  if (trips.Count > 0)
  {
    return Results.Ok(trips);
  }
  else
  {
    return Results.NotFound(new { Message = "No upcoming trips found." });
  }
}
static async Task<IResult> RateTrip_Handler(Users.RateTrip_Args ratingData, HttpContext context, Config config)
{
  if (!context.Session.TryGetValue("UserId", out byte[]? userIdBytes) || userIdBytes == null)
  {
    return Results.Unauthorized();
  }
  int userId = BitConverter.ToInt32(userIdBytes, 0);
  if (ratingData.rating < 1 || ratingData.rating > 5)
  {
    return Results.BadRequest(new { Message = "Rating must be between 1 and 5 starts." });
  }
  int affectedRows = await Users_Post_Handler.RateTrip(ratingData, userId, config);
  if (affectedRows > 0)
  {
    return Results.Ok(new { Message = "Rating successfully recorded." });
  }
  else
  {
    return Results.NotFound(new { Message = "Booking not found, unauthorized access, or trip is not yet complete." });
  }
}



//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)