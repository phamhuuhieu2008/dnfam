using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bảo_Tàng_Đà_Nẵng.Models
{
    /// <summary>
    /// Entity đại diện cho bảng SessionDetails.
    /// Lưu chi tiết từng câu trả lời trong một phiên chơi.
    ///
    /// ★ Ràng buộc quan trọng nhất: Composite Unique (SessionId, QuestionId)
    ///   được khai báo trong AppDbContext.OnModelCreating() và phản ánh
    ///   constraint UQ_SessionDetails_Session_Question ở tầng Database.
    /// </summary>
    [Table("SessionDetails")]
    public class SessionDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>FK tới QuizSessions.Id — thuộc phiên chơi nào.</summary>
        [Required]
        public int SessionId { get; set; }

        /// <summary>FK tới Questions.Id — câu hỏi nào trong phiên này.</summary>
        [Required]
        public int QuestionId { get; set; }

        /// <summary>
        /// Đáp án người dùng chọn: "A", "B", "C", "D" hoặc null (chưa trả lời).
        /// Null xảy ra khi hết giờ mà người dùng chưa chọn đáp án.
        /// </summary>
        [MaxLength(1)]
        public string? SelectedOption { get; set; }

        /// <summary>Kết quả chấm: true nếu SelectedOption == Question.CorrectOption.</summary>
        public bool IsCorrect { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ─── Navigation Properties ───────────────────────────────────

        /// <summary>Phiên chơi chứa câu trả lời này.</summary>
        [ForeignKey(nameof(SessionId))]
        public virtual QuizSession? QuizSession { get; set; }

        /// <summary>Câu hỏi tương ứng.</summary>
        [ForeignKey(nameof(QuestionId))]
        public virtual Question? Question { get; set; }
    }
}
