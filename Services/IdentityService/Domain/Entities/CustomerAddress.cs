using SharedLibrary.Seedwork;

namespace Domain.Entities
{
    public class CustomerAddress : BaseEntity<string>
    {
        public string AccountId { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; }

        public Accounts Account { get; set; } = null!;
    }
}
