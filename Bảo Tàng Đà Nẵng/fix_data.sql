﻿USE GiaiMaDiSan;

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
    N'Quản Trị Viên Mỹ Thuật',
    N'Admin'
);

INSERT INTO dbo.Questions (Content, OptionA, OptionB, OptionC, OptionD, CorrectOption, Points, LocationName)
VALUES
-- ── CHỦ ĐỀ: TIN TỨC / SỰ KIỆN ──
(N'Sự kiện ''Bảo tàng Đà Nẵng'' được tổ chức vào ngày nào?', N'2025-12-25', N'2025-01-01', N'2025-06-15', N'1', 'D', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''NGÀY HỘI DI SẢN VĂN HÓA ĐÀ NẴNG NĂM 2025 CHỦ ĐỀ “ĐA DẠNG SẮC MÀU VĂN HÓA VÙNG CAO ĐÀ NẴNG”'' được tổ chức vào ngày nào?', N'1', N'2025-01-01', N'2025-06-15', N'2025-12-25', 'A', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Chương trình trải nghiệm “Hè vui sáng tạo” năm 2025 tại Bảo tàng Mỹ thuật Đà Nẵng'' được tổ chức vào ngày nào?', N'2025-12-25', N'2025-06-15', N'1', N'2025-01-01', 'C', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Tọa đàm “Đổi mới và nâng cao chất lượng công tác triển lãm chuyên đề tại các bảo tàng mỹ thuật ở Việt Nam” tại Bảo tàng Mỹ thuật Đà Nẵng'' được tổ chức vào ngày nào?', N'1', N'2025-06-15', N'2025-01-01', N'2025-12-25', 'A', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Tuần lễ Bảo tàng - Chủ đề 7: Đại dương (#OceansMW)'' được tổ chức vào ngày nào?', N'1', N'2025-12-25', N'2025-06-15', N'2025-01-01', 'A', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Chủ đề 4: Sự gắn kết (#TogethernessMW)'' được tổ chức vào ngày nào?', N'2025-01-01', N'1', N'2025-06-15', N'2025-12-25', 'B', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Chủ đề 6: Tham quan bảo tàng (#VisitMuseumsMW)'' được tổ chức vào ngày nào?', N'2025-12-25', N'1', N'2025-01-01', N'2025-06-15', 'B', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Chủ đề 5: Khả năng tiếp cận (#AccessibilityMW)'' được tổ chức vào ngày nào?', N'2025-06-15', N'2025-01-01', N'1', N'2025-12-25', 'C', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Triển lãm “Mỹ thuật trẻ - Đà Nẵng 2025”'' được tổ chức vào ngày nào?', N'2025-06-15', N'2025-01-01', N'2025-12-25', N'1', 'D', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Ngày Hội di sản văn hóa Đà Nẵng năm 2025 chủ đề “Đa dạng sắc màu văn hóa vùng cao Đà Nẵng”'' được tổ chức vào ngày nào?', N'2025-12-25', N'2025-01-01', N'1', N'2025-06-15', 'C', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Sơ đồ tổ chức'' được tổ chức vào ngày nào?', N'2025-12-25', N'2025-01-01', N'1', N'2025-06-15', 'C', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Chức năng, nhiệm vụ'' được tổ chức vào ngày nào?', N'2025-12-25', N'2025-06-15', N'2025-01-01', N'1', 'D', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Lịch sử hình thành'' được tổ chức vào ngày nào?', N'2025-12-25', N'1', N'2025-01-01', N'2025-06-15', 'B', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Dịch vụ Bảo tàng Đà Nẵng'' được tổ chức vào ngày nào?', N'1', N'2025-01-01', N'2025-12-25', N'2025-06-15', 'A', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Dịch vụ thuyết minh'' được tổ chức vào ngày nào?', N'2025-06-15', N'2025-12-25', N'2025-01-01', N'1', 'D', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Lễ khai mạc Triển lãm mỹ thuật "Đà Nẵng gấm hoa"'' được tổ chức vào ngày nào?', N'2025-01-01', N'2025-06-15', N'1', N'2025-12-25', 'C', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''KHÔNG GIAN TRƯNG BÀY CÁC HIỆN VẬT MỸ THUẬT DÂN GIAN TRUYỀN THỐNG'' được tổ chức vào ngày nào?', N'0', N'2025-01-01', N'2025-06-15', N'2025-12-25', 'A', 10, N'Tin tức/Sự kiện'),
(N'Sự kiện ''Bảo tàng Mỹ thuật Đà Nẵng/ The Da Nang Fine Arts Museum'' được tổ chức vào ngày nào?', N'2025-06-15', N'1', N'2025-12-25', N'2025-01-01', 'B', 10, N'Tin tức/Sự kiện'),

-- ── CHỦ ĐỀ: BẢO TÀNG LÀNG NGHỀ ──
(N'Đồ vật ''Hội An xưa 1'' đến từ đâu?', N'Hội An', N'Thành cổ Quảng Trị', N'Thành phố Đà Nẵng', N'Bảo tàng Đà Nẵng', 'A', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Thành cổ Quảng Trị'' đến từ đâu?', N'Bảo tàng Đà Nẵng', N'Hội An', N'Thành phố Đà Nẵng', N'Thành cổ Quảng Trị', 'D', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Bảng hiệu - Nghệ thuật Bài chòi xứ Quảng'' đến từ đâu?', N'Thành cổ Quảng Trị', N'Bảo tàng Đà Nẵng', N'Thành phố Đà Nẵng', N'Hội An', 'C', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Bộ cờ vây đỏ - Bài chòi xứ Quảng'' đến từ đâu?', N'Bảo tàng Đà Nẵng', N'Thành phố Đà Nẵng', N'Hội An', N'Thành cổ Quảng Trị', 'B', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Bộ cờ vây vàng - Nghệ thuật Bài chòi xứ Quảng'' đến từ đâu?', N'Hội An', N'Thành phố Đà Nẵng', N'Thành cổ Quảng Trị', N'Bảo tàng Đà Nẵng', 'B', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Bộ cờ xóc (thẻ rút) - Nghệ thuật bài chòi xứ Quảng'' đến từ đâu?', N'Hội An', N'Bảo tàng Đà Nẵng', N'Thành phố Đà Nẵng', N'Thành cổ Quảng Trị', 'C', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Bộ thẻ bài lớn - Nghệ thuật bài chòi xứ Quảng'' đến từ đâu?', N'Bảo tàng Đà Nẵng', N'Thành cổ Quảng Trị', N'Hội An', N'Thành phố Đà Nẵng', 'D', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Bộ thẻ bài nhỏ - Nghệ thuật bài chòi xứ Quảng'' đến từ đâu?', N'Thành cổ Quảng Trị', N'Thành phố Đà Nẵng', N'Bảo tàng Đà Nẵng', N'Hội An', 'B', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Cờ hội - Nghệ thuật bài chòi xứ Quảng'' đến từ đâu?', N'Thành cổ Quảng Trị', N'Thành phố Đà Nẵng', N'Hội An', N'Bảo tàng Đà Nẵng', 'B', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Đôi guốc'' đến từ đâu?', N'Thành cổ Quảng Trị', N'Bảo tàng Đà Nẵng', N'Hội An', N'Thành phố Đà Nẵng', 'B', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Lon xóc và chân cờ xóc - Nghệ thuật bài chòi xứ Quảng'' đến từ đâu?', N'Bảo tàng Đà Nẵng', N'Hội An', N'Thành phố Đà Nẵng', N'Thành cổ Quảng Trị', 'C', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Dùi trống cái - Nghệ thuật bài chòi xứ Quảng'' đến từ đâu?', N'Bảo tàng Đà Nẵng', N'Thành cổ Quảng Trị', N'Hội An', N'Thành phố Đà Nẵng', 'D', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Đàn bầu - Nghệ thuật bài chòi xứ Quảng'' đến từ đâu?', N'Thành phố Đà Nẵng', N'Thành cổ Quảng Trị', N'Hội An', N'Bảo tàng Đà Nẵng', 'A', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Kèn bầu - Nghệ thuật bài chòi xứ Quảng'' đến từ đâu?', N'Thành phố Đà Nẵng', N'Bảo tàng Đà Nẵng', N'Thành cổ Quảng Trị', N'Hội An', 'A', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Áo anh hiệu - Nghệ thuật bài chòi xứ Quảng'' đến từ đâu?', N'Thành phố Đà Nẵng', N'Bảo tàng Đà Nẵng', N'Hội An', N'Thành cổ Quảng Trị', 'A', 10, N'Bảo tàng làng nghề'),
(N'Đồ vật ''Bộ áo dài vá quàng'' đến từ đâu?', N'Thành phố Đà Nẵng', N'Thành cổ Quảng Trị', N'Hội An', N'thành phố Đà Nẵng', 'D', 10, N'Bảo tàng làng nghề'),

-- ── CHỦ ĐỀ: NGHỆ NHÂN ──
(N'Nghệ nhân ''Nghệ nhân 2'' sinh năm nào?', N'1995', N'1977', N'1994', N'1985', 'D', 10, N'Nghệ nhân'),
(N'Nghệ nhân ''Nghệ nhân 3'' sinh năm nào?', N'1978', N'1979', N'1980', N'1976', 'C', 10, N'Nghệ nhân'),
(N'Nghệ nhân ''Nghệ nhân Lương Đáng'' sinh năm nào?', N'1940', N'1958', N'1950', N'1944', 'C', 10, N'Nghệ nhân'),
(N'Nghệ nhân ''Nghệ nhân Ngọc Huệ'' sinh năm nào?', N'1966', N'1972', N'1967', N'1975', 'D', 10, N'Nghệ nhân'),

-- ── CHỦ ĐỀ: CÂU LẠC BỘ ──
(N'Câu lạc bộ ''Câu lạc bộ bài chòi khối phố Phú Thạnh'' nằm ở địa chỉ nào?', N'Đà Nẵng', N'Hội An', N'Đà Nẵng', N'Huế', 'A', 10, N'Câu lạc bộ'),
(N'Câu lạc bộ ''Câu lạc bộ bài chòi Hà Mỹ xã Nam Phước'' nằm ở địa chỉ nào?', N'Đà Nẵng', N'Huế', N'Đà Nẵng', N'Hội An', 'A', 10, N'Câu lạc bộ'),
(N'Câu lạc bộ ''Câu lạc bộ bài chòi Tịnh Thủy'' nằm ở địa chỉ nào?', N'Huế', N'Đà Nẵng', N'Hội An', N'Đà Nẵng', 'B', 10, N'Câu lạc bộ'),
(N'Câu lạc bộ ''Câu lạc bộ bài chòi Tiên Cảnh xã Thanh Bình'' nằm ở địa chỉ nào?', N'Huế', N'Hội An', N'Đà Nẵng', N'Đà Nẵng', 'C', 10, N'Câu lạc bộ'),
(N'Câu lạc bộ ''Câu lạc bộ bài chòi Sông Thu'' nằm ở địa chỉ nào?', N'Huế', N'Hội An', N'Đà Nẵng', N'Đà Nẵng', 'C', 10, N'Câu lạc bộ'),
(N'Câu lạc bộ ''Câu lạc bộ bài chòi xã Vu Gia'' nằm ở địa chỉ nào?', N'Huế', N'Đà Nẵng', N'Hội An', N'Đà Nẵng', 'B', 10, N'Câu lạc bộ'),

-- ── CHỦ ĐỀ: TỔNG HỢP ──
(N'Bảo tàng Đà Nẵng nằm ở thành phố nào?', N'Đà Nẵng', N'Hà Nội', N'TP. Hồ Chí Minh', N'Hội An', 'A', 10, N'Tổng hợp'),
(N'Nghệ thuật Bài chòi là di sản văn hóa của vùng nào?', N'Xứ Quảng', N'Bắc Bộ', N'Nam Bộ', N'Tây Nguyên', 'A', 10, N'Tổng hợp'),
(N'Bảo tàng Mỹ thuật Đà Nẵng trưng bày chủ yếu về lĩnh vực nào?', N'Mỹ thuật', N'Khoa học', N'Lịch sử quân sự', N'Tự nhiên', 'A', 10, N'Tổng hợp'),
(N'Thành Điện Hải là di tích lịch sử thuộc thời kỳ nào?', N'Thời Nguyễn', N'Thời Lê', N'Thời Trần', N'Thời Lý', 'A', 10, N'Tổng hợp'),
(N'Nghệ thuật Bài chòi xứ Quảng được UNESCO công nhận là gì?', N'Di sản văn hóa phi vật thể', N'Di sản thiên nhiên', N'Di sản kiến trúc', N'Di sản khảo cổ', 'A', 10, N'Tổng hợp'),
(N'Văn hóa Sa Huỳnh thuộc thời kỳ nào?', N'Đồ đá mới', N'Đồ sắt', N'Đồ đồng', N'Đồ gốm', 'A', 10, N'Tổng hợp'),
(N'Ấm Tỳ Bà là loại đồ vật gì?', N'Đồ gốm', N'Đồ đồng', N'Đồ đá', N'Đồ gỗ', 'A', 10, N'Tổng hợp'),
(N'Khuyên Tai Ba Màu thuộc nền văn hóa nào?', N'Sa Huỳnh', N'Đồng Sơn', N'Óc Eo', N'Phùng Nguyên', 'A', 10, N'Tổng hợp'),
(N'Đầu tượng thần Shiva là hiện vật thuộc nền văn hóa nào?', N'Champa', N'Việt Nam', N'Khmer', N'Chăm', 'A', 10, N'Tổng hợp'),
(N'Mộ Chum là loại hình mộ táng của dân tộc nào?', N'Cơ Tu', N'Kinh', N'Hoa', N'Chăm', 'A', 10, N'Tổng hợp');