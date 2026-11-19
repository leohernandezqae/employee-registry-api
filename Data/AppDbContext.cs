using EmployeeRegistry.Api.Modules.Auth;
using EmployeeRegistry.Api.Modules.Employees;
using Microsoft.EntityFrameworkCore;

namespace EmployeeRegistry.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(account => account.Login).IsUnique();
            entity.Property(account => account.Login).HasMaxLength(60).IsRequired();
            entity.Property(account => account.PasswordHash).IsRequired();
            entity.Property(account => account.Role).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(employee => employee.Cpf).IsUnique();
            entity.Property(employee => employee.Cpf).HasMaxLength(11).IsRequired();
            entity.Property(employee => employee.FirstName).HasMaxLength(60).IsRequired();
            entity.Property(employee => employee.LastName).HasMaxLength(60).IsRequired();
            entity.Property(employee => employee.Occupation).HasMaxLength(60).IsRequired();
            entity.Property(employee => employee.EmploymentType).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(employee => employee.Salary).HasPrecision(12, 2);
        });
    }
}
