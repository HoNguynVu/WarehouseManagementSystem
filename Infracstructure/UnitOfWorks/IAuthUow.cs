using Domain.Interfaces;
using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infracstructure.UnitOfWorks
{
    public interface IAuthUow : ITransactionManager
    {
        IAccountRepository Accounts { get; }
        IOtpRepository Otps { get; }
        IRefreshTokenRepository RefreshTokens { get; }
    }
}
