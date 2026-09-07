-- =========================================================================
-- HỆ QUẢN TRỊ CƠ SỞ DỮ LIỆU: BỔ SUNG CÁC DATABASE OBJECTS & GIAO TÁC CHUẨN MỰC
-- Áp dụng cho Hệ thống Quản Lý Độc Giả Mượn Sách (QuanLyMuonSach)
-- =========================================================================
USE [QuanLyMuonSach]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =========================================================================
-- PHẦN 1: CÁC KHUNG NHÌN (VIEWS) - PHÂN TÁCH ĐỘC LẬP TẦNG TRUY VẤN
-- =========================================================================

-- 1.1. View Quản Lý Tài Khoản (Dành cho Quản trị viên FrmMainAdmin)
-- Thay thế truy vấn JOIN 3 bảng/view tại FrmMainAdmin.LoadTaiKhoan
CREATE OR ALTER VIEW [dbo].[View_QuanLyTaiKhoan_Admin] AS
SELECT v.MaTaiKhoan, v.TenDangNhap, v.TenRole, v.TrangThai, v.NgayTao,
       ISNULL(dg.HoTen, nv.HoTen) AS HoTenNguoiDung,
       ISNULL(dg.SDT, nv.SDT) AS SDT
FROM View_TaiKhoan_Role v
LEFT JOIN DocGia dg ON v.MaTaiKhoan = dg.MaTaiKhoan
LEFT JOIN NhanVien nv ON v.MaTaiKhoan = nv.MaTaiKhoan;
GO

-- 1.2. View Quản Lý Kho Đầu Sách & Bản Sao (Dành cho FrmMainAdmin)
-- Thay thế truy vấn GROUP BY + SUM(CASE WHEN...) tại FrmMainAdmin.LoadSach
CREATE OR ALTER VIEW [dbo].[View_QuanLyKhoSach_Admin] AS
SELECT v.MaSach, v.TenSach, v.TenNXB, v.TenTheLoai, v.NamXB,
       COUNT(cs.MaCuonSach) AS TongCuon,
       SUM(CASE WHEN cs.TrangThai = 'CoSan' THEN 1 ELSE 0 END) AS CoSan,
       SUM(CASE WHEN cs.TrangThai = 'DangMuon' THEN 1 ELSE 0 END) AS DangMuon
FROM View_DanhSachDauSach v
LEFT JOIN CuonSach cs ON v.MaSach = cs.MaSach
GROUP BY v.MaSach, v.TenSach, v.TenNXB, v.TenTheLoai, v.NamXB;
GO

-- 1.3. View Danh Sách Phiếu Mượn Hiện Hành (Dành cho Thủ thư FrmMainThuThu)
-- Thay thế truy vấn JOIN PhieuMuon + DocGia + fn_TinhSoNgayTre tại FrmMainThuThu.LoadPhieuMuonDangMuon
CREATE OR ALTER VIEW [dbo].[View_DanhSachPhieuMuonHienHanh] AS
SELECT pm.MaPhieuMuon, dg.MaDG, dg.HoTen AS TenDocGia, pm.NgayMuon, pm.NgayHenTra, pm.TrangThai,
       dbo.fn_TinhSoNgayTre(pm.MaPhieuMuon) AS SoNgayTre
FROM PhieuMuon pm
JOIN DocGia dg ON pm.MaDG = dg.MaDG
WHERE pm.TrangThai IN ('DangMuon', 'QuaHan');
GO

-- 1.4. View Danh Sách Phiếu Phạt Chi Tiết (Dành cho Thủ thư FrmMainThuThu)
-- Thay thế truy vấn JOIN 3 bảng tại FrmMainThuThu.LoadPhieuPhat
CREATE OR ALTER VIEW [dbo].[View_DanhSachPhieuPhat_ChiTiet] AS
SELECT pp.MaPhieuPhat, pp.MaPhieuMuon, dg.MaDG, dg.HoTen AS TenDocGia,
       pp.LyDoPhat, pp.SoTienPhat, pp.NgayLap, pp.TrangThaiThanhToan
