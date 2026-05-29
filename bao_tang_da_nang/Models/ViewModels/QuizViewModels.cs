using System.ComponentModel.DataAnnotations;

namespace Bảo_Tàng_Đà_Nẵng.Models.ViewModels
{
    // ════════════════════════════════════════════════════════════
    // Auth ViewModels
    // ════════════════════════════════════════════════════════════

    /// <summary>ViewModel hiển thị tóm tắt thông tin một bộ đề.</summary>
    public class CategorySummaryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>ViewModel cho form Đăng nhập.</summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [MaxLength(100)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        /// <summary>URL để redirect sau khi đăng nhập thành công (phòng khi bị chặn bởi [Authorize]).</summary>
        public string? ReturnUrl { get; set; }
    }

    /// <summary>ViewModel cho form Đăng ký tài khoản mới.</summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [MaxLength(100)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [MaxLength(200)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel cho form Đổi mật khẩu.
    /// Sau khi đổi thành công, luồng sẽ redirect về Login (theo spec).
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    // ════════════════════════════════════════════════════════════
    // Quiz ViewModels
    // ════════════════════════════════════════════════════════════

    /// <summary>DTO gửi về client khi render 1 câu hỏi trên màn hình quiz.</summary>
    public class QuestionViewModel
    {
        public int QuestionId { get; set; }
        public int SessionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ContentEn { get; set; }
        public string OptionA { get; set; } = string.Empty;
        public string? OptionAEn { get; set; }
        public string OptionB { get; set; } = string.Empty;
        public string? OptionBEn { get; set; }
        public string OptionC { get; set; } = string.Empty;
        public string? OptionCEn { get; set; }
        public string OptionD { get; set; } = string.Empty;
        public string? OptionDEn { get; set; }
        public string? ImageUrl { get; set; }
        public string? LocationName { get; set; }
        public int Points { get; set; }

        /// <summary>Số thứ tự câu hiện tại (VD: 3) để hiển thị "Câu 3/10".</summary>
        public int CurrentIndex { get; set; }

        /// <summary>Tổng số câu trong phiên chơi.</summary>
        public int TotalQuestions { get; set; }

        /// <summary>Điểm hiện tại của người chơi trước câu này.</summary>
        public int CurrentScore { get; set; }
    }

    /// <summary>Payload JSON gửi lên khi người dùng chọn đáp án.</summary>
    public class SubmitAnswerRequest
    {
        [Required]
        public int SessionId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        /// <summary>Đáp án đã chọn: "A", "B", "C", hoặc "D".</summary>
        [Required]
        [RegularExpression("^[ABCD]$")]
        public string SelectedOption { get; set; } = string.Empty;
    }

    /// <summary>Phản hồi JSON sau khi chấm đáp án.</summary>
    public class SubmitAnswerResponse
    {
        public bool IsCorrect { get; set; }
        public string CorrectOption { get; set; } = string.Empty;
        public int PointsEarned { get; set; }
        public int NewTotalScore { get; set; }

        /// <summary>True nếu đây là câu hỏi cuối cùng → client redirect sang màn kết quả.</summary>
        public bool IsLastQuestion { get; set; }
    }

    /// <summary>ViewModel cho màn nhập tên trước khi vào chủ đề.</summary>
    public class StartSessionViewModel
    {
        [Required]
        public string Topic { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên của bạn.")]
        [MaxLength(200)]
        [Display(Name = "Tên người chơi")]
        public string PlayerName { get; set; } = string.Empty;
    }

    /// <summary>Màn hình Kết quả sau khi nộp bài.</summary>
    public class QuizResultViewModel
    {
        public int SessionId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers => TotalQuestions - CorrectAnswers;

        /// <summary>Phần trăm đúng (0-100).</summary>
        public double AccuracyRate => TotalQuestions == 0 ? 0 : Math.Round((double)CorrectAnswers / TotalQuestions * 100, 1);

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        /// <summary>Thời gian hoàn thành (phút:giây).</summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>Danh sách chi tiết từng câu để hiển thị bảng review.</summary>
        public List<AnswerReviewItem> Answers { get; set; } = new();
    }

    /// <summary>Chi tiết 1 câu hỏi trong màn hình review kết quả.</summary>
    public class AnswerReviewItem
    {
        public string QuestionContent { get; set; } = string.Empty;
        public string? QuestionContentEn { get; set; }
        public string SelectedOption { get; set; } = string.Empty;
        public string? SelectedOptionTextEn { get; set; }
        public string CorrectOption { get; set; } = string.Empty;
        public string? CorrectOptionTextEn { get; set; }
        public bool IsCorrect { get; set; }
        public int Points { get; set; }
        public string? LocationName { get; set; }
    }
}
