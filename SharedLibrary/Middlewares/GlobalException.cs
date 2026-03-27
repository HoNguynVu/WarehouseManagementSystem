using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Middlewares
{
    public class GlobalException(RequestDelegate next, ILogger<GlobalException> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);

                if (!context.Response.HasStarted && context.Response.StatusCode is 401 or 403 or 429)
                {
                    await HandleUnsuccessfulStatusCodeAsync(context);
                }               
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred while processing the request: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleUnsuccessfulStatusCodeAsync(HttpContext context)
        {
            var statusCode = context.Response.StatusCode;
            var (title, message) = statusCode switch
            {
                StatusCodes.Status401Unauthorized => ("Unauthorized", "You are not authorized to access."),
                StatusCodes.Status403Forbidden => ("Forbidden", "You do not have permission to access this resource."),
                StatusCodes.Status429TooManyRequests => ("Too Many Requests", "Too many requests. Please slow down."),
                _ => ("Error", "Unexpected error.")
            };

            await ModifyHeaders(context, message, statusCode, title);
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var message = "An unexpected error occurred. Please try again later.";
            var title = "Internal Server Error";
            if(ex is TimeoutException || ex is TaskCanceledException)
            {
                statusCode = (int)HttpStatusCode.RequestTimeout;
                message = "The request timed out. Please try again later.";
                title = "Request Timeout";
            }    
            await ModifyHeaders(context, message, statusCode, title);
        }

        private async Task ModifyHeaders(HttpContext context, string message, int statusCode, string title)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                StatusCode = statusCode,
                Title = title,
                Message = message
            }), CancellationToken.None);
        }
    }
}
