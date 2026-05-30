using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharedLibrary.Exceptions;
using SharedLibrary.Responses;

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
            var message = statusCode switch
            {
                StatusCodes.Status401Unauthorized => "You are not authorized to access.",
                StatusCodes.Status403Forbidden => "You do not have permission to access this resource.",
                StatusCodes.Status429TooManyRequests => "Too many requests. Please slow down.",
                _ => "Unexpected error."
            };

            await ModifyHeaders(context, message, statusCode);
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var message = "An unexpected error occurred. Please try again later.";
            List<string>? errors = null;

            if (ex is CustomException customEx)
            {
                statusCode = customEx.StatusCode;
                message = customEx.Message;
            }
            else if (ex is TimeoutException || ex is TaskCanceledException)
            {
                statusCode = (int)HttpStatusCode.RequestTimeout;
                message = "The request timed out. Please try again later.";
            }
            else if (ex.GetType().Name == "DbUpdateConcurrencyException")
            {
                statusCode = (int)HttpStatusCode.Conflict;
                message = "Dữ liệu đã bị thay đổi bởi người dùng khác. Vui lòng tải lại trang và thử lại.";
            }
            else if (ex.GetType().Name == "ValidationException")
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                message = "Dữ liệu không hợp lệ";

                // Reflection to extract FluentValidation errors without project reference
                var errorsProp = ex.GetType().GetProperty("Errors");
                if (errorsProp != null)
                {
                    var errorsVal = errorsProp.GetValue(ex) as System.Collections.IEnumerable;
                    if (errorsVal != null)
                    {
                        errors = new List<string>();
                        foreach (var err in errorsVal)
                        {
                            var errMsgProp = err.GetType().GetProperty("ErrorMessage");
                            if (errMsgProp != null)
                            {
                                var errMsg = errMsgProp.GetValue(err) as string;
                                if (errMsg != null) errors.Add(errMsg);
                            }
                        }
                    }
                }
            }

            await ModifyHeaders(context, message, statusCode, errors);
        }

        private async Task ModifyHeaders(HttpContext context, string message, int statusCode, List<string>? errors = null)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = ApiResponse<object>.Failure(message, statusCode, errors);

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }), System.Threading.CancellationToken.None);
        }
    }
}
