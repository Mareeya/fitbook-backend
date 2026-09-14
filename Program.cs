using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using FitBook_App.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}

var builder = WebApplication.CreateBuilder(args); //creates app's blueprint/setup

builder.Services.AddControllers(); //adds controller services
builder.Services.AddOpenApi(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => //adding CORS policy to allow requests from Angular app running on localhost:4200
    options.AddPolicy("AngularApp", policy => policy
        .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));
// appdbcontext creates scope type dependency by default
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppDb")));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ITrainerService, TrainerService>();

var app = builder.Build();

// Create one admin account for the evaluation demo.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    var adminExists = await dbContext.Users.AnyAsync(user => user.Email == "admin@fitbook.com");

    if (!adminExists)
    {
        var admin = new User
        {
            Name = "FitBook Admin",
            Email = "admin@fitbook.com",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin@123");
        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();
    }

    await EnsureTrainerAsync(dbContext, passwordHasher, "Sara Khan", "sara@fitbook.com", "Yoga");
    await EnsureTrainerAsync(dbContext, passwordHasher, "Ali Raza", "ali@fitbook.com", "Cardio");
    await EnsureTrainerAsync(dbContext, passwordHasher, "Hina Malik", "hina@fitbook.com", "Strength");

    if (!await dbContext.Classes.AnyAsync())
    {
        var sara = await dbContext.Trainers.FirstAsync(trainer => trainer.Name == "Sara Khan");
        var ali = await dbContext.Trainers.FirstAsync(trainer => trainer.Name == "Ali Raza");
        var hina = await dbContext.Trainers.FirstAsync(trainer => trainer.Name == "Hina Malik");
        var now = DateTime.UtcNow;

        dbContext.Classes.AddRange(
            new GymClass { Name = "Morning Yoga", CategoryId = 1, TrainerId = sara.Id, InitCapacity = 20, CreatedAt = now, UpdatedAt = now },
            new GymClass { Name = "HIIT Cardio", CategoryId = 2, TrainerId = ali.Id, InitCapacity = 15, CreatedAt = now, UpdatedAt = now },
            new GymClass { Name = "Strength Basics", CategoryId = 3, TrainerId = hina.Id, InitCapacity = 12, CreatedAt = now, UpdatedAt = now }
        );
        await dbContext.SaveChangesAsync();
    }
}

static async Task EnsureTrainerAsync(
    AppDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    string name,
    string email,
    string specialty)
{
    var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Email == email);
    if (user == null)
    {
        user = new User
        {
            Name = name,
            Email = email,
            Role = UserRole.Trainer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, "Trainer@123");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }

    var trainerExists = await dbContext.Trainers.AnyAsync(item => item.UserId == user.Id);
    if (!trainerExists)
    {
        dbContext.Trainers.Add(new Trainer
        {
            UserId = user.Id,
            Name = name,
            Specialty = specialty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }
}

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

app.UseCors("AngularApp");
app.UseAuthorization();
app.MapControllers();
app.Run();
