using Domain.Entities;
using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IOtpRepository : IGenericInterface<Otps, string>
    {
        Task<Otps?> GetByEmailAsync(string email);
        Task<Otps?> GetByCodeAsync(string code);
    }
}
