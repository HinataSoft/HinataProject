namespace HinataProject.Api;

public class ExceptionToProblemDetailsHandler(IProblemDetailsService problemDetailsService, IWebHostEnvironment webHostEnvironment) : Microsoft.AspNetCore.Diagnostics.IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        
        var isDevelopment = webHostEnvironment.IsDevelopment() || webHostEnvironment.IsEnvironment("Testing");

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred while processing your request",
                Detail = isDevelopment ? exception.Message : "An internal server error has occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
                Extensions =
                {
                    ["TraceIdentifier"] = httpContext.TraceIdentifier,
                    ["ErrorKey"] = isDevelopment ? exception.GetType().Name : true
                }
            },
            Exception = isDevelopment ? exception : null
        };
        
        return await problemDetailsService.TryWriteAsync(context);
    }
}