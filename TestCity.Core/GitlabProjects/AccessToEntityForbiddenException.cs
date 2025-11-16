namespace TestCity.Core.GitlabProjects;

public class AccessToEntityForbiddenException : Exception
{
    public AccessToEntityForbiddenException(string message) : base(message)
    {
    }

    public AccessToEntityForbiddenException() : base()
    {
    }

    public AccessToEntityForbiddenException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
