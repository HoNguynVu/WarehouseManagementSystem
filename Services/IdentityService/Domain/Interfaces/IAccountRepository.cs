using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using SharedLibrary.Seedwork;

namespace Domain.Interfaces
{
    public interface IAccountRepository : IGenericInterface<Accounts, string>
    {
        Task<Accounts?> GetByEmailAsync(string email);
        Task<IEnumerable<Accounts>> GetByStatusAsync(string status);
    }
}