FROM PhieuPhat pp
JOIN PhieuMuon pm ON pp.MaPhieuMuon = pm.MaPhieuMuon
JOIN DocGia dg ON pm.MaDG = dg.MaDG;
GO

-- 1.5. View Lịch Sử Nộp Phạt Của Độc Giả (Dành cho FrmMainDocGia)
-- Thay thế truy vấn JOIN PhieuPhat + PhieuMuon tại FrmMainDocGia.LoadLichSuPhat
CREATE OR ALTER VIEW [dbo].[View_LichSuPhat_DocGia] AS
SELECT pp.MaPhieuPhat, pp.MaPhieuMuon, pm.MaDG, pp.LyDoPhat, pp.SoTienPhat, pp.NgayLap, pp.TrangThaiThanhToan
FROM PhieuPhat pp
JOIN PhieuMuon pm ON pp.MaPhieuMuon = pm.MaPhieuMuon;
GO

-- 1.6. View Cuốn Sách Trả Về Chờ Kích Hoạt Giữ Chỗ (Dành cho FrmMainThuThu)
-- Thay thế truy vấn JOIN View_SachCoSan + PhieuDatTruoc tại FrmMainThuThu.LoadCuonSachTraVeChoDatTruoc
CREATE OR ALTER VIEW [dbo].[View_CuonSachTraVeChoDatTruoc] AS
SELECT DISTINCT v.MaCuonSach, v.DisplayText, v.MaSach
FROM View_SachCoSan v
JOIN PhieuDatTruoc pdt ON v.MaSach = pdt.MaSach
WHERE v.TrangThai = 'CoSan' AND v.TinhTrang = 'ConTot' AND pdt.TrangThai = 'DangCho';
GO

-- 1.7. View Thông Tin Chi Tiết Hồ Sơ Độc Giả (Dành cho FrmMainDocGia)
-- Thay thế truy vấn JOIN DocGia + TaiKhoan tại FrmMainDocGia.LoadThongTinCaNhan
CREATE OR ALTER VIEW [dbo].[View_ThongTinDocGia_ChiTiet] AS
SELECT dg.MaDG, dg.HoTen, dg.NgaySinh, dg.DiaChi, dg.SDT, dg.Email,
       dg.LoaiDocGia, dg.NgayDangKy, dg.TrangThai, tk.TenDangNhap
FROM DocGia dg
JOIN TaiKhoan tk ON dg.MaTaiKhoan = tk.MaTaiKhoan;
GO

-- 1.8. View Thông Tin Chi Tiết Hồ Sơ Nhân Viên (Dành cho FrmMainThuThu)
-- Thay thế truy vấn JOIN NhanVien + View_TaiKhoan_Role tại FrmMainThuThu.LoadThongTinCaNhanNV
CREATE OR ALTER VIEW [dbo].[View_ThongTinNhanVien_ChiTiet] AS
SELECT nv.MaNV, nv.HoTen, nv.ChucVu, nv.SDT, nv.Email, v.TenDangNhap
FROM NhanVien nv
JOIN View_TaiKhoan_Role v ON nv.MaTaiKhoan = v.MaTaiKhoan;
GO


-- =========================================================================
-- PHẦN 2: CÁC HÀM NGƯỜI DÙNG (USER-DEFINED FUNCTIONS)
-- =========================================================================

-- 2.1. Hàm kiểm tra độc giả có đang giữ sách thuộc cùng đầu sách hay không
-- Đóng gói logic JOIN 3 bảng tại FrmMainThuThu.BtnAddSach_Click
CREATE OR ALTER FUNCTION [dbo].[fn_KiemTraDocGiaDangGiuDauSach] (
    @p_MaDG   VARCHAR(10),
    @p_MaSach VARCHAR(10)
)
RETURNS BIT
AS
BEGIN
    DECLARE @v_DangGiu BIT = 0;
    IF EXISTS (
        SELECT 1
        FROM CT_PhieuMuon ct
        JOIN PhieuMuon pm ON ct.MaPhieuMuon = pm.MaPhieuMuon
        JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach
        WHERE pm.MaDG = @p_MaDG 
          AND cs.MaSach = @p_MaSach 
          AND ct.TinhTrangSachKhiTra = 'ChuaTra'
    )
        SET @v_DangGiu = 1;
    RETURN @v_DangGiu;
