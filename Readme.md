# TravelAgency API

Detta är ett backend-projekt byggt med **C# Minimal API** och **MySQL**. API:et hanterar bokningar, hotell, rum, paketresor och användare för en resebyrå.

Projektet använder en anpassad autentiseringslösning med Sessions och Middleware för att hantera roller (Admin, Client, etc.).

## 📋 Krav (Requirements)

För att kunna köra projektet behöver du ha följande installerat på din dator:

1.  **C# / .NET SDK**
    * .NET 9.0 (eller nyare).
    * [Ladda ner här](https://dotnet.microsoft.com/download)
2.  **MySQL Server**
    * Du behöver en lokal eller extern MySQL-databas som körs.
    * [Ladda ner MySQL Community Server](https://dev.mysql.com/downloads/mysql/)
3.  **Kodeditor**
    * Visual Studio 2022, Visual Studio Code, eller Rider.
4.  **API Client** (För att testa endpoints)
    * Thunder Client (VS Code extension), Postman.

---

## ⚙️ Installation & Konfiguration

### 1. Klona eller ladda ner projektet
Öppna en terminal i projektmappen.
```bash
git clone: git@github.com:GroupProjectNBI/TravelAgency1.git
```
### 2. Sätt upp Databasen (MySQL)
Öppna din MySQL-klient (t.ex. MySQL Workbench eller terminalen) och kör följande kommandon för att skapa databasen, användaren och tabellerna.

**Steg A: Skapa databas och användare**
```sql
CREATE DATABASE <DIN_DATABAS>;


CREATE USER '<DITT_USER>'@'localhost' IDENTIFIED BY '<DITT_LÖSENORD>';

GRANT ALL PRIVILEGES ON <DIN_DATABAS>.* TO '<DITT_USER>'@'localhost';

FLUSH PRIVILEGES;
```


### 3. Uppdatera Konfigurationen

Öppna filen `Program.cs` och leta upp raden där databaskopplingen (Connection String) sätts. Se till att `uid` (användarnamn) och `pwd` (lösenord) stämmer överens med din lokala MySQL-installation.

```csharp
// I Program.cs
Config config = new("server=127.0.0.1;uid=DITT_USER;pwd=DITT_LÖSENORD;database=<din_databas>");
```
### 4. Installera beroenden (Packages)
För att projektet ska fungera måste du installera MySQL-kopplingen. Kör följande kommando i din terminal:

```bash
# Installera MySQL Data (Version 9.5.0)
dotnet add package MySql.Data --version 9.5.0

# Återställ beroenden
dotnet restore
```

## 🚀 Hur man startar (Run)

```bash
dotnet run
```
Du bör se texten Now listening on: http://localhost:5xxx i terminalen.

## 🔐 Autentisering (Så funkar inloggning)
### Detta API använder Sessions-baserad autentisering.

**Logga in**: Skicka en POST till /login med email och lösenord.

**Om lyckat**: Servern sätter en Cookie i din webbläsare/API-klient.

**Access**: *Mellanlagret (SessionAuthMiddleware)* läser kakan vid varje anrop och ger dig behörighet baserat på din roll i databasen.

### Roller:

*Admin* - Har full tillgång (kan resetta DB, hantera användare).

*Client* - Kan boka resor och se sina bokningar.

*Guest (Ej inloggad)* - Kan söka resor och se hotell.


## 📡 Exempel på Endpoints
| Metod  | Endpoint    | Beskrivning              | Behörighet |
| :----- | :---------- | :----------------------- | :--------- |
| POST   | `/register` | Skapa ny användare       | Alla       |
| POST   | `/login`    | Logga in användare       | Alla       |
| GET    | `/locations`| Hämta alla destinationer | Alla       |
| GET    | `/trips`    | Sök resor                | Alla       |
| POST   | `/bookings` | Boka en resa             | Client     |
| DELETE | `/db`       | Återställ databasen      | Admin      |

## 🛠 Felsökning
- Error 403 Forbidden: Du är inloggad men har fel roll. (Kontrollera om rollen heter "admin" eller "Admin" i databasen - systemet är skiftlägeskänsligt).

- Database Connection Error: Kontrollera att MySQL-servern är igång och att uppgifterna i Program.cs stämmer exakt med det du skapade i SQL.

- Session fungerar inte: Om du använder Thunder Client/Postman, se till att Cookies är aktiverat i inställningarna.