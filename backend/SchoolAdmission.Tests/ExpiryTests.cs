using System.Net;
using FluentAssertions;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class ExpiryTests
{
    private static readonly TimeSpan PastExpiry = TimeSpan.FromSeconds(ApiFixture.HoldDurationSeconds + 1);

    private readonly ApiFixture _fixture;
    private readonly ApiHelper _api;

    public ExpiryTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _api = new ApiHelper(fixture.Client);
    }

    [Fact]
    public async Task Expired_hold_is_reported_as_expired_and_its_seat_is_released()
    {
        var classId = await _api.CreateClassAsync(1);
        var first = ApiHelper.NewStudentId();
        var second = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, first);

        // While the hold is alive, the only seat is taken.
        (await _api.ApplyAsync(classId, second)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        _fixture.Clock.Advance(PastExpiry);

        (await _api.HoldStatusAsync(holdId, first)).Should().Be("Expired");
        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("heldSeats").GetInt32().Should().Be(0);
        cls.GetProperty("availableSeats").GetInt32().Should().Be(1);

        // The released seat can be taken by someone else.
        (await _api.ApplyAsync(classId, second)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Expired_hold_cannot_be_confirmed()
    {
        var classId = await _api.CreateClassAsync(3);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);

        _fixture.Clock.Advance(PastExpiry);
        var response = await _api.ConfirmAsync(holdId, studentId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("hold_expired");
        (await _api.HoldStatusAsync(holdId, studentId)).Should().Be("Expired");
        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("confirmedSeats").GetInt32().Should().Be(0);
        cls.GetProperty("availableSeats").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Confirmed_application_never_expires()
    {
        var classId = await _api.CreateClassAsync(3);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);
        (await _api.ConfirmAsync(holdId, studentId)).StatusCode.Should().Be(HttpStatusCode.OK);

        _fixture.Clock.Advance(PastExpiry);

        (await _api.HoldStatusAsync(holdId, studentId)).Should().Be("Confirmed");
        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("confirmedSeats").GetInt32().Should().Be(1);
    }
}
