using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class PaymentServiceHelper
    {
        public string GenerateAppTransId(string prefix, string refId)
        {
            // Format: yyMMdd_PREFIX_xxxx (Ví dụ: 231201_INV_1234)
            var rnd = new Random();
            return $"{DateTime.Now:yyMMdd}_{prefix}_{rnd.Next(1000, 9999)}";
        }
    }
}
