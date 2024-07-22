using ecommerce.Models;
using ecommerce.Repository;
using ecommerce.ViewModel;
using ecommerce.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Claims;

namespace ecommerce.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IRepository<Shipment> shipmentRepository;    // saeed : edit it
        private readonly IOrderItemRepository orderItemRepository;
        private readonly IProductRepository productRepository;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Context _context;

        public AccountController(UserManager<ApplicationUser> _userManager,
            SignInManager<ApplicationUser> _signInManager, RoleManager<IdentityRole> _roleManager,
            IRepository<Shipment> _Repository, IOrderItemRepository _orderItemRepository,
            IProductRepository _productRepository,
            RoleManager<IdentityRole> roleManager, Context context)
        {
            userManager = _userManager;
            signInManager = _signInManager;
            roleManager = _roleManager;
            shipmentRepository = _Repository;
            orderItemRepository = _orderItemRepository;
            productRepository = _productRepository;
            this._context = context;
        }
        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public IActionResult register(bool isAdmin = false, bool isSupplier = false)
        {
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsSupplier = isSupplier;
            return View("register");
        }


        [HttpGet]
        public async Task<IActionResult> mailConfirmed(string email)
        {
            ApplicationUser? user = await userManager.FindByEmailAsync(email);
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
            return RedirectToAction("login");
        }


        [HttpPost]
        public async Task<IActionResult> register(RegisterViewModel model, bool isAdmin, bool isSupplier)
        {
            ApplicationUser applicationUser = new ApplicationUser
            {
                UserName = model.userName,
                PasswordHash = model.password,
                PhoneNumber = model.phoneNumber,
                EmailConfirmed = true,
                Email = model.Email?.Trim()
            };

            if (ModelState.IsValid)
            {
                IdentityResult result = new IdentityResult();
                try
                {
                    result = await userManager.CreateAsync(applicationUser,
                    applicationUser.PasswordHash);
                }
                catch (Exception ex)
                {
                    if (ex.InnerException.Message.StartsWith("Cannot insert duplicate key"))
                        ModelState.AddModelError(string.Empty, "Already existing email");

                    else { ModelState.AddModelError(string.Empty, ex.InnerException.Message); }
                }

                if (result.Succeeded)
                {
                    if(isAdmin) {
                        await userManager.AddToRoleAsync(applicationUser, "Admin");
                        return RedirectToAction("admins", "dashbourd");
                    } else if (isSupplier) {
                        await userManager.AddToRoleAsync(applicationUser, "Supplier");
                        return View("login");
                    } else {
                        await userManager.AddToRoleAsync(applicationUser, "User");
                        return View("login");
                    }
                }

                foreach (IdentityError err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
            }

            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsSupplier = isSupplier;
            return View("register");
        }

        [HttpGet]
        public IActionResult login()
        {
            return View("login");
        }

        // omar : saeed take a look at what happens when the user enters a wrong passwprd at login
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = await userManager.FindByNameAsync(model.userName);
                if (user != null && !user.IsAccountDeleted)
                {
                    bool matched = await userManager.CheckPasswordAsync(user, model.password);
                    if (matched)
                    {
                        if (user.EmailConfirmed)
                        {
                            List<Claim> claims = new List<Claim>();
                            claims.Add(new Claim("name", model.userName));
                            await signInManager.SignInWithClaimsAsync(user, model.rememberMe, claims);
                            return RedirectToAction("Index", "Home");
                        }
                        ModelState.AddModelError("", "Unconfirmed email");
                        return View("login");
                    }
                    ModelState.AddModelError("", "invalid password");
                    return View("login");
                }
            }
            ModelState.AddModelError("", "invalid user name");
            return View("login", model);
        }


        public async Task<IActionResult> logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("login");
        }

        public async Task<IActionResult> confirmMakeAdmin(string userName)
        {
            ApplicationUser user = await userManager.FindByNameAsync(userName);
            if (user != null)
            {
                await userManager.RemoveFromRoleAsync(user, "User");
                await userManager.AddToRoleAsync(user, "Admin");
                return RedirectToAction("users", "Dashbourd");
            }
            return RedirectToAction("users", "Dashbourd");
        }

        public async Task<IActionResult> confirmRemoveAdmin(string userName)
        {
            ApplicationUser appUser = await userManager.FindByNameAsync(userName);
            if (appUser != null)
            {
                await userManager.RemoveFromRoleAsync(appUser, "Admin");
                await userManager.AddToRoleAsync(appUser, "User");
                if (User.FindFirst("name")?.Value == userName)
                {
                    return RedirectToAction("logout");
                }
                return RedirectToAction("admins", "dashbourd");
            }
            return RedirectToAction("admins", "dashbourd");   // which view should be returned if user not found!!!!!
        }


        [HttpGet]
        public IActionResult forgotPassword()
        {
            return View("forgotPassword");
        }


        [HttpPost]
        public async Task<IActionResult> forgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Email = model?.Email.Trim();
                ApplicationUser? user = await userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    string token = await userManager.GeneratePasswordResetTokenAsync(user);
                    
                    string callBackUrl = Url.Action("resetPassword", "account", values: new { token, userName = user.UserName },
                        protocol: Request.Scheme);

                    return RedirectToAction("SendMail", "Mail",
                        routeValues: new { emailTo = user.Email, username = user.UserName, callBackUrl = callBackUrl });

                }
                ModelState.AddModelError("", "Email not existed");
                return View("forgotPassword", model);
            }
            return View("forgotPassword", model);
        }


        [HttpGet]
        public IActionResult ResetPassword([FromQueryAttribute] string userName, [FromQueryAttribute] string token)
        {
            ViewBag.UserName = userName;
            ViewBag.Token = token;
            return View("resetPassword");
        }

        [HttpGet]
        public async Task<IActionResult> RequestDeleteAccount()
        {
            ApplicationUser? user = await userManager.GetUserAsync(User);
            if(user != null) {
                user.RequestDelete = true;
                await userManager.UpdateAsync(user);
            }

            return View("login");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(resetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser? user = await userManager.FindByNameAsync(model.userName);
                IdentityResult result = await userManager
                       .ResetPasswordAsync(user, model.token, model.newPassword);


                if (result.Succeeded)
                    return View("login");

                else
                {
                    foreach (IdentityError error in result.Errors)
                    { ModelState.AddModelError(string.Empty, error.Description); }

                    return View("resetPassword", model);
                }
            }
            return View("resetPassword", model);
        }

        public async Task<IActionResult> myAccount(string selectedPartial = "_accountInfoPartial", resetPasswordViewModel changePasswordModel = null)
        {
            ApplicationUser user = await userManager.FindByNameAsync(User.Identity.Name);  // err
            AccountInfoViewModel model = new AccountInfoViewModel()
            {
                userName = user.UserName,
                phoneNumber = user.PhoneNumber,
                Email = user.Email,
                City = user.City,
                Region = user.Region,
                Country = user.Country,
                PostalCode = user.PostalCode,
                Address = user.Address,
                Balance = user.Balance,
                RequestDelete = user.RequestDelete,
                IsAccountDeleted = user.IsAccountDeleted
            };
            ViewBag.selectedPartial = selectedPartial;
            ViewBag.resetPasswordModel = changePasswordModel;
            return View(model);
        }



        public async Task<IActionResult> getAccountInfoPartial()
        {
            ApplicationUser user = await userManager.FindByNameAsync(User.Identity.Name);
            AccountInfoViewModel model = new AccountInfoViewModel()
            {
                userName = user.UserName,
                phoneNumber = user.PhoneNumber,
                Email = user.Email,
                City = user.City,
                Region = user.Region,
                Country = user.Country,
                PostalCode = user.PostalCode,
                Address = user.Address,
                Balance = user.Balance,
                RequestDelete = user.RequestDelete,
                IsAccountDeleted = user.IsAccountDeleted
            };
            return View("_accountInfoPartial", model);  // send v.m 
        }

        public IActionResult getAccountChangePasswordPartial()
        {
            return View("_accountChangePasswordPartial");
        }


        public IActionResult getAccountOrdersPartial()
        {
            return View("_accountOrdersPartial");
        }


        public async Task<IActionResult> getAccountShipmentsPartial()
        {
            List<string> randomProductImages = new List<string>();
            
            ApplicationUser? user = await userManager.FindByNameAsync(User.Identity.Name);
            List<Shipment>? shipments = shipmentRepository.Get(s => s.UserId == user.Id);

            return View("_accountShipmentsPartial", shipments);
        }


        [HttpPost]
        public async Task<IActionResult> editAccountInfo(string userName, string phoneNumber, string Email, string? Address, string? City, string? Region, string? PostalCode, string? Country, decimal Balance = 0)
        {
            AccountInfoViewModel model = new AccountInfoViewModel()
            {
                userName = userName,
                phoneNumber = phoneNumber,
                Email = Email,
                Address = Address,
                City = City,
                Country = Country, 
                Region = Region,
                PostalCode = PostalCode,
                Balance = Balance,
            };

            if (ModelState.IsValid)
            {
                ApplicationUser user = await userManager.FindByEmailAsync(Email);
                if (user != null)
                {
                    try
                    {
                        if (Balance != 0)
                        {
                            _context.Statement.Add(new Statement()
                            {
                                UserId = user.Id,
                                Amount = Balance
                            });
                        }


                        user.UserName = model.userName;
                        user.PhoneNumber = model.phoneNumber;
                        user.Address = model.Address;
                        user.City = model.City;
                        user.Region = model.Region;
                        user.PostalCode = model.PostalCode;
                        user.Country = model.Country;

                        decimal prevBalance = user.Balance;

                        user.Balance = prevBalance + model.Balance;

                        await userManager.UpdateAsync(user);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, ex.Message);
                        return RedirectToAction("myAccount", "account", model);
                    }

                    if (User.Identity.Name != user.UserName)
                        return RedirectToAction("logout", "account"); 

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("invalidSentEmail", "User not found");
                return RedirectToAction("myAccount", "account", model);
            }
            return RedirectToAction("myAccount", "account", model);
        }


        public async Task<IActionResult> editAccountPassword(resetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = await userManager.FindByNameAsync(User.Identity?.Name);

                if (user != null)
                {
                    string token = await userManager.GeneratePasswordResetTokenAsync(user);
                    await userManager.ResetPasswordAsync(user, token, model.newPassword);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid user");
                return RedirectToAction("myAccount", "_accountChangePasswordPartial", model);
            }
            return RedirectToAction("myAccount", new { selectedPartial = "_accountChangePasswordPartial", changePasswordModel = model });
        }
    }
}
