# RestaurantReservation — Restaurant Reservation Management System

A Service Oriented Architecture project: a layered **ASP.NET Core Web API (.NET 10)** for managing restaurant tables and reservations, with JWT authentication, role-based authorization, a repository layer with dependency injection, unit tests, a CI/CD pipeline for Azure, and a bonus single-page front-end.

**Developer:** Emira Bislimi

---

## Solution layout

```
RestaurantReservation/
├─ RestaurantReservation.sln
├─ README.md
├─ .gitignore
├─ .editorconfig
├─ .github/workflows/azure-deploy.yml      # CI/CD pipeline
├─ frontend/
│  └─ index.html                           # Bonus single-page UI
├─ RestaurantReservation/                        # The Web API project
│  ├─ RestaurantReservation.csproj
│  ├─ Program.cs
│  ├─ appsettings.json
│  ├─ appsettings.Development.json
│  ├─ Properties/
│  ├─ Controllers/                          # Auth, Tables, Reservations
│  ├─ Data/                                 # DbContext, seeder, middleware, exceptions, claims helper
│  ├─ Models/                               # Entities, Enums, DTOs, JwtSettings
│  ├─ Repositories/                         # Interfaces + Implementations
│  └─ Services/                             # Interfaces + Implementations + MappingProfile
└─ RestaurantReservation.Tests/                  # xUnit test project
   ├─ Controllers/
   ├─ Repositories/
   └─ Services/
```

The architecture flows one way: **Controllers → Services → Repositories → EF Core DbContext → PostgreSQL.** Each layer depends only on the interfaces of the layer beneath it, and everything is wired through the built-in dependency injection container in `Program.cs`.

---

## Tech stack

| Concern | Choice |
| --- | --- |
| Framework | ASP.NET Core Web API, **.NET 10** |
| ORM | Entity Framework Core 9 |
| Database | SQLite by default (local, zero-setup); PostgreSQL for deployment (`Npgsql.EntityFrameworkCore.PostgreSQL`) |
| Auth | JWT bearer tokens |
| Password hashing | `PasswordHasher<User>` (ASP.NET Core Identity primitives) |
| Object mapping | AutoMapper |
| API docs | Swagger / OpenAPI (Swashbuckle) |
| Testing | xUnit + NSubstitute + EF Core InMemory |
| CI/CD | GitHub Actions → Azure App Service |
| Front-end | Single-file vanilla HTML/CSS/JS |

