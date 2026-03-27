using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Middlewares
{
    public static class MiddlewareExtensions
    {
        // Extension method để dễ dàng thêm middleware vào pipeline.
        // fBiến đoạn code gọi middleware phức tạp thành một dòng duy nhất: app.UseGlobalExceptionHandler();
        public static IApplicationBuilder UseGlobalException(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalException>();
        }
    }
}
