using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.API.Controllers;

[ApiController]
public class AdminController : ControllerBase
{
    private readonly IAdmissionService _service;
    public AdminController(IAdmissionService service) => _service = service;

    // All admission records, newest first. Held rows past their expiry are reported as EXPIRED.
    [HttpGet("/admin/holds")]
    public async Task<IActionResult> ListHolds(CancellationToken ct) =>
        Ok(await _service.ListAllHoldsAsync(ct));
}
