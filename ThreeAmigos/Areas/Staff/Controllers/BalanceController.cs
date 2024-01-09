using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing.Drawing2D;
using ThreeAmigos.Areas.Identity.Data;
using ThreeAmigos.Models.AccountBalances;
using ThreeAmigos.Models.Products;

namespace ThreeAmigos.Areas.Admin.Controllers
{
    [Area("Staff")]
    public class BalanceController : Controller
    {

        private readonly ThreeAmigosContext _context;
        private readonly UserManager<ThreeAmigosUser> _userManager;
        private readonly SignInManager<ThreeAmigosUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public BalanceController(ThreeAmigosContext context, UserManager<ThreeAmigosUser> userManager, SignInManager<ThreeAmigosUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {

            return View();
        }

        public JsonResult GetBalance()
        {
            var data = _context.AccountBalances
                .ToList()  // Execute the query to retrieve data from the database
                .Select(balance => new
                {
                    FullName = _context.Users.FirstOrDefault(u => u.Id == balance.CustomerID)?.FullName,
                    Balance = balance.Balance
                })
                .ToList();

            return Json(new { data });
        }




        public async Task<IActionResult> CreateBal()
        {
            try
            {
                var users = await _userManager.GetUsersInRoleAsync("Customer");

                // Select both FirstName and LastName for display in the dropdown
                var userSelectList = users.Select(u => new
                {
                    Id = u.Id,
                    FullName =  u.FullName
                });

                // Pass the filtered list of users to the view
                ViewBag.UserList = new SelectList(userSelectList, "Id", "FullName");

                return View();
            }
            catch (Exception ex)
            {
                // Set an error message in TempData
                TempData["ErrorMessage"] = "An error occurred while processing your request.";

                return View("Error");
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateBal(AccountBalance model)
        {
            try
            {
                _context.AccountBalances.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Balance insert successfully";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Set an error message in TempData
                TempData["ErrorMessage"] = "An error occurred while inserting the brand.";

                return View("Error");
            }
        }


		public async Task<IActionResult> Edit(int id)
		{
			var dataBalance = await _context.AccountBalances.FindAsync(id);
			if (dataBalance == null)
			{
				return NotFound();
			}
			var users = await _userManager.GetUsersInRoleAsync("Customer");

			// Select both FirstName and LastName for display in the dropdown
			var userSelectList = users.Select(u => new
			{
				Id = u.Id,
				FullName = u.FullName
			});

			// Pass the filtered list of users to the view
			ViewBag.UserList = new SelectList(userSelectList, "Id", "FullName");
			return View(dataBalance);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(int id, AccountBalance accountBalance)
		{
			_context.Update(accountBalance);
			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}

	}
}