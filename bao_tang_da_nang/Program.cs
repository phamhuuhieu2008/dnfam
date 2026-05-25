using Bảo_Tàng_Đà_Nẵng.Data;
using Microsoft.EntityFrameworkCore;

namespace Bảo_Tàng_Đà_Nẵng
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── 1. Database: Entity Framework Core + SQL Server ──────────
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null
                    )
                )
            );

            // ── 2. Session (dùng để lưu UserId sau khi đăng nhập) ────────
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(
                    builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes", 30)
                );
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // ── 3. MVC Controllers + Views ────────────────────────────────
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // ── 4. HTTP Pipeline ──────────────────────────────────────────
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Session phải được đặt SAU UseRouting và TRƯỚC UseAuthorization
            app.UseSession();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                ;

            // ── 5. Nạp dữ liệu tự động ────────────────────────────────────
            try 
            {
                Bảo_Tàng_Đà_Nẵng.ParseAndSeed.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi nạp dữ liệu: " + ex.Message);
            }

            app.Run();
        }
    }
}
