using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RestaurantReservation.Data;
using RestaurantReservation.Services;
using RestaurantReservation.Models;
using RestaurantReservation.Repositories.Implementations;
using RestaurantReservation.Repositories.Interfaces;
using RestaurantReservation.Services.Implementations;
using RestaurantReservation.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ----- Database -----
// Provider is configurable via "Database:Provider" ("Sqlite" or "Postgres").
// Defaults to SQLite so the app runs locally with zero setup (it just creates a .db file).
// Set it to "Postgres" (e.g. on Azure) to use the PostgreSQL connection string instead.
var dbProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(dbProvider, "Postgres", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
    else
    {
        var sqlite = builder.Configuration.GetConnectionString("SqliteConnection")
                     ?? "Data Source=restaurant_reservation.db";
        options.UseSqlite(sqlite);
    }
});

// ----- AutoMapper -----
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// ----- Password hashing (from the ASP.NET Core shared framework) -----
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// ----- Repositories (interface -> implementation, DI) -----
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

// ----- Services -----
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

// ----- JWT settings + authentication -----
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("Jwt settings are missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ----- CORS (for the front-end) -----
const string CorsPolicy = "FrontendCors";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ----- Controllers + Swagger -----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Restaurant Reservation API",
        Version = "v1",
        Description = "Service Oriented Architecture final project."
    });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without the 'Bearer ' prefix).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// ----- Pipeline -----
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Lightweight health endpoint for Azure / CI smoke checks.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

// ----- Migrate + seed on startup -----
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(context, app.Configuration);
}

app.Run();

// Exposed so the integration/WebApplicationFactory tests can reference the entry point.
public partial class Program { }