END;
GO

-- 2.2. Table-Valued Function tra cứu các cuốn sách vật lý và vị trí kệ của một đầu sách
-- Thay thế inline query tại FrmMainDocGia.BtnXemViTriKe_Click
CREATE OR ALTER FUNCTION [dbo].[fn_TraCuuViTriSach] (
    @p_MaSach VARCHAR(10)
)
RETURNS TABLE
AS
RETURN (
    SELECT MaCuonSach, TinhTrang, TrangThai, ViTriKe
    FROM CuonSach
    WHERE MaSach = @p_MaSach
);
GO

-- 2.3. Hàm đếm số lượng yêu cầu đặt trước đã sẵn sàng nhận sách của độc giả
-- Thay thế inline query tại FrmMainDocGia.CheckSanSangNhan
CREATE OR ALTER FUNCTION [dbo].[fn_DemPhieuDatTruocChoNhan] (
    @p_MaDG VARCHAR(10)
)
RETURNS INT
AS
BEGIN
    DECLARE @v_Count INT;
    SELECT @v_Count = COUNT(*)
    FROM PhieuDatTruoc
    WHERE MaDG = @p_MaDG AND TrangThai = 'ChoNhan';
    RETURN ISNULL(@v_Count, 0);
END;
GO


-- =========================================================================
-- PHẦN 3: CÁC STORED PROCEDURES & GIAO TÁC NGHIỆP VỤ (TRANSACTIONS)
-- =========================================================================

-- 3.1. Stored Procedure Xác thực Đăng nhập & Lấy Hồ Sơ Phân Quyền
-- Thay thế hoàn toàn câu lệnh SELECT JOIN 4 bảng và kiểm tra mật khẩu inline trong C#
CREATE OR ALTER PROCEDURE [dbo].[sp_XacThucDangNhap]
    @p_TenDangNhap VARCHAR(50),
    @p_MatKhau     VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra sự tồn tại và tính hợp lệ của tài khoản
    IF NOT EXISTS (
        SELECT 1
        FROM View_TaiKhoan_Role v
        JOIN TaiKhoan tk ON v.MaTaiKhoan = tk.MaTaiKhoan
        WHERE v.TenDangNhap = @p_TenDangNhap AND tk.MatKhau = @p_MatKhau
    )
    BEGIN
        SELECT 0 AS KetQua, N'Tên đăng nhập hoặc mật khẩu không chính xác!' AS ThongBao;
        RETURN;
    END

    -- Trả về đầy đủ thông tin định danh và vai trò
    SELECT 1 AS KetQua,
           v.MaTaiKhoan, v.TenDangNhap, v.MaRole, v.TenRole, v.TrangThai,
           dg.MaDG, dg.HoTen AS TenDocGia,
           nv.MaNV, nv.HoTen AS TenNhanVien
    FROM View_TaiKhoan_Role v
    JOIN TaiKhoan tk ON v.MaTaiKhoan = tk.MaTaiKhoan
    LEFT JOIN DocGia dg ON v.MaTaiKhoan = dg.MaTaiKhoan
    LEFT JOIN NhanVien nv ON v.MaTaiKhoan = nv.MaTaiKhoan
    WHERE v.TenDangNhap = @p_TenDangNhap AND tk.MatKhau = @p_MatKhau;
END;
GO

