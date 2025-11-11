namespace TestCity.Api.Exceptions;

public class HttpStatusException : Exception
{
    public int StatusCode { get; }

    public HttpStatusException(int statusCode) : base()
    {
        StatusCode = statusCode;
    }

    public HttpStatusException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusException(int statusCode, string message, Exception innerException) : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
