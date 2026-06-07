namespace Application.DTOs.Requests
{
    public class CreateCustomerAddressRequest
    {
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
