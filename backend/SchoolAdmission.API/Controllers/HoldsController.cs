using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.API.Controllers;

[ApiController]
public class HoldsController : ControllerBase
{
    private const string StudentHeader = "X-Student-Id";
    private readonly IAdmissionService _service;
    public HoldsController(IAdmissionService service) => _service = service;

    [HttpPost("/classes/{classId:int}/holds")]
    public async Task<IActionResult> Create(int classId,
        [FromHeader(Name = StudentHeader)] string? studentId, CancellationToken ct)
    {
        var dto = await _service.CreateHoldAsync(classId, studentId, ct);
        return Created($"/holds/{dto.Id}", dto);
    }

    [HttpGet("/holds/{holdId:guid}")]
    public async Task<IActionResult> Get(Guid holdId,
        [FromHeader(Name = StudentHeader)] string? studentId, CancellationToken ct) =>
        Ok(await _service.GetHoldAsync(holdId, studentId, ct));

    [HttpPost("/holds/{holdId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid holdId,
        [FromHeader(Name = StudentHeader)] string? studentId, CancellationToken ct) =>
        Ok(await _service.ConfirmHoldAsync(holdId, studentId, ct));

    [HttpDelete("/holds/{holdId:guid}")]
    public async Task<IActionResult> Cancel(Guid holdId,
        [FromHeader(Name = StudentHeader)] string? studentId, CancellationToken ct) =>
        Ok(await _service.CancelHoldAsync(holdId, studentId, ct));

    [HttpGet("/me/holds")]
    public async Task<IActionResult> Mine(
        [FromHeader(Name = StudentHeader)] string? studentId, CancellationToken ct) =>
        Ok(await _service.GetMyHoldsAsync(studentId, ct));
}
