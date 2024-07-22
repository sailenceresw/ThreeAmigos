using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        public bool IsParent {get; set; } = true;

        public string? Name { get; set; }

        [DisplayFormat(NullDisplayText = "No Description yet")]
        public string? Description { get; set; }

        public string ImageUrl { get; set; } = "wwwroot/images/sfdsdf"; 

        [Column(TypeName = "Money")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }
        public string StockStatus { get; set; } = "OUT_OF_STOCK";

        public string? Color { get; set; }

        [DisplayFormat(NullDisplayText = "No Rating yet")]
        [Column(TypeName = "Money")]
        public decimal? Rating { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public List<Comment>? Comments { get; set; }

        // Add Supplier Relation
        [ForeignKey("Supplier")]
        public string? SupplierId { get; set; }

        public ApplicationUser? Supplier { get; set; }
    }
}
