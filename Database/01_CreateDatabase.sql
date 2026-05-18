-- ============================================================
-- HỆ THỐNG: Giải Mã Di Sản - Bảo Tàng Mỹ Thuật Đà Nẵng
-- Script:    01_CreateDatabase.sql
-- Mục đích:  Tạo CSDL, các bảng, khóa ngoại, ràng buộc
-- Tác giả:   Senior Full-stack Architect
-- Phiên bản: 1.0
-- ============================================================

-- ────────────────────────────────────────────────────────────
-- SECTION 0: Tạo Database (chạy với quyền sysadmin)
-- ────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GiaiMaDiSan')
BEGIN
    CREATE DATABASE GiaiMaDiSan
    COLLATE Vietnamese_CI_AS;  -- Hỗ trợ tiếng Việt
    PRINT '✓ Database GiaiMaDiSan đã được tạo.';
END
GO

USE GiaiMaDiSan;
GO

-- ────────────────────────────────────────────────────────────
-- SECTION 1: Bảng Users
-- Lưu thông tin tài khoản người chơi và quản trị viên
-- ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users (
    Id           INT             NOT NULL IDENTITY(1,1),   -- Khóa chính tự tăng
    Username     NVARCHAR(100)   NOT NULL,                 -- Tên đăng nhập (duy nhất)
    PasswordHash NVARCHAR(255)   NOT NULL,                 -- Mật khẩu đã băm (BCrypt)
    FullName     NVARCHAR(200)   NOT NULL,                 -- Họ và tên hiển thị
    Role         NVARCHAR(20)    NOT NULL                  -- 'Admin' hoặc 'Player'
                 CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin', 'Player')),
    CreatedAt    DATETIME2(7)    NOT NULL DEFAULT GETUTCDATE(),
    IsActive     BIT             NOT NULL DEFAULT 1,       -- Soft delete flag

    -- Khóa chính
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id ASC),

    -- Đảm bảo Username là duy nhất trên toàn hệ thống
    CONSTRAINT UQ_Users_Username UNIQUE (Username)
);
GO

-- Index để tăng tốc truy vấn đăng nhập theo username
CREATE NONCLUSTERED INDEX IX_Users_Username
ON dbo.Users (Username ASC)
INCLUDE (PasswordHash, Role, IsActive);
GO

PRINT '✓ Bảng Users đã được tạo.';
GO


-- ────────────────────────────────────────────────────────────
-- SECTION 2: Bảng Questions
-- Ngân hàng câu hỏi cho các địa điểm di sản
-- ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Questions', 'U') IS NOT NULL DROP TABLE dbo.Questions;
GO

CREATE TABLE dbo.Questions (
    Id            INT             NOT NULL IDENTITY(1,1),  -- Khóa chính tự tăng
    Content       NVARCHAR(1000)  NOT NULL,                -- Nội dung câu hỏi
    OptionA       NVARCHAR(500)   NOT NULL,                -- Đáp án A
    OptionB       NVARCHAR(500)   NOT NULL,                -- Đáp án B
    OptionC       NVARCHAR(500)   NOT NULL,                -- Đáp án C
    OptionD       NVARCHAR(500)   NOT NULL,                -- Đáp án D
    CorrectOption CHAR(1)         NOT NULL                 -- Đáp án đúng: 'A','B','C','D'
                  CONSTRAINT CK_Questions_CorrectOption CHECK (CorrectOption IN ('A','B','C','D')),
    Points        INT             NOT NULL DEFAULT 10      -- Điểm thưởng cho câu đúng
                  CONSTRAINT CK_Questions_Points CHECK (Points > 0),
    LocationName  NVARCHAR(200)   NULL,                   -- Tên địa điểm di sản liên quan
    ImageUrl      NVARCHAR(500)   NULL,                   -- URL ảnh minh hoạ (tuỳ chọn)
    IsActive      BIT             NOT NULL DEFAULT 1,      -- Ẩn/hiện câu hỏi

    -- Khóa chính
    CONSTRAINT PK_Questions PRIMARY KEY CLUSTERED (Id ASC)
);
GO

PRINT '✓ Bảng Questions đã được tạo.';
GO


