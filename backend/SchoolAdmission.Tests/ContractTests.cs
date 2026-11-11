using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class ContractTests
{
    private readonly ApiFixture _fixture;
    private readonly ApiHelper _api;

    public ContractTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _api = new ApiHelper(fixture.Client);
    }

    [Fact]
    public async Task Class_can_be_created_with_only_name_and_total_seats()
    {
        var name = "Class " + Guid.NewGuid().ToString("N")[..8] + " - Morning Shift";

        var response = await _fixture.Client.PostAsJsonAsync("/classes", new { name, totalSeats = 120 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ApiHelper.ReadAsync(response);
        body.GetProperty("name").GetString().Should().Be(name);
        body.GetProperty("shiftName").GetString().Should().Be("General");
        body.GetProperty("totalSeats").GetInt32().Should().Be(120);
        body.GetProperty("availableSeats").GetInt32().Should().Be(120);
    }

    [Fact]
    public async Task Hold_response_uses_holdId_and_uppercase_status()
    {
        var classId = await _api.CreateClassAsync(2);
        var studentId = ApiHelper.NewStudentId();

        var response = await _api.ApplyAsync(classId, studentId);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ApiHelper.ReadAsync(response);
        body.TryGetProperty("holdId", out _).Should().BeTrue();
        body.TryGetProperty("id", out _).Should().BeFalse();
        body.GetProperty("classId").GetInt32().Should().Be(classId);
        body.GetProperty("studentId").GetString().Should().Be(studentId);
        body.GetProperty("status").GetString().Should().Be("HELD");
        body.TryGetProperty("expiresAt", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_returns_no_content_with_an_empty_body()
    {
        var classId = await _api.CreateClassAsync(2);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);

        var response = await _api.CancelAsync(holdId, studentId);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }
}
