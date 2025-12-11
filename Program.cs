global using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
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

app.MapGet("/register", Users.GetAll);
app.MapGet("/register/{Id}", Users.Get);
app.MapPost("/register", Users.Post);
app.MapDelete("/db", db_reset_to_default);

app.MapGet("/", () => "Hello world!");

app.MapGet("/profile", Profile.Get);
app.MapPost("/login", Login.Post);
app.MapDelete("/login", Login.Delete);
app.MapPatch("/newpassword/{temp_key}", Users.Patch);

//app.MapPost("/location", Locations.Post);

//Hämta alla hotell 
//Lägg till så att man även kan ta bort användare och uppdatera, GHERKIN
app.MapGet("/locations/{UserInput}", Destinations.Search);
app.MapPost("/locations", Destinations.Post);
app.MapDelete("/locations/{Id}", Destinations.Delete);
app.MapGet("/hotels", Hotels.GetAll);
app.MapGet("/hotels/{Id}", Hotels.Get);
app.MapPost("/hotels", Hotels.Post);
app.MapDelete("/hotels/{Id}", Hotels.DeleteHotel);
app.MapGet("/restaurants", Restaurants.GetAll);
app.MapGet("/restaurants/{id}", Restaurants.Get);
app.MapPost("/restaurants", Restaurants.Post);
app.MapPut("/restaurants/{id}", Restaurants.Put);
app.MapDelete("/restaurants/{id}", Restaurants.Delete);
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
  string createHotels = @"
CREATE TABLE IF NOT EXISTS hotels
(
    Id INT PRIMARY KEY AUTO_INCREMENT,
    location_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    has_breakfast BOOLEAN NOT NULL,
    address VARCHAR(255),
    FOREIGN KEY (location_id) REFERENCES locations(Id)
);";
  await MySqlHelper.ExecuteNonQueryAsync(config.db, createLocations);
  await MySqlHelper.ExecuteNonQueryAsync(config.db, createCulinaryExpreriences);
  await MySqlHelper.ExecuteNonQueryAsync(config.db, createHotels);
}

//void
async Task db_reset_to_default(Config config)
{


  // string db = "server=127.0.0.1;uid=travelagency;pwd=travelagency;database=travelagency";

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
  name ENUM ('Single', 'Double', 'Suite'),
  capacity INT NOT NULL,
  price_per_night DECIMAL(10,2) NOT NULL,
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

  """;



  await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS rooms");
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

  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO `restaurants` (location_id, name, is_veggie_friendly, is_fine_dining, is_wine_focused) VALUES (1, 'roserio', 1, 1, 0), (1, 'pizza hut', 1, 0, 0), (1, 'stinas grill', 1, 1, 1), (2, 'grodans boll', 0, 0, 0);");
  await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO hotels (id, location_id, name, address, price_class, has_breakfast) VALUES(1, 1, 'SwingIn', 'Stockholsgatan', 5, 1)");
  // await MySqlHelper.ExecuteNonQueryAsync(config.db, "CALL create_password_request('edvin@example.com')");
  //, NOW() + INTERVAL 1 DAY
}

//List<Users> UsersGet()
//{
// return Users;
//}
//Users? UsersGetById(int Id)
// Test