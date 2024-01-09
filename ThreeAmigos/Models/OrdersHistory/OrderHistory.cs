using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThreeAmigos.Areas.Identity.Data;
using ThreeAmigos.Models.Orders;

namespace ThreeAmigos.Models.OrdersHistory
{
    public class OrderHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderHistoryId { get; set; }
     
        public string CustomerId { get; set; }
        public ThreeAmigosUser ThreeAmigosUser { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public string Status { get; set; }
    }
}
