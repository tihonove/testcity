using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.SystemTests.Api;

[Collection("System")]
public class GitlabControllerTests(ITestOutputHelper output) : ApiTestBase(output)
{

    // Примечание: Это простые заглушки для тестов, которые будут реализованы позже
    // Они показывают, как можно использовать созданный клиент API

    [Fact(Skip = "Требуется подключение к рабочему API")]
    public async Task GetCodeQuality_WithValidIds_ReturnsData()
    {
        // Arrange
        var projectId = 12345L;
        var jobId = 67890L;

        // Act
        var result = await GitlabApiClient.GetCodeQuality(projectId, jobId);

        // Assert
        Assert.NotNull(result);
    }

    [Fact(Skip = "Требуется подключение к рабочему API")]
    public async Task GetCodeQuality_WithInvalidIds_ReturnsNotFound()
    {
        // Arrange
        var projectId = 99999L;
        var jobId = 99999L;

        // Act
        var statusCode = await GitlabApiClient.GetCodeQualityStatusCode(projectId, jobId);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, statusCode);
    }

    [Fact(Skip = "Требуется подключение к рабочему API")]
    public async Task PostWebhook_WithValidData_ReturnsOk()
    {
        // Arrange
        var webhookData = new GitLabJobEventInfo
        {
            ProjectId = 12345,
            BuildId = 67890,
            BuildStatus = "success"
        };

        // Act
        var response = await GitlabApiClient.PostWebhook(webhookData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CheckProjectAccess_WithValidProjectId_ReturnsOk()
    {
        var projectId = 70134580L;
        var statusCode = await GitlabApiClient.CheckProjectAccessStatusCode(projectId);
        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task CheckHealth()
    {
        await TestCityApiClient.CheckHealth();
    }

    [Fact]
    public async Task AddProject_WithValidProjectId_ReturnsOk()
    {
        var projectId = 70134580L;
        await GitlabApiClient.AddProjectStatusCode(projectId);
    }

    [Fact(Skip = "Требуется подключение к рабочему API")]
    public async Task GetManualJobInfos_WithValidIds_ReturnsData()
    {
        // Arrange
        var projectId = 12345L;
        var pipelineId = 67890L;

        // Act
        var result = await GitlabApiClient.GetManualJobInfos(projectId, pipelineId);

        // Assert
        Assert.NotNull(result);
    }
}
