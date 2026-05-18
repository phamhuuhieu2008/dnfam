using Bảo_Tàng_Đà_Nẵng.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Bảo_Tàng_Đà_Nẵng.Controllers
{
    // Filter kiểm tra quyền Admin
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
            base.OnActionExecuting(context);
        }
    }

    [AdminAuthorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalQuestions = await _db.Questions.CountAsync();
            var totalSessions = await _db.QuizSessions.CountAsync();
            var completedSessions = await _db.QuizSessions.CountAsync(s => s.IsCompleted);

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalQuestions = totalQuestions;
            ViewBag.TotalSessions = totalSessions;
            ViewBag.CompletedSessions = completedSessions;

            // Top 5 người chơi
            var topPlayers = await _db.QuizSessions
                .Where(s => s.IsCompleted)
                .Include(s => s.User)
                .OrderByDescending(s => s.TotalScore)
                .ThenBy(s => EF.Functions.DateDiffSecond(s.StartTime, s.EndTime)) // Cùng điểm thì ưu tiên thời gian nhanh hơn
                .Take(5)
                .Select(s => new
                {
                    FullName = s.User != null ? s.User.FullName : "Người chơi vô danh",
                    s.TotalScore,
                    Date = s.StartTime
                })
                .ToListAsync();
            
            ViewBag.TopPlayers = topPlayers;

            return View();
        }

        public async Task<IActionResult> Questions()
        {
            var questions = await _db.Questions
                .OrderByDescending(q => q.Id)
                .ToListAsync();
            return View(questions);
        }

        public async Task<IActionResult> Users()
        {
            var users = await _db.Users
                .OrderByDescending(u => u.Id)
                .ToListAsync();
            return View(users);
        }

        // ════════════════════════════════════════════════════════
        // QUẢN LÝ CÂU HỎI (CRUD)
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult CreateQuestion()
        {
            return View("QuestionForm", new Bảo_Tàng_Đà_Nẵng.Models.Question());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestion(Bảo_Tàng_Đà_Nẵng.Models.Question model)
        {
            if (!ModelState.IsValid)
                return View("QuestionForm", model);

            model.IsActive = true;
            _db.Questions.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMsg"] = "Thêm câu hỏi mới thành công!";
            return RedirectToAction(nameof(Questions));
        }

        [HttpGet]
        public async Task<IActionResult> EditQuestion(int id)
        {
            var question = await _db.Questions.FindAsync(id);
            if (question == null) return NotFound();
            return View("QuestionForm", question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int id, Bảo_Tàng_Đà_Nẵng.Models.Question model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
                return View("QuestionForm", model);

            var existing = await _db.Questions.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Content = model.Content;
            existing.OptionA = model.OptionA;
            existing.OptionB = model.OptionB;
            existing.OptionC = model.OptionC;
            existing.OptionD = model.OptionD;
            existing.CorrectOption = model.CorrectOption;
            existing.Points = model.Points;
            existing.LocationName = model.LocationName;
            existing.ImageUrl = model.ImageUrl;

            await _db.SaveChangesAsync();
            
            TempData["SuccessMsg"] = "Cập nhật câu hỏi thành công!";
            return RedirectToAction(nameof(Questions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleQuestionStatus(int id)
        {
            var question = await _db.Questions.FindAsync(id);
            if (question == null) return NotFound();

            question.IsActive = !question.IsActive;
            await _db.SaveChangesAsync();
            
            TempData["SuccessMsg"] = question.IsActive ? "Đã mở khóa câu hỏi." : "Đã khóa câu hỏi.";
            return RedirectToAction(nameof(Questions));
        }

        // ════════════════════════════════════════════════════════
        // QUẢN LÝ NGƯỜI DÙNG (Khóa/Mở khóa)
        // ════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Không cho phép Admin khóa chính mình
            var currentUserIdStr = HttpContext.Session.GetInt32("UserId");
            if (currentUserIdStr.HasValue && currentUserIdStr.Value == id)
            {
                TempData["ErrorMsg"] = "Bạn không thể tự khóa tài khoản của mình!";
                return RedirectToAction(nameof(Users));
            }

            // Không cho phép khóa Admin khác (nếu cần bảo mật cao hơn có thể thêm role check)
            if (user.Role == "Admin")
            {
                TempData["ErrorMsg"] = "Không thể khóa tài khoản Quản trị viên!";
                return RedirectToAction(nameof(Users));
            }

            user.IsActive = !user.IsActive;
            await _db.SaveChangesAsync();
            
            TempData["SuccessMsg"] = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản thành công.";
            return RedirectToAction(nameof(Users));
        }
    }
}
