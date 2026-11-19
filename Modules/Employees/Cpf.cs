namespace EmployeeRegistry.Api.Modules.Employees;

public static class Cpf
{
    public static string DigitsOnly(string? value) =>
        string.Concat((value ?? "").Where(char.IsDigit));

    public static bool IsValid(string? value)
    {
        var cpf = DigitsOnly(value);
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        return CheckDigit(cpf, 9) == cpf[9] - '0' && CheckDigit(cpf, 10) == cpf[10] - '0';
    }

    public static string Mask(string? value)
    {
        var digits = DigitsOnly(value);
        if (digits.Length != 11)
            return digits;

        return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
    }

    private static int CheckDigit(string cpf, int length)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
            sum += (cpf[index] - '0') * (length + 1 - index);

        var rest = (sum * 10) % 11;
        return rest is 10 or 11 ? 0 : rest;
    }
}
