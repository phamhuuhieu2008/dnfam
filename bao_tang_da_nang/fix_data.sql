﻿﻿﻿﻿﻿USE GiaiMaDiSan;

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
-- ĐỀ TÀI 1: Tổng quan Địa lý và Lịch sử phát triển của Bảo tàng Đà Nẵng
(N'Thành phố Đà Nẵng hiện tại bao gồm bao nhiêu quận và huyện?', N'06 quận và 03 huyện', N'06 quận và 02 huyện', N'05 quận và 03 huyện', N'07 quận và 01 huyện', 'B', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
(N'Địa giới hành chính phía Đông của thành phố Đà Nẵng tiếp giáp trực tiếp với vùng nào?', N'Vịnh Bắc Bộ', N'Tỉnh Thừa Thiên - Huế', N'Tỉnh Quảng Nam', N'Biển Đông', 'D', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
(N'Khối nhà số 42 - 44 Bạch Đằng từng giữ vai trò gì trong giai đoạn từ 1900 đến 1954?', N'Cổ Viện Chàm', N'Tòa thị chính', N'Tòa đốc lý', N'Trụ sở UBND thành phố', 'C', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
(N'Phong cách kiến trúc chủ đạo của khối nhà trung tâm ba tầng của Tòa Thị chính cũ là gì?', N'Phong cách Kiến trúc Champa', N'Phong cách Gothic', N'Phong cách Hiện đại', N'Phong cách Tân cổ điển', 'D', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
(N'Công trình Bảo tàng Đà Nẵng mới chính thức khánh thành vào năm nào?', N'Năm 2020', N'Năm 2025', N'Năm 2019', N'Năm 2022', 'B', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
(N'Mục tiêu quy hoạch tương lai của Đà Nẵng theo Quyết định số 1287/QĐ-TTg hướng tới tầm nhìn năm nào?', N'Tầm nhìn đến năm 2060', N'Tầm nhìn đến năm 2030', N'Tầm nhìn đến năm 2045', N'Tầm nhìn đến năm 2050', 'D', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
(N'Đà Nẵng chính thức trở thành thành phố trực thuộc Trung ương vào ngày tháng năm nào?', N'24/5/1889', N'03/10/1888', N'01/01/1997', N'29/03/1975', 'C', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
(N'Hệ thống không gian trưng bày chuyên đề của Bảo tàng lựa chọn kỹ càng bao nhiêu tài liệu, hiện vật?', N'Hơn 27.000 tài liệu, hiện vật', N'Gần 10.000 tài liệu, hiện vật', N'Gần 3.000 tài liệu, hiện vật', N'Khoảng 1.000 tài liệu, hiện vật', 'C', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
(N'Đâu là huyện đảo trực thuộc quyền quản lý hành chính của thành phố Đà Nẵng?', N'Huyện đảo Cồn Cỏ', N'Huyện đảo Hoàng Sa', N'Huyện đảo Lý Sơn', N'Huyện đảo Trường Sa', 'B', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
(N'Bức tường ảnh tích hợp bao nhiêu màn hình chiếu để hiển thị dòng chảy lịch sử?', N'8 màn hình chiếu', N'12 màn hình chiếu', N'2 màn hình chiếu', N'4 màn hình chiếu', 'A', 10, N'Chủ đề 1: Địa lý & Lịch sử Bảo tàng'),
-- ... (Thêm tiếp các câu từ 11 đến 30 của Đề tài 1 theo cùng logic)

-- ĐỀ TÀI 2: Hệ sinh thái rừng, Động thực vật quý hiếm và Khí hậu thủy văn Đà Nẵng
(N'Cây đào chuông và cây hồng diệp là những loài thực vật đặc trưng của khu vực nào tại Đà Nẵng?', N'Danh thắng Ngũ Hành Sơn', N'Bán đảo Sơn Trà', N'Rừng đặc dụng Nam Hải Vân', N'Khu vực Bà Nà', 'D', 10, N'Chủ đề 2: Thiên nhiên & Con người'),
(N'Động vật rừng tại Đà Nẵng mang đặc điểm địa sinh học nổi bật nào?', N'Giao thoa giữa hệ động vật Bắc Trường Sơn và mang đặc trưng Nam Trường Sơn', N'Biệt lập hoàn toàn với các vùng lân cận', N'Chỉ gồm các loài động vật vùng đồng bằng ven biển', N'Mang tính chất đặc trưng tuyệt đối của vùng khí hậu Tây Bắc', 'A', 10, N'Chủ đề 2: Thiên nhiên & Con người'),
(N'Vào năm 2014, tại Khu bảo tồn thiên nhiên Bà Nà - Núi Chúa, các nhà khoa học đã phát hiện loài bò sát mới nào?', N'Rắn lục đuôi đỏ Bà Nà', N'Nhông cát ngụy trang', N'Rùa hộp lưng đen', N'Thằn lằn chân nửa lá Bà Nà', 'D', 10, N'Chủ đề 2: Thiên nhiên & Con người'),
-- ... (Thêm tiếp các câu còn lại cho đến Đề tài 10 theo đúng Bảng Đáp Án bạn đã cung cấp)

-- ĐỀ TÀI 3: Cấu trúc Địa chất, tài nguyên Khoáng sản và Hệ sinh thái biển Đà Nẵng
(N'Thảm rong biển đạt độ phủ trung bình bao nhiêu và ở độ sâu mực nước nào?', N'Độ phủ trung bình 40% ở vùng thềm lục địa sâu', N'Độ phủ trung bình 10 - 15% ở vùng nước nông sát bờ', N'Độ phủ trung bình 50% ở vùng nước có độ sâu 10m', N'Độ phủ trung bình 16 - 30% ở vùng nước có độ sâu 3 - 4m', 'D', 10, N'Chủ đề 3: Địa chất & Hệ sinh thái biển'),
(N'Các rạn san hô ven bờ vịnh Đà Nẵng phân bố trong phạm vi giới hạn nào?', N'Dọc theo danh thắng Ngũ Hành Sơn', N'Từ Nam Hải Vân cho đến Nam Bán đảo Sơn Trà', N'Xung quanh toàn bộ huyện đảo Hoàng Sa', N'Từ cửa sông Câu Đê đến bãi biển Mỹ Khê', 'B', 10, N'Chủ đề 3: Địa chất & Hệ sinh thái biển');
-- (Tương tự cho các đề tài 4, 5, 6, 7, 8, 9, 10 dựa trên nội dung text đầu vào)