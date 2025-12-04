using MySql.Data.MySqlClient;
using TravelAgency;

var builder = WebApplication.CreateBuilder(args);

Config config = new("server=127.0.0.1;uid=root;pwd=rootroot;database=test");
builder.Services.AddSingleton<Config>(config);
builder.Services.AddDistributedMemoryCache();



var app = builder.Build();

app.MapPost("/register", Users.Post);


app.MapGet("/", () => "Hello World!");

app.Run();
