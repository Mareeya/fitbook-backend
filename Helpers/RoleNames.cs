namespace FitBook_App.Helpers;

// Role claims are written as the UserRole enum name, so these must match it exactly.
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Trainer = "Trainer";
    public const string Member = "Member";
    public const string Staff = "Staff";

    // Staff manage the timetable alongside admins.
    public const string Manage = $"{Admin},{Staff}";
    public const string ManageOrTrainer = $"{Admin},{Staff},{Trainer}";
}
