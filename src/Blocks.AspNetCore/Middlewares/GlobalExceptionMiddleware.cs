using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FluentValidation;
using Blocks.Domain;
using Blocks.Exceptions;

namespace Blocks.AspNetCore.Middlewares;

public sealed class GlobalExceptionMiddleware(RequestDelegate _next, ILogger<GlobalExceptionMiddleware> _logger, IWebHostEnvironment _env)
{
    private const string UnexpectedErrorMessage = "An unexpected error occurred.";

    private static readonly JsonSerializerOptions ReplyJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static HttpStatusCode MapStatusCode(Exception ex) => ex switch
    {
        ValidationException => HttpStatusCode.BadRequest,
        ArgumentException => HttpStatusCode.BadRequest,
        BadRequestException => HttpStatusCode.BadRequest,
        NotFoundException => HttpStatusCode.NotFound,
        DomainException => HttpStatusCode.BadRequest,
        UnauthorizedException => HttpStatusCode.Unauthorized,
        ForbiddenException => HttpStatusCode.Forbidden,
        ConflictException => HttpStatusCode.Conflict,
        BadGatewayException => HttpStatusCode.BadGateway,
        HttpException httpException => httpException.HttpStatusCode,
        _ => HttpStatusCode.InternalServerError
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (context.RequestAborted.IsCancellationRequested && IsCausedByCancellation(ex))
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499;
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static bool IsCausedByCancellation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException)
                return true;
        }
        return false;
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = MapStatusCode(exception);
        if (statusCode >= HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);
        }

        var response = new
        {
            StatusCode = (int)statusCode,
            Message = statusCode == HttpStatusCode.InternalServerError && !_env.IsDevelopment()
                ? UnexpectedErrorMessage
                : exception.Message,
            TraceId = context.TraceIdentifier,
            Details = _env.IsDevelopment() ? exception.StackTrace : null
        };

        return WriteResponseAsync(context, statusCode, response);
    }

    private Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var validationErrors = exception.Errors.Select(e => new
        {
            e.PropertyName,
            e.ErrorMessage
        });

        var response = new
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "One or more validation errors occurred.",
            TraceId = context.TraceIdentifier,
            Errors = validationErrors,
            Details = _env.IsDevelopment() ? exception.StackTrace : null
        };

        return WriteResponseAsync(context, HttpStatusCode.BadRequest, response);
    }

    private static Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, object response)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var responseJson = JsonSerializer.Serialize(response, ReplyJsonOptions);
        return context.Response.WriteAsync(responseJson);
    }
}
