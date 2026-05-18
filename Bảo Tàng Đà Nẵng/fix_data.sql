USE GiaiMaDiSan;

DELETE FROM dbo.SessionDetails;
DELETE FROM dbo.QuizSessions;
DELETE FROM dbo.Questions;
DELETE FROM dbo.Users;

DBCC CHECKIDENT ('dbo.Questions', RESEED, 0);
DBCC CHECKIDENT ('dbo.Users', RESEED, 0);

INSERT INTO dbo.Users (Username, PasswordHash, FullName, Role)
VALUES (
    N'admin',
    N'$2a$11$ExampleHashPlaceholderReplaceWithRealBCryptHash000000000',
    N'Quản Trị Viên Bảo Tàng',
    N'Admin'
);

INSERT INTO dbo.Questions (Content, OptionA, OptionB, OptionC, OptionD, CorrectOption, Points, LocationName)
VALUES
(
    N'Thành Điện Hải được xây dựng vào năm nào?',
    N'Năm 1812', N'Năm 1825', N'Năm 1835', N'Năm 1858',
    'A', 10,
    N'Thành Điện Hải'
),
(
    N'Bảo tàng Điêu khắc Chăm Đà Nẵng được thành lập vào năm nào?',
    N'Năm 1902', N'Năm 1915', N'Năm 1936', N'Năm 1956',
    'A', 10,
    N'Bảo tàng Điêu khắc Chăm'
),
(
    N'Cầu Rồng Đà Nẵng phun lửa vào tối thứ mấy trong tuần?',
    N'Thứ Sáu và Thứ Bảy', N'Thứ Bảy và Chủ Nhật', N'Chỉ Chủ Nhật', N'Thứ Sáu, Thứ Bảy và Chủ Nhật',
    'B', 10,
    N'Cầu Rồng'
),
(
    N'Ngũ Hành Sơn gồm bao nhiêu ngọn núi đá cẩm thạch?',
    N'3 ngọn', N'4 ngọn', N'5 ngọn', N'6 ngọn',
    'C', 10,
    N'Ngũ Hành Sơn'
),
(
    N'Bảo tàng Đà Nẵng nằm ở địa điểm nào sau đây?',
    N'Số 24 Trần Phú', N'Số 78 Lê Duẩn', N'Số 24 Lê Lợi', N'Thành Điện Hải',
    'D', 15,
    N'Bảo tàng Đà Nẵng'
);