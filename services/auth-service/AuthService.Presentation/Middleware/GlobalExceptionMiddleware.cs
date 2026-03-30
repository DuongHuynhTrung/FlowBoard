using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AuthService.Application.Exceptions;

namespace AuthService.Presentation.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occurred while executing the request.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var problemDetails = new ProblemDetails
            {
                Instance = context.Request.Path,
                Title = "An error occurred while processing your request.",
                Detail = exception.Message,
                Status = (int)HttpStatusCode.InternalServerError
            };

            switch (exception)
            {
                case UnauthorizedException e:
                    problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                    problemDetails.Title = "Unauthorized";
                    break;
                case ConflictException e:
                    problemDetails.Status = (int)HttpStatusCode.Conflict;
                    problemDetails.Title = "Conflict";
                    break;
                case FluentValidation.ValidationException e:
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Title = "Validation Failed";
                    problemDetails.Detail = JsonSerializer.Serialize(e.Errors);
                    break;
            }

            response.StatusCode = problemDetails.Status.Value;
            var result = JsonSerializer.Serialize(problemDetails);
            return response.WriteAsync(result);
        }
    }
}
