# Restaurant Reservation System

This is my final project for the Service Oriented Architecture course. It's a Web API
built with ASP.NET Core where users can register, log in, look at the restaurant's
tables and book them, and admins can manage the tables and see all the reservations.
I built it with a layered architecture (controllers, services, repositories) and used
Entity Framework Core for the database.

## Developer

- Emira Bislimi
- Course: Service Oriented Architecture (Final Project)

## Tech used

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core
- SQLite for local development, PostgreSQL for deployment
- JWT for authentication and roles
- AutoMapper for mapping entities to DTOs
- xUnit and NSubstitute for the unit tests
- Swagger for testing the endpoints

## Setup instructions

What you need:
- .NET 10 SDK installed

Steps:
1. Open a terminal in the folder that has the `.sln` file.
2. Restore the packages:

       dotnet restore

3. Run the API:

       dotnet run --project RestaurantReservation

The app uses SQLite by default, so you don't have to install or set up any database.
The first time it runs it creates the database file automatically and seeds an admin
account plus a few sample tables.

When it's running, open the Swagger page in your browser to try the endpoints:

    http://localhost:5080/swagger

Seeded admin account:
- email: admin@restaurant.local
- password: Admin123!

(You can also register your own account through the API - it will be created as a
normal Customer.)

## Usage guidelines

The API uses JWT tokens, so most endpoints need you to be logged in.

1. Register a new user with `POST /api/auth/register`, or log in with the seeded admin
   using `POST /api/auth/login`.
2. The login response gives you a token. In Swagger, click the **Authorize** button at
   the top and paste the token to use the protected endpoints.
3. Now you can call the endpoints depending on your role.

Roles:
- **Customer** - can view tables, create and auto-create their own reservations, and
  view or cancel their own reservations.
- **Admin** - can do everything a customer can, plus create/edit/delete tables, view
  all reservations, and change a reservation's status.

Main endpoints:
- `POST /api/auth/register` - create a customer account
- `POST /api/auth/login` - log in and get a token
- `GET /api/tables` - list the tables
- `POST /api/tables` - add a table (admin only)
- `POST /api/reservations` - book a specific table
- `POST /api/reservations/auto` - let the system pick a free table for you
- `GET /api/reservations/mine` - see your own reservations
- `POST /api/reservations/{id}/cancel` - cancel a reservation

Some of the rules the API checks: you can't book a table in the past, you can't book a
table that's smaller than your group, and you can't double book a table that's already
taken for that time.

## Running the tests

From the same folder, run:

    dotnet test

There are unit tests for the controllers, services and repositories, using xUnit with
NSubstitute for mocking.

## Frontend

There's a simple frontend in the `frontend` folder.You can log in and make reservations from there instead
of using Swagger.

## Deployment

For deployment the database switches to PostgreSQL. To do that, change
`Database:Provider` in `appsettings.json` to `Postgres` and set the connection string.
The project also has a GitHub Actions workflow that builds, runs the tests, and deploys
the API automatically.