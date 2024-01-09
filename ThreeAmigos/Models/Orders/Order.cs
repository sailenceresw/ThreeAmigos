using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThreeAmigos.Areas.Identity.Data;
using ThreeAmigos.Models.OrderItems;

namespace ThreeAmigos.Models.Orders
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }
         public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public int StockQuantity { get; set; }
        public string CustomerID { get; set; }
        public ThreeAmigosUser ThreeAmigosUser { get; set; }
        public List<OrderItem> OrderItems { get; set; }
    }
}
