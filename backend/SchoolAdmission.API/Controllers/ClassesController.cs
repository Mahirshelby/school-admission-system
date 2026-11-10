using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.Dtos;

namespace SchoolAdmission.API.Controllers;

[ApiController]
public class ClassesController : ControllerBase
{
    private readonly IAdmissionService _service;
    public ClassesController(IAdmissionService service) => _service = service;

    [HttpPost("/classes")]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request, CancellationToken ct)
    {
        var dto = await _service.CreateClassAsync(request, ct);
        return Created($"/classes/{dto.Id}", dto);
    }

    [HttpGet("/classes")]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _service.ListClassesAsync(ct));

    [HttpGet("/classes/{classId:int}")]
    public async Task<IActionResult> Get(int classId, CancellationToken ct) =>
        Ok(await _service.GetClassAsync(classId, ct));
}
