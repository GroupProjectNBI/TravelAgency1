# **TravelAgency Project Architecture**

#### **1. Overview**

The **TravelAgency** system is a web-based travel booking platform built with **C#** and **ASP.NET Core**, using **MySQL** as the database. It supports user authentication, hotel and package management, meal plans, experience-based bookings, and secure booking lifecycle handling.

The system is organized into modular domain-focused classes:

- **Users & Authentication**
- **Locations & Countries**
- **Hotels & Rooms**
- **Packages & Package Meals**
- **Experiences (Offers & Bookings)**
- **Profiles & User-Owned Data**
- **Sessions, Claims & Middleware**

#### **1.1 High-Level Architecture Diagram**

```mermaid
flowchart LR
    User --> API
    API --> Auth[Session + Claims]
    API --> DB[(MySQL)]
    Auth --> API
 ```


---

#### **2. Technology Stack**

| Layer              | Technology / Library                                    |
| ------------------ | -------------------------------------------------------- |
| Backend            | C#, ASP.NET Core Minimal API                             |
| Database           | MySQL                                                    |
| Data Access        | MySql.Data.MySqlClient, MySqlHelper                      |
| Session Management | ASP.NET Core Session                                     |
| Security           | Claims-based authentication via SessionAuthMiddleware   |
| API Responses      | Minimal APIs (`IResult`, JSON)                           |

---

#### **3. Database Entities**

##### **3.1 Users**

- **Table:** `users`
- **Fields:**  
  `id`, `email`, `first_name`, `last_name`, `date_of_birth`, `password`, `role_id`

- **Functionality:**
  - Registration with validation
  - Email uniqueness enforcement
  - Login via session
  - Password reset via temporary tokens
  - CRUD operations

---

##### **3.2 Roles**

- **Table:** `roles`
- **Fields:** `id`, `role`
- Linked to users to support authorization and role-based access.

---

##### **3.3 Password Reset**

- **Table:** `password_request`
- **Fields:** `user_id`, `temp_key`, `expire_date`

- **Flow:**
  - Token generated via stored procedure
  - Token stored as `UUID_TO_BIN`
  - Expiration date validated
  - Password update + token deletion handled transactionally

---

##### **3.4 Locations & Countries**

- **Tables:** `locations`, `countries`
- `locations` includes `city` and `countries_id`
- `countries` stores country names
- Supports:
  - CRUD operations
  - City search using SQL `LIKE`

---

##### **3.5 Hotels & Rooms**

- **Tables:** `hotels`, `rooms`
- **Hotels**
  - `name`, `address`, `price_class`, `has_breakfast`, `max_rooms`
- **Rooms**
  - `name`, `capacity`, `price_per_night`

- Supports:
  - CRUD operations
  - Availability checks
  - Capacity-based booking logic

---

##### **3.6 Packages & Meals**

- **Tables:** `packages`, `packages_meals`
- **Packages**
  - `name`, `description`, `package_type`, `location_id`
- **Package Meals**
  - `restaurant_id`
  - `day_kind` (`Arrival`, `Stay`, `Departure`)
  - `meal_type` (`Breakfast`, `Lunch`, `Dinner`)

- Business rules enforced:
  - Arrival → Dinner only
  - Departure → Breakfast only
  - Prevents invalid or duplicate meal combinations

---

##### **3.7 Restaurants**

- **Table:** `restaurants`
- Linked to:
  - Package meals
  - Experience offers
  - Booking meals

---

##### **3.8 Bookings**

- **Table:** `bookings`
- Fields include:
  - `user_id`, `hotel_id`, `package_id`, `location_id`
  - `check_in`, `check_out`
  - `status` (`pending`, `confirmed`)

- Represents the lifecycle of a user’s reservation

---

##### **3.9 Booking Meals**

- **Table:** `booking_meals`
- Generated when a booking is created from an experience
- Stores finalized meal plan

---

#### **4. Application Layers**

##### **4.1 Data Access Layer**

- Direct SQL access using:
  - `ExecuteReaderAsync`
  - `ExecuteScalarAsync`
  - `ExecuteNonQueryAsync`

- Each domain has a dedicated class:
  - `Users`, `Login`, `Profile`
  - `Locations`, `Hotels`, `Rooms`
  - `Packages`, `PackageMeals`
  - `Experiences`, `Bookings`

---

