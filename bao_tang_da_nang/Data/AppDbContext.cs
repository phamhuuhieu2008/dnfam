using Bảo_Tàng_Đà_Nẵng.Models;
using Microsoft.EntityFrameworkCore;

namespace Bảo_Tàng_Đà_Nẵng.Data
{
    /// <summary>
    /// AppDbContext — DbContext chính của ứng dụng Giải Mã Di Sản.
    /// Cấu hình tất cả Entity, Constraint và Relationship qua Fluent API.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ─── DbSets ──────────────────────────────────────────────────
        public DbSet<User> Users { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuizSession> QuizSessions { get; set; }
        public DbSet<SessionDetail> SessionDetails { get; set; }
        public DbSet<AdminEmail> AdminEmails { get; set; }
        public DbSet<AdminOtp> AdminOtps { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ════════════════════════════════════════════════════════
            // ENTITY: User
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);

                // Unique constraint: tên đăng nhập phải là duy nhất
                entity.HasIndex(u => u.Username)
                      .IsUnique()
                      .HasDatabaseName("UQ_Users_Username");

                // Check constraint: Role chỉ nhận "Admin" hoặc "Player"
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Users_Role",
                    "\"Role\" IN ('Admin', 'Player')"
                ));

                entity.Property(u => u.Username).HasMaxLength(100).IsRequired();
                entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
                entity.Property(u => u.Role).HasMaxLength(20).IsRequired().HasDefaultValue("Player");
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(u => u.IsActive).HasDefaultValue(true);
            });

            // ════════════════════════════════════════════════════════
            // ENTITY: Question
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("Questions");
                entity.HasKey(q => q.Id);

                // Check constraint: CorrectOption chỉ nhận A, B, C, D
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Questions_CorrectOption",
                    "\"CorrectOption\" IN ('A', 'B', 'C', 'D')"
                ));

                // Check constraint: Points phải > 0
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Questions_Points",
                    "\"Points\" > 0"
                ));

                entity.Property(q => q.Content).HasMaxLength(1000).IsRequired();
                entity.Property(q => q.ContentEn).HasMaxLength(1000);
                entity.Property(q => q.OptionA).HasMaxLength(500).IsRequired();
                entity.Property(q => q.OptionAEn).HasMaxLength(500);
                entity.Property(q => q.OptionB).HasMaxLength(500).IsRequired();
                entity.Property(q => q.OptionBEn).HasMaxLength(500);
                entity.Property(q => q.OptionC).HasMaxLength(500).IsRequired();
                entity.Property(q => q.OptionCEn).HasMaxLength(500);
                entity.Property(q => q.OptionD).HasMaxLength(500).IsRequired();
                entity.Property(q => q.OptionDEn).HasMaxLength(500);
                entity.Property(q => q.CorrectOption).HasMaxLength(1).IsFixedLength().IsRequired();
                entity.Property(q => q.Points).HasDefaultValue(10);
                entity.Property(q => q.LocationName).HasMaxLength(200);
                entity.Property(q => q.ImageUrl).HasMaxLength(500);
                entity.Property(q => q.IsActive).HasDefaultValue(true);
            });

            // ════════════════════════════════════════════════════════
            // ENTITY: QuizSession
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<QuizSession>(entity =>
            {
                entity.ToTable("QuizSessions");
                entity.HasKey(s => s.Id);

                entity.Property(s => s.StartTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(s => s.TotalScore).HasDefaultValue(0);
                entity.Property(s => s.IsCompleted).HasDefaultValue(false);

                // Relationship: QuizSession → User (N:1)
                entity.HasOne(s => s.User)
                      .WithMany(u => u.QuizSessions)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade)  // Xoá user → xoá toàn bộ session
                      .HasConstraintName("FK_QuizSessions_Users");

                // Index để tìm nhanh session theo UserId
                entity.HasIndex(s => new { s.UserId, s.StartTime })
                      .HasDatabaseName("IX_QuizSessions_UserId");
            });

            // ════════════════════════════════════════════════════════
            // ENTITY: SessionDetail
            //
            // ★ RÀNG BUỘC CỐT LÕI ★
            // Composite Unique Index (SessionId, QuestionId):
            // Mỗi câu hỏi chỉ được xuất hiện ĐÚNG MỘT LẦN trong một phiên.
            // Phản ánh đúng constraint UQ_SessionDetails_Session_Question ở DB.
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<SessionDetail>(entity =>
            {
                entity.ToTable("SessionDetails");
                entity.HasKey(d => d.Id);

                // ★ Composite Unique Index — đây là ràng buộc quan trọng nhất ★
                entity.HasIndex(d => new { d.SessionId, d.QuestionId })
                      .IsUnique()
                      .HasDatabaseName("UQ_SessionDetails_Session_Question");

                // Check constraint: SelectedOption hợp lệ
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_SessionDetails_SelectedOption",
                    "\"SelectedOption\" IS NULL OR \"SelectedOption\" IN ('A', 'B', 'C', 'D')"
                ));

                entity.Property(d => d.SelectedOption).HasMaxLength(1).IsFixedLength();
                entity.Property(d => d.IsCorrect).HasDefaultValue(false);
                entity.Property(d => d.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relationship: SessionDetail → QuizSession (N:1)
                entity.HasOne(d => d.QuizSession)
                      .WithMany(s => s.SessionDetails)
                      .HasForeignKey(d => d.SessionId)
                      .OnDelete(DeleteBehavior.Cascade)   // Xoá session → xoá detail
                      .HasConstraintName("FK_SessionDetails_QuizSessions");

                // Relationship: SessionDetail → Question (N:1)
                // NO ACTION: Không cho xoá Question đã có detail
                entity.HasOne(d => d.Question)
                      .WithMany(q => q.SessionDetails)
                      .HasForeignKey(d => d.QuestionId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .HasConstraintName("FK_SessionDetails_Questions");
            });

            // ════════════════════════════════════════════════════════
            // ENTITY: AdminEmail
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<AdminEmail>(entity =>
            {
                entity.ToTable("AdminEmails");
                entity.HasKey(e => e.Id);

                // Unique constraint: mỗi email chỉ xuất hiện một lần
                entity.HasIndex(e => e.Email)
                      .IsUnique()
                      .HasDatabaseName("UQ_AdminEmails_Email");

                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.AddedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // ════════════════════════════════════════════════════════
            // ENTITY: AdminOtp
            // ════════════════════════════════════════════════════════
            modelBuilder.Entity<AdminOtp>(entity =>
            {
                entity.ToTable("AdminOtps");
                entity.HasKey(o => o.Id);

                // Index để tra cứu nhanh OTP theo email
                entity.HasIndex(o => o.Email)
                      .HasDatabaseName("IX_AdminOtps_Email");

                entity.Property(o => o.Email).HasMaxLength(255).IsRequired();
                entity.Property(o => o.OtpCode).HasMaxLength(6).IsRequired();
                entity.Property(o => o.IsUsed).HasDefaultValue(false);
                entity.Property(o => o.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