-- 3.2. Stored Procedure Cập nhật Trạng thái Tài khoản
-- Đóng gói thao tác DML quản lý người dùng có kiểm tra ràng buộc tại FrmMainAdmin
CREATE OR ALTER PROCEDURE [dbo].[sp_DoiTrangThaiTaiKhoan]
    @p_MaTaiKhoan   VARCHAR(10),
    @p_TrangThaiMoi VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM TaiKhoan WITH (UPDLOCK) WHERE MaTaiKhoan = @p_MaTaiKhoan)
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS KetQua, N'Lỗi: Mã tài khoản không tồn tại!' AS ThongBao;
            RETURN;
        END

        UPDATE TaiKhoan 
        SET TrangThai = @p_TrangThaiMoi 
        WHERE MaTaiKhoan = @p_MaTaiKhoan;

        COMMIT TRANSACTION;
        SELECT 1 AS KetQua, N'Đã cập nhật trạng thái tài khoản thành công!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS KetQua, ERROR_MESSAGE() AS ThongBao;
    END CATCH
END;
GO

-- 3.3. Stored Procedure Đặt lại Mật khẩu Tài khoản
-- Đóng gói thao tác reset password an toàn tại FrmMainAdmin
CREATE OR ALTER PROCEDURE [dbo].[sp_DatLaiMatKhau]
    @p_MaTaiKhoan     VARCHAR(10),
    @p_MatKhauMacDinh VARCHAR(255) = '123456'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM TaiKhoan WITH (UPDLOCK) WHERE MaTaiKhoan = @p_MaTaiKhoan)
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS KetQua, N'Lỗi: Mã tài khoản không tồn tại!' AS ThongBao;
            RETURN;
        END

        UPDATE TaiKhoan 
        SET MatKhau = @p_MatKhauMacDinh 
        WHERE MaTaiKhoan = @p_MaTaiKhoan;

        COMMIT TRANSACTION;
        SELECT 1 AS KetQua, N'Đã reset mật khẩu tài khoản thành công về mặc định!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS KetQua, ERROR_MESSAGE() AS ThongBao;
    END CATCH
END;
GO

