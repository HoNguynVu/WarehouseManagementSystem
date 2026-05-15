namespace Application.DTOs
{
    public class PaymentLinkDTO
    {
        public bool IsSuccess { get; set; }
        public string PaymentId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
