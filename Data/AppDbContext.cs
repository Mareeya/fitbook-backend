using FitBook_App.Domain;
using FitBook_App.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
     // Declaring tables
    public DbSet<User> Users => Set<User>(); // {get; set;}
    public DbSet<Lookup> Lookups => Set<Lookup>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<GymClass> Classes => Set<GymClass>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Booking> Bookings => Set<Booking>();

    /* OnModelCreating —> This method is where you tell EF Core extra details it can't guess just from your class definitions,
    things like "this column is required," "this combination must be unique," "here's how these two tables connect."
    Everything inside runs once, when EF Core is figuring out how to build/read your database.*/

   
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(user => user.Name).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(user => user.Role).HasConversion<byte>();
        });

        modelBuilder.Entity<Lookup>(entity =>
        {
            entity.ToTable("Lookups");
            entity.Property(lookup => lookup.Type).HasMaxLength(40).IsRequired();
            entity.Property(lookup => lookup.Value).HasMaxLength(80).IsRequired();
            entity.HasIndex(lookup => new { lookup.Type, lookup.Value }).IsUnique();
        });

        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.ToTable("Trainers");
            entity.Property(trainer => trainer.Name).HasMaxLength(120).IsRequired();
            entity.Property(trainer => trainer.Specialty).HasMaxLength(80).IsRequired();
            entity.HasIndex(trainer => trainer.UserId).IsUnique();
            entity.HasOne(trainer => trainer.User)
                .WithMany()
                .HasForeignKey(trainer => trainer.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GymClass>(entity =>
        {
            entity.ToTable("Classes");
            entity.Property(gymClass => gymClass.Name).HasMaxLength(80).IsRequired();
            entity.HasIndex(gymClass => gymClass.Name).IsUnique();
            entity.HasOne(gymClass => gymClass.Category)
                .WithMany()
                .HasForeignKey(gymClass => gymClass.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(gymClass => gymClass.Trainer)
                .WithMany(trainer => trainer.Classes)
                .HasForeignKey(gymClass => gymClass.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasOne(session => session.Class)
                .WithMany(gymClass => gymClass.Sessions)
                .HasForeignKey(session => session.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(session => session.Status)
                .WithMany()
                .HasForeignKey(session => session.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings");
            entity.HasIndex(booking => new { booking.SessionId, booking.UserId }).IsUnique(); //it means one user can't book the same session twice.
            entity.HasOne(booking => booking.User)
                .WithMany(user => user.Bookings)
                .HasForeignKey(booking => booking.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(booking => booking.Session)
                .WithMany(session => session.Bookings)
                .HasForeignKey(booking => booking.SessionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(booking => booking.Status)
                .WithMany()
                .HasForeignKey(booking => booking.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        var now = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Lookup>().HasData(
            new Lookup { Id = 1, Type = "class", Value = "Yoga", CreatedAt = now, UpdatedAt = now },
            new Lookup { Id = 2, Type = "class", Value = "Cardio", CreatedAt = now, UpdatedAt = now },
            new Lookup { Id = 3, Type = "class", Value = "Strength", CreatedAt = now, UpdatedAt = now }
        );
    }

}
