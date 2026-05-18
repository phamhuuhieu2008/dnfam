using Bảo_Tàng_Đà_Nẵng.Data;
using Bảo_Tàng_Đà_Nẵng.Models;
using Bảo_Tàng_Đà_Nẵng.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Bảo_Tàng_Đà_Nẵng.Controllers
{
    /// <summary>
    /// AuthController — Quản lý toàn bộ luồng xác thực người dùng.
    ///
    /// Luồng quan trọng:
    ///   Register → Login → [Chơi] → ChangePassword → Logout → Login
    ///
    /// Đặc biệt: ChangePassword BẮT BUỘC xóa session và redirect về Login
    /// để người dùng phải đăng nhập lại bằng mật khẩu mới.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly AppDbContext _db;

        // Session keys — tập trung để dễ bảo trì
        private const string SESSION_USER_ID       = "UserId";
        private const string SESSION_USER_FULLNAME = "UserFullName";
        private const string SESSION_USER_ROLE     = "UserRole";
        private const string SESSION_CURRENT_SCORE = "CurrentScore";

        public AuthController(AppDbContext db)
        {
            _db = db;
        }

        // ════════════════════════════════════════════════════════
        // ĐĂNG KÝ
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Register()
        {
            // Nếu đã đăng nhập thì redirect về trang chủ
            if (IsLoggedIn()) return RedirectToAction("Index", "Home");
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Kiểm tra username đã tồn tại chưa
            bool usernameExists = await _db.Users
                .AnyAsync(u => u.Username == model.Username);

            if (usernameExists)
            {
                ModelState.AddModelError(nameof(model.Username),
                    "Tên đăng nhập này đã được sử dụng. Vui lòng chọn tên khác.");
                return View(model);
            }

            // Tạo user mới với mật khẩu đã được hash bằng BCrypt
            var newUser = new User
            {
                Username     = model.Username.Trim(),
                FullName     = model.FullName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 11),
                Role         = "Player",
                CreatedAt    = DateTime.UtcNow,
                IsActive     = true
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            // Tự động đăng nhập ngay sau khi đăng ký thành công
            SetUserSession(newUser);

            TempData["SuccessMessage"] = $"Chào mừng {newUser.FullName}! Tài khoản của bạn đã được tạo thành công.";
            return RedirectToAction("Index", "Home");
        }

        // ════════════════════════════════════════════════════════
        // ĐĂNG NHẬP
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Login(string? returnUrl = null, string? message = null)
        {
            if (IsLoggedIn()) return RedirectToAction("Index", "Home");

            // Hiển thị thông báo đặc biệt (VD: "Đã đổi mật khẩu, vui lòng đăng nhập lại")
            if (!string.IsNullOrEmpty(message))
                ViewData["InfoMessage"] = message;

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Tìm user theo username, loại trừ user bị vô hiệu hóa
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

            // Kiểm tra mật khẩu bằng BCrypt.Verify (timing-safe)
            bool isPasswordValid = user != null &&
                BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                // Không tiết lộ thông tin: username sai hay password sai
                ModelState.AddModelError(string.Empty,
                    "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Lưu thông tin vào Session
            SetUserSession(user!);

            // Redirect về trang yêu cầu ban đầu (nếu có)
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ════════════════════════════════════════════════════════
        // ĐỔI MẬT KHẨU
        //
        // ★ LUỒNG BẮT BUỘC THEO ĐẶC TẢ ★
        //   Sau khi đổi mật khẩu thành công:
        //   1. Xóa toàn bộ Session cũ (invalidate)
        //   2. Redirect THẲNG về trang Login
        //   3. Truyền thông báo để người dùng biết lý do
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!IsLoggedIn()) return RedirectToAction(nameof(Login));
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Lấy userId từ Session của người dùng đang đăng nhập
            int userId = HttpContext.Session.GetInt32(SESSION_USER_ID) ?? 0;
            var user   = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                // Session hết hạn hoặc user bị xóa
                return RedirectToAction(nameof(Login));
            }

            // Xác minh mật khẩu hiện tại
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword),
                    "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

            // Cập nhật mật khẩu mới (hash lại với BCrypt)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, workFactor: 11);
            await _db.SaveChangesAsync();

            // ★ XÓA SESSION CŨ — đây là bước bắt buộc theo đặc tả ★
            // Người dùng PHẢI đăng nhập lại bằng mật khẩu mới
            HttpContext.Session.Clear();

            // ★ REDIRECT VỀ LOGIN với thông báo rõ ràng ★
            return RedirectToAction(nameof(Login), new
            {
                message = "✅ Đổi mật khẩu thành công! Vui lòng đăng nhập lại bằng mật khẩu mới."
            });
        }

        // ════════════════════════════════════════════════════════
        // ĐĂNG XUẤT
        // ════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // Hỗ trợ GET logout (cho link đơn giản trên navbar)
        [HttpGet]
        public IActionResult LogoutGet()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // ════════════════════════════════════════════════════════
        // HELPER METHODS
        // ════════════════════════════════════════════════════════

        /// <summary>Lưu thông tin người dùng vào Session sau khi đăng nhập thành công.</summary>
        private void SetUserSession(User user)
        {
            HttpContext.Session.SetInt32(SESSION_USER_ID, user.Id);
            HttpContext.Session.SetString(SESSION_USER_FULLNAME, user.FullName);
            HttpContext.Session.SetString(SESSION_USER_ROLE, user.Role);
            HttpContext.Session.SetInt32(SESSION_CURRENT_SCORE, 0);
        }

        /// <summary>Kiểm tra người dùng đã đăng nhập chưa.</summary>
        private bool IsLoggedIn() =>
            HttpContext.Session.GetInt32(SESSION_USER_ID).HasValue;
    }
}
