using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using DigitalTwin.API.Controllers;
using DigitalTwin.IntegrationTests.Fixtures;

namespace DigitalTwin.IntegrationTests.Tests;

public class CoachingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CoachingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    // ---- Goals ----

    [Fact]
    public async Task CreateGoal_ReturnsCreatedGoal()
    {
        // Arrange
        var request = new CreateGoalRequest
        {
            Title = "Exercise more",
            Description = "Run at least 3 times a week",
            Category = "health",
            TargetDate = DateTime.UtcNow.AddMonths(3)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/coaching/goals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetProperty("title").GetString().Should().Be("Exercise more");
        data.GetProperty("description").GetString().Should().Be("Run at least 3 times a week");
        data.GetProperty("category").GetString().Should().Be("health");
        data.GetProperty("status").GetString().Should().Be("active");
        data.GetProperty("progress").GetDouble().Should().Be(0.0);
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetGoals_ReturnsUserGoals()
    {
        // Arrange -- create a goal first
        await _client.PostAsJsonAsync("/api/coaching/goals", new CreateGoalRequest
        {
            Title = "Read more books",
            Description = "Read one book per month",
            Category = "personal_growth"
        });

        // Act
        var response = await _client.GetAsync("/api/coaching/goals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetGoals_FilteredByStatus_ReturnsMatchingGoals()
    {
        // Arrange -- create an active goal
        await _client.PostAsJsonAsync("/api/coaching/goals", new CreateGoalRequest
        {
            Title = "Learn piano",
            Category = "personal_growth"
        });

        // Act -- filter by active status
        var response = await _client.GetAsync("/api/coaching/goals?status=active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var data = json.RootElement.GetProperty("data");

        foreach (var goal in data.EnumerateArray())
        {
            goal.GetProperty("status").GetString().Should().Be("active");
        }
    }

    // ---- Journal ----

    [Fact]
    public async Task CreateJournalEntry_ReturnsCreatedEntry()
    {
        // Arrange
        var request = new CreateJournalRequest
        {
            Content = "Today was a productive day. I finished my project and felt accomplished.",
            Mood = "happy",
            Tags = new List<string> { "productivity", "accomplishment" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/coaching/journal", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetProperty("content").GetString().Should().Contain("productive day");
        data.GetProperty("mood").GetString().Should().Be("happy");
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetJournalEntries_ReturnsEntries()
    {
        // Arrange -- create an entry first
        await _client.PostAsJsonAsync("/api/coaching/journal", new CreateJournalRequest
        {
            Content = "Integration test journal entry.",
            Mood = "neutral"
        });

        // Act
        var response = await _client.GetAsync("/api/coaching/journal?limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        json.RootElement.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
    }

    // ---- Habits ----

    [Fact]
    public async Task LogHabit_ReturnsCreatedRecord()
    {
        // Arrange
        var request = new LogHabitRequest
        {
            HabitName = "meditation",
            Completed = true,
            Date = DateTime.UtcNow.Date
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/coaching/habits", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetProperty("habitName").GetString().Should().Be("meditation");
        data.GetProperty("completed").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetHabitStreak_ReturnsStreakCount()
    {
        // Arrange -- log a habit for today
        await _client.PostAsJsonAsync("/api/coaching/habits", new LogHabitRequest
        {
            HabitName = "exercise",
            Completed = true,
            Date = DateTime.UtcNow.Date
        });

        // Act
        var response = await _client.GetAsync("/api/coaching/habits/exercise/streak");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeTrue();

        var data = root.GetProperty("data");
        data.GetProperty("habitName").GetString().Should().Be("exercise");
        data.GetProperty("streak").GetInt32().Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task LogHabit_MultipleConsecutiveDays_BuildsStreak()
    {
        // Arrange -- log a habit for three consecutive days
        var habitName = $"running_{Guid.NewGuid():N}";
        for (int i = 2; i >= 0; i--)
        {
            await _client.PostAsJsonAsync("/api/coaching/habits", new LogHabitRequest
            {
                HabitName = habitName,
                Completed = true,
                Date = DateTime.UtcNow.Date.AddDays(-i)
            });
        }

        // Act
        var response = await _client.GetAsync($"/api/coaching/habits/{habitName}/streak");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var data = json.RootElement.GetProperty("data");

        data.GetProperty("streak").GetInt32().Should().BeGreaterOrEqualTo(1);
    }

    // ---- Insights ----

    [Fact]
    public async Task GetInsight_ReturnsCoachingInsight()
    {
        // Act
        var response = await _client.GetAsync("/api/coaching/insights");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    // ---- Auth ----

    [Fact]
    public async Task Coaching_RequiresAuthentication()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var goalsGet = await unauthenticatedClient.GetAsync("/api/coaching/goals");
        goalsGet.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var goalsPost = await unauthenticatedClient.PostAsJsonAsync("/api/coaching/goals",
            new CreateGoalRequest { Title = "test" });
        goalsPost.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var journalGet = await unauthenticatedClient.GetAsync("/api/coaching/journal");
        journalGet.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var habitsPost = await unauthenticatedClient.PostAsJsonAsync("/api/coaching/habits",
            new LogHabitRequest { HabitName = "test" });
        habitsPost.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
