global using MySql.Data.MySqlClient;
using TravelAgency;

Config config = new("server=127.0.0.1;uid=travel_agent;pwd=travel_agent;database=travelagency");
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

// endpoints for users
app.MapGet("/register", Users.GetAll);
app.MapGet("/register/{Id}", Users.Get);
// app.MapPost("/register", Users.Post);
app.MapPost("/register", Users_Post_Handler);

// reset all the tables for the databse
app.MapDelete("/db", db_reset_to_default);



// later use
app.MapGet("/profile", Profile.Get);


// endpoints for login
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
// app.MapPost("/login", Login.Post);
app.MapDelete("/login", Login.Delete);

// enpoint for reset password
app.MapPatch("/newpassword/{temp_key}", Users.Patch);
app.MapGet("/reset/{email}", Users.Reset);

// endpoints for locations
app.MapGet("/locations", Locations.Get_All);
app.MapGet("/locations/search", Locations_Search_Handler);
app.MapPost("/location", Locations_Post_Handler);
app.MapGet("location/{id}", Locations_Get_Handler);
app.MapDelete("/location/{id}", Locations_Delete_Handler);

// endpoints for hotels 
app.MapGet("/hotels", Hotels.GetAll);
app.MapGet("/hotels/{Id}", Hotels.Get);
app.MapPost("/hotels", Hotels.Post);
app.MapDelete("/hotels/{Id}", Hotels.DeleteHotel);
app.MapPut("/hotels/{id}", Hotels.Put);

//endpoints for rooms
app.MapGet("/hotels/{hotelId}/rooms", Rooms.GetByHotel);
app.MapGet("/rooms", Rooms.GetAll);
app.MapGet("/rooms/{id}", Rooms.Get);
app.MapPost("/rooms", Rooms_Post_Handler);
app.MapPut("/rooms/{id}", Rooms_Put_Handler);
app.MapDelete("/rooms/{id}", Rooms.Delete);

// endpoints for restaurants
app.MapGet("/restaurants", Restaurants.GetAll);
app.MapGet("/restaurants/{id}", Restaurants.Get);
app.MapPost("/restaurants", Restaurants.Post);
app.MapPut("/restaurants/{id}", Restaurants.Put);
app.MapDelete("/restaurants/{id}", Restaurants.Delete);

// endpoint for packages 
app.MapGet("/packages", Package.GetAll);
app.MapGet("/packages/{Id}", Package.Get);
app.MapPost("/packages", Package.Post);
app.MapPut("/packages/{id}", Package.Put);
app.MapDelete("/packages/{id}", Package.DeletePackage);

// endpoints for package meals
app.MapPost("/packages_meals", package_meals.Post);
app.MapGet("/packages_meals", PackagesMeals_Get_All_Handler);
app.MapPut("/packages_meals/{id}", package_meals.Put);
app.MapDelete("/packages_meals/{id}", package_meals.Delete);

//endpoint for bookings
app.MapGet("/bookings", Bookings_Get_All_Handler);

app.Run();