##### **4.2 Business Logic Layer**

- Implemented inside each domain class
- Handles:
  - Validation (email, password, ownership)
  - Availability checks
  - Booking rules
  - Status transitions
  - Aggregation of data across tables

---

##### **4.3 Session & Authentication Layer**

- `Login` manages session creation and destruction
- `SessionAuthMiddleware`:
  - Reads session values
  - Converts them into `ClaimsPrincipal`
- Claims used:
  - `ClaimTypes.NameIdentifier` → user_id
  - `ClaimTypes.Role` → role

  ##### **4.4 Architecture Style**
  The system follows a **Modular Monolithic Architecture**:

- Single ASP.NET Core application
- Clear separation of domain logic per entity
- Shared database
- No microservices (by design)

This approach was chosen for simplicity, maintainability, and ease of development.

---

##### **4.5 Error Handling & HTTP Semantics**

- 200 / 204 → Successful operations
- 400 → Validation errors
- 401 → Not authenticated
- 403 → Not authorized (ownership violation)
- 404 → Resource not found
- 409 → Conflict (invalid state transitions)

All endpoints return JSON responses using `IResult`.

---

##### **4.6 Authorization Strategy**

- Session-based authentication
- Claims-based authorization
- Ownership enforced at database level
- Role support via `roles` table (admin/user ready)
---
#### 5. API Endpoints (Summary)
| Entity       | Supported Actions                                   |
| ------------ | --------------------------------------------------- |
| Users        | GET, POST, PATCH (password), DELETE, Reset password |
| Login        | POST (login), DELETE (logout)                       |
| Profile      | GET (profile), GET (my packages)                    |
| Locations    | GET all, GET by id, POST, DELETE, Search            |
| Hotels       | GET all, GET by id, POST, PUT, DELETE               |
| Rooms        | GET all, GET by id, GET by hotel, POST, PUT, DELETE |
| Packages     | GET all, GET by id, POST, PUT, DELETE, GetDetails   |
| PackageMeals | GET all, POST, PUT, DELETE                          |
| Experiences  | SearchOffers, SearchHotels, BookFromExperienceOffer |
| Bookings     | POST, DELETE, CONFIRM                               |


#### **6. Booking Confirmation Architecture**
**Endpoint**

PUT /bookings/{id}/confirm

**Rules**

A booking can only be confirmed if:

- User is logged in

- Booking exists

- Booking belongs to the logged-in user

- Booking status is pending

| Case              | HTTP Status |
| ----------------- | ----------- |
| Not logged in     | 401         |
| Booking not found | 404         |
| Not owned by user | 403         |
| Already confirmed | 409         |
| Success           | 204         |


**Purpose**
- Enforces ownership at databse level
- Prvents double confirmation
- Guarantees booking state integrity


---
#### **7. Profile & User-Owned Data**
###### **Profile**
GET /profile

Returns:

- Email

- First name

- Last name

###### **My Packages**
GET /profile/packages

Returns:
- Package ID
- Package name
- Package type
- Check-in/ Check-out
- City
- Country

Used for dashboards and user overviews


---
#### **8. Key Patterns**

- **Record Types:** DTOs and arguments (Post_Args, Get_Data)

- **Async/Await:** All database access is asynchronous

- **Enums:** Controlled values (DayKind, MealType, RegistrationStatus)

- **Validation Helpers:** Email, password, uniqueness

- **Transactions:** Password reset and sensitive updates
---
#### **9. Security Considerations**

- Session-based authentication

- Claims-based authorization

- Ownership checks on bookings

- Parameterized SQL queries

- Passwords currently stored in plain text (**should be hashed**)
---
#### **10. Data Flow for Booking an Experience**

1. User selects location, dates, guests, rooms, package type

2. System searches available hotels and restaurants

3. Meal plan is generated dynamically

4. User selects an experience offer

5. Booking is created with status pending

6. Booking meals are stored

7. User confirms booking → status becomes confirmed
---
#### **11. Notes & Future Improvements**

- Hash passwords using BCrypt or Argon2

- Add booking cancellation flow

- Add token cleanup job

- Pagination for large datasets

- Admin-only endpoints

- Audit logging for booking status changes

- Payment integration
---
This architecture ensures clear separation of concerns, strong ownership enforcement, and a scalable foundation for future features such as payments, reviews, and loyalty systems.