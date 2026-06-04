using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Sagas
{
    public class OrderStateMap : IEntityTypeConfiguration<OrderState>
    {
        public void Configure(EntityTypeBuilder<OrderState> entity)
        {
            entity.ToTable("OrderStates");
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.OrderId).HasMaxLength(64);
            entity.Property(x => x.AccountId).HasMaxLength(64);
            
            // Map optimistic concurrency property
            entity.Property(x => x.Version);
            
            // Map decimal properties
            entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        }
    }
}
