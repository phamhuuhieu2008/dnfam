using Bảo_Tàng_Đà_Nẵng.Data;
using Bảo_Tàng_Đà_Nẵng.Models;
using Bảo_Tàng_Đà_Nẵng.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bảo_Tàng_Đà_Nẵng.Controllers
{
    // Filter kiểm tra quyền Admin (hỗ trợ nhiều action public)
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        // Danh sách action không cần xác thực đầy đủ
        private static readonly HashSet<string> PublicActions = new(StringComparer.OrdinalIgnoreCase)
        {
            "Gate", "GateEmail", "GateOtp"
        };

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var action = context.ActionDescriptor.RouteValues["action"] ?? "";

            if (PublicActions.Contains(action))
            {
                base.OnActionExecuting(context);
                return;
            }

            var role = context.HttpContext.Session.GetString("UserRole");
            var adminUnlocked = context.HttpContext.Session.GetString("AdminUnlocked") == "1";

            if (role != "Admin" && !adminUnlocked)
            {
                context.Result = new RedirectToActionResult("Gate", "Admin", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    [AdminAuthorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;

        private const string SESSION_ADMIN_UNLOCKED = "AdminUnlocked";
        private const string SESSION_PIN_VERIFIED    = "AdminPinVerified";
        private const string SESSION_PENDING_EMAIL   = "AdminPendingEmail";

        public AdminController(AppDbContext db, IConfiguration config, IWebHostEnvironment env, IEmailService emailService)
        {
            _db = db;
            _config = config;
            _env = env;
            _emailService = emailService;
        }

        // ════════════════════════════════════════════════════════
        // BƯỚC 1: NHẬP MÃ PIN
        // ════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Gate()
        {
            if (HttpContext.Session.GetString(SESSION_ADMIN_UNLOCKED) == "1" || HttpContext.Session.GetString("UserRole") == "Admin")
                return RedirectToAction(nameof(Index));

            // Nếu đã xác thực PIN, chuyển sang bước 2
            if (HttpContext.Session.GetString(SESSION_PIN_VERIFIED) == "1")
                return RedirectToAction(nameof(GateEmail));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public IActionResult Gate(string pinCode)
        {
            if (string.IsNullOrWhiteSpace(pinCode) || !pinCode.All(char.IsDigit))
            {
                TempData["ErrorMsg"] = "Vui lòng nhập mã số quản trị chỉ gồm chữ số.";
                return View();
            }

            var adminPin = _config["AppSettings:AdminPin"];
            if (string.IsNullOrWhiteSpace(adminPin))
            {
                TempData["ErrorMsg"] = "Hệ thống chưa cấu hình mã số truy cập.";
                return View();
            }

            if (!string.Equals(pinCode.Trim(), adminPin.Trim(), StringComparison.Ordinal))
            {
                TempData["ErrorMsg"] = "Mã số quản trị không đúng.";
                return View();
            }

            // PIN đúng → lưu session và chuyển sang bước 2
            HttpContext.Session.SetString(SESSION_PIN_VERIFIED, "1");
            return RedirectToAction(nameof(GateEmail));
        }

        // ════════════════════════════════════════════════════════
        // BƯỚC 2: NHẬP GMAIL ĐỂ NHẬN OTP
        // ════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GateEmail()
        {
            if (HttpContext.Session.GetString(SESSION_ADMIN_UNLOCKED) == "1" || HttpContext.Session.GetString("UserRole") == "Admin")
                return RedirectToAction(nameof(Index));

            // Phải qua bước 1 trước
            if (HttpContext.Session.GetString(SESSION_PIN_VERIFIED) != "1")
                return RedirectToAction(nameof(Gate));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> GateEmail(string email)
        {
            // Kiểm tra đã qua bước 1 chưa
            if (HttpContext.Session.GetString(SESSION_PIN_VERIFIED) != "1")
                return RedirectToAction(nameof(Gate));

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMsg"] = "Vui lòng nhập địa chỉ Gmail.";
                return View();
            }

            email = email.Trim().ToLowerInvariant();

            // Kiểm tra email có trong danh sách được cấp quyền không
            var adminEmail = await _db.AdminEmails
                .FirstOrDefaultAsync(e => e.Email.ToLower() == email && e.IsActive);

            var superAdminEmail = _config["AppSettings:SuperAdminEmail"]?.Trim().ToLowerInvariant();
            if (adminEmail == null && !string.IsNullOrEmpty(superAdminEmail) && email == superAdminEmail)
            {
                // Tự động tạo và lưu Super Admin vào DB nếu chưa tồn tại
                adminEmail = new AdminEmail
                {
                    Email = superAdminEmail,
                    FullName = "Super Admin",
                    IsActive = true,
                    AddedAt = DateTime.UtcNow
                };
                _db.AdminEmails.Add(adminEmail);
                await _db.SaveChangesAsync();
            }

            // Luôn hiện thông báo giống nhau để tránh lộ thông tin
            if (adminEmail == null)
            {
                TempData["ErrorMsg"] = "Email không hợp lệ hoặc chưa được cấp quyền truy cập. Vui lòng liên hệ Super Admin.";
                return View();
            }

            // Xóa các OTP cũ chưa dùng của email này
            var oldOtps = _db.AdminOtps.Where(o => o.Email == email && !o.IsUsed);
            _db.AdminOtps.RemoveRange(oldOtps);

            // Sinh OTP mới 6 số
            var otpCode = new Random().Next(100000, 999999).ToString();
            var otp = new AdminOtp
            {
                Email     = email,
                OtpCode   = otpCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed    = false,
                CreatedAt = DateTime.UtcNow
            };
            _db.AdminOtps.Add(otp);
            await _db.SaveChangesAsync();

            // Gửi OTP qua Gmail
            try
            {
                await _emailService.SendOtpAsync(email, adminEmail.FullName, otpCode);
            }
            catch (Exception ex)
            {
                TempData["ErrorMsg"] = $"Không thể gửi OTP. Lỗi: {ex.Message}. Vui lòng kiểm tra cấu hình Gmail SMTP.";
                return View();
            }

            // Lưu email đang chờ vào session và chuyển bước 3
            HttpContext.Session.SetString(SESSION_PENDING_EMAIL, email);
            TempData["SuccessMsg"] = $"Mã OTP đã được gửi đến {email}. Vui lòng kiểm tra hộp thư.";
            return RedirectToAction(nameof(GateOtp));
        }

        // ════════════════════════════════════════════════════════
        // BƯỚC 3: NHẬP MÃ OTP
        // ════════════════════════════════════════════════════════

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GateOtp()
        {
            if (HttpContext.Session.GetString(SESSION_ADMIN_UNLOCKED) == "1" || HttpContext.Session.GetString("UserRole") == "Admin")
                return RedirectToAction(nameof(Index));

            if (HttpContext.Session.GetString(SESSION_PIN_VERIFIED) != "1")
                return RedirectToAction(nameof(Gate));

            var pendingEmail = HttpContext.Session.GetString(SESSION_PENDING_EMAIL);
            if (string.IsNullOrEmpty(pendingEmail))
                return RedirectToAction(nameof(GateEmail));

            ViewBag.PendingEmail = pendingEmail;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> GateOtp(string otpCode)
        {
            if (HttpContext.Session.GetString(SESSION_PIN_VERIFIED) != "1")
                return RedirectToAction(nameof(Gate));

            var pendingEmail = HttpContext.Session.GetString(SESSION_PENDING_EMAIL);
            if (string.IsNullOrEmpty(pendingEmail))
                return RedirectToAction(nameof(GateEmail));

            ViewBag.PendingEmail = pendingEmail;

            if (string.IsNullOrWhiteSpace(otpCode) || otpCode.Length != 6 || !otpCode.All(char.IsDigit))
            {
                TempData["ErrorMsg"] = "Mã OTP phải gồm đúng 6 chữ số.";
                return View();
            }

            // Tìm OTP hợp lệ trong DB
            var otp = await _db.AdminOtps
                .Where(o => o.Email == pendingEmail && o.OtpCode == otpCode.Trim() && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                TempData["ErrorMsg"] = "Mã OTP không đúng. Vui lòng kiểm tra lại.";
                return View();
            }

            if (otp.IsExpired)
            {
                otp.IsUsed = true;
                await _db.SaveChangesAsync();
                TempData["ErrorMsg"] = "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.";
                HttpContext.Session.Remove(SESSION_PENDING_EMAIL);
                return RedirectToAction(nameof(GateEmail));
            }

            // OTP hợp lệ → đánh dấu đã dùng và mở khóa Admin
            otp.IsUsed = true;
            await _db.SaveChangesAsync();

            // Kiểm tra có phải Super Admin không
            var superAdminEmail = _config["AppSettings:SuperAdminEmail"]?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(superAdminEmail) && pendingEmail == superAdminEmail)
                HttpContext.Session.SetString("IsSuperAdmin", "1");

            // Xóa session tạm thời
            HttpContext.Session.Remove(SESSION_PIN_VERIFIED);
            HttpContext.Session.Remove(SESSION_PENDING_EMAIL);

            // Mở khóa Admin
            HttpContext.Session.SetString(SESSION_ADMIN_UNLOCKED, "1");
            TempData["SuccessMsg"] = "Xác thực quản trị thành công. Chào mừng!";
            return RedirectToAction(nameof(Index));
        }

        // ════════════════════════════════════════════════════════
        // QUẢN LÝ TÀI KHOẢN ADMIN (chỉ Super Admin)
        // ════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> AdminAccounts()
        {
            if (HttpContext.Session.GetString("IsSuperAdmin") != "1")
            {
                TempData["ErrorMsg"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction(nameof(Index));
            }

            var adminEmails = await _db.AdminEmails
                .OrderByDescending(e => e.AddedAt)
                .ToListAsync();

            return View(adminEmails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantAdmin(string email, string fullName)
        {
            if (HttpContext.Session.GetString("IsSuperAdmin") != "1")
                return Forbid();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
            {
                TempData["ErrorMsg"] = "Vui lòng nhập đầy đủ Email và Họ tên.";
                return RedirectToAction(nameof(AdminAccounts));
            }

            email = email.Trim().ToLowerInvariant();
            fullName = fullName.Trim();

            // Kiểm tra email đã tồn tại chưa
            var existing = await _db.AdminEmails.FirstOrDefaultAsync(e => e.Email == email);
            if (existing != null)
            {
                if (existing.IsActive)
                {
                    TempData["ErrorMsg"] = $"Email '{email}' đã được cấp quyền Admin.";
                }
                else
                {
                    // Kích hoạt lại
                    existing.IsActive = true;
                    existing.FullName = fullName;
                    await _db.SaveChangesAsync();
                    TempData["SuccessMsg"] = $"Đã kích hoạt lại quyền Admin cho '{email}'.";
                }
                return RedirectToAction(nameof(AdminAccounts));
            }

            var adminEmail = new AdminEmail
            {
                Email     = email,
                FullName  = fullName,
                IsActive  = true,
                AddedAt   = DateTime.UtcNow
            };
            _db.AdminEmails.Add(adminEmail);
            await _db.SaveChangesAsync();

            TempData["SuccessMsg"] = $"Đã cấp quyền Admin cho '{email}' ({fullName}) thành công!";
            return RedirectToAction(nameof(AdminAccounts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAdmin(int id)
        {
            if (HttpContext.Session.GetString("IsSuperAdmin") != "1")
                return Forbid();

            var adminEmail = await _db.AdminEmails.FindAsync(id);
            if (adminEmail == null) return NotFound();

            // Không cho phép thu hồi quyền của Super Admin
            var superAdminEmail = _config["AppSettings:SuperAdminEmail"]?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(superAdminEmail) && adminEmail.Email == superAdminEmail)
            {
                TempData["ErrorMsg"] = "Không thể thu hồi quyền của Super Admin.";
                return RedirectToAction(nameof(AdminAccounts));
            }

            adminEmail.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["SuccessMsg"] = $"Đã thu hồi quyền Admin của '{adminEmail.Email}'.";
            return RedirectToAction(nameof(AdminAccounts));
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
                .ThenBy(s => s.EndTime - s.StartTime)
                .Take(5)
                .Select(s => new
                {
                    FullName = s.User != null ? s.User.FullName : "Người chơi vô danh",
                    s.TotalScore,
                    Date = s.StartTime
                })
                .ToListAsync();

            ViewBag.TopPlayers = topPlayers;

            // Thống kê số lượng câu hỏi theo chuyên mục di sản (LocationName)
            var questionStats = await _db.Questions
                .GroupBy(q => q.LocationName)
                .Select(g => new
                {
                    Category = g.Key ?? "Chưa phân loại",
                    Count = g.Count()
                })
                .ToListAsync();

            ViewBag.QuestionStats = questionStats;

            return View();
        }

        public async Task<IActionResult> Questions(string? search)
        {
            var query = _db.Questions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchLower = search.Trim().ToLower();
                query = query.Where(q => q.Content.ToLower().Contains(searchLower) || (q.LocationName != null && q.LocationName.ToLower().Contains(searchLower)));
                ViewData["SearchString"] = search;
            }

            var questions = await query
                .OrderByDescending(q => q.Id)
                .ToListAsync();
            return View(questions);
        }

        public async Task<IActionResult> Users(string? search)
        {
            var query = _db.Users.Include(u => u.QuizSessions).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchLower = search.Trim().ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(searchLower) || u.FullName.ToLower().Contains(searchLower));
                ViewData["SearchString"] = search;
            }

            var users = await query
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
        public async Task<IActionResult> CreateQuestion(Bảo_Tàng_Đà_Nẵng.Models.Question model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return View("QuestionForm", model);

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "questions");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                model.ImageUrl = "/images/questions/" + uniqueFileName;
            }

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
        public async Task<IActionResult> EditQuestion(int id, Bảo_Tàng_Đà_Nẵng.Models.Question model, IFormFile? imageFile, bool RemoveImage = false)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
                return View("QuestionForm", model);

            var existing = await _db.Questions.FindAsync(id);
            if (existing == null) return NotFound();

            if (RemoveImage)
            {
                existing.ImageUrl = null;
            }
            else if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "questions");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                existing.ImageUrl = "/images/questions/" + uniqueFileName;
            }

            existing.Content = model.Content;
            existing.OptionA = model.OptionA;
            existing.OptionB = model.OptionB;
            existing.OptionC = model.OptionC;
            existing.OptionD = model.OptionD;
            existing.CorrectOption = model.CorrectOption;
            existing.Points = model.Points;
            existing.LocationName = model.LocationName;

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

            // Không cho phép khóa Admin khác
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var currentUserIdStr = HttpContext.Session.GetInt32("UserId");
            if (currentUserIdStr.HasValue && currentUserIdStr.Value == id)
            {
                TempData["ErrorMsg"] = "Bạn không thể tự xóa tài khoản của mình!";
                return RedirectToAction(nameof(Users));
            }

            if (user.Role == "Admin")
            {
                TempData["ErrorMsg"] = "Không thể xóa tài khoản Quản trị viên!";
                return RedirectToAction(nameof(Users));
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            TempData["SuccessMsg"] = "Đã xóa tài khoản thành công.";
            return RedirectToAction(nameof(Users));
        }

        // ════════════════════════════════════════════════════════
        // ĐỒNG BỘ DỮ LIỆU TỪ FILE JSON (quiz_data.json)
        // ════════════════════════════════════════════════════════

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> ImportJsonData()
        {
            string jsonFilePath = @"C:\Users\ADMIN\Downloads\quiz_data.json";
            if (!System.IO.File.Exists(jsonFilePath))
            {
                TempData["ErrorMsg"] = $"Không tìm thấy file dữ liệu tại đường dẫn: {jsonFilePath}";
                return RedirectToAction(nameof(Questions));
            }

            try
            {
                string jsonContent = await System.IO.File.ReadAllTextAsync(jsonFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var root = JsonSerializer.Deserialize<QuizJsonRoot>(jsonContent, options);
                if (root == null)
                {
                    TempData["ErrorMsg"] = "Phân tích nội dung file JSON thất bại.";
                    return RedirectToAction(nameof(Questions));
                }

                int importedCount = 0;

                var strategy = _db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();
                    try
                    {
                        _db.SessionDetails.RemoveRange(_db.SessionDetails);
                        _db.QuizSessions.RemoveRange(_db.QuizSessions);
                        _db.Questions.RemoveRange(_db.Questions);
                        await _db.SaveChangesAsync();

                        await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('dbo.Questions', RESEED, 0);");

                        void InsertCategoryQuestions(List<QuizJsonItem>? items, string locationName)
                        {
                            if (items == null || items.Count == 0) return;

                            foreach (var item in items)
                            {
                                if (string.IsNullOrWhiteSpace(item.Question)) continue;

                                string optA = "A", optB = "B", optC = "C", optD = "D";
                                if (item.Answers != null && item.Answers.Count >= 4)
                                {
                                    optA = item.Answers[0].ToString() ?? "A";
                                    optB = item.Answers[1].ToString() ?? "B";
                                    optC = item.Answers[2].ToString() ?? "C";
                                    optD = item.Answers[3].ToString() ?? "D";
                                }

                                string correctOpt = "A";
                                if (item.CorrectAnswer == 0) correctOpt = "A";
                                else if (item.CorrectAnswer == 1) correctOpt = "B";
                                else if (item.CorrectAnswer == 2) correctOpt = "C";
                                else if (item.CorrectAnswer == 3) correctOpt = "D";

                                var question = new Bảo_Tàng_Đà_Nẵng.Models.Question
                                {
                                    Content = item.Question,
                                    OptionA = optA,
                                    OptionB = optB,
                                    OptionC = optC,
                                    OptionD = optD,
                                    CorrectOption = correctOpt,
                                    Points = 10,
                                    LocationName = locationName,
                                    IsActive = true
                                };

                                _db.Questions.Add(question);
                                importedCount++;
                            }
                        }

                        InsertCategoryQuestions(root.News, "Tin tức/Sự kiện");
                        InsertCategoryQuestions(root.BcArtifacts, "Bảo tàng làng nghề");
                        InsertCategoryQuestions(root.Artisans, "Nghệ nhân");
                        InsertCategoryQuestions(root.Clubs, "Câu lạc bộ");
                        InsertCategoryQuestions(root.General, "Tổng hợp");

                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                TempData["SuccessMsg"] = $"Đồng bộ thành công {importedCount} câu hỏi từ file JSON vào CSDL!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMsg"] = $"Lỗi khi đồng bộ dữ liệu: {ex.Message}";
            }

            return RedirectToAction(nameof(Questions));
        }
    }

    // ════════════════════════════════════════════════════════
    // CÁC LỚP HỖ TRỢ PHÂN TÍCH DỮ LIỆU JSON
    // ════════════════════════════════════════════════════════

    public class QuizJsonRoot
    {
        [JsonPropertyName("artifacts")]
        public List<QuizJsonItem>? Artifacts { get; set; }

        [JsonPropertyName("news")]
        public List<QuizJsonItem>? News { get; set; }

        [JsonPropertyName("bc_artifacts")]
        public List<QuizJsonItem>? BcArtifacts { get; set; }

        [JsonPropertyName("artisans")]
        public List<QuizJsonItem>? Artisans { get; set; }

        [JsonPropertyName("clubs")]
        public List<QuizJsonItem>? Clubs { get; set; }

        [JsonPropertyName("general")]
        public List<QuizJsonItem>? General { get; set; }
    }

    public class QuizJsonItem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("question")]
        public string? Question { get; set; }

        [JsonPropertyName("answers")]
        public List<JsonElement>? Answers { get; set; }

        [JsonPropertyName("correct_answer")]
        public int CorrectAnswer { get; set; }
    }
}
