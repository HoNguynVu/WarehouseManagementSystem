using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public class AccountStatus
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
        public const string Suspended = "Suspended";
        public const string EmailUnverified = "EmailUnverified";
    }
}
