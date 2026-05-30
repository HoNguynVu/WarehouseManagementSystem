using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogInformation("Starting request {RequestName}", requestName);

            var timer = Stopwatch.StartNew();

            try
            {
                var response = await next();
                timer.Stop();
                _logger.LogInformation("Completed request {RequestName} in {ElapsedMilliseconds}ms", requestName, timer.ElapsedMilliseconds);
                return response;
            }
            catch (System.Exception ex)
            {
                timer.Stop();
                _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMilliseconds}ms with message: {Message}", requestName, timer.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
