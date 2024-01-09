using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThreeAmigos.Areas.Identity.Data;

namespace ThreeAmigos.Areas.Staff.Controllers
{
	[Area("Staff")]
	public class DashboardController : Controller
	{
		private readonly ThreeAmigosContext _context;
		private readonly UserManager<ThreeAmigosUser> _userManager;
		private readonly SignInManager<ThreeAmigosUser> _signInManager;
		private readonly RoleManager<IdentityRole> _roleManager;

		public DashboardController(ThreeAmigosContext context, UserManager<ThreeAmigosUser> userManager, SignInManager<ThreeAmigosUser> signInManager, RoleManager<IdentityRole> roleManager)
		{
			_context = context;
			_userManager = userManager;
			_signInManager = signInManager;
			_roleManager = roleManager;
		}

		public async Task<IActionResult> Index()
		{
			// Order count Details 

			var customersInRole = await _userManager.GetUsersInRoleAsync("Customer");
			int customerCount = customersInRole.Count;            // Get the count of users in the "Customer" role
			ViewBag.CountCustomer = customerCount;            // Store the count in the ViewBag to be used in the view


			// Order count Details 

			var countRole = _context.Orders.ToList();
			int countOrder = countRole.Count;
			ViewBag.OrderCount = countOrder;

			var productList = _context.Products.ToList();
			int ProsuctCount = productList.Count;
			ViewBag.CountProduct = ProsuctCount;


			return View();
		}




	}
}