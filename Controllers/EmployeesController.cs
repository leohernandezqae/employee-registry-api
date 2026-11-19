using EmployeeRegistry.Api.Modules.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeRegistry.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public sealed class EmployeesController(EmployeeService employees) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await employees.ListAsync(cancellationToken));

    [HttpGet("summary")]
    public async Task<ActionResult<EmployeeSummaryResponse>> Summary(CancellationToken cancellationToken) =>
        Ok(await employees.SummaryAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var employee = await employees.GetAsync(id, cancellationToken);
        return employee is null
            ? NotFound(new { message = "Employee not found" })
            : Ok(employee);
    }

    [Authorize(Policy = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var result = await employees.CreateAsync(request, cancellationToken);
        return result is EmployeeWriteResult.Ok created
            ? Created($"/api/employees/{created.Employee.Id}", created.Employee)
            : ToResult(result);
    }

    [Authorize(Policy = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken) =>
        ToResult(await employees.UpdateAsync(id, request, cancellationToken));

    [Authorize(Policy = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ToResult(await employees.DeleteAsync(id, cancellationToken));

    private IActionResult ToResult(EmployeeWriteResult result) => result switch
    {
        EmployeeWriteResult.Ok ok => Ok(ok.Employee),
        EmployeeWriteResult.Deleted => NoContent(),
        EmployeeWriteResult.NotFound => NotFound(new { message = "Employee not found" }),
        EmployeeWriteResult.Conflict conflict => Conflict(new { message = conflict.Message }),
        EmployeeWriteResult.Invalid invalid => BadRequest(new { message = invalid.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unexpected error." })
    };
}
