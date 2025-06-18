using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.SystemTests.Api;

[Collection("System")]
public class GroupsControllerTests : ApiTestBase
{
    public GroupsControllerTests(ITestOutputHelper output)
        : base(output)
    {
    }

    // Примечание: Это простые заглушки для тестов, которые будут реализованы позже
    // Они показывают, как можно использовать созданный клиент API

    [Fact(Skip = "Требуется подключение к рабочему API")]
    public async Task GetRootGroups_ReturnsGroups()
    {
        // Arrange & Act
        var result = await GroupsApiClient.GetRootGroups();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<GroupDto>>(result);
    }

    [Fact(Skip = "Требуется подключение к рабочему API")]
    public async Task GetGroup_WithValidId_ReturnsGroup()
    {
        // Arrange
        var testGroupId = "test-group-id";

        // Act
        var result = await GroupsApiClient.GetGroup(testGroupId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testGroupId, result.Id);
    }

    [Fact(Skip = "Требуется подключение к рабочему API")]
    public async Task GetGroup_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidGroupId = "non-existent-group";

        // Act
        var statusCode = await GroupsApiClient.GetGroupStatusCode(invalidGroupId);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, statusCode);
    }
}