-- ────────────────────────────────────────────────────────────
-- SECTION 3: Bảng QuizSessions
-- Mỗi lần người dùng bắt đầu chơi = 1 phiên (session)
-- ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.QuizSessions', 'U') IS NOT NULL DROP TABLE dbo.QuizSessions;
GO

CREATE TABLE dbo.QuizSessions (
    Id          INT          NOT NULL IDENTITY(1,1),  -- Khóa chính tự tăng
    UserId      INT          NOT NULL,                -- FK → Users.Id
    StartTime   DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    EndTime     DATETIME2(7) NULL,                   -- NULL khi chưa kết thúc
    TotalScore  INT          NOT NULL DEFAULT 0,      -- Tổng điểm tích luỹ
    IsCompleted BIT          NOT NULL DEFAULT 0,      -- 0 = đang chơi, 1 = đã nộp

    -- Khóa chính
    CONSTRAINT PK_QuizSessions PRIMARY KEY CLUSTERED (Id ASC),

    -- Khóa ngoại tới Users
    CONSTRAINT FK_QuizSessions_Users
        FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
        ON DELETE CASCADE   -- Xoá user → xoá toàn bộ session của user đó
        ON UPDATE CASCADE
);
GO

-- Index để truy vấn lịch sử theo UserId
CREATE NONCLUSTERED INDEX IX_QuizSessions_UserId
ON dbo.QuizSessions (UserId ASC, StartTime DESC);
GO

PRINT '✓ Bảng QuizSessions đã được tạo.';
GO


-- ────────────────────────────────────────────────────────────
-- SECTION 4: Bảng SessionDetails
-- Chi tiết từng câu trả lời trong một phiên chơi
--
-- ★ RÀNG BUỘC QUAN TRỌNG NHẤT ★
-- Composite Unique Key (SessionId, QuestionId):
--   Đảm bảo tuyệt đối rằng trong 1 phiên, mỗi câu hỏi
--   chỉ được lưu đúng 1 lần — tương tự ràng buộc của
--   bill-detail trong hệ thống hóa đơn thương mại.
--   Ngăn chặn hoàn toàn lỗi duplicate do double-click
--   hoặc race condition từ phía client.
-- ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.SessionDetails', 'U') IS NOT NULL DROP TABLE dbo.SessionDetails;
GO

CREATE TABLE dbo.SessionDetails (
    Id             INT          NOT NULL IDENTITY(1,1),   -- Surrogate PK
    SessionId      INT          NOT NULL,                 -- FK → QuizSessions.Id
    QuestionId     INT          NOT NULL,                 -- FK → Questions.Id
    SelectedOption CHAR(1)      NULL                      -- Đáp án người dùng chọn
                   CONSTRAINT CK_SessionDetails_SelectedOption
                   CHECK (SelectedOption IS NULL OR SelectedOption IN ('A','B','C','D')),
    IsCorrect      BIT          NOT NULL DEFAULT 0,       -- Kết quả chấm điểm
    CreatedAt      DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),

    -- Khóa chính (Surrogate Key để EF Core dễ thao tác)
    CONSTRAINT PK_SessionDetails PRIMARY KEY CLUSTERED (Id ASC),

    -- ★ COMPOSITE UNIQUE CONSTRAINT ★
    -- Ràng buộc cốt lõi: (SessionId, QuestionId) phải là duy nhất.
    -- Một phiên thi chỉ được có đúng MỘT bản ghi cho mỗi câu hỏi.
    CONSTRAINT UQ_SessionDetails_Session_Question
        UNIQUE NONCLUSTERED (SessionId ASC, QuestionId ASC),

    -- Khóa ngoại tới QuizSessions
    CONSTRAINT FK_SessionDetails_QuizSessions
        FOREIGN KEY (SessionId) REFERENCES dbo.QuizSessions(Id)
        ON DELETE CASCADE   -- Xoá session → xoá chi tiết tương ứng
        ON UPDATE CASCADE,

    -- Khóa ngoại tới Questions
    CONSTRAINT FK_SessionDetails_Questions
        FOREIGN KEY (QuestionId) REFERENCES dbo.Questions(Id)
        ON DELETE NO ACTION  -- Không cho xoá question khi đã có detail
        ON UPDATE NO ACTION
);
GO

PRINT '✓ Bảng SessionDetails đã được tạo (với Unique Constraint UQ_SessionDetails_Session_Question).';
GO


