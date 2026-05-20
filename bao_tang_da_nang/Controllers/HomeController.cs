using Bảo_Tàng_Đà_Nẵng.Data;
using Bảo_Tàng_Đà_Nẵng.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Bảo_Tàng_Đà_Nẵng.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _db;

        public HomeController(ILogger<HomeController> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            // Lấy danh sách các danh mục / chủ đề từ CSDL và đếm số lượng câu hỏi hoạt động
            var categories = await _db.Questions
                .Where(q => q.IsActive && !string.IsNullOrEmpty(q.LocationName))
                .GroupBy(q => q.LocationName)
                .Select(g => new CategorySummaryViewModel
                {
                    Name = g.Key ?? "Khác",
                    QuestionCount = g.Count()
                })
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category; // Pass selected category to view

            // Bảng xếp hạng Top 5 (Tổng điểm)
            var leaderboard = await _db.QuizSessions
                .Where(s => s.IsCompleted)
                .GroupBy(s => new { s.UserId, s.User.FullName })
                .Select(g => new PlayerRankViewModel
                {
                    UserId = g.Key.UserId,
                    FullName = g.Key.FullName ?? "Người chơi",
                    TotalScore = g.Sum(s => s.TotalScore)
                })
                .OrderByDescending(x => x.TotalScore)
                .Take(5)
                .ToListAsync();

            ViewBag.Leaderboard = leaderboard;

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                // Thống kê cá nhân
                var userSessions = await _db.QuizSessions
                    .Include(s => s.SessionDetails)
                    .Where(s => s.UserId == userId.Value && s.IsCompleted)
                    .ToListAsync();

                int totalPlayed = userSessions.Count;
                int totalQuestions = userSessions.SelectMany(s => s.SessionDetails).Count();
                int totalCorrect = userSessions.SelectMany(s => s.SessionDetails).Count(d => d.IsCorrect);

                double winRate = totalQuestions > 0 ? Math.Round((double)totalCorrect / totalQuestions * 100) : 0;
                int totalScore = userSessions.Sum(s => s.TotalScore);

                // Cập nhật lại tổng điểm trên Session (Navbar)
                HttpContext.Session.SetInt32("CurrentScore", totalScore);

                // Tìm thứ hạng của người dùng hiện tại
                var allUserScores = await _db.QuizSessions
                    .Where(s => s.IsCompleted)
                    .GroupBy(s => s.UserId)
                    .Select(g => new { UserId = g.Key, TotalScore = g.Sum(s => s.TotalScore) })
                    .OrderByDescending(x => x.TotalScore)
                    .ToListAsync();

                int rank = allUserScores.FindIndex(x => x.UserId == userId.Value) + 1;

                ViewBag.UserStats = new UserStatsViewModel
                {
                    TotalPlayed = totalPlayed,
                    WinRate = winRate,
                    Rank = rank > 0 ? rank : 0,
                    TotalScore = totalScore
                };
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class CategorySummaryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
    }

    public class PlayerRankViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
    }

    public class UserStatsViewModel
    {
        public int TotalPlayed { get; set; }
        public double WinRate { get; set; }
        public int Rank { get; set; }
        public int TotalScore { get; set; }
    }
}
