using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreeAmigos.Areas.Identity.Data;

namespace ThreeAmigos.Areas.Customer.Controllers
{
	[Area("Customer")]
	public class ProfileController : Controller
	{
		private readonly ThreeAmigosContext _dbContext;
		private readonly UserManager<ThreeAmigosUser> _userManager;

		public ProfileController(ThreeAmigosContext dbContext, UserManager<ThreeAmigosUser> userManager)
		{
			_dbContext = dbContext;
			_userManager = userManager;
		}
		public IActionResult Index()
		{
			return View();
		}
		public async Task<IActionResult> Edit()
		{
			var currentUser = await _userManager.GetUserAsync(User);

			var currentUserId = currentUser.Id;

			var userSpecificData = await _dbContext.Users.Where(e => e.Id == currentUserId).FirstOrDefaultAsync();
			ViewBag.UserId = currentUserId;
 			
			return View(userSpecificData);
		}
		[HttpPost]
		public async Task<IActionResult> Edit(ThreeAmigosUser usr)
		{
			 
				// Check if the user with the specified ID exists
				var existingUser = await _dbContext.Users.FindAsync(usr.Id);

				if (existingUser != null)
				{
					// Update the properties of the existing user
					existingUser.FullName = usr.FullName;
					existingUser.PaymentAddress = usr.PaymentAddress;
					existingUser.DeliveryAddress = usr.DeliveryAddress;
					existingUser.TelephoneNumber = usr.TelephoneNumber;

					// Update the user in the database
					_dbContext.Entry(existingUser).State = EntityState.Modified;
					await _dbContext.SaveChangesAsync();

					return RedirectToAction("Index", "CustomerDashboard", new { area = "Customer" });
				}
				else
				{
					// Handle the case where the user with the specified ID does not exist
					ModelState.AddModelError(string.Empty, "User not found");
				}
		 

			// If ModelState is not valid, return to the edit view with validation errors
			return View(usr);
		}


	}
}