-- ────────────────────────────────────────────────────────────
-- SECTION 5: Dữ liệu mẫu (Seed Data)
-- ────────────────────────────────────────────────────────────

-- Tài khoản Admin mặc định
-- Password: Admin@123  (đã hash BCrypt - trong production thay bằng hash thật)
INSERT INTO dbo.Users (Username, PasswordHash, FullName, Role)
VALUES (
    N'admin',
    N'$2a$11$ExampleHashPlaceholderReplaceWithRealBCryptHash000000000',
    N'Quản Trị Viên Mỹ Thuật',
    N'Admin'
);
GO

-- Câu hỏi mẫu về Mỹ thuật Đà Nẵng
INSERT INTO dbo.Questions (Content, OptionA, OptionB, OptionC, OptionD, CorrectOption, Points, LocationName)
VALUES
(
    N'Bảo tàng Mỹ thuật Đà Nẵng khánh thành và đi vào hoạt động vào năm nào?',
    N'Năm 2014', N'Năm 2015', N'Năm 2016', N'Năm 2017',
    'C', 10,
    N'Bảo tàng Mỹ thuật'
),
(
    N'Bảo tàng Mỹ thuật Đà Nẵng tọa lạc tại địa chỉ nào?',
    N'Số 24 Trần Phú', N'Số 78 Lê Duẩn', N'Số 42 Bạch Đằng', N'Số 01 Phan Bội Châu',
    'B', 10,
    N'Bảo tàng Mỹ thuật'
),
(
    N'Không gian trưng bày của Bảo tàng Mỹ thuật Đà Nẵng bao gồm mấy tầng?',
    N'2 tầng', N'3 tầng', N'4 tầng', N'5 tầng',
    'B', 10,
    N'Không gian trưng bày'
),
(
    N'Bảo tàng Mỹ thuật Đà Nẵng trưng bày loại hình nghệ thuật nào là chủ yếu?',
    N'Khảo cổ học', N'Mỹ thuật hiện đại và dân gian', N'Lịch sử quân sự', N'Sinh học tự nhiên',
    'B', 10,
    N'Nội dung trưng bày'
),
(
    N'Tác phẩm "Bình minh trên công trường" tại bảo tàng thuộc thể loại nào?',
    N'Tranh sơn dầu', N'Tranh lụa', N'Tranh sơn mài', N'Điêu khắc gỗ',
    'C', 15,
    N'Tác phẩm tiêu biểu'
);
GO

PRINT '✓ Dữ liệu mẫu đã được chèn thành công.';
GO


-- ────────────────────────────────────────────────────────────
-- SECTION 6: Kiểm tra kết quả cuối
-- ────────────────────────────────────────────────────────────
PRINT '';
PRINT '=== KIỂM TRA SCHEMA ===';

SELECT
    t.name                         AS [Tên Bảng],
    c.name                         AS [Tên Cột],
    tp.name                        AS [Kiểu Dữ Liệu],
    c.max_length                   AS [Độ Dài],
    c.is_nullable                  AS [Cho Phép NULL],
    c.is_identity                  AS [Tự Tăng]
FROM sys.tables t
JOIN sys.columns c  ON c.object_id = t.object_id
JOIN sys.types tp   ON tp.user_type_id = c.user_type_id
WHERE t.name IN ('Users','Questions','QuizSessions','SessionDetails')
ORDER BY t.name, c.column_id;
GO

-- Liệt kê tất cả các ràng buộc Unique để xác nhận
SELECT
    tc.TABLE_NAME   AS [Bảng],
    tc.CONSTRAINT_NAME AS [Tên Ràng Buộc],
    tc.CONSTRAINT_TYPE AS [Loại],
    STRING_AGG(kcu.COLUMN_NAME, ', ') WITHIN GROUP (ORDER BY kcu.ORDINAL_POSITION) AS [Cột]
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE tc.TABLE_SCHEMA = 'dbo'
  AND tc.CONSTRAINT_TYPE IN ('UNIQUE','PRIMARY KEY')
GROUP BY tc.TABLE_NAME, tc.CONSTRAINT_NAME, tc.CONSTRAINT_TYPE
ORDER BY tc.TABLE_NAME;
GO

PRINT '=== HOÀN TẤT SETUP DATABASE GiaiMaDiSan ===';
GO
