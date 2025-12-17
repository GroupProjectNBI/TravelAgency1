global using MySql.Data.MySqlClient;
using TravelAgency;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using MySqlX.XDevAPI.Common;

// --- 1. Konfiguration och Services ---
var builder = WebApplication.CreateBuilder(args);

// Databas-config (Överväg att flytta detta till appsettings.json i framtiden)
Config config = new("server=127.0.0.1;uid=travel_agent;pwd=travel_agent;database=travelagency");
builder.Services.AddSingleton(config);

// Caching och Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
  options.Cookie.HttpOnly = true;
  options.Cookie.IsEssential = true;
  options.IdleTimeout = TimeSpan.FromMinutes(30); // Logga ut vid inaktivitet
});

// Autentisering (Kräver din AuthExtensions.cs fil)
builder.Services.AddTravelAgencyAuthentication();

// Auktorisering
builder.Services.AddAuthorization();

var app = builder.Build();

// --- 2. Middleware Pipeline (Ordningen är viktig!) ---
app.UseSession();                           // Läs in kakan
app.UseMiddleware<SessionAuthMiddleware>(); // Omvandla session till User Claims
app.UseAuthorization();                     // Kontrollera behörighet

// --- 3. Endpoints ---
app.MapGet("/experiences", async (
int location_id,
DateOnly check_in,
DateOnly check_out,
int rooms,
int guests,
int max_price_class,
string package,  // Veggie | Fine dining | Wine
int limit,
Config config) =>
{
  if (rooms <= 0) return Results.BadRequest(new { message = "rooms must be > 0" });
  if (guests <= 0) return Results.BadRequest(new { message = "guests must be > 0" });
  if (check_out <= check_in) return Results.BadRequest(new { message = "check_out must be after check_in" });
  if (max_price_class <= 0) return Results.BadRequest(new { message = "max_price_class must be > 0" });

  package = package.Trim();

  var offers = await Experiences.SearchOffers(location_id, check_in, check_out, rooms, guests, max_price_class, package, limit, config);
  return Results.Ok(offers);
});
app.MapPost("/bookings/from-offer", Experiences_BookFromExperienceOffer_Handler);


// --- Login & Auth ---
app.MapPost("/login", async (Login.Post_Args credentials, Config config, HttpContext ctx) =>
{
  bool success = await Login.Post(credentials, config, ctx);

  if (!success)
  {
    return Results.Json(
        new { message = "Invalid credentials" },
        statusCode: StatusCodes.Status401Unauthorized);
  }

  return Results.Ok(new { message = "Login successful" });
});

app.MapDelete("/login", Login.Delete).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPatch("/newpassword/{temp_key}", Users.Patch).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/reset/{email}", Users.Reset).RequireAuthorization(p => p.RequireRole("admin"));

// --- Admin & System ---
app.MapDelete("/db", Data.db_reset_to_default).AllowAnonymous();//.RequireAuthorization(p => p.RequireRole("admin"));

// endpoints for locations -- no update for location
app.MapGet("/locations", Locations.Get_All).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/locations/search", Locations_Search_Handler).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/locations/{id}", Locations_Get_Handler).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPost("/locations", Locations_Post_Handler).RequireAuthorization(p => p.RequireRole("admin"));
app.MapDelete("/locations/{id}", Locations_Delete_Handler).RequireAuthorization(p => p.RequireRole("admin"));

app.MapGet("/admin/get_users", Admin.GetAllUsers)
   .RequireAuthorization(p => p.RequireRole("admin"));

// --- Users ---
app.MapGet("/register", Users.GetAll).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/register/{Id}", Users.Get).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPost("/register", Users_Post_Handler).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/profile", Profile.Get).RequireAuthorization(); // Kräver inloggning --- bra att ha

// --- Hotels ---
app.MapGet("/hotels", Hotels.GetAll).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/hotels/{Id}", Hotels.Get).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPost("/hotels", Hotels.Post).RequireAuthorization(p => p.RequireRole("admin"));
app.MapDelete("/hotels/{Id}", Hotels.DeleteHotel).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPut("/hotels/{id}", Hotels.Put).RequireAuthorization(p => p.RequireRole("admin"));

// --- Rooms ---
app.MapGet("/hotels/{hotelId}/rooms", Rooms.GetByHotel).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/rooms", Rooms.GetAll).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/rooms/{id}", Rooms.Get).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPost("/rooms", Rooms_Post_Handler).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPut("/rooms/{id}", Rooms_Put_Handler).RequireAuthorization(p => p.RequireRole("admin"));
app.MapDelete("/rooms/{id}", Rooms.Delete).RequireAuthorization(p => p.RequireRole("admin"));

// --- Restaurants ---
app.MapGet("/restaurants", Restaurants.GetAll).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/restaurants/{id}", Restaurants.Get).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPost("/restaurants", Restaurants.Post).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPut("/restaurants/{id}", Restaurants.Put).RequireAuthorization(p => p.RequireRole("admin"));
app.MapDelete("/restaurants/{id}", Restaurants.Delete).RequireAuthorization(p => p.RequireRole("admin"));

// --- Packages ---
app.MapGet("/packages", Package.GetAll).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/packages/{Id}", Package.Get).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/packages_details/{locationid}/{packageid}/{hotelid}", Package.GetDetails);
app.MapPost("/packages", Package.Post).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPut("/packages/{id}", Package.Put).RequireAuthorization(p => p.RequireRole("admin"));
app.MapDelete("/packages/{id}", Package.DeletePackage).RequireAuthorization(p => p.RequireRole("admin"));

// --- Package Meals ---
app.MapPost("/packages_meals", package_meals.Post).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/packages_meals", PackagesMeals_Get_All_Handler).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPut("/packages_meals/{id}", package_meals.Put).RequireAuthorization(p => p.RequireRole("admin"));
app.MapDelete("/packages_meals/{id}", package_meals.Delete).RequireAuthorization(p => p.RequireRole("admin"));

// --- Bookings ---
app.MapGet("/bookings", Bookings_Get_All_Handler).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPost("/bookings", Bookings.Post).RequireAuthorization(p => p.RequireRole("admin"));
app.MapDelete("/bookings/{id}", Bookings.Delete).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPut("/bookings/{id}", Bookings.Put).RequireAuthorization(p => p.RequireRole("admin"));


// --- Booking Meals ---
app.MapGet("/booking_meals", bookings_meals.GetAll).RequireAuthorization(p => p.RequireRole("admin"));
app.MapGet("/booking_meals/{id}", bookings_meals.Get).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPut("/booking_meals/{id}", bookings_meals.Put).RequireAuthorization(p => p.RequireRole("admin"));
app.MapDelete("/booking_meals/{id}", bookings_meals.Delete).RequireAuthorization(p => p.RequireRole("admin"));
app.MapPost("/booking_meals", bookings_meals.Post).RequireAuthorization(p => p.RequireRole("admin"));

app.Run();

// --- 4. Local Handler Functions ---

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

static async Task<IResult> Experiences_BookFromExperienceOffer_Handler(
  Experiences.BookFromExperienceArgs req, Config config)
{
  return await Experiences.BookFromExperienceOffer(req, config);
}


