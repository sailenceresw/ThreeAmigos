using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThreeAmigos.Areas.Identity.Data;
using ThreeAmigos.Models.Orders;
using ThreeAmigos.Models.Products;

namespace ThreeAmigos.Models.OrderItems
{
    public class OrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderItemID { get; set; }
       
      
        public int Quantity { get; set; }
        public int UnitPrice { get; set; }

        public int OrderID { get; set; }
        public Order Order { get; set; }
        public int ProductID { get; set; }
        public Product Product { get; set; }
    }
}
