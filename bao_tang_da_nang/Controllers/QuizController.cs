using Bảo_Tàng_Đà_Nẵng.Data;
using Bảo_Tàng_Đà_Nẵng.Models;
using Bảo_Tàng_Đà_Nẵng.Models.ViewModels;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bảo_Tàng_Đà_Nẵng.Controllers
{
    /// <summary>
    /// QuizController — Xử lý toàn bộ luồng chơi quiz.
    ///
    /// Luồng chính: StartSession → Question → SubmitAnswer (lặp) → FinishQuiz
    /// Người chơi chỉ cần nhập tên trước khi vào chủ đề.
    /// </summary>
    public class QuizController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        private const string SESSION_USER_ID = "UserId";
        private const string SESSION_USER_FULLNAME = "UserFullName";
        private const string SESSION_USER_ROLE = "UserRole";
        private const string SESSION_QUIZ_ID = "ActiveQuizSessionId";
        private const string SESSION_Q_LIST = "QuizQuestionIds";
        private const string SESSION_Q_INDEX = "QuizCurrentIndex";
        private const string SESSION_SCORE = "CurrentScore";

        public QuizController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // ════════════════════════════════════════════════════════
        // BẮT ĐẦU PHIÊN CHƠI
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult StartSession(string? topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn một chủ đề trước khi bắt đầu.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new StartSessionViewModel
            {
                Topic = topic.Trim(),
                PlayerName = HttpContext.Session.GetString(SESSION_USER_FULLNAME) ?? string.Empty
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartSession(StartSessionViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string topic = model.Topic.Trim();
            string playerName = model.PlayerName.Trim();

            if (string.IsNullOrWhiteSpace(playerName))
            {
                ModelState.AddModelError(nameof(model.PlayerName), "Vui lòng nhập tên của bạn.");
                return View(model);
            }

            int questionCount = _config.GetValue<int>("AppSettings:QuestionsPerQuiz", 5);
            var questionIds = await GetQuestionIdsByTopicAsync(topic, questionCount);

            if (questionIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Hiện chưa có câu hỏi nào cho chủ đề này. Vui lòng chọn chủ đề khác.";
                return RedirectToAction("Index", "Home");
            }

            var player = await GetOrCreatePlayerAsync(playerName);
            var quizSession = new QuizSession
            {
                UserId = player.Id,
                StartTime = DateTime.UtcNow,
                TotalScore = 0,
                IsCompleted = false
            };

            _db.QuizSessions.Add(quizSession);
            await _db.SaveChangesAsync();

            ApplyPlayerSession(player);
            HttpContext.Session.SetInt32(SESSION_QUIZ_ID, quizSession.Id);
            HttpContext.Session.SetString(SESSION_Q_LIST, string.Join(",", questionIds));
            HttpContext.Session.SetInt32(SESSION_Q_INDEX, 0);
            HttpContext.Session.SetInt32(SESSION_SCORE, 0);

            return RedirectToAction(nameof(Question));
        }

        // ════════════════════════════════════════════════════════
        // DUYỆT ĐỀ TÀI THEO DANH MỤC
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> BrowseCategory(int id)
        {
            var mainCategories = new List<(string Name, string Description, List<string> Prefixes)>
            {
                ("DANH MỤC 1: TỰ NHIÊN VÀ CON NGƯỜI ĐÀ NẴNG", "Khám phá vị trí địa lý, hệ sinh thái và văn hóa tiền sử.", new List<string> { "Chủ đề 1", "Chủ đề 2", "Chủ đề 3" }),
                ("DANH MỤC 2: LỊCH SỬ ĐÔ THỊ VÀ KHÁNG CHIẾN", "Tiến trình lịch sử từ thời phong kiến đến hai cuộc kháng chiến chống Pháp - Mỹ.", new List<string> { "Chủ đề 4", "Chủ đề 5", "Chủ đề 6", "Chủ đề 7", "Chủ đề 8" }),
                ("DANH MỤC 3: VĂN HÓA VÀ DI SẢN TRUYỀN THỐNG", "Đời sống văn hóa, phong tục và các bộ sưu tập kỷ vật quý hiếm.", new List<string> { "Chủ đề 9", "Chủ đề 10" })
            };

            if (id < 0 || id >= mainCategories.Count)
                return RedirectToAction("Index", "Home");

            var category = mainCategories[id];
            var prefixes = category.Prefixes;

            // Lấy danh sách các chủ đề từ DB và đếm số câu hỏi
            var topics = await _db.Questions
                .Where(q => q.IsActive && q.LocationName != null)
                .GroupBy(q => q.LocationName)
                .Select(g => new CategorySummaryViewModel
                {
                    Name = g.Key!,
                    QuestionCount = g.Count()
                })
                .ToListAsync();

            // Lọc các chủ đề thuộc danh mục này
            var filteredTopics = topics.Where(t => prefixes.Any(p => t.Name.StartsWith(p))).ToList();

            ViewBag.CategoryName = category.Name;
            ViewBag.CategoryDescription = category.Description;

            return View(filteredTopics);
        }

        // ════════════════════════════════════════════════════════
        // HIỂN THỊ CÂU HỎI
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Question()
        {
            int? sessionId = HttpContext.Session.GetInt32(SESSION_QUIZ_ID);
            string? qList = HttpContext.Session.GetString(SESSION_Q_LIST);
            int? qIndex = HttpContext.Session.GetInt32(SESSION_Q_INDEX);
            int currentScore = HttpContext.Session.GetInt32(SESSION_SCORE) ?? 0;

            if (sessionId == null || qList == null || qIndex == null)
                return RedirectToAction("Index", "Home");

            var questionIds = qList.Split(',').Select(int.Parse).ToList();

            if (qIndex >= questionIds.Count)
                return RedirectToAction(nameof(FinishQuiz));

            int currentQuestionId = questionIds[qIndex.Value];
            var question = await _db.Questions.FindAsync(currentQuestionId);

            if (question == null)
                return RedirectToAction(nameof(FinishQuiz));

            var viewModel = new QuestionViewModel
            {
                QuestionId = question.Id,
                SessionId = sessionId.Value,
                Content = question.Content,
                OptionA = question.OptionA,
                OptionB = question.OptionB,
                OptionC = question.OptionC,
                OptionD = question.OptionD,
                ImageUrl = question.ImageUrl,
                LocationName = question.LocationName,
                Points = question.Points,
                CurrentIndex = qIndex.Value + 1,
                TotalQuestions = questionIds.Count,
                CurrentScore = currentScore
            };

            return View(viewModel);
        }

        // ════════════════════════════════════════════════════════
        // NỘP ĐÁP ÁN
        // ════════════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Dữ liệu không hợp lệ." });

            int? userId = HttpContext.Session.GetInt32(SESSION_USER_ID);
            if (userId == null) return Unauthorized(new { error = "Phiên chơi đã hết hạn." });

            var quizSession = await _db.QuizSessions
                .FirstOrDefaultAsync(s => s.Id == request.SessionId
                                       && s.UserId == userId
                                       && !s.IsCompleted);

            if (quizSession == null)
                return BadRequest(new { error = "Phiên chơi không hợp lệ hoặc đã kết thúc." });

            var question = await _db.Questions.FindAsync(request.QuestionId);
            if (question == null)
                return BadRequest(new { error = "Câu hỏi không tồn tại." });

            var existingDetail = await _db.SessionDetails
                .FirstOrDefaultAsync(d => d.SessionId == request.SessionId
                                       && d.QuestionId == request.QuestionId);

            bool isCorrect = string.Equals(
                request.SelectedOption?.Trim(), question.CorrectOption?.Trim(),
                StringComparison.OrdinalIgnoreCase
            );

            if (existingDetail != null)
            {
                return Ok(new SubmitAnswerResponse
                {
                    IsCorrect = existingDetail.IsCorrect,
                    CorrectOption = question.CorrectOption,
                    PointsEarned = existingDetail.IsCorrect ? question.Points : 0,
                    NewTotalScore = quizSession.TotalScore,
                    IsLastQuestion = IsLastQuestion()
                });
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    int pointsEarned = isCorrect ? question.Points : 0;

                    var detail = new SessionDetail
                    {
                        SessionId = request.SessionId,
                        QuestionId = request.QuestionId,
                        SelectedOption = request.SelectedOption.ToUpper(),
                        IsCorrect = isCorrect,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.SessionDetails.Add(detail);

                    quizSession.TotalScore += pointsEarned;
                    _db.QuizSessions.Update(quizSession);

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    HttpContext.Session.SetInt32(SESSION_SCORE, quizSession.TotalScore);

                    int currentIndex = HttpContext.Session.GetInt32(SESSION_Q_INDEX) ?? 0;
                    HttpContext.Session.SetInt32(SESSION_Q_INDEX, currentIndex + 1);

                    return Ok(new SubmitAnswerResponse
                    {
                        IsCorrect = isCorrect,
                        CorrectOption = question.CorrectOption,
                        PointsEarned = pointsEarned,
                        NewTotalScore = quizSession.TotalScore,
                        IsLastQuestion = IsLastQuestion()
                    });
                }
                catch (DbUpdateException ex)
                    when (ex.InnerException?.Message.Contains("UQ_SessionDetails_Session_Question") == true)
                {
                    await transaction.RollbackAsync();

                    var savedDetail = await _db.SessionDetails
                        .FirstOrDefaultAsync(d => d.SessionId == request.SessionId
                                               && d.QuestionId == request.QuestionId);

                    return Ok(new SubmitAnswerResponse
                    {
                        IsCorrect = savedDetail?.IsCorrect ?? false,
                        CorrectOption = question.CorrectOption,
                        PointsEarned = (savedDetail?.IsCorrect ?? false) ? question.Points : 0,
                        NewTotalScore = quizSession.TotalScore,
                        IsLastQuestion = IsLastQuestion()
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { error = "Lỗi hệ thống khi chấm điểm", details = ex.InnerException?.Message ?? ex.Message });
                }
            });
        }

        // ════════════════════════════════════════════════════════
        // KẾT THÚC BÀI THI
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> FinishQuiz()
        {
            int? sessionId = HttpContext.Session.GetInt32(SESSION_QUIZ_ID);
            int? userId = HttpContext.Session.GetInt32(SESSION_USER_ID);

            if (sessionId == null || userId == null)
                return RedirectToAction("Index", "Home");

            var session = await _db.QuizSessions
                .Include(s => s.SessionDetails)
                    .ThenInclude(d => d.Question)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return RedirectToAction("Index", "Home");

            if (!session.IsCompleted)
            {
                session.IsCompleted = true;
                session.EndTime = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            int correctCount = session.SessionDetails.Count(d => d.IsCorrect);

            var resultVm = new QuizResultViewModel
            {
                SessionId = session.Id,
                PlayerName = session.User?.FullName ?? "Người chơi",
                TotalScore = session.TotalScore,
                TotalQuestions = session.SessionDetails.Count,
                CorrectAnswers = correctCount,
                StartTime = session.StartTime,
                EndTime = session.EndTime ?? DateTime.UtcNow,
                Answers = session.SessionDetails.Select(d => new AnswerReviewItem
                {
                    QuestionContent = d.Question?.Content ?? "",
                    SelectedOption = d.SelectedOption ?? "-",
                    CorrectOption = d.Question?.CorrectOption ?? "",
                    IsCorrect = d.IsCorrect,
                    Points = d.Question?.Points ?? 0,
                    LocationName = d.Question?.LocationName
                }).ToList()
            };

            HttpContext.Session.Remove(SESSION_QUIZ_ID);
            HttpContext.Session.Remove(SESSION_Q_LIST);
            HttpContext.Session.Remove(SESSION_Q_INDEX);
            HttpContext.Session.SetInt32(SESSION_SCORE, session.TotalScore);

            return View(resultVm);
        }

        // ════════════════════════════════════════════════════════
        // HELPER
        // ════════════════════════════════════════════════════════

        private async Task<List<int>> GetQuestionIdsByTopicAsync(string topic, int questionCount)
        {
            var query = _db.Questions.Where(q => q.IsActive);

            if (!string.IsNullOrWhiteSpace(topic))
            {
                query = query.Where(q => q.LocationName == topic);
            }

            return await query
                .OrderBy(q => Guid.NewGuid())
                .Take(questionCount)
                .Select(q => q.Id)
                .ToListAsync();
        }

        private async Task<User> GetOrCreatePlayerAsync(string playerName)
        {
            var existingPlayer = await _db.Users.FirstOrDefaultAsync(u =>
                u.IsActive &&
                u.Role == "Player" &&
                u.FullName == playerName);

            if (existingPlayer != null)
                return existingPlayer;

            var player = new User
            {
                Username = $"player_{Guid.NewGuid():N}",
                FullName = playerName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"), workFactor: 10),
                Role = "Player",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.Users.Add(player);
            await _db.SaveChangesAsync();
            return player;
        }

        private void ApplyPlayerSession(User user)
        {
            HttpContext.Session.SetInt32(SESSION_USER_ID, user.Id);
            HttpContext.Session.SetString(SESSION_USER_FULLNAME, user.FullName);
            HttpContext.Session.SetString(SESSION_USER_ROLE, user.Role);
            HttpContext.Session.SetInt32(SESSION_SCORE, 0);
        }

        private bool IsLastQuestion()
        {
            string? qList = HttpContext.Session.GetString(SESSION_Q_LIST);
            int? qIndex = HttpContext.Session.GetInt32(SESSION_Q_INDEX);

            if (qList == null || qIndex == null) return true;

            var ids = qList.Split(',').Select(int.Parse).ToList();
            return qIndex >= ids.Count;
        }
    }
}
