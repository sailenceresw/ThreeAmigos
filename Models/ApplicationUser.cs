using Microsoft.AspNetCore.Identity;

namespace ecommerce.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Address {get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public decimal Balance { get; set; } = 0;
        public bool RequestDelete {get; set; } = false;
        public bool IsAccountDeleted {get; set; } = false;
        public List<Shipment>? Shipments { get; set; }

        public List<Order>? Orders { get; set;}

        public List<Comment>? Comments { get; set; }
    }
}
