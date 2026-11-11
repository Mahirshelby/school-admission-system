using System.Net;
using FluentAssertions;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class ReservationTests
{
    private readonly ApiHelper _api;

    public ReservationTests(ApiFixture fixture) => _api = new ApiHelper(fixture.Client);

    [Fact]
    public async Task Applying_holds_a_seat_and_lowers_availability()
    {
        var classId = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();

        var response = await _api.ApplyAsync(classId, studentId);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var hold = await ApiHelper.ReadAsync(response);
        hold.GetProperty("status").GetString().Should().Be("HELD");
        hold.GetProperty("studentId").GetString().Should().Be(studentId);
        var lifetime = hold.GetProperty("expiresAt").GetDateTimeOffset() - hold.GetProperty("createdAt").GetDateTimeOffset();
        lifetime.Should().Be(TimeSpan.FromSeconds(ApiFixture.HoldDurationSeconds));

        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("heldSeats").GetInt32().Should().Be(1);
        cls.GetProperty("confirmedSeats").GetInt32().Should().Be(0);
        cls.GetProperty("availableSeats").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task Full_class_rejects_the_next_student()
    {
        var classId = await _api.CreateClassAsync(1);
        await _api.ApplyAndGetIdAsync(classId, ApiHelper.NewStudentId());

        var response = await _api.ApplyAsync(classId, ApiHelper.NewStudentId());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("class_full");
    }

    [Fact]
    public async Task Same_student_cannot_hold_the_same_class_twice()
    {
        var classId = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();
        await _api.ApplyAndGetIdAsync(classId, studentId);

        var second = await _api.ApplyAsync(classId, studentId);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiHelper.ReadErrorCodeAsync(second)).Should().Be("already_applied");
        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("heldSeats").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Same_student_can_hold_two_different_classes()
    {
        var first = await _api.CreateClassAsync(5);
        var second = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();

        (await _api.ApplyAsync(first, studentId)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await _api.ApplyAsync(second, studentId)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Missing_student_id_is_rejected()
    {
        var classId = await _api.CreateClassAsync(5);

        var response = await _api.ApplyAsync(classId, null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("student_id_required");
    }

    [Fact]
    public async Task Malformed_student_id_is_rejected()
    {
        var classId = await _api.CreateClassAsync(5);

        var response = await _api.ApplyAsync(classId, "12345");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("student_id_invalid");
    }

    [Fact]
    public async Task Applying_to_an_unknown_class_returns_not_found()
    {
        var response = await _api.ApplyAsync(999_999, ApiHelper.NewStudentId());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("class_not_found");
    }
}
