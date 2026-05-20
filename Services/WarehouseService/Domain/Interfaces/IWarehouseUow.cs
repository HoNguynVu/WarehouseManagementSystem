using SharedLibrary.Seedwork;

namespace Domain.Interfaces
{
    public interface IWarehouseUow : ITransactionManager
    {
        IWarehouseRepository Warehouse { get; }
        Task<bool> SaveChangeAsync(CancellationToken cancellationToken = default);
        void ClearTracker();
    }
}
