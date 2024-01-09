using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreeAmigos.Areas.Identity.Data;

namespace ThreeAmigos.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ThreeAmigosContext _context;
        private readonly UserManager<ThreeAmigosUser> _userManager;
        private readonly SignInManager<ThreeAmigosUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CartController(ThreeAmigosContext context, UserManager<ThreeAmigosUser> userManager, SignInManager<ThreeAmigosUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

		 
		public async Task<IActionResult> AddCart(int productId)
		{
			// Retrieve the product based on the productId
			var product = await _context.Products
				.Where(p => p.ProductId == productId)
				.ToListAsync();

			if (product == null || product.Count == 0)
			{
				// Handle the case where the product is not found
				return NotFound();
			}
 

			return View(product);
		}




	}
}
