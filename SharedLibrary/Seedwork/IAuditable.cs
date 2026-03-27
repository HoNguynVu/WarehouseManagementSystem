using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Seedwork
{
    public interface IAuditable
    {
        DateTimeOffset CreatedAt { get; set; }
        string? CreatedBy { get; set; }
        DateTimeOffset? UpdatedAt { get; set; }
        string? UpdatedBy { get; set; }
    }
}