-- 3.4. Stored Procedure Giao tác Xóa Đầu Sách An Toàn
-- Thay thế câu lệnh DELETE trực tiếp tại FrmMainAdmin.BtnDeleteSach_Click
CREATE OR ALTER PROCEDURE [dbo].[sp_XoaDauSach]
    @p_MaSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Kiểm tra đầu sách tồn tại
        IF NOT EXISTS (SELECT 1 FROM Sach WITH (UPDLOCK) WHERE MaSach = @p_MaSach)
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS KetQua, N'Lỗi: Đầu sách không tồn tại trong hệ thống!' AS ThongBao;
            RETURN;
        END

        -- 2. Kiểm tra có cuốn sách nào đang được mượn chưa trả
        IF EXISTS (
            SELECT 1 
            FROM CuonSach cs
            JOIN CT_PhieuMuon ct ON cs.MaCuonSach = ct.MaCuonSach
            WHERE cs.MaSach = @p_MaSach AND ct.TinhTrangSachKhiTra = 'ChuaTra'
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS KetQua, N'Không thể xóa: Hiện vẫn còn bản sao của đầu sách này đang được mượn chưa trả!' AS ThongBao;
            RETURN;
        END

        -- 3. Kiểm tra có phiếu đặt trước đang hoạt động
        IF EXISTS (
            SELECT 1 
            FROM PhieuDatTruoc 
            WHERE MaSach = @p_MaSach AND TrangThai IN ('DangCho', 'ChoNhan')
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT 0 AS KetQua, N'Không thể xóa: Đầu sách này đang có độc giả đặt trước chưa xử lý xong!' AS ThongBao;
            RETURN;
        END

        -- 4. Xóa các cuốn sách vật lý thuộc đầu sách này
        DELETE FROM CuonSach WHERE MaSach = @p_MaSach;

        -- 5. Xóa đầu sách
        DELETE FROM Sach WHERE MaSach = @p_MaSach;

        COMMIT TRANSACTION;
        SELECT 1 AS KetQua, N'Đã xóa đầu sách và các bản sao thành công!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS KetQua, ERROR_MESSAGE() AS ThongBao;
    END CATCH
END;
GO

-- 3.5. Cải tiến Giao tác sp_NhanSachDatTruoc: Triệt tiêu Race Condition tại Client
-- Tự động khóa và tìm cuốn sách 'GiuCho' trong cùng 1 Transaction nếu client không truyền mã cuốn
CREATE OR ALTER PROCEDURE [dbo].[sp_NhanSachDatTruoc]
    @p_MaPhieuDatTruoc VARCHAR(10),
    @p_MaCuonSach      VARCHAR(10) = NULL,
    @p_MaPhieuMuon     VARCHAR(10),
    @p_MaNV            VARCHAR(10),
    @p_NgayHenTra      DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_MaDG      VARCHAR(10);
    DECLARE @v_MaSach    VARCHAR(10);
    DECLARE @v_TrangThai VARCHAR(20);
    DECLARE @v_DieuKien  NVARCHAR(200);

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Kiểm tra phiếu đặt trước có hợp lệ ở trạng thái 'ChoNhan' không (sử dụng UPDLOCK để chống can thiệp)
        SELECT @v_MaDG = MaDG, @v_MaSach = MaSach, @v_TrangThai = TrangThai
        FROM PhieuDatTruoc WITH (UPDLOCK, ROWLOCK)
        WHERE MaPhieuDatTruoc = @p_MaPhieuDatTruoc;

        IF @v_TrangThai IS NULL OR @v_TrangThai <> 'ChoNhan'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Phiếu đặt trước không tồn tại hoặc chưa có sách sẵn sàng để nhận' AS ThongBao;
            RETURN;
        END

        -- 2. Kiểm tra điều kiện mượn tổng quát của độc giả (đang mượn dưới 3 cuốn)
        SET @v_DieuKien = dbo.fn_KiemTraDuDieuKienMuon(@v_MaDG);
        IF @v_DieuKien <> N'Đủ điều kiện mượn sách'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @v_DieuKien AS ThongBao;
            RETURN;
        END

        -- 3. Nếu Client không truyền mã cuốn, SP tự động khóa và chọn cuốn sách 'GiuCho' nguyên tử trong DB
        IF @p_MaCuonSach IS NULL OR LTRIM(RTRIM(@p_MaCuonSach)) = ''
        BEGIN
            SELECT TOP 1 @p_MaCuonSach = MaCuonSach
            FROM CuonSach WITH (UPDLOCK, ROWLOCK)
            WHERE MaSach = @v_MaSach AND TrangThai = 'GiuCho';
        END

        -- Kiểm tra tính hợp lệ của cuốn sách được gán
        IF @p_MaCuonSach IS NULL OR NOT EXISTS (
            SELECT 1 FROM CuonSach WITH (UPDLOCK, ROWLOCK)
            WHERE MaCuonSach = @p_MaCuonSach AND MaSach = @v_MaSach AND TrangThai = 'GiuCho'
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Không tìm thấy cuốn sách nào ở trạng thái Giữ chỗ phù hợp cho đầu sách này' AS ThongBao;
            RETURN;
        END

        -- 4. Lập Phiếu mượn chính thức
        INSERT INTO PhieuMuon (MaPhieuMuon, MaDG, MaNV, NgayMuon, NgayHenTra, TrangThai)
        VALUES (@p_MaPhieuMuon, @v_MaDG, @p_MaNV, CAST(GETDATE() AS DATE), @p_NgayHenTra, 'DangMuon');

        -- 5. Lập chi tiết phiếu mượn
        INSERT INTO CT_PhieuMuon (MaPhieuMuon, MaCuonSach, TinhTrangSachKhiTra)
        VALUES (@p_MaPhieuMuon, @p_MaCuonSach, 'ChuaTra');

        -- 6. Cập nhật trạng thái cuốn sách thành 'DangMuon'
        UPDATE CuonSach SET TrangThai = 'DangMuon' WHERE MaCuonSach = @p_MaCuonSach;

        -- 7. Hoàn tất phiếu đặt trước ('DaXuLy')
        UPDATE PhieuDatTruoc SET TrangThai = 'DaXuLy' WHERE MaPhieuDatTruoc = @p_MaPhieuDatTruoc;

        COMMIT TRANSACTION;
        SELECT N'Độc giả đã nhận sách ' + @p_MaCuonSach + N' và lập phiếu mượn thành công!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Không thể nhận sách: ' + ERROR_MESSAGE() AS ThongBao;
    END CATCH
END;
GO

-- 3.6. Cải tiến Giao tác sp_HuyGiuChoHetHan: Tự động giải phóng cuốn sách GiuCho nguyên tử
CREATE OR ALTER PROCEDURE [dbo].[sp_HuyGiuChoHetHan]
    @p_MaPhieuDatTruoc VARCHAR(10),
    @p_MaCuonSach      VARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_MaSach VARCHAR(10);
    DECLARE @v_TrangThai VARCHAR(20);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @v_MaSach = MaSach, @v_TrangThai = TrangThai
        FROM PhieuDatTruoc WITH (UPDLOCK, ROWLOCK)
        WHERE MaPhieuDatTruoc = @p_MaPhieuDatTruoc;

        IF @v_TrangThai IS NULL OR @v_TrangThai <> 'ChoNhan'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Phiếu đặt trước không ở trạng thái Chờ nhận để có thể hủy giữ chỗ' AS ThongBao;
            RETURN;
        END

        -- Nếu Client không truyền, SP tự tìm cuốn sách đang giữ chỗ cho đầu sách đó
        IF @p_MaCuonSach IS NULL OR LTRIM(RTRIM(@p_MaCuonSach)) = ''
        BEGIN
            SELECT TOP 1 @p_MaCuonSach = MaCuonSach
            FROM CuonSach WITH (UPDLOCK, ROWLOCK)
            WHERE MaSach = @v_MaSach AND TrangThai = 'GiuCho';
        END

        -- Hủy trạng thái giữ chỗ của phiếu đặt trước -> chuyển sang 'DaHuy'
        UPDATE PhieuDatTruoc 
        SET TrangThai = 'DaHuy' 
        WHERE MaPhieuDatTruoc = @p_MaPhieuDatTruoc;

        -- Trả cuốn sách về trạng thái 'CoSan'
        IF @p_MaCuonSach IS NOT NULL
        BEGIN
            UPDATE CuonSach 
            SET TrangThai = 'CoSan' 
            WHERE MaCuonSach = @p_MaCuonSach;
        END

        COMMIT TRANSACTION;
        SELECT N'Đã hủy giữ chỗ thành công cho phiếu ' + @p_MaPhieuDatTruoc + N', cuốn sách ' + ISNULL(@p_MaCuonSach, '') + N' đã trở lại trạng thái Có sẵn!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi khi hủy giữ chỗ: ' + ERROR_MESSAGE() AS ThongBao;
    END CATCH
END;
GO

-- 3.7. Stored Procedure Demo Tương tranh: Cho mượn sách (Non-Repeatable Read Demo)
-- Đóng gói câu lệnh UPDATE inline tại FrmConcurrencyWindows.cs
CREATE OR ALTER PROCEDURE [dbo].[sp_Demo_NonRepeatableRead_ChoMuon]
    @p_MaCuonSach VARCHAR(10) = 'CS002'
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE CuonSach 
        SET TrangThai = 'DangMuon' 
        WHERE MaCuonSach = @p_MaCuonSach;

        COMMIT TRANSACTION;
        SELECT 1 AS KetQua, N'Đã cho mượn thành công cuốn sách ' + @p_MaCuonSach + N' (Trạng thái chuyển sang DangMuon)!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0 AS KetQua, ERROR_MESSAGE() AS ThongBao;
    END CATCH
END;
GO
