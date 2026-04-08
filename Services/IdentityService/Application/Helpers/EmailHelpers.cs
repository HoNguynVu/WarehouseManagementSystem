using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class EmailHelpers
    {
        public string CreateEmailByTemplate(string title, string otp, string purpose)
        {
            string primaryColor = "#0056b3";

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; }}
                    .card {{ background-color: #ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                    .header {{ text-align: center; border-bottom: 2px solid {primaryColor}; padding-bottom: 10px; margin-bottom: 20px; }}
                    .otp-box {{ font-size: 32px; font-weight: bold; color: {primaryColor}; letter-spacing: 5px; text-align: center; margin: 20px 0; padding: 15px; background-color: #eef6fc; border-radius: 5px; border: 1px dashed {primaryColor}; }}
                    .footer {{ margin-top: 30px; font-size: 12px; color: #777; text-align: center; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='card'>
                        <div class='header'>
                            <h2 style='color: {primaryColor}; margin: 0;'>{title}</h2>
                        </div>
                
                        <p>Xin chào,</p>
                        <p>Bạn vừa yêu cầu <strong>{purpose}</strong>. Đây là mã xác thực (OTP) của bạn:</p>
                
                        <div class='otp-box'>{otp}</div>
                
                        <p>Mã này sẽ hết hạn trong vòng <strong>5 phút</strong>.</p>
                        <p style='color: #dc3545;'><strong>Lưu ý:</strong> Vui lòng không chia sẻ mã này cho bất kỳ ai, kể cả nhân viên quản lý.</p>
                
                        <p>Trân trọng,<br>Đội ngũ quản lý Warehouse System.</p>
                    </div>
            
                    <div class='footer'>
                        <p>Đây là email tự động, vui lòng không trả lời.<br>
                        &copy; {DateTime.Now.Year} Warehouse Management System.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}
