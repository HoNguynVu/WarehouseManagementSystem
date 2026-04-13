using SharedLibrary.Seedwork;

namespace Domain.Interfaces
{
    public interface IWarehouseUow : ITransactionManager
    {
        IWarehouseRepository Warehouse { get; }
    }
}
