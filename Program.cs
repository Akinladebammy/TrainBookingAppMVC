using Microsoft.EntityFrameworkCore;
using Npgsql; // ✅ PostgreSQL provider
using TrainBookinAppWeb.Data;
using TrainBookingAppMVC.PasswordValidation;
using TrainBookingAppMVC.Repository.Implementations;
using TrainBookingAppMVC.Repository.Interfaces;
using TrainBookingAppMVC.Services;
using TrainBookingAppMVC.Services.Implementation;
using TrainBookingAppMVC.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

// ✅ Build PostgreSQL connection string from Render environment vars
string BuildConnectionString(IConfiguration cfg)
{
    var host = Environment.GetEnvironmentVariable("DB_HOST");
    var db = Environment.GetEnvironmentVariable("DB_NAME");
    var user = Environment.GetEnvironmentVariable("DB_USERNAME");
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";

    if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(db) &&
        !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.Parse(port),
            Username = user,
            Password = password,
            Database = db,
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };
        return csb.ToString();
    }

    // fallback for local dev (appsettings.json)
    return cfg.GetConnectionString("DefaultConnection");
}

var connectionString = BuildConnectionString(builder.Configuration);

// ✅ Switch EF Core to PostgreSQL
builder.Services.AddDbContext<TrainAppContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Repository Registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITrainRepository, TrainRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Service Registration
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITrainService, TrainService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<PaystackService>();

// Password Hashing Service Registration
builder.Services.AddScoped<IPasswordHashing, PasswordHashing>();

// Cookie-based Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Name = "TrainBookingAppAuth";
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Authorization Configuration
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RegularUser", policy => policy.RequireRole("Regular"));
    options.AddPolicy("AdminOrRegular", policy => policy.RequireRole("Admin", "Regular"));
});

// Add session and other services
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Name = "TrainBookingAppSession";
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 🔥 FIX: Set Render port BEFORE building the app
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
