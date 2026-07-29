using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OnlineStore.Application.Exceptions;

namespace OnlineStore.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Not Found"),
            BusinessException => (HttpStatusCode.BadRequest, "Business Rule Violation"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problem.Status.Value;
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