//void
async Task db_reset_to_default(Config config)
{

  string users_create = """ 

  /* adding a new table to the database : */


  CREATE TABLE users
  (
  Id INT PRIMARY KEY AUTO_INCREMENT,
  email VARCHAR(256) UNIQUE,
  first_name VARCHAR(50),
  last_name VARCHAR(100),
  date_of_birth DATE,
  password VARCHAR(256));
  
  CREATE TABLE password_request
  (
  user INT REFERENCES users(id),
  temp_key binary(16) PRIMARY KEY DEFAULT (UUID_TO_BIN(UUID()))
  /* expire_date  DATE NOT NULL */
  );
  CREATE TABLE countries
  (
  id INT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(100) NOT NULL
  );

  CREATE TABLE locations
  (
  id INT PRIMARY KEY AUTO_INCREMENT,
  countries_id INT NOT NULL,
  city VARCHAR (100) NOT NULL,
  FOREIGN KEY (countries_id) REFERENCES countries(id)
  );

  
  CREATE TABLE hotels (
  id INT AUTO_INCREMENT PRIMARY KEY,
  location_id INT NOT NULL,
  name VARCHAR(100) NOT NULL,
  address VARCHAR(255) NOT NULL,
  price_class INT NOT NULL,
  has_breakfast BOOLEAN NOT NULL DEFAULT FALSE,
  FOREIGN KEY (location_id) REFERENCES locations(id)
  );

  CREATE TABLE rooms (
  id INT AUTO_INCREMENT PRIMARY KEY,
  hotel_id INT NOT NULL,
  room_number INT NOT NULL,
  name ENUM ('Single', 'Double', 'Suite'),
  capacity INT NOT NULL,
  price_per_night DECIMAL(10,2) NOT NULL,
  UNIQUE KEY roomnumber_per_hotel (hotel_id, room_number),
  FOREIGN KEY (hotel_id) REFERENCES hotels(id)
  );

  CREATE TABLE restaurants (
  id INT AUTO_INCREMENT PRIMARY KEY,
  location_id INT NOT NULL,
  name VARCHAR(100),
  is_veggie_friendly BOOLEAN NOT NULL DEFAULT FALSE,
  is_fine_dining BOOLEAN NOT NULL DEFAULT FALSE,
  is_wine_focused BOOLEAN NOT NULL DEFAULT FALSE,
  FOREIGN KEY (location_id) REFERENCES locations(id)
  );

  CREATE TABLE packages (
  id INT AUTO_INCREMENT PRIMARY KEY,
  location_id INT NOT NULL,
  name VARCHAR(100),
  description VARCHAR (254),
  package_type ENUM ('Veggie', 'Fish', 'Fine dining'),
  FOREIGN KEY (location_id) REFERENCES locations(id)
  );

  CREATE TABLE packages_meals (
  id INT AUTO_INCREMENT PRIMARY KEY,
  package_id INT NOT NULL,
  restaurant_id INT NOT NULL,
  day_kind ENUM ('Arrival', 'Stay', 'Departure') NOT NULL,
  meal_type ENUM ('Breakfast', 'Lunch', 'Dinner') NOT NULL,
  UNIQUE KEY unique_pkg_meal (package_id, day_kind, meal_type),
  FOREIGN KEY (package_id) REFERENCES packages(id),
  FOREIGN KEY (restaurant_id) REFERENCES restaurants(id)
  );

  CREATE TABLE bookings (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL,
  location_id INT NOT NULL,
  hotel_id INT NOT NULL,
  package_id INT NOT NULL,
  check_in DATE NOT NULL,
  check_out DATE NOT NULL,
  guests INT NOT NULL,
  rooms INT NOT NULL,
  status ENUM('pending','confirmed','cancelled'),
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  total_price DECIMAL(10,2) NOT NULL,
  FOREIGN KEY (user_id) REFERENCES users(id),
  FOREIGN KEY (location_id) REFERENCES locations(id),
  FOREIGN KEY (hotel_id) REFERENCES hotels(id),
  FOREIGN KEY (package_id) REFERENCES packages(id)
  );

  CREATE TABLE booking_meals (
  id INT AUTO_INCREMENT PRIMARY KEY,
  bookings_id INT NOT NULL,
  date DATE,
  meal_type ENUM ('Breakfast', 'Lunch', 'Dinner'),
  FOREIGN KEY (bookings_id) REFERENCES bookings(id)
  );

  """;

  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS booking_meals");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS packages_meals");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS bookings");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS rooms");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS packages");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS restaurants");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS hotels");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS locations");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS countries");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS password_request");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS users");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, users_create);
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO users(email, first_name, last_name, date_of_birth, password) VALUES ('edvin@example.com', 'Edvin', 'Lindborg', '1997-08-20', 'travelagency')");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO countries (id, name) VALUES (1,'Sweden'),(2,'Norway'),(3,'Denmark')");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO locations VALUES(1, 1, 'Stockholm'),(2, 1, 'Malmoe'),(3, 1, 'Gothenburg'),(4, 2, 'Copenhagen'),(5, 2, 'Aarhus'),(6, 2, 'Rodby'),(7, 3, 'Oslo'),(8, 3, 'Stavanger'),(9, 3, 'Bergen')");

  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO restaurants (location_id, name, is_veggie_friendly, is_fine_dining, is_wine_focused) VALUES (1, 'roserio', 1, 1, 0), (1, 'pizza hut', 1, 0, 0), (1, 'stinas grill', 1, 1, 1), (2, 'grodans boll', 0, 0, 0);");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO hotels (id, location_id, name, address, price_class, has_breakfast) VALUES(1, 1, 'SwingIn', 'Stockholsgatan', 5, 1)");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO packages (id, location_id, name, description, package_type) VALUES (1, 1, 'Weekend Stockholm', 'Arrival dinner + stay meals + departure breakfast', 'Fine dining');");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO packages_meals (package_id, restaurant_id, day_kind, meal_type) VALUES (1, 1, 'Arrival', 'Dinner');");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO packages_meals (package_id, restaurant_id, day_kind, meal_type) VALUES (1, 2, 'Stay', 'Lunch');");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO packages_meals (package_id, restaurant_id, day_kind, meal_type) VALUES (1, 3, 'Departure', 'Breakfast');");
  // await MySqlHelper.ExecuteNonQueryAsync(config.db, "CALL create_password_request('edvin@example.com')");
  //, NOW() + INTERVAL 1 DAY
}

