using Domain.Entities;
using SharedLibrary.Seedwork;

namespace Domain.Interfaces
{
    public interface ICustomerAddressRepository : IGenericInterface<CustomerAddress, string>
    {
        Task<IEnumerable<CustomerAddress>> GetByAccountIdAsync(string accountId);
        Task<CustomerAddress?> GetByIdForAccountAsync(string id, string accountId);
        Task<bool> AnyForAccountAsync(string accountId);
    }
}
