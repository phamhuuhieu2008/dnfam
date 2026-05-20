using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bảo_Tàng_Đà_Nẵng.Models
{
    /// <summary>
    /// Entity đại diện cho bảng QuizSessions.
    /// Mỗi lần người dùng bấm "Bắt đầu chơi" = tạo 1 phiên (session) mới.
    /// </summary>
    [Table("QuizSessions")]
    public class QuizSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>FK tới Users.Id — ai đang chơi phiên này.</summary>
        [Required]
        public int UserId { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>Null khi phiên chưa kết thúc; được gán khi gọi FinishQuiz.</summary>
        public DateTime? EndTime { get; set; }

        /// <summary>Tổng điểm tích luỹ sau mỗi lần SubmitAnswer đúng.</summary>
        public int TotalScore { get; set; } = 0;

        /// <summary>
        /// False = phiên đang diễn ra.
        /// True  = người dùng đã bấm "Nộp bài" hoặc hết thời gian.
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        // ─── Navigation Properties ───────────────────────────────────

        /// <summary>Người dùng sở hữu phiên này.</summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        /// <summary>Danh sách chi tiết từng câu trả lời trong phiên.</summary>
        public virtual ICollection<SessionDetail> SessionDetails { get; set; } = new List<SessionDetail>();
    }
}
