using SharedLibrary.Seedwork;

namespace Domain.Interfaces
{
    public interface IWarehouseUow
    {
        IWarehouseRepository Warehouse { get; }
        Task<bool> SaveChangeAsync(CancellationToken cancellationToken = default);
        void ClearTracker();
    }
}
