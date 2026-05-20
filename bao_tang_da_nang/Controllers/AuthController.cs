using Microsoft.AspNetCore.Mvc;

namespace Bảo_Tàng_Đà_Nẵng.Controllers
{
    /// <summary>
    /// AuthController đã được rút gọn.
    /// Luồng đăng ký/đăng nhập không còn dùng cho người chơi.
    /// Các route cũ sẽ tự redirect về trang chủ để tránh lỗi liên kết cũ.
    /// </summary>
    public class AuthController : Controller
    {
        [AcceptVerbs("GET", "POST")]
        public IActionResult Login() => RedirectToAction("Index", "Home");

        [AcceptVerbs("GET", "POST")]
        public IActionResult Register() => RedirectToAction("Index", "Home");

        [AcceptVerbs("GET", "POST")]
        public IActionResult ChangePassword() => RedirectToAction("Index", "Home");

        [HttpGet, HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
