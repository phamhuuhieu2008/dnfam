using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bảo_Tàng_Đà_Nẵng.Models
{
    /// <summary>
    /// Entity đại diện cho bảng Users trong CSDL.
    /// Lưu thông tin tài khoản người chơi và quản trị viên.
    /// </summary>
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Tên đăng nhập — bắt buộc, duy nhất (do Unique Constraint DB).</summary>
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        /// <summary>Mật khẩu đã mã hoá bằng BCrypt. KHÔNG lưu plain-text.</summary>
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Họ tên hiển thị trên bảng xếp hạng và giao diện.</summary>
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Vai trò: "Admin" hoặc "Player".</summary>
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Player";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Soft-delete flag. Không xoá thật khỏi DB, chỉ đánh dấu ẩn.</summary>
        public bool IsActive { get; set; } = true;

        // ─── Navigation Properties ───────────────────────────────────
        /// <summary>Danh sách các phiên chơi của người dùng này.</summary>
        public virtual ICollection<QuizSession> QuizSessions { get; set; } = new List<QuizSession>();
    }
}