static async Task<IResult> Locations_Search_Handler(string search, Config config)
{
  try

  {
    var result = await Locations.Search(search, config);
    return Results.Ok(result);
  }
  catch (ArgumentException ex)
  {
    return Results.BadRequest(new { Message = ex.Message });
  }
}
static async Task<IResult> Locations_Get_Handler(int id, Config config)
{
  var location = await Locations.Get(config, id);
  if (location is null)
  {
    return Results.NotFound(new { Message = $"Location with ID {id} was not found." });
  }
  return Results.Ok(location);
}
static async Task<IResult> Locations_Post_Handler(Post_Location_Args args, Config config)
{
  try
  {
    int newId = await Locations.Post(args, config);
    return Results.Created($"/location/{newId}", new { id = newId, Message = "Location created successfully." });
  }
  catch (Exception)
  {
    return Results.StatusCode(StatusCodes.Status500InternalServerError);
  }
}
static async Task<IResult> Locations_Delete_Handler(int id, Config config)
{
  int affectedRows = await Locations.Delete(id, config);

  if (affectedRows == 0)
  {
    return Results.NotFound(new { Message = $"Location with ID {id} was not found or could not be deleted." });
  }
  return Results.NoContent();
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

static async Task<IResult> Rooms_Post_Handler(Rooms.Post_Args room, Config config)
{
  var (status, roomId) = await Rooms.Post(room, config);
  return status switch
  {
    Rooms.RoomCreationStatus.Success => Results.Created("", roomId),

    Rooms.RoomCreationStatus.InvalidFormat => Results.BadRequest(new { Message = "Invalid room data." }),

    Rooms.RoomCreationStatus.HotelNotFound => Results.NotFound(new { Message = "Hotel not found." }),

    _ => Results.StatusCode(500)

  };
}

static async Task<IResult> Rooms_Put_Handler(int id, Rooms.Put_Args room, Config config)
{
  var status = await Rooms.Put(id, room, config);

  return status switch
  {
    Rooms.RoomUpdateStatus.Success => Results.NoContent(),
    Rooms.RoomUpdateStatus.InvalidFormat => Results.BadRequest(new { Message = "Invalid room data" }),
    Rooms.RoomUpdateStatus.HotelNotFound => Results.NotFound(new { Message = "Hotel not found" }),
    Rooms.RoomUpdateStatus.NotFound => Results.NotFound(new { Message = "Room not found" }),
    _ => Results.StatusCode(500)
  };
}
static async Task<IResult> PackagesMeals_Get_All_Handler(Config config)
{
  try
  {
    var meals = await package_meals.Get_All(config);
    return Results.Ok(meals);
  }
  catch (Exception)
  {
    return Results.StatusCode(StatusCodes.Status500InternalServerError);
  }
}
static async Task<IResult> Bookings_Get_All_Handler(Config config)
{
  try
  {
    var bookings = await Bookings.Get_All(config);
    return Results.Ok(bookings);
  }
  catch (Exception)
  {
    return Results.StatusCode(StatusCodes.Status500InternalServerError);
  }
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


// await MySqlHelper.ExecuteNonQueryAsync(config.db, "CALL create_password_request('edvin@example.com')");
//, NOW() + INTERVAL 1 DAY


//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)
// Test