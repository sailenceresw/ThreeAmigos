using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThreeAmigos.Areas.Identity.Data;

namespace ThreeAmigos.Areas.Staff.Controllers
{
    [Area("Staff")]
    public class OrderHistoryController : Controller
    {
        private readonly ThreeAmigosContext _context;
        private readonly UserManager<ThreeAmigosUser> _userManager;
        private readonly SignInManager<ThreeAmigosUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public OrderHistoryController(ThreeAmigosContext context, UserManager<ThreeAmigosUser> userManager, SignInManager<ThreeAmigosUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

		public IActionResult Index()
		{
			 

			return View();
		}

		public JsonResult OrderHistory()
		{

			var data = _context.OrdersHistory
				.Select(o => new
				{
					OrderId = o.OrderId,
					CustomerId = o.CustomerId,
					CustomerFullName = _userManager.Users
				.Where(u => u.Id == o.CustomerId)
				.Select(u => u.FullName)
				.FirstOrDefault(),

					Status = o.Status,
					ProductName = o.Order.OrderItems.FirstOrDefault().Product.Name,
					Price = o.Order.OrderItems.FirstOrDefault().Product.Price,
				})
				.ToList();
			return Json(new { data });
		}


	}
}
