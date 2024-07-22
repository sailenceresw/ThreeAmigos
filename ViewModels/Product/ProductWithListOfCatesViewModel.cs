using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using ecommerce.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ecommerce.ViewModels.Product
{
    public class ProductWithListOfCatesViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [DisplayFormat(NullDisplayText = "No Description yet")]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile image { get; set; }


        [Column(TypeName = "Money")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string Color { get; set; }

        // we want to make array of colors for each product
        //public int MyProperty { get; set; }

        [DisplayFormat(NullDisplayText = "No Rating yet")]
        [Column(TypeName = "Money")]

        public decimal? Rating { get; set; }

        //----------------------------------

        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        public List<Models.Category>? categories { get; set; }

        [ForeignKey("Supplier")]
        public string? SupplierId { get; set; }
        public List<Models.ApplicationUser>? suppliers { get; set; }

    }
}
