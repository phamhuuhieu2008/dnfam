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
            Console.WriteLine($"[DEBUG] StartSession GET called with Topic: '{topic}' (IsNull: {topic == null})");

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

            Console.WriteLine($"[DEBUG] StartSession POST called with Topic: '{topic}' (Length: {topic.Length})");

            HttpContext.Session.SetString(SESSION_USER_FULLNAME, playerName);

            if (string.IsNullOrWhiteSpace(playerName))
            {
                ModelState.AddModelError(nameof(model.PlayerName), "Vui lòng nhập tên của bạn.");
                return View(model);
            }

            // Dùng số câu người dùng chọn từ slider, giới hạn theo chủ đề
            int questionCount = model.QuestionCount > 0 ? model.QuestionCount : 30;

            // Giới hạn tối đa theo từng loại chủ đề
            bool isTonghopMode = model.Topic.StartsWith("Tổng hợp", StringComparison.OrdinalIgnoreCase);
            if (model.Topic.Contains("Tổng hợp 300 câu", StringComparison.OrdinalIgnoreCase))
            {
                // Bộ 300 câu cho phép lấy tối đa 300
                questionCount = Math.Min(questionCount, 300);
            }
            else if (isTonghopMode)
            {
                // 4 chủ đề Tổng hợp: mỗi chủ đề lấy tối đa 100 câu ngẫu nhiên
                questionCount = Math.Min(questionCount, 100);
            }
            else
            {
                // Mỗi đề tài tối đa 30 câu (số câu thực có)
                questionCount = Math.Min(questionCount, 30);
            }

            var questionIds = await GetQuestionIdsByTopicAsync(model.Topic, questionCount);

            Console.WriteLine($"[DEBUG] GetQuestionIdsByTopicAsync returned {questionIds.Count} questions.");

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
                ("DANH MỤC 1: TỰ NHIÊN VÀ CON NGƯỜI ĐÀ NẴNG", "Khám phá vị trí địa lý, hệ sinh thái và văn hóa tiền sử.", new List<string> { "ĐỀ TÀI 1", "ĐỀ TÀI 2", "ĐỀ TÀI 3" }),
                ("DANH MỤC 2: LỊCH SỬ ĐÔ THỊ VÀ KHÁNG CHIẾN", "Tiến trình lịch sử từ thời phong kiến đến hai cuộc kháng chiến chống Pháp - Mỹ.", new List<string> { "ĐỀ TÀI 4", "ĐỀ TÀI 5", "ĐỀ TÀI 6", "ĐỀ TÀI 7", "ĐỀ TÀI 8" }),
                ("DANH MỤC 3: VĂN HÓA VÀ DI SẢN TRUYỀN THỐNG", "Đời sống văn hóa, phong tục và các bộ sưu tập kỷ vật quý hiếm.", new List<string> { "ĐỀ TÀI 9", "ĐỀ TÀI 10" })
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

            int? userId = HttpContext.Session.GetInt32(SESSION_USER_ID);
            if (userId.HasValue)
            {
                var completedTopics = await _db.QuizSessions
                    .Where(s => s.UserId == userId.Value && s.IsCompleted)
                    .SelectMany(s => s.SessionDetails)
                    .Where(d => d.Question != null && d.Question.LocationName != null)
                    .Select(d => d.Question!.LocationName!)
                    .Distinct()
                    .ToListAsync();

                foreach(var t in topics)
                {
                    t.IsCompleted = completedTopics.Contains(t.Name);
                }
            }

            // Lọc các chủ đề thuộc danh mục này
            var filteredTopics = topics.Where(t => prefixes.Any(p => t.Name.StartsWith(p))).ToList();

            ViewBag.CategoryName = category.Name;
            ViewBag.CategoryDescription = category.Description;

            return View(filteredTopics);
        }

        public IActionResult ForceSeed()
        {
            Bảo_Tàng_Đà_Nẵng.ParseAndSeed.Run();
            return Ok("Force seed executed. Check terminal logs.");
        }

        // ════════════════════════════════════════════════════════
        // KHO BÀI LÀM (TẤT CẢ CHỦ ĐỀ)
        // ════════════════════════════════════════════════════════
        
        [HttpGet]
        public async Task<IActionResult> Library(string? query)
        {
            var categories = await _db.Questions
                .Where(q => q.IsActive && !string.IsNullOrEmpty(q.LocationName))
                .GroupBy(q => q.LocationName)
                .Select(g => new CategorySummaryViewModel
                {
                    Name = g.Key ?? "Khác",
                    QuestionCount = g.Count()
                })
                .ToListAsync();

            int? userId = HttpContext.Session.GetInt32(SESSION_USER_ID);
            if (userId.HasValue)
            {
                var completedTopics = await _db.QuizSessions
                    .Where(s => s.UserId == userId.Value && s.IsCompleted)
                    .SelectMany(s => s.SessionDetails)
                    .Where(d => d.Question != null && d.Question.LocationName != null)
                    .Select(d => d.Question!.LocationName!)
                    .Distinct()
                    .ToListAsync();

                foreach(var cat in categories)
                {
                    cat.IsCompleted = completedTopics.Contains(cat.Name);
                }
            }

            if (!string.IsNullOrEmpty(query))
            {
                string RemoveDiacritics(string text)
                {
                    if (string.IsNullOrWhiteSpace(text)) return text;
                    var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
                    var sb = new System.Text.StringBuilder();
                    foreach (var c in normalized)
                    {
                        if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                            sb.Append(c);
                    }
                    return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
                }

                string queryLower = RemoveDiacritics(query).Trim().ToLower();
                categories = categories.Where(c => RemoveDiacritics(c.Name).ToLower().Contains(queryLower)).ToList();
                ViewBag.SearchQuery = query;
            }

            return View(categories);
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
                ContentEn = question.ContentEn,
                OptionA = question.OptionA,
                OptionAEn = question.OptionAEn,
                OptionB = question.OptionB,
                OptionBEn = question.OptionBEn,
                OptionC = question.OptionC,
                OptionCEn = question.OptionCEn,
                OptionD = question.OptionD,
                OptionDEn = question.OptionDEn,
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

            var distinctTopics = session.SessionDetails.Where(d => d.Question != null && d.Question.LocationName != null).Select(d => d.Question!.LocationName).Distinct().ToList();
            string topicName = "Trắc nghiệm Tổng hợp";
            if (distinctTopics.Count == 1)
            {
                topicName = distinctTopics.First() ?? "Trắc nghiệm Tổng hợp";
            }
            else if (distinctTopics.Count > 1)
            {
                topicName = "Chủ đề Tổng hợp (Nhiều đề tài)";
            }
            
            ViewBag.TopicName = topicName;

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
                    QuestionContentEn = d.Question?.ContentEn,
                    SelectedOption = d.SelectedOption ?? "-",
                    SelectedOptionTextEn = d.SelectedOption == "A" ? d.Question?.OptionAEn : 
                                           d.SelectedOption == "B" ? d.Question?.OptionBEn : 
                                           d.SelectedOption == "C" ? d.Question?.OptionCEn : 
                                           d.SelectedOption == "D" ? d.Question?.OptionDEn : null,
                    CorrectOption = d.Question?.CorrectOption ?? "",
                    CorrectOptionTextEn = d.Question?.CorrectOption == "A" ? d.Question?.OptionAEn : 
                                          d.Question?.CorrectOption == "B" ? d.Question?.OptionBEn : 
                                          d.Question?.CorrectOption == "C" ? d.Question?.OptionCEn : 
                                          d.Question?.CorrectOption == "D" ? d.Question?.OptionDEn : null,
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
                var allTopics = await _db.Questions.Where(q => q.IsActive && q.LocationName != null)
                                                   .Select(q => q.LocationName)
                                                   .Distinct()
                                                   .ToListAsync();
                
                string Normalize(string? s) => s?.Replace('\u00A0', ' ').Replace("\r", "").Replace("\n", "").Trim() ?? "";
                
                string normalizedInput = Normalize(topic);

                // Các chủ đề Tổng hợp 1-4: lấy ngẫu nhiên từ TOÀN BỘ ngân hàng câu hỏi
                bool isTonghop = normalizedInput.Equals("Tổng hợp 1", StringComparison.OrdinalIgnoreCase) ||
                                  normalizedInput.Equals("Tổng hợp 2", StringComparison.OrdinalIgnoreCase) ||
                                  normalizedInput.Equals("Tổng hợp 3", StringComparison.OrdinalIgnoreCase) ||
                                  normalizedInput.Equals("Tổng hợp 4", StringComparison.OrdinalIgnoreCase) ||
                                  normalizedInput.Equals("Tổng hợp 300 câu", StringComparison.OrdinalIgnoreCase) ||
                                  normalizedInput.StartsWith("Tổng hợp", StringComparison.OrdinalIgnoreCase);

                if (isTonghop)
                {
                    // Không filter — lấy toàn bộ câu hỏi từ tất cả đề tài
                }
                else if (normalizedInput.StartsWith("Chủ đề 1"))
                {
                    // Địa lý & Lịch sử
                    query = query.Where(q => q.LocationName != null && q.LocationName.Contains("ĐỀ TÀI 1"));
                }
                else if (normalizedInput.StartsWith("Chủ đề 2"))
                {
                    // Thiên nhiên & Con người
                    query = query.Where(q => q.LocationName != null && q.LocationName.Contains("ĐỀ TÀI 2"));
                }
                else if (normalizedInput.StartsWith("Chủ đề 3"))
                {
                    // Địa chất & Biển
                    query = query.Where(q => q.LocationName != null && q.LocationName.Contains("ĐỀ TÀI 3"));
                }
                else if (normalizedInput.StartsWith("Chủ đề 4"))
                {
                    // Tiền sơ sử & Sa Huỳnh
                    query = query.Where(q => q.LocationName != null && q.LocationName.Contains("ĐỀ TÀI 4"));
                }
                else
                {
                    string? exactDbTopic = allTopics.FirstOrDefault(t => Normalize(t) == normalizedInput);

                    if (exactDbTopic != null)
                    {
                        query = query.Where(q => q.LocationName == exactDbTopic);
                    }
                    else
                    {
                        // Tương thích ngược với dữ liệu mẫu (Seeded data) nếu chưa Import JSON mới
                        if (normalizedInput == "Tổng hợp") query = query.Where(q => q.LocationName != null && q.LocationName.Contains("Chủ đề 1"));
                        else if (normalizedInput == "Bảo tàng làng nghề") query = query.Where(q => q.LocationName != null && q.LocationName.Contains("Chủ đề 2"));
                        else if (normalizedInput == "Nghệ nhân") query = query.Where(q => q.LocationName != null && q.LocationName.Contains("Chủ đề 3"));
                        else if (normalizedInput == "Câu lạc bộ") query = query.Where(q => q.LocationName != null && q.LocationName.Contains("Chủ đề 4"));
                        else query = query.Where(q => q.LocationName == topic);
                    }
                }
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
