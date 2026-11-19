using EmployeeRegistry.Api.Modules.Auth;
using EmployeeRegistry.Api.Modules.Employees;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeRegistry.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment,
        PasswordHasher<Account> passwordHasher,
        CancellationToken cancellationToken = default)
    {
        if (await db.Accounts.AnyAsync(cancellationToken))
            return;

        var adminLogin = configuration["Admin:Login"];
        var adminPassword = configuration["Admin:Password"];
        var viewerLogin = configuration["Viewer:Login"];
        var viewerPassword = configuration["Viewer:Password"];

        if (environment.IsDevelopment())
        {
            adminLogin = string.IsNullOrWhiteSpace(adminLogin) ? "admin" : adminLogin;
            adminPassword = string.IsNullOrWhiteSpace(adminPassword) ? "Admin@123" : adminPassword;
            viewerLogin = string.IsNullOrWhiteSpace(viewerLogin) ? "viewer" : viewerLogin;
            viewerPassword = string.IsNullOrWhiteSpace(viewerPassword) ? "Viewer@123" : viewerPassword;
        }

        if (string.IsNullOrWhiteSpace(adminLogin) || string.IsNullOrWhiteSpace(adminPassword)
            || string.IsNullOrWhiteSpace(viewerLogin) || string.IsNullOrWhiteSpace(viewerPassword))
        {
            throw new InvalidOperationException(
                "Set Admin__Login, Admin__Password, Viewer__Login, and Viewer__Password before the first startup.");
        }

        var admin = new Account { Login = adminLogin.Trim(), Role = Roles.Admin };
        admin.PasswordHash = passwordHasher.HashPassword(admin, adminPassword);

        var viewer = new Account { Login = viewerLogin.Trim(), Role = Roles.Viewer };
        viewer.PasswordHash = passwordHasher.HashPassword(viewer, viewerPassword);

        db.Accounts.AddRange(admin, viewer);
        db.Employees.AddRange(
            new Employee
            {
                Cpf = "52998224725",
                FirstName = "Ana",
                LastName = "Souza",
                BirthDate = new DateOnly(1990, 5, 20),
                Occupation = "Software Engineer",
                EmploymentType = EmploymentType.CLT,
                Salary = 12000.50m
            },
            new Employee
            {
                Cpf = "11144477735",
                FirstName = "Bruno",
                LastName = "Lima",
                BirthDate = new DateOnly(1988, 11, 2),
                Occupation = "Designer",
                EmploymentType = EmploymentType.PJ,
                Salary = 8500.00m
            },
            new Employee
            {
                Cpf = "39053344705",
                FirstName = "Carla",
                LastName = "Dias",
                BirthDate = new DateOnly(2002, 3, 15),
                Occupation = "QA Analyst",
                EmploymentType = EmploymentType.Intern,
                Salary = 2200.00m
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
