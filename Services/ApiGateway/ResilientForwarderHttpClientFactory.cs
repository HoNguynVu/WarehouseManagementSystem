using Polly;
using Polly.Extensions.Http;
using Yarp.ReverseProxy.Forwarder;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApiGateway
{
    // A custom DelegatingHandler to execute Polly policies
    public class PolicyHandler : DelegatingHandler
    {
        private readonly IAsyncPolicy<HttpResponseMessage> _policy;

        public PolicyHandler(IAsyncPolicy<HttpResponseMessage> policy)
        {
            _policy = policy;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _policy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
        }
    }

    public class ResilientForwarderHttpClientFactory : IForwarderHttpClientFactory
    {
        private readonly ForwarderHttpClientFactory _defaultFactory;

        public ResilientForwarderHttpClientFactory(ILogger<ForwarderHttpClientFactory> logger)
        {
            _defaultFactory = new ForwarderHttpClientFactory(logger);
        }

        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
        {
            var handler = new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = System.Net.DecompressionMethods.None,
                UseCookies = false,
                ActivityHeadersPropagator = System.Diagnostics.DistributedContextPropagator.CreateDefaultPropagator()
            };

            // Configure Polly policies
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

            var circuitBreakerHandler = new PolicyHandler(circuitBreakerPolicy)
            {
                InnerHandler = handler
            };

            var retryHandler = new PolicyHandler(retryPolicy)
            {
                InnerHandler = circuitBreakerHandler
            };

            return new HttpMessageInvoker(retryHandler, disposeHandler: true);
        }
    }
}
