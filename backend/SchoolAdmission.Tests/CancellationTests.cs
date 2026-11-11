using System.Net;
using System.Text.Json;
using FluentAssertions;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class CancellationTests
{
    private readonly ApiFixture _fixture;
    private readonly ApiHelper _api;

    public CancellationTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _api = new ApiHelper(fixture.Client);
    }

    [Fact]
    public async Task Cancelling_releases_the_seat_for_someone_else()
    {
        var classId = await _api.CreateClassAsync(1);
        var first = ApiHelper.NewStudentId();
        var second = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, first);

        var cancel = await _api.CancelAsync(holdId, first);

        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var hold = await ApiHelper.ReadAsync(await _api.GetHoldAsync(holdId, first));
        hold.GetProperty("status").GetString().Should().Be("CANCELLED");
        hold.GetProperty("cancelledAt").ValueKind.Should().NotBe(JsonValueKind.Null);
        (await _api.ApplyAsync(classId, second)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Cancelling_twice_is_harmless()
    {
        var classId = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);

        (await _api.CancelAsync(holdId, studentId)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var again = await _api.CancelAsync(holdId, studentId);

        again.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _api.HoldStatusAsync(holdId, studentId)).Should().Be("CANCELLED");
    }

    [Fact]
    public async Task Confirmed_application_can_be_cancelled_and_releases_its_seat()
    {
        var classId = await _api.CreateClassAsync(1);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);
        await _api.ConfirmAsync(holdId, studentId);

        var cancel = await _api.CancelAsync(holdId, studentId);

        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("confirmedSeats").GetInt32().Should().Be(0);
        cls.GetProperty("availableSeats").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Student_can_apply_again_after_cancelling()
    {
        var classId = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);
        await _api.CancelAsync(holdId, studentId);

        var again = await _api.ApplyAsync(classId, studentId);

        again.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task History_keeps_cancelled_applications_newest_first_and_is_private()
    {
        var firstClass = await _api.CreateClassAsync(5);
        var secondClass = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();

        var firstHold = await _api.ApplyAndGetIdAsync(firstClass, studentId);
        await _api.CancelAsync(firstHold, studentId);
        _fixture.Clock.Advance(TimeSpan.FromSeconds(1)); // so the two applications have different times
        await _api.ApplyAndGetIdAsync(secondClass, studentId);

        var mine = await _api.MyHoldsAsync(studentId);
        var items = (await ApiHelper.ReadAsync(mine)).EnumerateArray().ToList();

        mine.StatusCode.Should().Be(HttpStatusCode.OK);
        items.Should().HaveCount(2);
        items[0].GetProperty("classId").GetInt32().Should().Be(secondClass);
        items[1].GetProperty("classId").GetInt32().Should().Be(firstClass);
        items[1].GetProperty("status").GetString().Should().Be("CANCELLED");

        var other = await _api.MyHoldsAsync(ApiHelper.NewStudentId());
        (await ApiHelper.ReadAsync(other)).GetArrayLength().Should().Be(0);
    }
}
