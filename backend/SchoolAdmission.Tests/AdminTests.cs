using System.Net;
using FluentAssertions;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class AdminTests
{
    private readonly ApiFixture _fixture;
    private readonly ApiHelper _api;

    public AdminTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _api = new ApiHelper(fixture.Client);
    }

    [Fact]
    public async Task Admin_can_list_all_applications_with_student_class_and_status()
    {
        var classId = await _api.CreateClassAsync(2);
        var first = ApiHelper.NewStudentId();
        var second = ApiHelper.NewStudentId();
        var firstHold = await _api.ApplyAndGetIdAsync(classId, first);
        var secondHold = await _api.ApplyAndGetIdAsync(classId, second);
        (await _api.ConfirmAsync(secondHold, second)).EnsureSuccessStatusCode();

        var response = await _fixture.Client.GetAsync("/admin/holds");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = (await ApiHelper.ReadAsync(response)).EnumerateArray().ToList();

        var held = items.Single(i => i.GetProperty("holdId").GetGuid() == firstHold);
        held.GetProperty("studentId").GetString().Should().Be(first);
        held.GetProperty("classId").GetInt32().Should().Be(classId);
        held.GetProperty("className").GetString().Should().NotBeNullOrEmpty();
        held.GetProperty("status").GetString().Should().Be("HELD");

        var confirmed = items.Single(i => i.GetProperty("holdId").GetGuid() == secondHold);
        confirmed.GetProperty("studentId").GetString().Should().Be(second);
        confirmed.GetProperty("status").GetString().Should().Be("CONFIRMED");
    }
}
