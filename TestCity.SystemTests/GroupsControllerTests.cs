using FluentAssertions;
using TestCity.SystemTests.Api;
using Xunit;
using Xunit.Abstractions;

namespace TestCity.SystemTests;

[Collection("System")]
public class GroupsControllerTests(ITestOutputHelper output) : ApiTestBase(output)
{
    [Fact]
    public async Task GetRootGroups_ReturnsGroups()
    {
        var result = await GroupsApiClient.GetRootGroups();
        result.Should().NotBeNull().And.BeOfType<List<GroupDto>>();
    }
}
