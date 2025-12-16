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
app.MapDelete("/db", Data.db_reset_to_default);


app.MapGet("/admin/get_users", Admin.GetAllUsers);

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
app.MapPost("/locations", Locations_Post_Handler);
app.MapGet("/locations/{id}", Locations_Get_Handler);
app.MapDelete("/locations/{id}", Locations_Delete_Handler);

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
app.MapPost("/packages/check_availability", Packages_CheckAvailability_Handler);


// endpoints for package meals
app.MapPost("/packages_meals", package_meals.Post);
app.MapGet("/packages_meals", PackagesMeals_Get_All_Handler);
app.MapPut("/packages_meals/{id}", package_meals.Put);
app.MapDelete("/packages_meals/{id}", package_meals.Delete);

//endpoint for bookings   
/*
vi måste bestämma var vi hanterar valideringen. 
Nu är det kod som är blandad i progam och i klasserna. 
Program.cs är super lång så jag föreslår att varje validering görs 
för varje funktion istället för att kladda i program.cs
handler funktionerna undertill är bara mer kod som måste hanteras. 
*/
app.MapGet("/bookings", Bookings_Get_All_Handler);
app.MapPost("/bookings", Bookings.Post);
app.MapDelete("/bookings/{id}", Bookings.Delete);
app.MapPut("/bookings/{id}", Bookings.Put);




//endpoit for booking meals
app.MapGet("/booking_meals", bookings_meals.GetAll);
app.MapGet("/booking_meals/{id}", bookings_meals.Get);
app.MapPut("/booking_meals/{id}", bookings_meals.Put);
app.MapDelete("/booking_meals/{id}", bookings_meals.Delete);
app.MapPost("/booking_meals", bookings_meals.Post);

app.Run();

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

static async Task<IResult> Packages_CheckAvailability_Handler(Package.CheckAvailability_Args args, Config config)
{
  if (!DateOnly.TryParse(args.check_in, out var checkInDate))
    return Results.BadRequest(new { message = "Invalid check_in date format" });

  if (!DateOnly.TryParse(args.check_out, out var checkOutDate))
    return Results.BadRequest(new { message = "Invalid check_out date format" });

  return await Package.CheckAvailability(args.package_id, checkInDate, checkOutDate, config);
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


// await MySqlHelper.ExecuteNonQueryAsync(config.db, "CALL create_password_request('edvin@example.com')");
//, NOW() + INTERVAL 1 DAY


//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)
// Test