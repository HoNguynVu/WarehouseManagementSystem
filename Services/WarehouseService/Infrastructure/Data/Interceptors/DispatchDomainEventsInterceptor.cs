using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedLibrary.Seedwork;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Data.Interceptors
{
    public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
    {
        private readonly IMediator _mediator;

        public DispatchDomainEventsInterceptor(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();
            return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await DispatchDomainEvents(eventData.Context);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private async Task DispatchDomainEvents(DbContext? context)
        {
            if (context == null) return;

            // Tìm các thực thể đang được track bởi EF Core có chứa DomainEvents
            var entriesWithEvents = context.ChangeTracker.Entries()
                .Where(e => {
                    var prop = e.Entity.GetType().GetProperty("DomainEvents");
                    if (prop == null) return false;
                    var val = prop.GetValue(e.Entity) as IReadOnlyCollection<IDomainEvent>;
                    return val != null && val.Any();
                })
                .ToList();

            if (!entriesWithEvents.Any()) return;

            // Gom tất cả các Domain Events
            var domainEvents = entriesWithEvents
                .SelectMany(e => {
                    var prop = e.Entity.GetType().GetProperty("DomainEvents")!;
                    var events = (IReadOnlyCollection<IDomainEvent>)prop.GetValue(e.Entity)!;
                    return events.ToList(); // clone danh sách
                })
                .ToList();

            // Clear sạch Domain Events trên các thực thể để không bị dispatch lại
            foreach (var entry in entriesWithEvents)
            {
                var clearMethod = entry.Entity.GetType().GetMethod("ClearDomainEvents");
                clearMethod?.Invoke(entry.Entity, null);
            }

            // Publish từng sự kiện thông qua MediatR
            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent);
            }
        }
    }
}
