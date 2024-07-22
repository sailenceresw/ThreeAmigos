using Azure.Identity;
using ecommerce.Models;
using ecommerce.Services;
using ecommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ecommerce.Controllers
{
	//[Authorize]
	public class OrderController : Controller
	{
		private readonly IOrderService orderService;
		private readonly UserManager<ApplicationUser> userManager;
		private readonly ICartItemService cartItemService;
		private readonly ICartService cartService;
		private readonly IOrderItemService orderItemService;
		private readonly IProductService productService;
		private readonly IShipmentService shipmentService;
		private readonly ICategoryService categoryService;
		private readonly Context _context;
		private readonly IConfiguration _configuration;

		public OrderController(IOrderService orderService,
			UserManager<ApplicationUser> userManager,
			ICartService cartService ,
			ICartItemService cartItemService,
			IOrderItemService orderItemService,
			IProductService productService,
			IShipmentService shipmentService,
			ICategoryService categoryService,
			IConfiguration configuration,
			Context context)
		{
			this.orderService = orderService;
			this.userManager = userManager;
			this.cartItemService = cartItemService;
			this.cartService = cartService;
			this.orderItemService = orderItemService;
			this.productService = productService;
			this.shipmentService = shipmentService;
			this.categoryService = categoryService;
			this._context = context;
			this._configuration = configuration;
		}

		//***********************************************

		[HttpGet]
		public IActionResult GetAll(string? include = null)
		{
			List<Order> orders = orderService.GetAll(include);

			return View(orders);
		}

		[HttpGet]
		public IActionResult Get(int id)
		{
			Order order = orderService.Get(id);

			if (order != null)
			{
				return View(order);
			}

			return RedirectToAction("GetAll");
		}

		[HttpGet]
		public IActionResult Get(Func<Order, bool> where)
		{
			List<Order> orders = orderService.Get(where);

			return View(orders);
		}

		//--------------------------------------------

		[HttpGet]
		[Authorize(Roles = "Admin")]
		public IActionResult Insert()
		{
			/// TODO : continue from here make the VM and test the view
			/// Note : check Saeed for the register and login pages
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin")]
		public IActionResult Insert(Order order)
		{
			if (ModelState.IsValid)
			{
				orderService.Insert(order);

				orderService.Save();

				return RedirectToAction("GetAll");
			}

			return View(order);
		}

		//--------------------------------------------

		[HttpGet]
		[Authorize(Roles = "Admin")]
		public IActionResult Update(int id)
		{
			Order order = orderService.Get(id);

			if (order != null)
			{
				return View(order);
			}

			return RedirectToAction("GetAll");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Update(Order order)
		{
			var o = _context.Order.Where(o => o.Id == order.Id).FirstOrDefault();

			o.Status = order.Status;

			_context.Order.Update(o);

			_context.SaveChanges();

			// Send email notification
			var user = await userManager.FindByIdAsync(o.ApplicationUserId);
			if (user != null)
			{
				var emailService = new EmailService(_configuration);

				var emailMessage = BuildOrderStatusChangeEmailMessage(o);
				await emailService.SendEmailAsync(user.Email, "Order Status Updated", emailMessage);
			}

			return RedirectToAction("orders", "dashbourd");
		}

		//--------------------------------------------

		[HttpGet]
		[Authorize(Roles = "Admin")]
		public IActionResult Delete(int id)
		{
			Order order = orderService.Get(id);

			if (order != null)
			{
				return View(order);
			}

			return RedirectToAction("GetAll");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin")]
		public IActionResult Delete(Order order)
		{
			orderService.Delete(order);

			orderService.Save();

			return RedirectToAction("GetAll");
		}

		//--------------------------------------------

		public async Task<IActionResult> checkout(int CartId, string UserId)
		{
            ViewData["AllProductsNames"] = productService.GetAll().Select(c => c.Name).ToList();

            HttpContext.Session.SetString("uId", UserId);
			HttpContext.Session.SetInt32("cId", CartId);

			List<CartItem> items = cartItemService.Get(i => i.CartId == CartId);
			ApplicationUser? user = await userManager.FindByIdAsync(UserId);
			if (user != null)
			{
				Order order = new Order()
				{ ApplicationUserId = user.Id, OrderDate = DateTime.Now, OrderItems = new List<OrderItem>() };

				foreach (var item in items)
				{
					OrderItem orderItem = new OrderItem()
					{ ProductId = item.ProductId, OrderId = order.Id, Quantity = item.Quantity };
					order.OrderItems.Add(orderItem);
				}
				var serializedRecords = JsonSerializer.Serialize(order);
				HttpContext.Session.SetString("order", serializedRecords);

				Shipment shipment = new Shipment() { Date = DateTime.Now.AddDays(3) };

				List<Cart>? carts = cartService.GetAll();

                CheckoutViewModel checkoutVM = new CheckoutViewModel
                {
                    Address = user.Address,
                    City = user.City,
                    Region = user.Region,
                    Country = user.Country,
                    PostalCode = user.PostalCode
                };

                Cart cart = new Cart() { CartItems = new List<CartItem>() };

				if (carts.Count == 0)
				{
					checkoutVM.Date = shipment.Date;
					checkoutVM.Categories = categoryService.GetAll();
					checkoutVM.Cart = cart;

					decimal total = 0;
					foreach (OrderItem item in order.OrderItems)
					{
						item.Product = productService.Get(item.ProductId);
						total += item.Product.Price * item.Quantity;
					}
					ViewBag.order = order;
					ViewBag.total = total;

					return View(checkoutVM);
				}
				else
				{
					checkoutVM.Date = shipment.Date;
					checkoutVM.Categories = categoryService.GetAll();
					checkoutVM.Cart = cartService.GetAll("CartItems").FirstOrDefault();

					decimal total = 0;
					foreach (OrderItem item in order.OrderItems)
					{
						item.Product = productService.Get(item.ProductId);
						total += item.Product.Price * item.Quantity;
					}
					ViewBag.order = order;
					ViewBag.total = total;

					return View(checkoutVM);
				}
			}
			return Json("Error");
		}

		[HttpPost]
		public async Task<IActionResult> checkout(CheckoutViewModel checkoutVM)
		{
            ViewData["AllProductsNames"] = productService.GetAll().Select(c => c.Name).ToList();

			var user = await userManager.GetUserAsync(User);

			if (ModelState.IsValid)
			{
				var order = HttpContext.Session.Get("order");
				Order orderDesrialized = JsonSerializer.Deserialize<Order>(order);

				decimal totalPrice = 0;

				foreach (OrderItem item in orderDesrialized.OrderItems)
				{
					Product prod = productService.Get(item.ProductId);
					totalPrice += item.Quantity * prod.Price;

					if (prod.Quantity < item.Quantity)
					{
						return Json("Insufficient Quantity for " + prod.Name);
					}
				}

				if(user == null ||totalPrice > user.Balance) {
					return Json("Insufficient Balance");
				}

				Order o = new Order()
				{
					OrderDate = orderDesrialized.OrderDate,
					ApplicationUserId = orderDesrialized.ApplicationUserId,
					Status = "PENDING"
				};
				shipmentService.Save();

				orderService.Insert(o);
				
				foreach (OrderItem item in orderDesrialized.OrderItems)
				{
					item.OrderId = o.Id;
					Product prod = productService.Get(item.ProductId);

					_context.Stock.Add(new Stock()
					{
						ProductId = prod.Id,
						Quantity = -item.Quantity
					});

					prod.Quantity -= item.Quantity;

					productService.Update(prod);
					productService.Save();

					_context.OrderItem.Add(new OrderItem() {
						Quantity = item.Quantity,
						OrderId = o.Id,
						ProductId = item.ProductId,
						Price = prod.Price
					});
				}

				o.TotalValue = totalPrice;

				_context.Order.Update(o);

				user.Balance -= totalPrice;
				await userManager.UpdateAsync(user);

				_context.Statement.Add(new Statement() {
					UserId = user.Id,
					Amount = -totalPrice
				});

				checkoutVM.OrderId = o.Id;
				checkoutVM.Date = DateTime.Now.AddDays(3);

				Shipment shipment = new Shipment()
				{
					Id = checkoutVM.Id,
					Address = checkoutVM.Address,
					City = checkoutVM.City,
					Region = checkoutVM.Region,
					PostalCode = checkoutVM.PostalCode,
					Country = checkoutVM.Country,
					Order = checkoutVM.Order,
					OrderId = checkoutVM.OrderId,
					User = checkoutVM.User,
					UserId = checkoutVM.UserId,

					Date = checkoutVM.Date,
				};
				shipmentService.Insert(shipment);
				shipmentService.Save();

				o.ShipmentId = shipment.Id;
				orderService.Save();
				checkoutVM.Order = null;

				_context.SaveChanges();

				// Send Email
				var emailService = new EmailService(_configuration);
				var emailMessage = BuildOrderEmailMessage(o, orderDesrialized.OrderItems);
				await emailService.SendEmailAsync(user.Email, "Order Confirmation", emailMessage);

				return View("PlaceOrder");
			}
			string? userid = HttpContext.Session.GetString("uId");
			int? cartid = HttpContext.Session.GetInt32("cId");

			return RedirectToAction("checkout", new { CartId = cartid, UserId = userid });
		}

		private string BuildOrderEmailMessage(Order order, List<OrderItem> orderItems)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<h1>Order Confirmation</h1>");
			sb.AppendLine($"<p>Order Date: {order.OrderDate}</p>");
			sb.AppendLine("<table>");
			sb.AppendLine("<tr><th>Product</th><th>Quantity</th><th>Price</th></tr>");

			foreach (var item in orderItems)
			{
				sb.AppendLine("<tr>");
				sb.AppendLine($"<td>{item.ProductId}</td>");
				sb.AppendLine($"<td>{item.Quantity}</td>");
				sb.AppendLine($"<td>{item.Price:C}</td>");
				sb.AppendLine("</tr>");
			}

			sb.AppendLine("</table>");
			sb.AppendLine($"<p>Total: {order.TotalValue:C}</p>");
			return sb.ToString();
		}

		public string BuildOrderStatusChangeEmailMessage(Order order)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<h1>Order Status Update</h1>");
			sb.AppendLine($"<p>Your order with ID {order.Id} has been updated to status: {order.Status}</p>");
			return sb.ToString();
		}
	}
}