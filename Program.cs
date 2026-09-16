using FitBook_App.Configuration;
using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Helpers;
using FitBook_App.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Local file is gitignored. Copy appsettings.Local.example.json to appsettings.Local.json.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddPolicy("AngularApp", policy => policy
        .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppDb")));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ITrainerService, TrainerService>();

var app = builder.Build();

await SeedDatabaseIfNeeded(app);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseCors("AngularApp");
app.UseAuthorization();
app.MapControllers();
app.Run();

static async Task SeedDatabaseIfNeeded(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        return;
    }

    var seed = app.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>();
    if (seed == null || !seed.Enabled)
    {
        return;
    }

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    await DatabaseSeeder.SeedAsync(dbContext, passwordHasher, seed, app.Logger);
}
