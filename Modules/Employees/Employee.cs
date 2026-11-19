namespace EmployeeRegistry.Api.Modules.Employees;

public class Employee
{
    public int Id { get; set; }

    public string Cpf { get; set; } = "";

    public string FirstName { get; set; } = "";

    public string LastName { get; set; } = "";

    public DateOnly BirthDate { get; set; }

    public string Occupation { get; set; } = "";

    public EmploymentType EmploymentType { get; set; }

    public decimal Salary { get; set; }
}

public enum EmploymentType
{
    CLT,
    PJ,
    Intern,
    Contractor
}
