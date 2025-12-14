namespace TravelAgency;


class Data
{

  public static async Task
  db_reset_to_default(Config config)
  {

    string users_create = """ 

  /* adding a new table to the database : */

  CREATE TABLE roles
  (
    id INT PRIMARY KEY AUTO_INCREMENT,
    role ENUM ('admin', 'client')
  );

  CREATE TABLE users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    email VARCHAR(255) NOT NULL UNIQUE,
    first_name VARCHAR(50),
    last_name VARCHAR(100),
    role_id INT NOT NULL,
    date_of_birth DATE,
    password VARCHAR(255),
    FOREIGN KEY (role_id) REFERENCES roles (id)
  );
  
    CREATE TABLE password_request
  (
    user_id INT NOT NULL,
    temp_key BINARY(16) PRIMARY KEY DEFAULT (UUID_TO_BIN(UUID())),
    expire_date DATE,
    FOREIGN KEY (user_id) REFERENCES users(id)
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

    CREATE TABLE hotels 
  (
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
  name ENUM ('Single', 'Double', 'Suite') NOT NULL,
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
  meal_type ENUM ('Breakfast', 'Lunch', 'Dinner'),
  day_offset TIMESTAMP,
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
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS bookings");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS packages_meals");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS packages");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS restaurants");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS rooms");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS hotels");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS locations");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS password_request");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS users");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS countries");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "DROP TABLE IF EXISTS roles");

    await MySqlHelper.ExecuteNonQueryAsync(config.db, users_create);

    await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO roles(role) VALUES ('admin'),('client');");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, @"
    INSERT INTO users (email, first_name, last_name, role_id, date_of_birth, password) VALUES
    ('edvin@example.com', 'Edvin', 'Lindborg', 2, '1997-08-20', 'travelagency'),
    ('admin1@example.com','Admin','One',1,'1975-01-01','adminpass'),
    ('admin2@example.com','Admin','Two',1,'1978-02-02','adminpass'),
    ('admin3@example.com','Admin','Three',1,'1980-03-03','adminpass'),
    ('admin4@example.com','Admin','Four',1,'1972-04-04','adminpass'),
    ('admin5@example.com','Admin','Five',1,'1969-05-05','adminpass'),
    ('user1@example.com','Alice','Andersson',2,'1990-06-06','password'),
    ('user2@example.com','Bob','Berg',2,'1988-07-07','password'),
    ('user3@example.com','Carl','Carlsson',2,'1992-08-08','password'),
    ('user4@example.com','Diana','Dahl',2,'1991-09-09','password'),
    ('user5@example.com','Erik','Ek',2,'1985-10-10','password'),
    ('user6@example.com','Frida','Fors',2,'1993-11-11','password'),
    ('user7@example.com','Gustav','Gustafsson',2,'1987-12-12','password'),
    ('user8@example.com','Hanna','Hansson',2,'1994-01-13','password'),
    ('user9@example.com','Isak','Ivarsson',2,'1996-02-14','password'),
    ('user10@example.com','Julia','Jonsson',2,'1995-03-15','password'),
    ('user11@example.com','Karin','Karlsson',2,'1989-04-16','password'),
    ('user12@example.com','Lars','Larsson',2,'1990-05-17','password'),
    ('user13@example.com','Maja','Magnusson',2,'1992-06-18','password'),
    ('user14@example.com','Nils','Nilsson',2,'1986-07-19','password'),
    ('user15@example.com','Olivia','Olofsson',2,'1991-08-20','password'),
    ('user16@example.com','Peter','Persson',2,'1984-09-21','password'),
    ('user17@example.com','Qarin','Quist',2,'1993-10-22','password'),
    ('user18@example.com','Rasmus','Rosen',2,'1992-11-23','password'),
    ('user19@example.com','Sara','Svensson',2,'1995-12-24','password'),
    ('user20@example.com','Tobias','Thomsson',2,'1996-01-25','password'),
    ('user21@example.com','Ulla','Ulfsson',2,'1983-02-26','password'),
    ('user22@example.com','Viktor','Vik',2,'1994-03-27','password'),
    ('user23@example.com','Wilma','Wester',2,'1990-04-28','password'),
    ('user24@example.com','Xavier','Xen',2,'1988-05-29','password'),
    ('user25@example.com','Ylva','Yng',2,'1991-06-30','password'),
    ('user26@example.com','Zlatan','Zetter',2,'1985-07-01','password'),
    ('user27@example.com','Anna','Ahl',2,'1992-08-02','password'),
    ('user28@example.com','Bertil','Bergman',2,'1987-09-03','password'),
    ('user29@example.com','Cecilia','Ceder',2,'1993-10-04','password'),
    ('user30@example.com','David','Dahlberg',2,'1994-11-05','password'),
    ('user31@example.com','Elin','Eklund',2,'1995-12-06','password'),
    ('user32@example.com','Filip','Fredriksson',2,'1996-01-07','password'),
    ('user33@example.com','Greta','Gran',2,'1997-02-08','password'),
    ('user34@example.com','Henrik','Hedlund',2,'1998-03-09','password'),
    ('user35@example.com','Ida','Isaksson',2,'1999-04-10','password'),
    ('user36@example.com','Jonas','Jansson',2,'1990-05-11','password'),
    ('user37@example.com','Klara','Kling',2,'1991-06-12','password'),
    ('user38@example.com','Leo','Lind',2,'1992-07-13','password'),
    ('user39@example.com','Mona','Mårtensson',2,'1993-08-14','password'),
    ('user40@example.com','Noah','Nord',2,'1994-09-15','password'),
    ('user41@example.com','Ola','Olsson',2,'1995-10-16','password'),
    ('user42@example.com','Pia','Pettersson',2,'1996-11-17','password'),
    ('user43@example.com','Rune','Ryd',2,'1997-12-18','password'),
    ('user44@example.com','Signe','Sjöberg',2,'1998-01-19','password'),
    ('user45@example.com','Tom','Törn',2,'1999-02-20','password'),
    ('user46@example.com','Ulf','Unger',2,'1986-03-21','password'),
    ('user47@example.com','Vera','Vikström',2,'1987-04-22','password'),
    ('user48@example.com','Wille','Wikström',2,'1988-05-23','password'),
    ('user49@example.com','Ximena','Xavier',2,'1989-06-24','password'),
    ('user50@example.com','Yusuf','Yildiz',2,'1990-07-25','password');
    ");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO countries (id, name) VALUES (1, 'Sweden'), (2, 'Norway'), (3, 'Denmark'), (4, 'Finland'), (5, 'Iceland'), (6, 'Germany'), (7, 'Netherlands'), (8, 'France'), (9, 'Spain'), (10, 'Italy')");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO locations (id, countries_id, city) VALUES (1, 1, 'Stockholm'), (2, 1, 'Malmö'), (3, 1, 'Gothenburg'), (4, 2, 'Oslo'), (5, 2, 'Bergen'), (6, 2, 'Trondheim'), (7, 3, 'Copenhagen'), (8, 3, 'Aarhus'), (9, 3, 'Odense'), (10, 4, 'Helsinki'), (11, 4, 'Espoo'), (12, 4, 'Tampere'), (13, 5, 'Reykjavik'), (14, 5, 'Akureyri'), (15, 6, 'Berlin'), (16, 6, 'Munich'), (17, 6, 'Hamburg'), (18, 7, 'Amsterdam'), (19, 7, 'Rotterdam'), (20, 7, 'Utrecht'), (21, 8, 'Paris'), (22, 8, 'Lyon'), (23, 8, 'Nice'), (24, 9, 'Madrid'), (25, 9, 'Barcelona'), (26, 9, 'Valencia'), (27, 10, 'Rome'), (28, 10, 'Milan'), (29, 10, 'Venice'), (30, 1, 'Uppsala'), (31, 1, 'Vasteras'), (32, 1, 'Linkoping'), (33, 2, 'Kristiansand'), (34, 2, 'Tromso'), (35, 3, 'Helsingor'), (36, 3, 'Roskilde'), (37, 4, 'Oulu'), (38, 5, 'Selfoss'), (39, 6, 'Frankfurt'), (40, 6, 'Stuttgart'),(41, 7, 'Eindhoven'), (42, 8, 'Bordeaux'), (43, 9, 'Seville'), (44, 9, 'Bilbao'), (45, 10, 'Naples'), (46, 10, 'Turin'), (47, 1, 'Kalmar'), (48, 1, 'Halmstad'),(49, 2, 'Fredrikstad'), (50, 3, 'Hillerod')");
    await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO hotels (id, location_id, name, address, price_class, has_breakfast) VALUES (1, 1, 'Hotel Aurora', 'Storgatan 1', 4, 1), (2, 2, 'Seaside Inn', 'Hamngatan 2', 3, 1), (3, 3, 'City Lodge', 'Centralvägen 3', 2, 0), (4, 4, 'Fjord Hotel', 'Fjordveien 4', 5, 1), (5, 5, 'Bergen Suites', 'Bryggen 5', 4, 1), (6, 6, 'Trondheim Comfort', 'Torget 6', 3, 1), (7, 7, 'Copenhagen Plaza', 'Kongens Nytorv 7', 5, 1), (8, 8, 'Aarhus Stay', 'Strøget 8', 3, 0),(9, 9, 'Odense Hotel', 'Algade 9', 2, 1), (10, 10, 'Helsinki Harbor', 'Satamakatu 10', 4, 1), (11, 11, 'Espoo Business', 'Keilaranta 11', 3, 1), (12, 12, 'Tampere Central', 'Hämeenkatu 12', 2, 0), (13, 13, 'Reykjavik Inn', 'Laugavegur 13', 3, 1), (14, 14, 'Akureyri Guesthouse', 'Hafnarstræti 14', 2, 0), (15, 15, 'Berlin Grand', 'Unter den Linden 15', 5, 1), (16, 16, 'Munich Comfort', 'Marienplatz 16', 4, 1),(17, 17, 'Hamburg Harbor', 'Reeperbahn 17', 3, 1), (18, 18, 'Amsterdam Canal', 'Prinsengracht 18', 5, 1), (19, 19, 'Rotterdam View', 'Witte de Withstraat 19', 3, 0), (20, 20, 'Utrecht Inn', 'Neude 20', 2, 1), (21, 21, 'Paris Central', 'Rue de Rivoli 21', 5, 1), (22, 22, 'Lyon Suites', 'Place Bellecour 22', 4, 1), (23, 23, 'Nice Beach Hotel', 'Promenade des Anglais 23', 4, 1), (24, 24, 'Madrid Plaza', 'Gran Via 24', 5, 1), (25, 25, 'Barcelona Stay', 'La Rambla 25', 4, 1), (26, 26, 'Valencia Comfort', 'Plaza del Ayuntamiento 26', 3, 0), (27, 27, 'Rome Classic', 'Via Veneto 27', 5, 1), (28, 28, 'Milan Fashion', 'Via Montenapoleone 28', 5, 1), (29, 29, 'Venice Canal', 'Riva degli Schiavoni 29', 4, 1), (30, 30, 'Uppsala Guest', 'Dragarbrunnsgatan 30', 2, 0), (31, 31, 'Vasteras Hotel', 'Stora Gatan 31', 2, 1), (32, 32, 'Linkoping Inn', 'Storgatan 32', 2, 0), (33, 33, 'Kristiansand Bay', 'Strandpromenaden 33', 3, 1), (34, 34, 'Tromso Arctic', 'Storgata 34', 3, 1), (35, 35, 'Helsingor Harbor', 'Helsingorvej 35', 2, 0), (36, 36, 'Roskilde Stay', 'Algade 36', 2, 0), (37, 37, 'Oulu Central', 'Isokatu 37', 3, 1), (38, 38, 'Selfoss Hotel', 'Austurvegur 38', 2, 0), (39, 39, 'Frankfurt Business', 'Zeil 39', 4, 1), (40, 40, 'Stuttgart Comfort', 'Königstraße 40', 3, 1), (41, 41, 'Eindhoven Tech', 'Strijp 41', 3, 0), (42, 42, 'Bordeaux Wine', 'Cours de l''Intendance 42', 4, 1), (43, 43, 'Seville Plaza', 'Calle Sierpes 43', 3, 1),(44, 44, 'Bilbao River', 'Gran Via 44', 3, 0),(45, 45, 'Naples Bay', 'Via Partenope 45', 4, 1), (46, 46, 'Turin Classic', 'Via Roma 46', 3, 1),(47, 47, 'Kalmar Inn', 'Stortorget 47', 2, 0),(48, 48, 'Halmstad Beach', 'Tylösand 48', 3, 1), (49, 49, 'Fredrikstad Fjord', 'Torvet 49', 2, 0),(50, 50, 'Hillerod Garden', 'Slotsgade 50', 2, 0)");

    await MySqlHelper.ExecuteNonQueryAsync(config.db, @"INSERT INTO rooms (hotel_id, room_number, name, capacity, price_per_night) 
    VALUES (1, 101, 'Single', 1, 79.00), (2, 102, 'Double', 2, 99.00), (3, 103, 'Single', 1, 69.50), (4, 104, 'Suite', 4, 249.00), (5, 105, 'Double', 2, 129.00), 
    (6, 106, 'Single', 1, 85.00), (7, 107, 'Suite', 3, 299.00), (8, 108, 'Double', 2, 109.00), (9, 109, 'Single', 1, 75.00), (10, 110, 'Double', 2, 139.00), 
    (11, 111, 'Single', 1, 82.00), (12, 112, 'Double', 2, 95.00), (13, 113, 'Single', 1, 88.00), (14, 114, 'Double', 2, 92.00), (15, 115, 'Suite', 4, 320.00), (16, 116, 'Double', 2, 150.00), 
    (17, 117, 'Single', 1, 80.00), (18, 118, 'Suite', 3, 280.00), (19, 119, 'Double', 2, 110.00), (20, 120, 'Single', 1, 70.00), (21, 121, 'Suite', 4, 350.00), (22, 122, 'Double', 2, 145.00), (23, 123, 'Single', 1, 78.00), 
    (24, 124, 'Suite', 3, 260.00), (25, 125, 'Double', 2, 155.00), (26, 126, 'Single', 1, 74.00), (27, 127, 'Double', 2, 165.00), (28, 128, 'Suite', 4, 340.00), (29, 129, 'Single', 1, 90.00), 
    (30, 130, 'Double', 2, 99.00), (31, 131, 'Single', 1, 68.00), (32, 132, 'Double', 2, 88.00), (33, 133, 'Single', 1, 76.00), (34, 134, 'Double', 2, 120.00), (35, 135, 'Single', 1, 71.00), (36, 136, 'Double', 2, 89.00), 
    (37, 137, 'Single', 1, 73.00), (38, 138, 'Double', 2, 85.00), (39, 139, 'Suite', 3, 210.00), (40, 140, 'Double', 2, 115.00), (41, 141, 'Single', 1, 67.00), (42, 142, 'Double', 2, 98.00), (43, 143, 'Single', 1, 72.00), 
    (44, 144, 'Double', 2, 94.00), (45, 145, 'Suite', 4, 230.00), (46, 146, 'Double', 2, 110.00), (47, 147, 'Single', 1, 66.00), (48, 148, 'Double', 2, 102.00), (49, 149, 'Single', 1, 69.00), (50, 150, 'Double', 2, 87.00);");

    await MySqlHelper.ExecuteNonQueryAsync(config.db, @"INSERT INTO restaurants (location_id, name, is_veggie_friendly, is_fine_dining, is_wine_focused) 
    VALUES (1, 'roserio', 1, 1, 0), (1, 'pizza hut', 1, 0, 0), (1, 'stinas grill', 1, 1, 1), (2, 'grodans boll', 0, 0, 0),(1,'Rosario Bistro',1,0,0),
    (2,'Seaside Pizza',1,0,0),(3,'Gothenburg Grill',0,0,1),(4,'Oslo Fine',1,1,1),(5,'Bryggen Cafe',1,0,0),
    (6,'Trondheim Taverna',0,0,0),(7,'Copenhagen Table',1,1,1),(8,'Aarhus Kitchen',1,0,0),(9,'Odense Deli',1,0,0),(10,'Helsinki Harbor',0,1,1),
    (11,'Espoo Eats',1,0,0),(12,'Tampere Tapas',1,0,0),(13,'Reykjavik Fish',0,1,0),(14,'Akureyri Cafe',1,0,0),(15,'Berlin Brasserie',1,1,1),
    (16,'Munich Biergarten',0,0,1),(17,'Hamburg Harbor Grill',1,0,1),(18,'Amsterdam Pancakes',1,0,0),(19,'Rotterdam Fusion',1,1,1),(20,'Utrecht Corner',1,0,0),
    (21,'Paris Gourmet',1,1,1),(22,'Lyon Bistro',1,1,1),(23,'Nice Seafood',0,1,1),(24,'Madrid Tapas',1,0,1),(25,'Barcelona Beach',1,0,1),
    (26,'Valencia Paella',0,1,0),(27,'Rome Trattoria',1,0,1),(28,'Milan Ristorante',1,1,1),(29,'Venice Osteria',1,1,0),(30,'Uppsala Cafe',1,0,0),
    (31,'Vasteras Diner',1,0,0),(32,'Linkoping Kitchen',1,0,0),(33,'Kristiansand Fish',0,1,0),(34,'Tromso Arctic',1,0,0),(35,'Helsingor Harbor',1,0,0),
    (36,'Roskilde Grill',1,0,0),(37,'Oulu Cafe',1,0,0),(38,'Selfoss Bistro',1,0,0),(39,'Frankfurt Steak',0,1,1),(40,'Stuttgart Table',1,0,1),
    (41,'Eindhoven Eats',1,0,0),(42,'Bordeaux Winebar',1,1,1),(43,'Seville Tapas',1,0,1),(44,'Bilbao Pintxos',1,0,1),(45,'Naples Pizza',1,0,0),
    (46,'Turin Truffle',1,1,1),(47,'Kalmar Harbor',1,0,0),(48,'Halmstad Beach Cafe',1,0,0),(49,'Fredrikstad Fjord',1,0,0),(50,'Hillerod Garden',1,0,0);");

    await MySqlHelper.ExecuteNonQueryAsync(config.db, @"
    INSERT INTO packages (location_id, name, description, package_type) VALUES
    (1, 'package_fish', 'a nice fish package for the drunken *hick*', 'Fish'),(1, 'Stockholm Veggie', 'Veggie weekend in Stockholm', 'Veggie'),
    (2, 'Malmoe Fish', 'Seafood special in Malmoe', 'Fish'),(3, 'Gothenburg Gourmet', 'Fine dining in Gothenburg', 'Fine dining'),
    (4, 'Oslo Explorer', 'City break in Oslo', 'Fish'),(5, 'Bergen Fjord', 'Fjord and food', 'Veggie'),
    (6, 'Trondheim Culture', 'Museums and meals', 'Fine dining'),(7, 'Copenhagen Classic', 'Canals and cuisine', 'Fine dining'),
    (8, 'Aarhus Relax', 'Cozy stay in Aarhus', 'Veggie'), (9, 'Odense Family', 'Family friendly package', 'Fish'),
    (10, 'Helsinki Winter', 'Northern lights and sauna', 'Fine dining'),(11, 'Espoo Escape', 'Weekend escape', 'Veggie'),
    (12, 'Tampere Taste', 'Local flavors', 'Fish'),(13, 'Reykjavik Adventure', 'Icelandic nature', 'Veggie'),
    (14, 'Akureyri Calm', 'Small town charm', 'Fish'),
    (15, 'Berlin Nights', 'City nightlife', 'Fine dining'),
    (16, 'Munich Beer', 'Oktoberfest style', 'Fish'),
    (17, 'Hamburg Harbor', 'Harbor walks and food', 'Veggie'),
    (18, 'Amsterdam Canals', 'Bike and dine', 'Fine dining'),
    (19, 'Rotterdam Modern', 'Architecture and meals', 'Fish'),
    (20, 'Utrecht Quiet', 'Historic canals', 'Veggie'),
    (21, 'Paris Romance', 'Romantic dinners', 'Fine dining'),
    (22, 'Lyon Food', 'Gastronomy tour', 'Fine dining'),
    (23, 'Nice Sun', 'Beach and seafood', 'Fish'),
    (24, 'Madrid Culture', 'Museums and tapas', 'Fine dining'),
    (25, 'Barcelona Art', 'Gaudi and gastronomy', 'Fish'),
    (26, 'Valencia Paella', 'Paella weekend', 'Fish'),
    (27, 'Rome History', 'Ancient sites and food', 'Fine dining'),
    (28, 'Milan Fashion', 'Shopping and dining', 'Fine dining'),
    (29, 'Venice Romance', 'Canals and cuisine', 'Fine dining'),
    (30, 'Uppsala Study', 'University town break', 'Veggie'),
    (31, 'Vasteras Calm', 'Lake views', 'Veggie'),
    (32, 'Linkoping Tech', 'Innovation and food', 'Fish'),
    (33, 'Kristiansand Coast', 'Coastal escape', 'Veggie'),
    (34, 'Tromso Lights', 'Northern lights package', 'Fish'),
    (35, 'Helsingor Castle', 'Castle and cuisine', 'Fine dining'),
    (36, 'Roskilde Festival', 'Music and meals', 'Veggie'),
    (37, 'Oulu Winter', 'Snow and sauna', 'Fish'),
    (38, 'Selfoss Nature', 'Icelandic countryside', 'Veggie'),
    (39, 'Frankfurt Business', 'Business friendly', 'Fine dining'),
    (40, 'Stuttgart Vine', 'Wine region', 'Fine dining'),
    (41, 'Eindhoven Tech', 'Design and dining', 'Veggie'),
    (42, 'Bordeaux Wine', 'Wine tasting', 'Fine dining'),
    (43, 'Seville Flamenco', 'Culture and tapas', 'Fish'),
    (44, 'Bilbao Art', 'Guggenheim visit', 'Fine dining'),
    (45, 'Naples Coast', 'Coastline and pizza', 'Fish'),
    (46, 'Turin Truffle', 'Gourmet truffle tour', 'Fine dining'),
    (47, 'Kalmar Island', 'Island hopping', 'Veggie'),(48, 'Halmstad Surf', 'Beach and surf', 'Fish'),
    (49, 'Fredrikstad Fort', 'Historic fort visit', 'Veggie'),(50, 'Hillerod Palace', 'Palace and gardens', 'Fine dining');
    ");

    await MySqlHelper.ExecuteNonQueryAsync(config.db, "INSERT INTO packages_meals(package_id, restaurant_id, meal_type, day_offset) VALUES(1, 3, 'Lunch', '2025-12-24');");

    await MySqlHelper.ExecuteNonQueryAsync(config.db, @"INSERT INTO bookings (user_id, location_id, hotel_id, package_id, check_in, check_out, guests, rooms, status, total_price) 
    VALUES (1,3,1,1, '2025-12-24', '2026-01-01', 2, 1, 'pending', 1000.00),(1,1,1,1,'2025-06-01','2025-06-05',2,1,'confirmed',499.00),(2,2,2,2,'2025-06-02','2025-06-06',1,1,'pending',299.00),
    (3,3,3,3,'2025-06-03','2025-06-07',2,1,'confirmed',399.00),(4,4,4,4,'2025-06-04','2025-06-08',3,2,'cancelled',0.00),(5,5,5,5,'2025-06-05','2025-06-09',2,1,'confirmed',599.00),(6,6,6,6,'2025-06-06','2025-06-10',1,1,'pending',199.00),
    (7,7,7,7,'2025-06-07','2025-06-11',2,1,'confirmed',699.00),(8,8,8,8,'2025-06-08','2025-06-12',2,1,'confirmed',349.00),(9,9,9,9,'2025-06-09','2025-06-13',4,2,'pending',899.00),(10,10,10,10,'2025-06-10','2025-06-14',2,1,'confirmed',459.00),
    (11,11,11,11,'2025-06-11','2025-06-15',1,1,'confirmed',219.00),(12,12,12,12,'2025-06-12','2025-06-16',2,1,'pending',329.00),(12,12,12,12,'2025-06-12','2025-06-16',2,1,'pending',329.00),(12,12,12,12,'2025-06-12','2025-06-16',2,1,'pending',329.00),
    (12,12,12,12,'2025-06-12','2025-06-16',2,1,'pending',329.00),(12,12,12,12,'2025-06-12','2025-06-16',2,1,'pending',329.00),(12,12,12,12,'2025-06-12','2025-06-16',2,1,'pending',329.00),(12,12,12,12,'2025-06-12','2025-06-16',2,1,'pending',329.00),
    
    (13,13,13,13,'2025-06-13','2025-06-17',2,1,'confirmed',389.00),(14,14,14,14,'2025-06-14','2025-06-18',3,2,'confirmed',799.00),(15,15,15,15,'2025-06-15','2025-06-19',2,1,'pending',259.00),(16,16,16,16,'2025-06-16','2025-06-20',2,1,'confirmed',499.00),
    (17,17,17,17,'2025-06-17','2025-06-21',1,1,'confirmed',199.00),(18,18,18,18,'2025-06-18','2025-06-22',2,1,'pending',349.00),(19,19,19,19,'2025-06-19','2025-06-23',2,1,'confirmed',429.00),(20,20,20,20,'2025-06-20','2025-06-24',3,2,'confirmed',899.00),
    (21,21,21,21,'2025-06-21','2025-06-25',2,1,'pending',559.00),(22,22,22,22,'2025-06-22','2025-06-26',1,1,'confirmed',239.00),(23,23,23,23,'2025-06-23','2025-06-27',2,1,'confirmed',379.00),(24,24,24,24,'2025-06-24','2025-06-28',2,1,'pending',449.00),
    (25,25,25,25,'2025-06-25','2025-06-29',4,2,'confirmed',999.00),(26,26,26,26,'2025-06-26','2025-06-30',2,1,'confirmed',329.00),(27,27,27,27,'2025-06-27','2025-07-01',2,1,'pending',359.00),(28,28,28,28,'2025-06-28','2025-07-02',1,1,'confirmed',199.00),
    (29,29,29,29,'2025-06-29','2025-07-03',2,1,'confirmed',419.00),(30,30,30,30,'2025-06-30','2025-07-04',2,1,'pending',289.00),(31,31,31,31,'2025-07-01','2025-07-05',3,2,'confirmed',749.00),(32,32,32,32,'2025-07-02','2025-07-06',2,1,'confirmed',339.00),
    (33,33,33,33,'2025-07-03','2025-07-07',2,1,'pending',379.00),(34,34,34,34,'2025-07-04','2025-07-08',1,1,'confirmed',199.00),(35,35,35,35,'2025-07-05','2025-07-09',2,1,'confirmed',429.00),(36,36,36,36,'2025-07-06','2025-07-10',2,1,'pending',319.00),
    (37,37,37,37,'2025-07-07','2025-07-11',3,2,'confirmed',799.00),(38,38,38,38,'2025-07-08','2025-07-12',2,1,'confirmed',359.00),(39,39,39,39,'2025-07-09','2025-07-13',1,1,'pending',219.00),(40,40,40,40,'2025-07-10','2025-07-14',2,1,'confirmed',449.00),
    (41,41,41,41,'2025-07-11','2025-07-15',2,1,'confirmed',329.00),(42,42,42,42,'2025-07-12','2025-07-16',1,1,'pending',199.00), (43,43,43,43,'2025-07-13','2025-07-17',2,1,'confirmed',379.00), (44,44,44,44,'2025-07-14','2025-07-18',2,1,'confirmed',399.00), 
    (45,45,45,45,'2025-07-15','2025-07-19',3,2,'pending',899.00), (46,46,46,46,'2025-07-16','2025-07-20',2,1,'confirmed',459.00),(47,47,47,47,'2025-07-17','2025-07-21',2,1,'confirmed',299.00), (48,48,48,48,'2025-07-18','2025-07-22',1,1,'pending',189.00),
    (49,49,49,49,'2025-07-19','2025-07-23',2,1,'confirmed',349.00),(50,50,50,50,'2025-07-20','2025-07-24',2,1,'confirmed',379.00);
    ");

    await MySqlHelper.ExecuteNonQueryAsync(config.db, @"INSERT INTO booking_meals (bookings_id, date, meal_type) VALUES
    (1,'2025-06-02','Breakfast'),(2,'2025-06-03','Lunch'),(3,'2025-06-04','Dinner'),(4,'2025-06-05','Lunch'),
    (5,'2025-06-06','Dinner'),(6,'2025-06-07','Breakfast'),(7,'2025-06-08','Dinner'),(8,'2025-06-09','Lunch'),
    (9,'2025-06-10','Dinner'),(10,'2025-06-11','Breakfast'),(11,'2025-06-12','Lunch'),(12,'2025-06-13','Breakfast'),
    (13,'2025-06-14','Dinner'),(14,'2025-06-15','Lunch'),(15,'2025-06-16','Dinner'),(16,'2025-06-17','Breakfast'),
    (17,'2025-06-18','Lunch'),(18,'2025-06-19','Dinner'),(19,'2025-06-20','Breakfast'),(20,'2025-06-21','Lunch'),
    (21,'2025-06-22','Dinner'),(22,'2025-06-23','Breakfast'),(23,'2025-06-24','Lunch'),(24,'2025-06-25','Dinner'),
    (25,'2025-06-26','Breakfast'),(26,'2025-06-27','Lunch'),(27,'2025-06-28','Dinner'),(28,'2025-06-29','Breakfast'),
    (29,'2025-06-30','Lunch'),(30,'2025-07-01','Dinner'),(31,'2025-07-02','Breakfast'),(32,'2025-07-03','Lunch'),
    (33,'2025-07-04','Dinner'),(34,'2025-07-05','Breakfast'),(35,'2025-07-06','Lunch'),(36,'2025-07-07','Dinner'),
    (37,'2025-07-08','Breakfast'),(38,'2025-07-09','Lunch'),(39,'2025-07-10','Dinner'),(40,'2025-07-11','Breakfast'),
    (41,'2025-07-12','Lunch'),(42,'2025-07-13','Dinner'),(43,'2025-07-14','Breakfast'),(44,'2025-07-15','Lunch'),
    (45,'2025-07-16','Dinner'),(46,'2025-07-17','Breakfast'),(47,'2025-07-18','Lunch'),(48,'2025-07-19','Dinner'),
    (49,'2025-07-20','Breakfast'),(50,'2025-07-21','Lunch');");


    // await MySqlHelper.ExecuteNonQueryAsync(config.db, "CALL create_password_request('edvin@example.com')");
    //, NOW() + INTERVAL 1 DAY
  }
}