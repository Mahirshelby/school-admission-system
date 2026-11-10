using System.Net;
using FluentAssertions;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class ConcurrencyTests
{
    private readonly ApiHelper _api;

    public ConcurrencyTests(ApiFixture fixture) => _api = new ApiHelper(fixture.Client);

    // Runs the same action many times at once. Every task waits at a gate, then all are released together.
    private static async Task<HttpResponseMessage[]> RunAtTheSameTimeAsync(int count, Func<int, Task<HttpResponseMessage>> action)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, count)
            .Select(async i =>
            {
                await gate.Task;
                return await action(i);
            })
            .ToList();

        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Forty_students_competing_for_ten_seats_get_exactly_ten()
    {
        const int seats = 10;
        const int students = 40;
        var classId = await _api.CreateClassAsync(seats);
        var ids = Enumerable.Range(0, students).Select(_ => ApiHelper.NewStudentId()).ToArray();

        var responses = await RunAtTheSameTimeAsync(students, i => _api.ApplyAsync(classId, ids[i]));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(seats);
        var rejected = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();
        rejected.Should().HaveCount(students - seats); // no 500s: every other request got a clean 409
        foreach (var response in rejected)
        {
            (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("class_full");
        }

        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("heldSeats").GetInt32().Should().Be(seats);
        cls.GetProperty("availableSeats").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task One_student_sending_ten_simultaneous_applies_gets_one_hold()
    {
        var classId = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();

        var responses = await RunAtTheSameTimeAsync(10, _ => _api.ApplyAsync(classId, studentId));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        foreach (var response in responses.Where(r => r.StatusCode != HttpStatusCode.Created))
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await ApiHelper.ReadErrorCodeAsync(response)).Should().Be("already_applied");
        }

        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("heldSeats").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Ten_simultaneous_confirms_count_the_seat_once()
    {
        var classId = await _api.CreateClassAsync(5);
        var studentId = ApiHelper.NewStudentId();
        var holdId = await _api.ApplyAndGetIdAsync(classId, studentId);

        var responses = await RunAtTheSameTimeAsync(10, _ => _api.ConfirmAsync(holdId, studentId));

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        var cls = await _api.GetClassAsync(classId);
        cls.GetProperty("confirmedSeats").GetInt32().Should().Be(1);
        cls.GetProperty("heldSeats").GetInt32().Should().Be(0);
        cls.GetProperty("availableSeats").GetInt32().Should().Be(4);
    }
}
