using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SharedLibrary.Observability
{
    public class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var correlationId = CorrelationContext.GetOrCreate();

            if (!request.Headers.Contains(CorrelationConstants.HeaderName))
            {
                request.Headers.TryAddWithoutValidation(CorrelationConstants.HeaderName, correlationId);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