> The project targets `net10.0`. To change versions, edit `<TargetFramework>` in both `.csproj` files and update the matching EF Core / ASP.NET package versions.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (Optional) [PostgreSQL](https://www.postgresql.org/download/) 14+ — only if you switch the provider to Postgres. By default the app uses SQLite and needs no database server.
- (Optional) EF Core CLI for migrations: `dotnet tool install --global dotnet-ef`

---

## Configuration

Edit `RestaurantReservation/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=restaurant_reservation;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "RestaurantReservationApi",
    "Audience": "RestaurantReservationClient",
    "Key": "REPLACE_THIS_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS",
    "ExpiryMinutes": 120
  },
  "Seed": {
    "AdminEmail": "admin@restaurant.local",
    "AdminPassword": "Admin123!"
  },
  "EnableSwagger": true
}
```

Before running, set two values:

1. **`Jwt:Key`** — replace with a random secret of **at least 32 characters** (tokens won't validate with the placeholder).
2. **`Database:Provider`** — leave as `"Sqlite"` to run locally with no database server (a `restaurant_reservation.db` file is created automatically). Set it to `"Postgres"` to use the `DefaultConnection` PostgreSQL connection string instead (recommended for Azure). When using Postgres, also point `ConnectionStrings:DefaultConnection` at your instance.

The relevant config:

```json
{
  "Database": { "Provider": "Sqlite" },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=restaurant_reservation;Username=postgres;Password=postgres",
    "SqliteConnection": "Data Source=restaurant_reservation.db"
  }
}
```

For local dev you can keep secrets out of source control:

```bash
cd RestaurantReservation
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<your-long-random-secret>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-connection-string>"
```

---

## Running locally

```bash
# from the solution root
dotnet restore
dotnet build
dotnet run --project RestaurantReservation
```

Default URLs:

- HTTP  → `http://localhost:5080`
- HTTPS → `https://localhost:7080`
- Swagger UI → `http://localhost:5080/swagger`
- Health probe → `/health`

On first startup the **`DbSeeder`** runs automatically: it applies any EF migrations (or creates the schema if none exist) and seeds an admin account plus six sample tables.

Seeded admin login:

- **Email:** `admin@restaurant.local`
- **Password:** `Admin123!`

---

## Database & migrations

The seeder creates the schema on first run even without a committed migration, so you can start immediately. For a migration-based workflow:

```bash
cd RestaurantReservation
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Data model (three entities):

- **User** — `Id`, `FullName`, `Email` (unique), `PasswordHash`, `Role` (Admin/Customer).
- **RestaurantTable** — `Id`, `TableNumber` (unique), `Capacity`, `Location`, `IsActive`.
- **Reservation** — `Id`, `UserId`, `TableId`, start/end (UTC), `PartySize`, `Status` (Pending/Confirmed/Cancelled/Completed).

---

## API reference

### Auth (`/api/auth`) — anonymous

| Method | Route | Description |
| --- | --- | --- |
| POST | `/register` | Register a new **Customer** |
| POST | `/login` | Authenticate, receive a JWT |

### Tables (`/api/tables`) — authenticated

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| GET | `/` | Any user | List tables |
| GET | `/{id}` | Any user | Get a table |
| POST | `/` | **Admin** | Create a table |
| PUT | `/{id}` | **Admin** | Update a table |
| DELETE | `/{id}` | **Admin** | Delete (blocked if it has upcoming reservations) |

### Reservations (`/api/reservations`) — authenticated

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| GET | `/` | **Admin** | List all reservations |
| GET | `/mine` | Owner | List the current user's reservations |
| GET | `/{id}` | Owner or Admin | Get a reservation |
| POST | `/` | Any user | **Create** a reservation for a specific table (business logic) |
| POST | `/auto` | Any user | **Auto-reserve**: system picks the best free table (complex business logic) |
| POST | `/{id}/cancel` | Owner or Admin | Cancel |
| PUT | `/{id}/status` | **Admin** | Update reservation status |

---

## Authentication & roles

1. Register a Customer, or use the seeded admin.
2. `POST /api/auth/login` returns a signed JWT with id, email, name, and role claims.
3. Send it on protected calls: `Authorization: Bearer <token>` (in Swagger, click **Authorize**).

**Customer** — browse tables, create/auto-create and manage their own reservations.
**Admin** — full table management, view all reservations, change reservation status.

Authorization uses `[Authorize]` / `[Authorize(Roles = "Admin")]` plus ownership checks in the service layer.

---

## Running the tests

```bash
# from the solution root
dotnet test
```

Coverage across all layers:

- **Repositories** — reservation overlap detection, candidate-table filtering/ordering, user lookups (EF Core InMemory).
- **Services** — past-time/over-capacity/double-booking rejection, auto-reserve selection, cancellation lead-time and ownership, duplicate-email and password-hashing checks.
- **Controllers** — correct status codes and that the authenticated user id flows into the service layer (faked `ClaimsPrincipal`).

NSubstitute provides mocks; the real AutoMapper profile is used in service tests so mappings are exercised too.

---

## Front-end (bonus)

Open `frontend/index.html` in a browser — no build step.

1. Run the API.
2. Open the file; set the **API base URL** field (defaults to `http://localhost:5080`).
3. Register or log in (use the seeded admin to manage tables), then create and manage reservations.

CORS is already enabled (`FrontendCors`) in `Program.cs`.

---

## Azure deployment & CI/CD

`.github/workflows/azure-deploy.yml` has two jobs:

1. **build-and-test** — restore, build, and run unit tests on every push and PR.
2. **deploy** — on pushes to `main`, publishes the API to **Azure App Service**.

One-time setup:

1. Create an **App Service** (Linux, .NET 10). The workflow uses app name `restaurant-reservation-api` — change it in the workflow if yours differs.
2. Provision PostgreSQL (e.g. Azure Database for PostgreSQL).
3. In App Service **Configuration**, set `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`.
4. Download the App Service **publish profile** and add it as a GitHub repository secret named **`AZURE_WEBAPP_PUBLISH_PROFILE`**.

Pushing to `main` then builds, tests, and deploys automatically.

---

## How the rubric is covered

| Rubric item | Where it lives |
| --- | --- |
| Simple CRUD service | `TablesController` + `TableService` |
| Business logic | `ReservationService.CreateAsync` (future-time, capacity, double-booking) |
| Complex business logic | `ReservationService.AutoReserveAsync` (orchestrates repositories to find the smallest fitting free table) |
| Repository layer (interfaces + DI) | `Repositories/Interfaces` + `Repositories/Implementations`, wired in `Program.cs` |
| Unit testing | `RestaurantReservation.Tests` |
| User login | `AuthController` + `AuthService` + `TokenService` |
| Role-based authorization | `UserRole` enum, `[Authorize(Roles = "Admin")]`, ownership checks |
| Secured endpoints | JWT bearer auth on Tables and Reservations |
| Azure deployment + CI/CD | `.github/workflows/azure-deploy.yml` |
| GitHub usage / README | This file + commit history |
| Bonus front-end | `frontend/index.html` |
