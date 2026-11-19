namespace EmployeeRegistry.Api.Modules.Auth;

public class Account
{
    public int Id { get; set; }

    public string Login { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Role { get; set; } = Roles.Viewer;
}

public static class Roles
{
    public const string Admin = "admin";

    public const string Viewer = "viewer";
}
