using Kontur.TestCity.Core.GitLab;
using NUnit.Framework;

namespace Kontur.TestCity.UnitTests.GitLab;

[TestFixture]
public class JobScopeExtensionsTests
{
    [Test]
    public void GetIndividualScopes_WithMultipleFlags_ReturnsIndividualScopes()
    {
        // Arrange
        var scope = JobScope.Failed | JobScope.Success | JobScope.Canceled;

        // Act
        var individualScopes = scope.GetIndividualScopes().ToList();

        // Assert
        Assert.That(individualScopes, Has.Count.EqualTo(4));
        Assert.That(individualScopes, Contains.Item(JobScope.Failed));
        Assert.That(individualScopes, Contains.Item(JobScope.Success));
        Assert.That(individualScopes, Contains.Item(JobScope.Canceled));
    }

    [Test]
    public void GetIndividualScopes_WithNone_ReturnsEmptyCollection()
    {
        // Arrange
        var scope = JobScope.None;

        // Act
        var individualScopes = scope.GetIndividualScopes().ToList();

        // Assert
        Assert.That(individualScopes, Is.Empty);
    }

    [Test]
    public void GetIndividualScopes_WithAll_ReturnsAllScopes()
    {
        // Arrange
        var scope = JobScope.All;

        // Act
        var individualScopes = scope.GetIndividualScopes().ToList();

        // Assert
        Assert.That(individualScopes, Has.Count.EqualTo(9));
        Assert.That(individualScopes, Contains.Item(JobScope.Pending));
        Assert.That(individualScopes, Contains.Item(JobScope.Running));
        Assert.That(individualScopes, Contains.Item(JobScope.Failed));
        Assert.That(individualScopes, Contains.Item(JobScope.Created));
        Assert.That(individualScopes, Contains.Item(JobScope.Success));
        Assert.That(individualScopes, Contains.Item(JobScope.Canceled));
        Assert.That(individualScopes, Contains.Item(JobScope.Skipped));
        Assert.That(individualScopes, Contains.Item(JobScope.WaitingForResource));
        Assert.That(individualScopes, Contains.Item(JobScope.Manual));
    }

    [TestCase(JobScope.Pending, "pending")]
    [TestCase(JobScope.Running, "running")]
    [TestCase(JobScope.Failed, "failed")]
    [TestCase(JobScope.Created, "created")]
    [TestCase(JobScope.Success, "success")]
    [TestCase(JobScope.Canceled, "canceled")]
    [TestCase(JobScope.Skipped, "skipped")]
    [TestCase(JobScope.WaitingForResource, "waiting_for_resource")]
    [TestCase(JobScope.Manual, "manual")]
    public void ToStringValue_WithValidScope_ReturnsCorrectString(JobScope scope, string expected)
    {
        // Act
        var result = scope.ToStringValue();

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ToStringValue_WithNone_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var scope = JobScope.None;

        // Act & Assert
        Assert.That(() => scope.ToStringValue(), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
    }
}
