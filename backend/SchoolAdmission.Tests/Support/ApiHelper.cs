using System.Net.Http.Json;
using System.Text.Json;

namespace SchoolAdmission.Tests.Support;

// Small wrapper around the API so each test reads like a story, not like HTTP plumbing.
public sealed class ApiHelper
{
    private static long _lastStudentId = Random.Shared.NextInt64(10_000_000_000_000_000L, 90_000_000_000_000_000L);

    private readonly HttpClient _client;

    public ApiHelper(HttpClient client) => _client = client;

    // A unique 17-digit registration number, so two tests never share a student.
    public static string NewStudentId() => Interlocked.Increment(ref _lastStudentId).ToString();

    public async Task<int> CreateClassAsync(int totalSeats)
    {
        var name = "Class " + Guid.NewGuid().ToString("N")[..8];
        var response = await _client.PostAsJsonAsync("/classes", new { name, shiftName = "Morning", totalSeats });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetInt32();
    }

    public async Task<JsonElement> GetClassAsync(int classId)
    {
        var response = await _client.GetAsync($"/classes/{classId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public Task<HttpResponseMessage> ApplyAsync(int classId, string? studentId) =>
        Send(HttpMethod.Post, $"/classes/{classId}/holds", studentId);

    public Task<HttpResponseMessage> GetHoldAsync(Guid holdId, string? studentId) =>
        Send(HttpMethod.Get, $"/holds/{holdId}", studentId);

    public Task<HttpResponseMessage> ConfirmAsync(Guid holdId, string? studentId) =>
        Send(HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);

    public Task<HttpResponseMessage> CancelAsync(Guid holdId, string? studentId) =>
        Send(HttpMethod.Delete, $"/holds/{holdId}", studentId);

    public Task<HttpResponseMessage> MyHoldsAsync(string? studentId) =>
        Send(HttpMethod.Get, "/me/holds", studentId);

    // Applies for a seat and returns the hold id. Throws if the apply was not accepted.
    public async Task<Guid> ApplyAndGetIdAsync(int classId, string studentId)
    {
        var response = await ApplyAsync(classId, studentId);
        response.EnsureSuccessStatusCode();
        var json = await ReadAsync(response);
        return json.GetProperty("holdId").GetGuid();
    }

    public async Task<string?> HoldStatusAsync(Guid holdId, string studentId)
    {
        var response = await GetHoldAsync(holdId, studentId);
        response.EnsureSuccessStatusCode();
        return (await ReadAsync(response)).GetProperty("status").GetString();
    }

    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response) =>
        (await ReadAsync(response)).GetProperty("code").GetString();

    private Task<HttpResponseMessage> Send(HttpMethod method, string url, string? studentId)
    {
        var request = new HttpRequestMessage(method, url);
        if (studentId is not null) request.Headers.Add("X-Student-Id", studentId);
        return _client.SendAsync(request);
    }
}
