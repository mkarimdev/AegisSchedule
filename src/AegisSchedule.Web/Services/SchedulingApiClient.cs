using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AegisSchedule.Web.Models;

namespace AegisSchedule.Web.Services;

public class SchedulingApiClient : ISchedulingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SchedulingApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SchedulingApiClient(HttpClient httpClient, ILogger<SchedulingApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<AcademicLevelDto>> GetLevelsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/catalog/levels", cancellationToken);
        response.EnsureSuccessStatusCode();

        var levels = await response.Content.ReadFromJsonAsync<List<AcademicLevelDto>>(JsonOptions, cancellationToken);
        return levels ?? [];
    }

    public async Task<List<CourseOfferingDto>> GetOfferingsAsync(int? academicLevel = null, Guid? termId = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (academicLevel.HasValue)
        {
            queryParams.Add($"academicLevel={academicLevel.Value}");
        }

        if (termId.HasValue && termId.Value != Guid.Empty)
        {
            queryParams.Add($"termId={termId.Value}");
        }

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var response = await _httpClient.GetAsync($"api/catalog/offerings{queryString}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var offerings = await response.Content.ReadFromJsonAsync<List<CourseOfferingDto>>(JsonOptions, cancellationToken);
        return offerings ?? [];
    }

    public async Task<GenerateScheduleResponse> GenerateSchedulesAsync(GenerateScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/schedules/generate", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorResult = await response.Content.ReadFromJsonAsync<GenerateScheduleResponse>(JsonOptions, cancellationToken);
                if (errorResult != null)
                {
                    return errorResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize error response from api/schedules/generate.");
            }

            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<GenerateScheduleResponse>(JsonOptions, cancellationToken);
        return result ?? new GenerateScheduleResponse { IsSuccess = false, ErrorMessage = "Failed to deserialize response." };
    }
}
