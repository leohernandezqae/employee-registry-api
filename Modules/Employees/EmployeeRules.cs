using System.Globalization;

namespace EmployeeRegistry.Api.Modules.Employees;

public readonly record struct NormalizedEmployee(
    string? Cpf,
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    string Occupation,
    EmploymentType EmploymentType,
    decimal Salary);

public static class EmployeeRules
{
    public const int NameMaxLength = 60;

    public static bool TryNormalize(
        string? firstName,
        string? lastName,
        string? birthDate,
        string? occupation,
        string? employmentType,
        decimal? salary,
        string? cpf,
        bool requireCpf,
        out NormalizedEmployee employee,
        out string? error)
    {
        employee = default;

        string? digits = null;
        if (requireCpf)
        {
            digits = Cpf.DigitsOnly(cpf);
            if (!Cpf.IsValid(digits))
            {
                error = "Invalid CPF";
                return false;
            }
        }

        var normalizedFirstName = NormalizeRequired(firstName, "First name", out error);
        if (normalizedFirstName is null)
            return false;

        var normalizedLastName = NormalizeRequired(lastName, "Last name", out error);
        if (normalizedLastName is null)
            return false;

        if (string.IsNullOrWhiteSpace(birthDate))
        {
            error = "Birth date is required.";
            return false;
        }

        if (!DateOnly.TryParseExact(birthDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedBirthDate))
        {
            error = "Birth date must use yyyy-MM-dd.";
            return false;
        }

        var normalizedOccupation = NormalizeRequired(occupation, "Occupation", out error);
        if (normalizedOccupation is null)
            return false;

        if (!TryParseEmploymentType(employmentType, out var parsedEmploymentType))
        {
            error = "An employment type is required.";
            return false;
        }

        if (salary is null)
        {
            error = "Salary is required.";
            return false;
        }

        if (salary < 0)
        {
            error = "Salary must be zero or greater.";
            return false;
        }

        employee = new NormalizedEmployee(
            digits,
            normalizedFirstName,
            normalizedLastName,
            parsedBirthDate,
            normalizedOccupation,
            parsedEmploymentType,
            decimal.Round(salary.Value, 2, MidpointRounding.AwayFromZero));
        error = null;
        return true;
    }

    private static string? NormalizeRequired(string? value, string label, out string? error)
    {
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            error = $"{label} is required.";
            return null;
        }

        if (trimmed.Length > NameMaxLength)
        {
            error = $"{label} must be at most {NameMaxLength} characters.";
            return null;
        }

        error = null;
        return trimmed;
    }

    public static bool TryParseEmploymentType(string? value, out EmploymentType employmentType)
    {
        employmentType = default;
        var trimmed = value?.Trim() ?? "";
        return trimmed is "CLT" or "PJ" or "Intern" or "Contractor"
            && Enum.TryParse(trimmed, ignoreCase: false, out employmentType);
    }
}
