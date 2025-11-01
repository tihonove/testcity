namespace TestCity.Cerberus.Client;

public class CerberusException : Exception
{
    public CerberusException()
    {
    }

    public CerberusException(string message) : base(message)
    {
    }

    public CerberusException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class CerberusAuthenticationException : CerberusException
{
    public string? ErrorStatus { get; init; }
    public string? ErrorFixUrl { get; init; }
    public string[]? ErrorMessages { get; init; }

    public CerberusAuthenticationException()
    {
    }

    public CerberusAuthenticationException(string message) : base(message)
    {
    }

    public CerberusAuthenticationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class CerberusAccessDeniedException : CerberusException
{
    public string? ErrorStatus { get; init; }
    public string? ErrorFixUrl { get; init; }
    public string[]? ErrorMessages { get; init; }

    public CerberusAccessDeniedException()
    {
    }

    public CerberusAccessDeniedException(string message) : base(message)
    {
    }

    public CerberusAccessDeniedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
