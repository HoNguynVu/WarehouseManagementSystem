using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.IntegrationEvents
{
    public class CreateOrderEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public IEnumerable<string> ItemIds { get; set; } = Enumerable.Empty<string>();
        public string AccountId { get; set; } = string.Empty;
    }
}
