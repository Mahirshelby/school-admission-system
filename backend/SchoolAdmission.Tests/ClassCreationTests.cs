using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SchoolAdmission.Tests.Support;

namespace SchoolAdmission.Tests;

[Collection(ApiCollection.Name)]
public class ClassCreationTests
{
    private readonly ApiFixture _api;

    public ClassCreationTests(ApiFixture api) => _api = api;

    [Fact]
    public async Task Created_class_is_stored_and_appears_in_the_class_list()
    {
        var name = "Class " + Guid.NewGuid().ToString("N")[..8];

        var created = await _api.Client.PostAsJsonAsync("/classes", new { name, shiftName = "Morning", totalSeats = 30 });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var listed = await _api.Client.GetAsync("/classes");
        listed.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        var names = json.RootElement.EnumerateArray().Select(e => e.GetProperty("name").GetString());
        names.Should().Contain(name);
    }
}
