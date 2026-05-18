using Bảo_Tàng_Đà_Nẵng.Data;
using Bảo_Tàng_Đà_Nẵng.Models;
using Bảo_Tàng_Đà_Nẵng.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bảo_Tàng_Đà_Nẵng.Controllers
{
    /// <summary>
    /// QuizController — Xử lý toàn bộ luồng chơi quiz.
    ///
    /// Luồng chính: StartSession → GetQuestion → SubmitAnswer (lặp) → FinishQuiz → Result
    ///
    /// Bảo vệ chống double-click: SubmitAnswer kiểm tra DB trước khi Insert
    /// dựa trên Unique Constraint (SessionId, QuestionId).
    /// </summary>
    public class QuizController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        private const string SESSION_USER_ID    = "UserId";
        private const string SESSION_QUIZ_ID    = "ActiveQuizSessionId";
        private const string SESSION_Q_LIST     = "QuizQuestionIds";   // CSV danh sách QuestionId
        private const string SESSION_Q_INDEX    = "QuizCurrentIndex";
        private const string SESSION_SCORE      = "CurrentScore";

        public QuizController(AppDbContext db, IConfiguration config)
        {
            _db     = db;
            _config = config;
        }

        // ════════════════════════════════════════════════════════
        // BẮT ĐẦU PHIÊN CHƠI
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> StartSession()
        {
            int? userId = HttpContext.Session.GetInt32(SESSION_USER_ID);
            if (userId == null) return RedirectToAction("Login", "Auth");

            int questionCount = _config.GetValue<int>("AppSettings:QuestionsPerQuiz", 5);

            // Lấy ngẫu nhiên N câu hỏi đang active từ ngân hàng
            var questionIds = await _db.Questions
                .Where(q => q.IsActive)
                .OrderBy(q => Guid.NewGuid())   // Random shuffle tại DB level
                .Take(questionCount)
                .Select(q => q.Id)
                .ToListAsync();

            if (questionIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Hiện chưa có câu hỏi nào. Vui lòng quay lại sau.";
                return RedirectToAction("Index", "Home");
            }

            // Tạo phiên chơi mới trong DB
            var session = new QuizSession
            {
                UserId    = userId.Value,
                StartTime = DateTime.UtcNow,
                TotalScore  = 0,
                IsCompleted = false
            };

            _db.QuizSessions.Add(session);
            await _db.SaveChangesAsync();

            // Lưu trạng thái phiên vào Session (không lưu toàn bộ object)
            HttpContext.Session.SetInt32(SESSION_QUIZ_ID, session.Id);
            HttpContext.Session.SetString(SESSION_Q_LIST, string.Join(",", questionIds));
            HttpContext.Session.SetInt32(SESSION_Q_INDEX, 0);
            HttpContext.Session.SetInt32(SESSION_SCORE, 0);

            return RedirectToAction(nameof(Question));
        }

        // ════════════════════════════════════════════════════════
        // HIỂN THỊ CÂU HỎI
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Question()
        {
            // Lấy trạng thái phiên từ Session
            int? sessionId  = HttpContext.Session.GetInt32(SESSION_QUIZ_ID);
            string? qList   = HttpContext.Session.GetString(SESSION_Q_LIST);
            int? qIndex     = HttpContext.Session.GetInt32(SESSION_Q_INDEX);
            int currentScore = HttpContext.Session.GetInt32(SESSION_SCORE) ?? 0;

            if (sessionId == null || qList == null || qIndex == null)
                return RedirectToAction("Index", "Home");

            var questionIds = qList.Split(',').Select(int.Parse).ToList();

            // Đã hoàn thành hết câu hỏi
            if (qIndex >= questionIds.Count)
                return RedirectToAction(nameof(FinishQuiz));

            int currentQuestionId = questionIds[qIndex.Value];
            var question = await _db.Questions.FindAsync(currentQuestionId);

            if (question == null)
                return RedirectToAction(nameof(FinishQuiz));

            var viewModel = new QuestionViewModel
            {
                QuestionId     = question.Id,
                SessionId      = sessionId.Value,
                Content        = question.Content,
                OptionA        = question.OptionA,
                OptionB        = question.OptionB,
                OptionC        = question.OptionC,
                OptionD        = question.OptionD,
                ImageUrl       = question.ImageUrl,
                LocationName   = question.LocationName,
                Points         = question.Points,
                CurrentIndex   = qIndex.Value + 1,
                TotalQuestions = questionIds.Count,
                CurrentScore   = currentScore
            };

            return View(viewModel);
        }

        // ════════════════════════════════════════════════════════
        // NỘP ĐÁP ÁN (API Endpoint — gọi bởi JS)
        //
        // ★ CHỐNG DOUBLE-CLICK (Concurrency Protection) ★
        //   Bước 1: Kiểm tra xem (SessionId, QuestionId) đã tồn tại chưa
        //   Bước 2: Nếu chưa → Insert mới
        //   Bước 3: Nếu đã có → Return kết quả cũ (idempotent)
        //   → Tầng DB còn có Unique Constraint UQ_SessionDetails_Session_Question
        //     làm lớp bảo vệ cuối cùng nếu race condition xảy ra.
        // ════════════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Dữ liệu không hợp lệ." });

            // Xác thực session người dùng
            int? userId = HttpContext.Session.GetInt32(SESSION_USER_ID);
            if (userId == null) return Unauthorized(new { error = "Phiên đăng nhập đã hết hạn." });

            // Xác thực phiên quiz còn hoạt động
            var quizSession = await _db.QuizSessions
                .FirstOrDefaultAsync(s => s.Id == request.SessionId
                                       && s.UserId == userId
                                       && !s.IsCompleted);

            if (quizSession == null)
                return BadRequest(new { error = "Phiên chơi không hợp lệ hoặc đã kết thúc." });

            // Lấy thông tin câu hỏi
            var question = await _db.Questions.FindAsync(request.QuestionId);
            if (question == null)
                return BadRequest(new { error = "Câu hỏi không tồn tại." });

            // ★ BƯỚC 1: Kiểm tra xem câu hỏi này đã được trả lời trong phiên chưa ★
            var existingDetail = await _db.SessionDetails
                .FirstOrDefaultAsync(d => d.SessionId   == request.SessionId
                                       && d.QuestionId  == request.QuestionId);

            bool isCorrect = string.Equals(
                request.SelectedOption?.Trim(), question.CorrectOption?.Trim(),
                StringComparison.OrdinalIgnoreCase
            );

            if (existingDetail != null)
            {
                // ★ ĐÃ TRẢ LỜI RỒI — trả về kết quả cũ (idempotent response)
                // Điều này xử lý trường hợp double-click hoặc request trùng lặp
                return Ok(new SubmitAnswerResponse
                {
                    IsCorrect       = existingDetail.IsCorrect,
                    CorrectOption   = question.CorrectOption,
                    PointsEarned    = existingDetail.IsCorrect ? question.Points : 0,
                    NewTotalScore   = quizSession.TotalScore,
                    IsLastQuestion  = IsLastQuestion(request.QuestionId)
                });
            }

            // ★ BƯỚC 2: INSERT MỚI — sử dụng ExecutionStrategy để hỗ trợ retry ★
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    int pointsEarned = isCorrect ? question.Points : 0;

                    // Lưu chi tiết câu trả lời
                    var detail = new SessionDetail
                    {
                        SessionId      = request.SessionId,
                        QuestionId     = request.QuestionId,
                        SelectedOption = request.SelectedOption.ToUpper(),
                        IsCorrect      = isCorrect,
                        CreatedAt      = DateTime.UtcNow
                    };

                    _db.SessionDetails.Add(detail);

                    // Cập nhật tổng điểm trong phiên
                    quizSession.TotalScore += pointsEarned;
                    _db.QuizSessions.Update(quizSession);

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Đồng bộ điểm vào Session cookie
                    HttpContext.Session.SetInt32(SESSION_SCORE, quizSession.TotalScore);

                    // Chuyển sang câu tiếp theo
                    int currentIndex = HttpContext.Session.GetInt32(SESSION_Q_INDEX) ?? 0;
                    HttpContext.Session.SetInt32(SESSION_Q_INDEX, currentIndex + 1);

                    bool isLast = IsLastQuestion(request.QuestionId);

                    return Ok(new SubmitAnswerResponse
                    {
                        IsCorrect      = isCorrect,
                        CorrectOption  = question.CorrectOption,
                        PointsEarned   = pointsEarned,
                        NewTotalScore  = quizSession.TotalScore,
                        IsLastQuestion = isLast
                    });
                }
                catch (DbUpdateException ex)
                    when (ex.InnerException?.Message.Contains("UQ_SessionDetails_Session_Question") == true)
                {
                    // ★ Tầng bảo vệ cuối: Unique Constraint DB bắt được race condition ★
                    await transaction.RollbackAsync();

                    var savedDetail = await _db.SessionDetails
                        .FirstOrDefaultAsync(d => d.SessionId  == request.SessionId
                                               && d.QuestionId == request.QuestionId);

                    return Ok(new SubmitAnswerResponse
                    {
                        IsCorrect      = savedDetail?.IsCorrect ?? false,
                        CorrectOption  = question.CorrectOption,
                        PointsEarned   = (savedDetail?.IsCorrect ?? false) ? question.Points : 0,
                        NewTotalScore  = quizSession.TotalScore,
                        IsLastQuestion = IsLastQuestion(request.QuestionId)
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Bắt mọi lỗi khác để hiển thị rõ nguyên nhân
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
            int? userId    = HttpContext.Session.GetInt32(SESSION_USER_ID);

            if (sessionId == null || userId == null)
                return RedirectToAction("Index", "Home");

            // Đánh dấu phiên đã hoàn thành
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
                session.EndTime     = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // Xây dựng ViewModel kết quả
            int correctCount = session.SessionDetails.Count(d => d.IsCorrect);

            var resultVm = new QuizResultViewModel
            {
                SessionId      = session.Id,
                PlayerName     = session.User?.FullName ?? "Người chơi",
                TotalScore     = session.TotalScore,
                TotalQuestions = session.SessionDetails.Count,
                CorrectAnswers = correctCount,
                StartTime      = session.StartTime,
                EndTime        = session.EndTime ?? DateTime.UtcNow,
                Answers        = session.SessionDetails.Select(d => new AnswerReviewItem
                {
                    QuestionContent = d.Question?.Content ?? "",
                    SelectedOption  = d.SelectedOption ?? "-",
                    CorrectOption   = d.Question?.CorrectOption ?? "",
                    IsCorrect       = d.IsCorrect,
                    Points          = d.Question?.Points ?? 0,
                    LocationName    = d.Question?.LocationName
                }).ToList()
            };

            // Dọn dẹp Session quiz (giữ lại UserId và thông tin user)
            HttpContext.Session.Remove(SESSION_QUIZ_ID);
            HttpContext.Session.Remove(SESSION_Q_LIST);
            HttpContext.Session.Remove(SESSION_Q_INDEX);
            HttpContext.Session.SetInt32(SESSION_SCORE, session.TotalScore);

            return View(resultVm);
        }

        // ════════════════════════════════════════════════════════
        // HELPER
        // ════════════════════════════════════════════════════════

        /// <summary>Kiểm tra câu hỏi hiện tại có phải câu cuối không.</summary>
        private bool IsLastQuestion(int questionId)
        {
            string? qList  = HttpContext.Session.GetString(SESSION_Q_LIST);
            int?    qIndex = HttpContext.Session.GetInt32(SESSION_Q_INDEX);

            if (qList == null || qIndex == null) return true;

            var ids = qList.Split(',').Select(int.Parse).ToList();
            // qIndex đã được tăng trong SubmitAnswer, nên nó chính là số câu đã làm xong.
            // Nếu số câu đã làm (qIndex) lớn hơn hoặc bằng tổng số câu (ids.Count), thì đây là câu cuối.
            return qIndex >= ids.Count;
        }
    }
}
