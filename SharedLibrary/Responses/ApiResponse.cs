using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Responses
{
    // Class generic <T> cho phép bạn trả về bất kỳ kiểu dữ liệu nào (Product, Order, List<User>...)
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        // Hàm tiện ích để trả về kết quả Thành công nhanh chóng
        public static ApiResponse<T> Success(T data, string message = "Operation completed successfully.", int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }

        // Hàm tiện ích để trả về kết quả Thất bại nhanh chóng
        public static ApiResponse<T> Failure(string message, int statusCode = 400, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                IsSuccess = false,
                Message = message,
                Errors = errors
            };
        }
    }
}
