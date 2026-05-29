using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bảo_Tàng_Đà_Nẵng.Models
{
    /// <summary>
    /// Entity đại diện cho bảng Questions — Ngân hàng câu hỏi về di sản Đà Nẵng.
    /// </summary>
    [Table("Questions")]
    public class Question
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Nội dung câu hỏi (tối đa 1000 ký tự).</summary>
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ContentEn { get; set; }

        [Required]
        [MaxLength(500)]
        public string OptionA { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? OptionAEn { get; set; }

        [Required]
        [MaxLength(500)]
        public string OptionB { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? OptionBEn { get; set; }

        [Required]
        [MaxLength(500)]
        public string OptionC { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? OptionCEn { get; set; }

        [Required]
        [MaxLength(500)]
        public string OptionD { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? OptionDEn { get; set; }

        /// <summary>
        /// Đáp án đúng: phải là "A", "B", "C", hoặc "D".
        /// Được validate thêm ở tầng DB bởi CK_Questions_CorrectOption.
        /// </summary>
        [Required]
        [MaxLength(1)]
        [RegularExpression("^[ABCD]$", ErrorMessage = "Đáp án đúng phải là A, B, C hoặc D.")]
        public string CorrectOption { get; set; } = "A";

        /// <summary>Điểm thưởng khi trả lời đúng câu này. Phải > 0.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Điểm phải lớn hơn 0.")]
        public int Points { get; set; } = 10;

        /// <summary>Tên địa điểm di sản liên quan (VD: "Thành Điện Hải").</summary>
        [MaxLength(200)]
        public string? LocationName { get; set; }

        /// <summary>URL ảnh minh hoạ cho câu hỏi (tuỳ chọn).</summary>
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>Ẩn/hiện câu hỏi mà không cần xoá khỏi CSDL.</summary>
        public bool IsActive { get; set; } = true;

        // ─── Navigation Properties ───────────────────────────────────
        public virtual ICollection<SessionDetail> SessionDetails { get; set; } = new List<SessionDetail>();
    }
}
