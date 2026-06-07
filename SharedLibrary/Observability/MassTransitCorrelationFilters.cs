using MassTransit;
using System;
using System.Threading.Tasks;

namespace SharedLibrary.Observability
{
    public class CorrelationPublishFilter<T> : IFilter<PublishContext<T>>
        where T : class
    {
        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope(nameof(CorrelationPublishFilter<T>));
        }

        public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            var correlationId = CorrelationContext.GetOrCreate();
            context.Headers.Set(CorrelationConstants.HeaderName, correlationId);
            return next.Send(context);
        }
    }

    public class CorrelationSendFilter<T> : IFilter<SendContext<T>>
        where T : class
    {
        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope(nameof(CorrelationSendFilter<T>));
        }

        public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            var correlationId = CorrelationContext.GetOrCreate();
            context.Headers.Set(CorrelationConstants.HeaderName, correlationId);
            return next.Send(context);
        }
    }

    public class CorrelationConsumeFilter<T> : IFilter<ConsumeContext<T>>
        where T : class
    {
        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope(nameof(CorrelationConsumeFilter<T>));
        }

        public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            var correlationId = context.Headers.Get<string>(CorrelationConstants.HeaderName)
                ?? context.CorrelationId?.ToString()
                ?? context.MessageId?.ToString()
                ?? Guid.NewGuid().ToString("N");

            CorrelationContext.CorrelationId = correlationId;

            using (Serilog.Context.LogContext.PushProperty(CorrelationConstants.LogPropertyName, correlationId))
            {
                await next.Send(context);
            }
        }
    }
}
