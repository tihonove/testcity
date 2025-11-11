using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TestCity.Api.Exceptions;

public class HttpStatusExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is HttpStatusException httpStatusException)
        {
            context.Result = new ObjectResult(httpStatusException.Message)
            {
                StatusCode = httpStatusException.StatusCode
            };
            context.ExceptionHandled = true;
        }
    }
}
