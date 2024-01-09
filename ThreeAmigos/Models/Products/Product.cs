using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThreeAmigos.Areas.Identity.Data;

namespace ThreeAmigos.Models.Products
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProducImageUrl { get; set; }
        public int Price { get; set; }
        public int Stock { get; set; }
    }
}
