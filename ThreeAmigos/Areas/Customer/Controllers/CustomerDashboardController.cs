using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThreeAmigos.Areas.Identity.Data;

namespace ThreeAmigos.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CustomerDashboardController : Controller
    {
        private readonly ThreeAmigosContext _context;
        private readonly UserManager<ThreeAmigosUser> _userManager;
        private readonly SignInManager<ThreeAmigosUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CustomerDashboardController(ThreeAmigosContext context, UserManager<ThreeAmigosUser> userManager, SignInManager<ThreeAmigosUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var product = _context.Products.ToList();


            return View(product);
        }




    }
}
