using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bảo_Tàng_Đà_Nẵng.Models
{
    /// <summary>
    /// Danh sách Gmail được Super Admin cấp quyền truy cập khu vực Admin.
    /// Chỉ những email có trong bảng này mới được gửi OTP.
    /// </summary>
    [Table("AdminEmails")]
    public class AdminEmail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Địa chỉ Gmail được cấp quyền (phải là @gmail.com).</summary>
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của người được cấp quyền.</summary>
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>Thời điểm Super Admin cấp quyền.</summary>
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        /// <summary>true = còn hoạt động; false = đã thu hồi quyền.</summary>
        public bool IsActive { get; set; } = true;
    }
}
