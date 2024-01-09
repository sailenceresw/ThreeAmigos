using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net.Mail;
using System.Net;
using ThreeAmigos.Areas.Identity.Data;
using ThreeAmigos.Models.OrderItems;
using ThreeAmigos.Models.Orders;
using ThreeAmigos.Models.OrdersHistory;
using System.Configuration;
using ThreeAmigos.Models;

namespace ThreeAmigos.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ThreeAmigosContext _context;
        private readonly UserManager<ThreeAmigosUser> _userManager;
        private readonly SignInManager<ThreeAmigosUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
		private readonly IConfiguration _configuration; // Add this field

		public OrderController(ThreeAmigosContext context, UserManager<ThreeAmigosUser> userManager, SignInManager<ThreeAmigosUser> signInManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
		{
			_context = context;
			_userManager = userManager;
			_signInManager = signInManager;
			_roleManager = roleManager;
			_configuration = configuration; // Initialize the configuration field
		}

		public IActionResult PlaceOrder(int[] productIds)
		{
			// Get the currently logged-in user
			var currentUser = _userManager.GetUserAsync(User).Result;

			// Retrieve the products selected for the order
			var products = _context.Products.Where(p => productIds.Contains(p.ProductId)).ToList();

			// Calculate total price of the selected products
			var totalPrice = products.Sum(p => p.Price);

			// Retrieve the user's account balance
			var userBalance = _context.AccountBalances.FirstOrDefault(a => a.CustomerID == currentUser.Id);

			// Check if the user has sufficient funds
			if (userBalance.Balance < totalPrice)
			{
				// Handle insufficient funds (redirect, show an error message, etc.)
				return RedirectToAction("InsufficientFunds");
			}

			// Create a new order
			var order = new Order
			{
				OrderDate = DateTime.Now,
				Status = "Pending", // You can set the initial status as needed
				CustomerID = currentUser.Id,
				ThreeAmigosUser = currentUser,
				OrderItems = products.Select(p => new OrderItem
				{
					Quantity = 1, // You may adjust the quantity based on your requirements
					UnitPrice = p.Price,
					Product = p
				}).ToList()
			};

			// Update user's account balance
			userBalance.Balance -= totalPrice;

			// Update product stock
			foreach (var product in products)
			{
				product.Stock -= 1; // Assuming you're decrementing the stock by 1 for each purchased item
			}

			// Save changes to the database
			_context.Orders.Add(order);
			_context.SaveChanges();

			// Create an OrderHistory entry
			var orderHistory = new OrderHistory
			{
				CustomerId = currentUser.Id,
 				OrderId = order.OrderId,
				Order = order,
				Status = order.Status
			};

			// Save OrderHistory to the database
			_context.OrdersHistory.Add(orderHistory);
			_context.SaveChanges();


			TempData["Success"] = "Your order has been placed successfully. Thank you for shopping with us!";

			//		SendOrderConfirmationEmail(currentUser.Email, order);

			// Redirect or return a success view
			return View();
		}
		private void SendOrderConfirmationEmail(string userEmail, Order order)
		{
			try
			{
				var smtpConfig = _configuration.GetSection("SMTPConfig").Get<SmtpConfigModel>();
				var smtpClient = new SmtpClient(smtpConfig.Host)
				{
					Port = int.Parse(smtpConfig.Port),
					Credentials = new NetworkCredential(smtpConfig.UserName, smtpConfig.Password),
					EnableSsl = bool.Parse(smtpConfig.EnableSSL),
				};

				var mailMessage = new MailMessage
				{
					From = new MailAddress(smtpConfig.SenderAddress, smtpConfig.SenderDisplayName),
					Subject = "Order Confirmation",
					Body = $"Thank you for your order! Your order ID is {order.OrderId}.",
					IsBodyHtml = bool.Parse(smtpConfig.IsBodyHtml),
				};

				mailMessage.To.Add(userEmail);

				smtpClient.Send(mailMessage);
			}
			catch (Exception ex)
			{
				// Handle exception (log, show error message, etc.)
				Console.WriteLine($"Error sending email: {ex.Message}");
			}
		}



	}
}
