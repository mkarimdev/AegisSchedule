using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Web.Models;
using Web.Services;
using Xunit;

namespace Api.Tests;

public class SchedulingApiClientTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = req => Task.FromResult(handler(req));
        }

        public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }

    private static SchedulingApiClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://localhost:7104/")
        };
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<SchedulingApiClient>();
        return new SchedulingApiClient(httpClient, logger);
    }

    private static SchedulingApiClient CreateClientAsync(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://localhost:7104/")
        };
        var logger = LoggerFactory.Create(_ => { }).CreateLogger<SchedulingApiClient>();
        return new SchedulingApiClient(httpClient, logger);
    }

    [Fact]
    public async Task GetLevelsAsync_RequestsCorrectUrlAndDeserializesLevels()
    {
        // Arrange
        var expectedLevelId = Guid.NewGuid();
        string requestedUri = string.Empty;

        var client = CreateClient(req =>
        {
            requestedUri = req.RequestUri!.PathAndQuery;
            var json = JsonSerializer.Serialize(new List<AcademicLevelDto>
            {
                new() { Id = expectedLevelId, LevelNumber = 1, DisplayName = "Level 1" }
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        // Act
        var result = await client.GetLevelsAsync();

        // Assert
        Assert.Equal("/api/catalog/levels", requestedUri);
        Assert.Single(result);
        Assert.Equal(expectedLevelId, result[0].Id);
        Assert.Equal(1, result[0].LevelNumber);
        Assert.Equal("Level 1", result[0].DisplayName);
    }

    [Fact]
    public async Task GetOfferingsAsync_WithFilters_EncodesQueryParametersAndDeserializesResponse()
    {
        // Arrange
        string requestedUri = string.Empty;
        var termGuid = Guid.NewGuid();
        var offeringGuid = Guid.NewGuid();

        var client = CreateClient(req =>
        {
            requestedUri = req.RequestUri!.PathAndQuery;
            var json = JsonSerializer.Serialize(new List<CourseOfferingDto>
            {
                new()
                {
                    Id = offeringGuid,
                    CourseCode = "CS201",
                    CourseName = "Databases",
                    AcademicLevelNumber = 2,
                    TermName = "Fall 2026",
                    ActivityTypes = ["Lecture", "Lab"],
                    Groups =
                    [
                        new() { Id = Guid.NewGuid(), Name = "Group 1", ActivityType = "Lecture" }
                    ]
                }
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        // Act
        var result = await client.GetOfferingsAsync(academicLevel: 2, termId: termGuid);

        // Assert
        Assert.Contains("academicLevel=2", requestedUri);
        Assert.Contains($"termId={termGuid}", requestedUri);
        Assert.Single(result);
        Assert.Equal("CS201", result[0].CourseCode);
        Assert.Equal(2, result[0].AcademicLevelNumber);
        Assert.Equal(2, result[0].ActivityTypes.Count);
        Assert.Single(result[0].Groups);
        Assert.Equal("Group 1", result[0].Groups[0].Name);
    }

    [Fact]
    public async Task GetOfferingsAsync_WithoutFilters_HasNoQueryString()
    {
        // Arrange
        string requestedUri = string.Empty;

        var client = CreateClient(req =>
        {
            requestedUri = req.RequestUri!.PathAndQuery;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            };
        });

        // Act
        var result = await client.GetOfferingsAsync();

        // Assert
        Assert.Equal("/api/catalog/offerings", requestedUri);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateSchedulesAsync_PostsJsonPayloadAndDeserializesResult()
    {
        // Arrange
        string requestedUri = string.Empty;
        HttpMethod? requestedMethod = null;
        string receivedPayload = string.Empty;

        var offeringId = Guid.NewGuid();
        var primaryGroupId = Guid.NewGuid();
        var request = new GenerateScheduleRequest
        {
            AcademicLevelNumber = 1,
            AssignedPrimaryGroupId = primaryGroupId,
            SelectedCourseOfferingIds = [offeringId]
        };

        var client = CreateClientAsync(async req =>
        {
            requestedUri = req.RequestUri!.PathAndQuery;
            requestedMethod = req.Method;
            receivedPayload = await req.Content!.ReadAsStringAsync();

            var responseObj = new GenerateScheduleResponse
            {
                IsSuccess = true,
                TotalCombinationsEvaluated = 1,
                Schedules =
                [
                    new()
                    {
                        ScheduleId = Guid.NewGuid(),
                        SelectedGroups =
                        [
                            new()
                            {
                                GroupId = primaryGroupId,
                                GroupName = "Group 1",
                                CourseCode = "CS101",
                                ActivityType = "Lecture",
                                Meetings =
                                [
                                    new()
                                    {
                                        DayOfWeek = DayOfWeek.Saturday,
                                        StartTime = new TimeOnly(8, 30),
                                        EndTime = new TimeOnly(10, 30),
                                        Room = "Hall 101"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };

            var json = JsonSerializer.Serialize(responseObj);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        // Act
        var response = await client.GenerateSchedulesAsync(request);

        // Assert
        Assert.Equal("/api/schedules/generate", requestedUri);
        Assert.Equal(HttpMethod.Post, requestedMethod);
        Assert.Contains(offeringId.ToString(), receivedPayload);
        Assert.Contains(primaryGroupId.ToString(), receivedPayload);

        Assert.True(response.IsSuccess);
        Assert.Single(response.Schedules);
        Assert.Single(response.Schedules[0].SelectedGroups);
        Assert.Equal("CS101", response.Schedules[0].SelectedGroups[0].CourseCode);
        Assert.Equal(new TimeOnly(8, 30), response.Schedules[0].SelectedGroups[0].Meetings[0].StartTime);
    }

    [Fact]
    public async Task GenerateSchedulesAsync_WhenApiReturnsBadRequest_DeserializesErrorResponse()
    {
        // Arrange
        var request = new GenerateScheduleRequest { AcademicLevelNumber = 1 };

        var client = CreateClient(req =>
        {
            var errorResponse = new GenerateScheduleResponse
            {
                IsSuccess = false,
                ErrorMessage = "Invalid course offering specified."
            };

            var json = JsonSerializer.Serialize(errorResponse);
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        // Act
        var result = await client.GenerateSchedulesAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid course offering specified.", result.ErrorMessage);
    }
}
