using System.Net;
using FluentAssertions;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class ConfirmationTests
{
    private readonly ApiHelper _api;

    public ConfirmationTests(ApiFixture fixture) => _api = new ApiHelper(fixture.Client);

    [Fact]
    public async Task Confirming_moves_the_seat_from_held_to_confirmed()
    {
        var classId = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);

        var response = await _api.ConfirmAsync(holdId, studentId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ApiHelper.ReadAsync(response);
        body.GetProperty("status").GetString().Should().Be("Confirmed");
        body.GetProperty("confirmedAt").ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Null);

        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("heldSeats").GetInt32().Should().Be(0);
        cls.GetProperty("confirmedSeats").GetInt32().Should().Be(1);
        cls.GetProperty("availableSeats").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task Confirming_twice_counts_the_seat_only_once()
    {
        var classId = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);

        var first = await _api.ConfirmAsync(holdId, studentId);
        var second = await _api.ConfirmAsync(holdId, studentId);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await ApiHelper.ReadAsync(first);
        var secondBody = await ApiHelper.ReadAsync(second);
        secondBody.GetProperty("status").GetString().Should().Be("Confirmed");
        secondBody.GetProperty("confirmedAt").GetDateTimeOffset()
            .Should().BeCloseTo(firstBody.GetProperty("confirmedAt").GetDateTimeOffset(), TimeSpan.FromMilliseconds(1)); // Postgres keeps microseconds, .NET keeps 100 ns

        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("confirmedSeats").GetInt32().Should().Be(1);
        cls.GetProperty("availableSeats").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task Another_student_cannot_read_confirm_or_cancel_the_hold()
    {
        var classId = await _api.CreateClassAsync(5);
        var owner = ApiHelper.NewStudentId();
        var intruder = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, owner);

        var read = await _api.GetHoldAsync(holdId, intruder);
        var confirm = await _api.ConfirmAsync(holdId, intruder);
        var cancel = await _api.CancelAsync(holdId, intruder);

        foreach (var response in new[] { read, confirm, cancel })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("not_your_hold");
        }
        // The owner's hold is untouched.
        (await _api.HoldStatusAsync(holdId, owner)).Should().Be("Held");
    }

    [Fact]
    public async Task Cancelled_hold_cannot_be_confirmed()
    {
        var classId = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);
        await _api.CancelAsync(holdId, studentId);

        var response = await _api.ConfirmAsync(holdId, studentId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("hold_cancelled");
    }

    [Fact]
    public async Task Unknown_hold_returns_not_found()
    {
        var response = await _api.ConfirmAsync(Guid.NewGuid(), ApiHelper.NewStudentId());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("hold_not_found");
    }
}

