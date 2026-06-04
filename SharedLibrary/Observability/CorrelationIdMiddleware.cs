using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SharedLibrary.Observability
{
    public class CorrelationIdMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var incomingCorrelationId = context.Request.Headers[CorrelationConstants.HeaderName].FirstOrDefault();
            var correlationId = string.IsNullOrWhiteSpace(incomingCorrelationId)
                ? Guid.NewGuid().ToString("N")
                : incomingCorrelationId;

            CorrelationContext.CorrelationId = correlationId;
            context.Request.Headers[CorrelationConstants.HeaderName] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationConstants.HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty(CorrelationConstants.LogPropertyName, correlationId))
            {
                await next(context);
            }
        }
    }
}
