using EmployeeRegistry.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeRegistry.Api.Modules.Employees;

public sealed record CreateEmployeeRequest(
    string? Cpf,
    string? FirstName,
    string? LastName,
    string? BirthDate,
    string? Occupation,
    string? EmploymentType,
    decimal? Salary);

public sealed record UpdateEmployeeRequest(
    string? FirstName,
    string? LastName,
    string? BirthDate,
    string? Occupation,
    string? EmploymentType,
    decimal? Salary);

public sealed record EmployeeResponse(
    int Id,
    string Cpf,
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    string Occupation,
    string EmploymentType,
    decimal Salary);

public sealed record EmployeeSummaryResponse(int Total, IReadOnlyDictionary<string, int> ByEmploymentType);

public abstract record EmployeeWriteResult
{
    private EmployeeWriteResult()
    {
    }

    public sealed record Ok(EmployeeResponse Employee) : EmployeeWriteResult;

    public sealed record Deleted : EmployeeWriteResult;

    public sealed record NotFound : EmployeeWriteResult;

    public sealed record Conflict(string Message) : EmployeeWriteResult;

    public sealed record Invalid(string Message) : EmployeeWriteResult;
}

public sealed class EmployeeService(AppDbContext db)
{
    public async Task<IReadOnlyList<EmployeeResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var employees = await db.Employees.AsNoTracking()
            .OrderBy(employee => employee.Id)
            .ToListAsync(cancellationToken);

        return employees.Select(ToResponse).ToList();
    }

    public async Task<EmployeeSummaryResponse> SummaryAsync(CancellationToken cancellationToken)
    {
        var counts = await db.Employees.AsNoTracking()
            .GroupBy(employee => employee.EmploymentType)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var byType = Enum.GetValues<EmploymentType>().ToDictionary(
            type => type.ToString(),
            type => counts.FirstOrDefault(count => count.Type == type)?.Count ?? 0);

        return new EmployeeSummaryResponse(byType.Values.Sum(), byType);
    }

    public async Task<EmployeeResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return employee is null ? null : ToResponse(employee);
    }

    public async Task<EmployeeWriteResult> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (!EmployeeRules.TryNormalize(
                request.FirstName,
                request.LastName,
                request.BirthDate,
                request.Occupation,
                request.EmploymentType,
                request.Salary,
                request.Cpf,
                requireCpf: true,
                out var normalized,
                out var error))
        {
            return new EmployeeWriteResult.Invalid(error!);
        }

        if (await db.Employees.AnyAsync(employee => employee.Cpf == normalized.Cpf, cancellationToken))
            return Conflict(normalized.Cpf!);

        var employee = new Employee();
        Apply(employee, normalized, includeCpf: true);
        db.Employees.Add(employee);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(normalized.Cpf!);
        }

        return new EmployeeWriteResult.Ok(ToResponse(employee));
    }

    public async Task<EmployeeWriteResult> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        if (!EmployeeRules.TryNormalize(
                request.FirstName,
                request.LastName,
                request.BirthDate,
                request.Occupation,
                request.EmploymentType,
                request.Salary,
                cpf: null,
                requireCpf: false,
                out var normalized,
                out var error))
        {
            return new EmployeeWriteResult.Invalid(error!);
        }

        var employee = await db.Employees.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (employee is null)
            return new EmployeeWriteResult.NotFound();

        Apply(employee, normalized, includeCpf: false);
        await db.SaveChangesAsync(cancellationToken);
        return new EmployeeWriteResult.Ok(ToResponse(employee));
    }

    public async Task<EmployeeWriteResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (employee is null)
            return new EmployeeWriteResult.NotFound();

        db.Employees.Remove(employee);
        await db.SaveChangesAsync(cancellationToken);
        return new EmployeeWriteResult.Deleted();
    }

    private static void Apply(Employee employee, NormalizedEmployee normalized, bool includeCpf)
    {
        if (includeCpf)
            employee.Cpf = normalized.Cpf!;

        employee.FirstName = normalized.FirstName;
        employee.LastName = normalized.LastName;
        employee.BirthDate = normalized.BirthDate;
        employee.Occupation = normalized.Occupation;
        employee.EmploymentType = normalized.EmploymentType;
        employee.Salary = normalized.Salary;
    }

    private static EmployeeWriteResult Conflict(string cpf) =>
        new EmployeeWriteResult.Conflict($"Document {Cpf.Mask(cpf)} already exists.");

    private static EmployeeResponse ToResponse(Employee employee) => new(
        employee.Id,
        employee.Cpf,
        employee.FirstName,
        employee.LastName,
        employee.BirthDate,
        employee.Occupation,
        employee.EmploymentType.ToString(),
        employee.Salary);
}
