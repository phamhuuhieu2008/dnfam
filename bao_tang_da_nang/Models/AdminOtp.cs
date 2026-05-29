using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bảo_Tàng_Đà_Nẵng.Models
{
    /// <summary>
    /// Lưu trữ mã OTP tạm thời cho quá trình xác thực Admin.
    /// Mỗi OTP có thời hạn sử dụng (ExpiresAt) và chỉ dùng một lần.
    /// </summary>
    [Table("AdminOtps")]
    public class AdminOtp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Gmail mà OTP được gửi đến.</summary>
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Mã OTP 6 chữ số.</summary>
        [Required]
        [MaxLength(6)]
        public string OtpCode { get; set; } = string.Empty;

        /// <summary>Thời điểm OTP hết hạn (UTC). Mặc định 5 phút sau khi tạo.</summary>
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5);

        /// <summary>true = OTP đã được sử dụng, không hợp lệ nữa.</summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>Thời điểm tạo OTP.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ─── Helper properties ───────────────────────────────────
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        [NotMapped]
        public bool IsValid => !IsUsed && !IsExpired;
    }
}
