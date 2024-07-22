using ecommerce.Hubs;
using ecommerce.Models;
using ecommerce.Repository;
using ecommerce.Seeders;
using ecommerce.Services;
using ecommerce.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ecommerce
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //Add comment
            builder.Services.AddSignalR();
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            //inject the context
            builder.Services.AddDbContext<Context>(
                options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("cs"));
                });

            //register Model.
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<ICommentRepository, CommentRepository>();
           

            builder.Services.AddScoped<ICommentService, CommentService>();

            //AbdElraheem
            builder.Services.AddScoped<ICommentRepository, CommentRepository>();
            builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
            builder.Services.AddScoped<IOrderItemService, OrderItemService>();


            // omar : registering order repo
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();


            // omar : registering orderservice
            builder.Services.AddScoped<IOrderService, OrderService>();

            // omar : registering ProductService
            builder.Services.AddScoped<IProductService, ProductService>();

            // omar : registering CategoryService
            builder.Services.AddScoped<ICategoryService, CategoryService>();

            builder.Services.AddScoped<IShipmentRepository, ShipmentRepository>();

            builder.Services.AddScoped<IShipmentService,ShipmentService>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
            builder.Services.AddScoped<ICartItemService, CartItemService>();

            // saeed : register shipment using generic repository
            builder.Services.AddScoped<IRepository<Shipment>, Repository<Shipment>>();

            // Register the TimedHostedService
            builder.Services.AddHostedService<TimedHostedService>();


            //register the identityuser 
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(
                options =>
                {
                    options.Password.RequireNonAlphanumeric = false;  // for easier testing  <= Omar : thanks Saeed :D
                    options.Password.RequireUppercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireDigit = false;

                    /**/                   // options.SignIn.RequireConfirmedAccount = true;        // saeed 
                }
                ).AddEntityFrameworkStores<Context>().AddDefaultTokenProviders();



            // saeed : mail configuration
            builder.Services.Configure<MailSettings>
                (builder.Configuration.GetSection("MailSettings"));

            builder.Services.AddTransient<IMailService, MailService>();
            
            builder.Services.AddSession();


            // omar : registering cart and cartItems
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<ICartItemService, CartItemService>();

            builder.Services.AddScoped<ICartRepository , CartRepository>();
            builder.Services.AddScoped<ICartItemRepository , CartItemRepository>();


            
            // saeed : try to change in claim without log user out using this service

            //builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            //    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            //{
            //    options.LoginPath = "/account/login";
            //    options.LogoutPath = "/account/logout";
            //});

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            // saeed
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            //Comment
            app.MapHub<CommentHub>("/CommentHub");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

