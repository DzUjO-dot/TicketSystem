using System.Net;
using Serilog;

namespace ITServiceHelpDesk.Infrastructure.Middleware;

/// <summary>
/// Middleware do globalnej obsługi wyjątków
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception
        Log.Error(exception, 
            "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
            context.TraceIdentifier,
            context.Request.Path,
            context.Request.Method);

        // Set response
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // For AJAX requests, return JSON
        if (IsAjaxRequest(context.Request))
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                error = "Wystąpił błąd podczas przetwarzania żądania.",
                traceId = context.TraceIdentifier
            };
            await context.Response.WriteAsJsonAsync(response);
        }
        else
        {
            // Redirect to error page for regular requests
            context.Response.Redirect($"/Home/Error?traceId={context.TraceIdentifier}");
        }
    }

    private static bool IsAjaxRequest(HttpRequest request)
    {
        return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               request.Headers["Accept"].ToString().Contains("application/json");
    }
}
