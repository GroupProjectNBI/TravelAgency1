namespace TravelAgency;

using MySql.Data.MySqlClient;

class Experiences
{
    // represents one planned meal on a specific date
    // restaurant_id/name are null when the breakfast is included
    public record MealPlanItem(DateOnly date, string meal_type, int? restaurant_id, string? restaurant_name);

    // Basic hotel availability + price estimate without the meal plan
    public record HotelOffer(
        int hotel_id,
        string name,
        string address,
        int price_class,
        bool has_breakfast,
        int max_rooms,
        int available_rooms,
        int nights,
        decimal estimated_price,
        decimal min_price_per_night
    );

    // this method represents when the client searches for available stays when a package is chosen and all the info about the stay is provided by the client
    // one available hotel for the stay + one restaurant choice based on package category + generated meal plan
    public record ExperienceOffer(
        int offer_id,
        string package,
        int hotel_id,
        string hotel_name,
        int price_class,
        bool has_breakfast,
        int available_rooms,
        int nights,
        decimal min_price_per_night,
        decimal estimated_price,
        List<MealPlanItem> meals
    );


    //helper to get a list of restaurants for the selected package category
    // note the $ sign is intentional because where is chosen from the switch so no risk for sql injections
    static async Task<List<(int id, string name)>> GetRestaurantsByPackage(
        int locationId, string package, int take, Config config)
    {
        string where = package switch
        {
            "Veggie" => "is_veggie_friendly = 1",
            "Fine dining" => "is_fine_dining =1",
            "Wine" => "is_wine_focused = 1",
            _ => "1=0" //unknown package => no matches
        };

        string query = $"""
            SELECT id, name
            FROM restaurants
            WHERE location_id = @loc
            AND {where}
            ORDER BY id
            LIMIT @take;
        """;

        var parameters = new MySqlParameter[]
        {
            new("@loc", locationId),
            new("@take", take)
        };

        var result = new List<(int id, string name)>();
        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);
        while (reader.Read())
            result.Add((reader.GetInt32(0), reader.GetString(1)));

