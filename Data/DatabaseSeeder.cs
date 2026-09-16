using FitBook_App.Configuration;
using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        SeedOptions seed,
        ILogger logger)
    {
        await SeedLookupsAsync(dbContext);
        await SeedAdminAsync(dbContext, passwordHasher, seed.Admin, logger);
        await SeedTrainersAsync(dbContext, passwordHasher, seed.Trainers, seed.TrainerPassword, logger);
        await SeedClassesAsync(dbContext, seed.Classes, logger);
    }

    private static async Task SeedLookupsAsync(AppDbContext dbContext)
    {
        var lookups = new[]
        {
            (LookupNames.ClassType, LookupNames.Yoga),
            (LookupNames.ClassType, LookupNames.Cardio),
            (LookupNames.ClassType, LookupNames.Strength),
            (LookupNames.SessionStatusType, LookupNames.Scheduled),
            (LookupNames.SessionStatusType, LookupNames.Cancelled),
            (LookupNames.BookingStatusType, LookupNames.Confirmed),
            (LookupNames.BookingStatusType, LookupNames.Cancelled)
        };

        foreach (var (type, value) in lookups)
        {
            var exists = await dbContext.Lookups.AnyAsync(lookup => lookup.Type == type && lookup.Value == value);
            if (exists)
            {
                continue;
            }

            var now = DateTime.UtcNow;
            dbContext.Lookups.Add(new Lookup
            {
                Type = type,
                Value = value,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        SeedAdminOptions admin,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(admin.Email))
        {
            return;
        }

        var adminEmail = EmailNormalizer.Normalize(admin.Email);
        var adminExists = await dbContext.Users.AnyAsync(user => user.Email == adminEmail);
        if (adminExists)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(admin.Password))
        {
            logger.LogWarning(
                "Skipping admin seed because Seed:Admin:Password is empty. Set it in appsettings.Local.json or with: dotnet user-secrets set \"Seed:Admin:Password\" \"your-password\"");
            return;
        }

        var user = new User
        {
            Name = admin.Name,
            Email = adminEmail,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, admin.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedTrainersAsync(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        List<SeedTrainerOptions> trainers,
        string trainerPassword,
        ILogger logger)
    {
        foreach (var trainer in trainers)
        {
            var trainerEmail = EmailNormalizer.Normalize(trainer.Email);
            var user = await dbContext.Users.FirstOrDefaultAsync(item => item.Email == trainerEmail);
            if (user == null)
            {
                if (string.IsNullOrWhiteSpace(trainerPassword))
                {
                    logger.LogWarning(
                        "Skipping trainer seed for {Email} because Seed:TrainerPassword is empty. Set it in appsettings.Local.json or with: dotnet user-secrets set \"Seed:TrainerPassword\" \"your-password\"",
                        trainer.Email);
                    continue;
                }

                user = new User
                {
                    Name = trainer.Name,
                    Email = trainerEmail,
                    Role = UserRole.Trainer,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                user.PasswordHash = passwordHasher.HashPassword(user, trainerPassword);
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
            }

            var trainerExists = await dbContext.Trainers.AnyAsync(item => item.UserId == user.Id);
            if (trainerExists)
            {
                continue;
            }

            dbContext.Trainers.Add(new Trainer
            {
                UserId = user.Id,
                Name = trainer.Name,
                Specialty = trainer.Specialty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task SeedClassesAsync(
        AppDbContext dbContext,
        List<SeedClassOptions> classes,
        ILogger logger)
    {
        if (classes.Count == 0 || await dbContext.Classes.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var gymClass in classes)
        {
            var trainer = await dbContext.Trainers
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.User.Email == EmailNormalizer.Normalize(gymClass.TrainerEmail));

            if (trainer == null)
            {
                logger.LogWarning(
                    "Skipping class seed for {ClassName} because trainer {TrainerEmail} was not found.",
                    gymClass.Name,
                    gymClass.TrainerEmail);
                continue;
            }

            dbContext.Classes.Add(new GymClass
            {
                Name = gymClass.Name,
                CategoryId = gymClass.CategoryId,
                TrainerId = trainer.Id,
                InitCapacity = gymClass.InitCapacity,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
