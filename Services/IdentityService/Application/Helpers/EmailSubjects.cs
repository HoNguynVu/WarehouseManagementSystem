using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class EmailSubjects
    {
        public class AccountVerification
        {
            public const string Subject = "Verify Your Account";
            public const string Title = "Account Verification";
            public const string Purpose = "Account verification";
        }

        public class PasswordReset
        {
            public const string Subject = "Reset Your Password";
            public const string Title = "Password Reset";
            public const string Purpose = "Password reset";
        }
    }
}
