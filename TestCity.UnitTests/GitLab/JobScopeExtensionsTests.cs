using TestCity.Core.GitLab.Models;
using Xunit;

namespace TestCity.UnitTests.GitLab;

[Collection("Global")]
public class JobScopeExtensionsTests
{
    [Fact]
    public void GetIndividualScopes_WithMultipleFlags_ReturnsIndividualScopes()
    {
        // Arrange
        var scope = JobScope.Failed | JobScope.Success | JobScope.Canceled;

        // Act
        var individualScopes = scope.GetIndividualScopes().ToList();

        // Assert
        Assert.Equal(3, individualScopes.Count);
        Assert.Contains(JobScope.Failed, individualScopes);
        Assert.Contains(JobScope.Success, individualScopes);
        Assert.Contains(JobScope.Canceled, individualScopes);
    }

    [Fact]
    public void GetIndividualScopes_WithNone_ReturnsEmptyCollection()
    {
        // Arrange
        var scope = JobScope.None;

        // Act
        var individualScopes = scope.GetIndividualScopes().ToList();

        // Assert
        Assert.Empty(individualScopes);
    }

    [Fact]
    public void GetIndividualScopes_WithAll_ReturnsAllScopes()
    {
        // Arrange
        var scope = JobScope.All;

        // Act
        var individualScopes = scope.GetIndividualScopes().ToList();

        // Assert
        Assert.Equal(9, individualScopes.Count);
        Assert.Contains(JobScope.Pending, individualScopes);
        Assert.Contains(JobScope.Running, individualScopes);
        Assert.Contains(JobScope.Failed, individualScopes);
        Assert.Contains(JobScope.Created, individualScopes);
        Assert.Contains(JobScope.Success, individualScopes);
        Assert.Contains(JobScope.Canceled, individualScopes);
        Assert.Contains(JobScope.Skipped, individualScopes);
        Assert.Contains(JobScope.WaitingForResource, individualScopes);
        Assert.Contains(JobScope.Manual, individualScopes);
    }

    [Theory]
    [InlineData(JobScope.Pending, "pending")]
    [InlineData(JobScope.Running, "running")]
    [InlineData(JobScope.Failed, "failed")]
    [InlineData(JobScope.Created, "created")]
    [InlineData(JobScope.Success, "success")]
    [InlineData(JobScope.Canceled, "canceled")]
    [InlineData(JobScope.Skipped, "skipped")]
    [InlineData(JobScope.WaitingForResource, "waiting_for_resource")]
    [InlineData(JobScope.Manual, "manual")]
    public void ToStringValue_WithValidScope_ReturnsCorrectString(JobScope scope, string expected)
    {
        // Act
        var result = scope.ToStringValue();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToStringValue_WithNone_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var scope = JobScope.None;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => scope.ToStringValue());
    }
}