        return result;
    }

    //Returns available hotels for a location during the time chosen
    //availability is checked by tracking rooms (pending/confirmed overlapping bookings)
    //from the hotel's max_rooms
    public static async Task<List<HotelOffer>> SearchHotels(
        int locationId, DateOnly checkIn, DateOnly checkOut, int roomsWanted, Config config)
    {
        //connects all the rooms to the hotel
        //to find the lowest price per night (MIN(price_per_night))
        //to find the biggest room (MAX(capacity))
        string query = """
        SELECT
        h.id,
        h.name,
        h.address,
        h.price_class,
        h.has_breakfast,
        h.max_rooms,
        (h.max_rooms - IFNULL(SUM(b.rooms), 0)) AS available_rooms,
        MIN(r.price_per_night) AS min_price_per_night
        FROM hotels h
        LEFT JOIN bookings b
        ON b.hotel_id = h.id
        AND b.status IN ('pending', 'confirmed')
        AND b.check_in < @checkOut
        AND b.check_out > @checkIn
        LEFT JOIN rooms r
        ON r.hotel_id = h.id
        WHERE h.location_id = @locationId
        GROUP BY h.id
        HAVING available_rooms >= @roomsWanted
        """;

        var parameters = new MySqlParameter[]
        {
            new("@locationId", locationId),

            new MySqlParameter("@checkIn", MySqlDbType.Date)
            {Value = checkIn.ToDateTime(TimeOnly.MinValue)},

            new MySqlParameter("@checkOut", MySqlDbType.Date)
            {Value = checkOut.ToDateTime(TimeOnly.MinValue)},

            new("@roomsWanted", roomsWanted),
        };

        var result = new List<HotelOffer>();
        int nights = (checkOut.ToDateTime(TimeOnly.MinValue) - checkIn.ToDateTime(TimeOnly.MinValue)).Days;
        if (nights <= 0) nights = 1; //safetycheck

        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, query, parameters);
        while (reader.Read())
        {
            decimal minPrice = reader.IsDBNull(7) ? 0m : reader.GetDecimal(7);
            decimal estimated = minPrice * roomsWanted * nights;

            result.Add(new HotelOffer(
                hotel_id: reader.GetInt32(0),
                name: reader.GetString(1),
                address: reader.GetString(2),
                price_class: reader.GetInt32(3),

                has_breakfast: reader.GetBoolean(4),

                max_rooms: reader.GetInt32(5),
                available_rooms: reader.GetInt32(6),
                nights: nights,
                estimated_price: estimated,
                min_price_per_night: minPrice

                ));
        }

        return result;
    }

    //returns a list of "experience offers" based on:
    //hotel availability
    //price class filter
    //guests capacity roomswanted * max_room_capacity
    //selected package category Veggie/fine dining/ wine package
    //each offer is coombination of one hotel + one matching restaurant option
    public static async Task<List<ExperienceOffer>> SearchOffers(
        int locationId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsWanted,
        int guests,
        int maxPriceClass,
        string package,
        int limit,
        Config config
    )
    {
        // select hotels in a specific location that can meet the requested
        //number of rooms and guests during the stay
        // joins bookings to fins rooms that overlap
        //joins rooms to calculater minimum price per night and max room capacity
        //filters by location and maximum price class
        //ensores enough available room and capacity
        string hotelQuery = """
        SELECT
        h.id,
        h.name,
        h.price_class,
        h.has_breakfast,
        h.max_rooms,
        (h.max_rooms - IFNULL(SUM(b.rooms), 0)) AS available_rooms,
        MIN(r.price_per_night) AS min_price_per_night,
        MAX(r.capacity) AS max_room_capacity
        FROM hotels h
        LEFT JOIN bookings b
        ON b.hotel_id = h.id
        AND b.status IN ('pending', 'confirmed')
        AND b.check_in < @checkOut
        AND b.check_out > @checkIn
        LEFT JOIN rooms r
        ON r.hotel_id = h.id
        WHERE h.location_id = @locationId
        AND h.price_class <= @maxPriceClass
        GROUP BY h.id
        HAVING available_rooms >= @roomsWanted
        AND (@guests <= (@roomsWanted * IFNULL(max_room_capacity, 0)));
        """;

        var hotelParams = new MySqlParameter[]
        {
            new("@locationId", locationId),
            new("@maxPriceClass", maxPriceClass),
            new("@roomsWanted", roomsWanted),
            new("@guests", guests),

            new MySqlParameter("@checkIn", MySqlDbType.Date)
            {Value = checkIn.ToDateTime(TimeOnly.MinValue)},

            new MySqlParameter("@checkOut", MySqlDbType.Date)
            {Value = checkOut.ToDateTime(TimeOnly.MinValue)},
        };

        int nights = (checkOut.ToDateTime(TimeOnly.MinValue) - checkIn.ToDateTime(TimeOnly.MinValue)).Days;
        if (nights <= 0) nights = 1; //safetycheck

        //restaurants matching the package
        var restaurants = await GetRestaurantsByPackage(locationId, package, take: 5, config: config);
        if (restaurants.Count == 0)
            return new List<ExperienceOffer>();// no matching restaurants

        //build offers for each available hotel + multiple restaurants = multiple options
        var offers = new List<ExperienceOffer>();
        int offerId = 1;

        using var reader = await MySqlHelper.ExecuteReaderAsync(config.db, hotelQuery, hotelParams);

        while (reader.Read() && offers.Count < limit)
        {
            int hotelId = reader.GetInt32(0);
            string hotelName = reader.GetString(1);
            int priceClass = reader.GetInt32(2);
            bool hasBreakFast = reader.GetBoolean(3);
            int maxRooms = reader.GetInt32(4); //not using yet but exists
            int availableRooms = reader.GetInt32(5);
            decimal minPrice = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6);

            foreach (var rest in restaurants)
            {
                if (offers.Count >= limit) break;

                var meals = new List<MealPlanItem>();

                //arrival dinner
                meals.Add(new MealPlanItem(checkIn, "Dinner", rest.id, rest.name));

                //stays lunch + dinner + breakfast if hotel doesn't have it
                for (var d = checkIn.AddDays(1); d < checkOut.AddDays(-1); d = d.AddDays(1))
                {
                    meals.Add(new MealPlanItem(d, "Lunch", rest.id, rest.name));
                    meals.Add(new MealPlanItem(d, "Dinner", rest.id, rest.name));
                }

                //Breakfast during stay and departure, at the hotel if breakfast included, at restaurant if not
                var breakFastStart = checkIn.AddDays(1);
                var breakFastEnd = checkOut.AddDays(-1);

                for (var d = breakFastStart; d <= breakFastEnd; d = d.AddDays(1))
                {
                    if (hasBreakFast)
                        meals.Add(new MealPlanItem(d, "Breakfast", null, "Hotel Breakfast"));
                    else
                        meals.Add(new MealPlanItem(d, "Breakfast", rest.id, rest.name));
                }


                decimal estimated = minPrice * roomsWanted * nights;

                offers.Add(new ExperienceOffer(
                    offer_id: offerId++,

                    package: package,
                    hotel_id: hotelId,
                    hotel_name: hotelName,
                    price_class: priceClass,
                    has_breakfast: hasBreakFast,
                    available_rooms: availableRooms,
                    nights: nights,
                    min_price_per_night: minPrice,
                    estimated_price: estimated,
                    meals: meals
                ));
            }
        }
        return offers;
    }
}