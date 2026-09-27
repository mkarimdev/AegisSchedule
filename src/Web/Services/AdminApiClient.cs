using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Web.Models;

namespace Web.Services;

public class AdminApiClient : IAdminApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdminApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public AdminApiClient(HttpClient httpClient, ILogger<AdminApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private static async Task HandleErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var doc = JsonDocument.Parse(errorBody);
            if (doc.RootElement.TryGetProperty("error", out var errProp))
            {
                throw new HttpRequestException(errProp.GetString(), null, response.StatusCode);
            }
        }
        catch (JsonException)
        {
        }

        throw new HttpRequestException(!string.IsNullOrWhiteSpace(errorBody) ? errorBody : response.ReasonPhrase, null, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Courses
    // -------------------------------------------------------------------------

    public async Task<List<AdminCourseDto>> GetCoursesAsync(int? academicLevel = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (academicLevel.HasValue) queryParams.Add($"academicLevel={academicLevel.Value}");
        if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var response = await _httpClient.GetAsync($"api/admin/courses{queryString}", cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var courses = await response.Content.ReadFromJsonAsync<List<AdminCourseDto>>(JsonOptions, cancellationToken);
        return courses ?? [];
    }

    public async Task<AdminCourseDto?> GetCourseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/admin/courses/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await HandleErrorAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<AdminCourseDto>(JsonOptions, cancellationToken);
    }

    public async Task<AdminCourseDto> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/courses", request, JsonOptions, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminCourseDto>(JsonOptions, cancellationToken);
        return result!;
    }

    public async Task<AdminCourseDto> UpdateCourseAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/courses/{id}", request, JsonOptions, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminCourseDto>(JsonOptions, cancellationToken);
        return result!;
    }

    public async Task DeleteCourseAsync(Guid id, bool force = false, CancellationToken cancellationToken = default)
    {
        var url = $"api/admin/courses/{id}" + (force ? "?force=true" : string.Empty);
        var response = await _httpClient.DeleteAsync(url, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Offerings
    // -------------------------------------------------------------------------

    public async Task<List<CourseOfferingDto>> GetOfferingsAsync(Guid? termId = null, int? academicLevel = null, Guid? courseId = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (termId.HasValue && termId.Value != Guid.Empty) queryParams.Add($"termId={termId.Value}");
        if (academicLevel.HasValue) queryParams.Add($"academicLevel={academicLevel.Value}");
        if (courseId.HasValue && courseId.Value != Guid.Empty) queryParams.Add($"courseId={courseId.Value}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var response = await _httpClient.GetAsync($"api/admin/offerings{queryString}", cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var offerings = await response.Content.ReadFromJsonAsync<List<CourseOfferingDto>>(JsonOptions, cancellationToken);
        return offerings ?? [];
    }

    public async Task<AdminOfferingDetailDto?> GetOfferingDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/admin/offerings/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await HandleErrorAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<AdminOfferingDetailDto>(JsonOptions, cancellationToken);
    }

    public async Task<AdminOfferingDetailDto> CreateOfferingAsync(CreateOfferingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/offerings", request, JsonOptions, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminOfferingDetailDto>(JsonOptions, cancellationToken);
        return result!;
    }

    public async Task DeleteOfferingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/offerings/{id}", cancellationToken);
        await HandleErrorAsync(response, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Schedule Slots & Meetings
    // -------------------------------------------------------------------------

    public async Task<AdminActivityResponse> CreateActivityAsync(Guid offeringId, CreateActivityRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/admin/offerings/{offeringId}/activities", request, JsonOptions, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminActivityResponse>(JsonOptions, cancellationToken);
        return result!;
    }

    public async Task DeleteActivityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/activities/{id}", cancellationToken);
        await HandleErrorAsync(response, cancellationToken);
    }

    public async Task<AdminActivityGroupResponse> CreateActivityGroupAsync(Guid activityId, CreateActivityGroupRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/admin/activities/{activityId}/groups", request, JsonOptions, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminActivityGroupResponse>(JsonOptions, cancellationToken);
        return result!;
    }

    public async Task<AdminActivityGroupResponse> UpdateActivityGroupAsync(Guid id, UpdateActivityGroupRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/groups/{id}", request, JsonOptions, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminActivityGroupResponse>(JsonOptions, cancellationToken);
        return result!;
    }

    public async Task DeleteActivityGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/groups/{id}", cancellationToken);
        await HandleErrorAsync(response, cancellationToken);
    }

    public async Task<AdminMeetingDetailDto> CreateMeetingAsync(Guid groupId, CreateMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/admin/groups/{groupId}/meetings", request, JsonOptions, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminMeetingDetailDto>(JsonOptions, cancellationToken);
        return result!;
    }

    public async Task<AdminMeetingDetailDto> UpdateMeetingAsync(Guid id, UpdateMeetingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/meetings/{id}", request, JsonOptions, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminMeetingDetailDto>(JsonOptions, cancellationToken);
        return result!;
    }

    public async Task DeleteMeetingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/meetings/{id}", cancellationToken);
        await HandleErrorAsync(response, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Terms
    // -------------------------------------------------------------------------

    public async Task<List<AdminTermDto>> GetTermsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/admin/terms", cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var terms = await response.Content.ReadFromJsonAsync<List<AdminTermDto>>(JsonOptions, cancellationToken);
        return terms ?? [];
    }

    public async Task<AdminTermDto> CreateTermAsync(CreateTermRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/terms", request, JsonOptions, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminTermDto>(JsonOptions, cancellationToken);
        return result!;
    }

    public async Task<AdminTermDto> SetCurrentTermAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsync($"api/admin/terms/{id}/set-current", null, cancellationToken);
        await HandleErrorAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<AdminTermDto>(JsonOptions, cancellationToken);
        return result!;
    }
}
