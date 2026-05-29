-- ══════════════════════════════════════════════════════════════
-- Migration: Tạo bảng AdminEmails và AdminOtps
-- Mô tả: Hỗ trợ hệ thống xác thực Admin 3 bước (PIN + Gmail + OTP)
-- Ngày: 2026-05-29
-- ══════════════════════════════════════════════════════════════

-- Bảng 1: AdminEmails
-- Lưu danh sách Gmail được Super Admin cấp quyền truy cập Admin
CREATE TABLE IF NOT EXISTS "AdminEmails" (
    "Id"        SERIAL          PRIMARY KEY,
    "Email"     VARCHAR(255)    NOT NULL,
    "FullName"  VARCHAR(200)    NOT NULL,
    "AddedAt"   TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsActive"  BOOLEAN         NOT NULL DEFAULT TRUE,

    CONSTRAINT "UQ_AdminEmails_Email" UNIQUE ("Email")
);

-- Index hỗ trợ tìm kiếm nhanh theo Email
CREATE INDEX IF NOT EXISTS "IX_AdminEmails_Email"
    ON "AdminEmails" ("Email");

-- Bảng 2: AdminOtps
-- Lưu mã OTP tạm thời (TTL 5 phút, chỉ dùng một lần)
CREATE TABLE IF NOT EXISTS "AdminOtps" (
    "Id"        SERIAL          PRIMARY KEY,
    "Email"     VARCHAR(255)    NOT NULL,
    "OtpCode"   CHAR(6)         NOT NULL,
    "ExpiresAt" TIMESTAMP       NOT NULL,
    "IsUsed"    BOOLEAN         NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Index hỗ trợ tìm kiếm nhanh OTP theo Email
CREATE INDEX IF NOT EXISTS "IX_AdminOtps_Email"
    ON "AdminOtps" ("Email");

-- ── Dọn dẹp OTP cũ định kỳ (tuỳ chọn) ─────────────────────────
-- Có thể chạy lệnh này thủ công hoặc đặt lịch để xoá OTP hết hạn
-- DELETE FROM "AdminOtps" WHERE "ExpiresAt" < NOW() - INTERVAL '1 day';

-- ── Kiểm tra ────────────────────────────────────────────────────
SELECT 'AdminEmails table created' AS status WHERE EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_name = 'AdminEmails'
);
SELECT 'AdminOtps table created' AS status WHERE EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_name = 'AdminOtps'
);
