namespace EmployeeRegistry.Api.Modules.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "EmployeeRegistry";

    public string Audience { get; set; } = "EmployeeRegistry";

    public string Key { get; set; } = "";

    public int ExpiresMinutes { get; set; } = 60;
}
