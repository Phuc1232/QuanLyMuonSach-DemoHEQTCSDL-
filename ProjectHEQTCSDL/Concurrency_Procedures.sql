USE [QuanLyMuonSach]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =========================================================================
-- 0. STORED PROCEDURE RESET DỮ LIỆU DEMO VỀ TRẠNG THÁI CHUẨN & CHUẨN HÓA CSDL
-- =========================================================================
-- Đảm bảo độ dài cột TinhTrang tối thiểu NVARCHAR(100) để không bị lỗi truncate (xử lý an toàn ràng buộc DEFAULT)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CuonSach' AND COLUMN_NAME = 'TinhTrang' AND (CHARACTER_MAXIMUM_LENGTH < 100 OR DATA_TYPE = 'varchar'))
BEGIN
    DECLARE @dfName NVARCHAR(256);
    SELECT @dfName = d.name 
    FROM sys.default_constraints d 
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID('CuonSach') AND c.name = 'TinhTrang';

    IF @dfName IS NOT NULL
        EXEC('ALTER TABLE CuonSach DROP CONSTRAINT ' + @dfName);

    ALTER TABLE CuonSach ALTER COLUMN TinhTrang NVARCHAR(100) NOT NULL;

    ALTER TABLE CuonSach ADD CONSTRAINT DF_CuonSach_TinhTrang DEFAULT N'ConTot' FOR TinhTrang;
END
GO

CREATE OR ALTER PROCEDURE sp_ResetDuLieuDemoTuongTranh
AS
BEGIN
    SET NOCOUNT ON;

    -- Đưa các cuốn sách mẫu về ConTot và CoSan
    UPDATE CuonSach 
    SET TinhTrang = N'ConTot', TrangThai = 'CoSan' 
    WHERE MaCuonSach IN ('CS001', 'CS002', 'CS003');

    -- Đảm bảo đầu sách S001 có đủ 3 cuốn CoSan
    UPDATE CuonSach 
    SET TrangThai = 'CoSan' 
    WHERE MaSach = 'S001';

    -- Xóa phiếu phạt bóng ma nếu có
    DELETE FROM PhieuPhat WHERE MaPhieuPhat = 'PP999';

    SELECT N'Đã khôi phục dữ liệu demo về trạng thái chuẩn ban đầu!' AS ThongBao;
END
GO


-- =========================================================================
-- 1. KỊCH BẢN 1: LOST DATA / LOST UPDATE (MẤT BẢN CẬP NHẬT)
-- =========================================================================
CREATE OR ALTER PROCEDURE sp_CapNhatTinhTrangCuonSach
    @p_MaCuonSach  VARCHAR(10),
    @p_TinhTrang   NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        WAITFOR DELAY '00:00:05';

        UPDATE CuonSach
        SET TinhTrang = @p_TinhTrang
        WHERE MaCuonSach = @p_MaCuonSach;

        COMMIT TRANSACTION;
        SELECT N'Cập nhật tình trạng sách thành công!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
-- GIAI PHAP CHO KICH BAN1:
CREATE OR ALTER PROCEDURE sp_CapNhatTinhTrangCuonSach
    @p_MaCuonSach  VARCHAR(10),
    @p_TinhTrang   NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @tmp NVARCHAR(100);
        SELECT @tmp = TinhTrang 
        FROM CuonSach WITH (UPDLOCK,HOLDLOCK)  
        WHERE MaCuonSach = @p_MaCuonSach;

        WAITFOR DELAY '00:00:05';
        UPDATE CuonSach
        SET TinhTrang = @p_TinhTrang
        WHERE MaCuonSach = @p_MaCuonSach;

        COMMIT TRANSACTION;
        SELECT N'Cập nhật tình trạng sách thành công!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- =========================================================================
-- 2. KỊCH BẢN 2: DIRTY DATA / DIRTY READ (ĐỌC DỮ LIỆU RÁC)
-- =========================================================================
-- 2.1. Phía Thủ thư: Làm thủ tục trả sách (Tạm đổi CoSan -> Chờ thanh toán phạt 10s -> Khách hủy -> ROLLBACK)
CREATE OR ALTER PROCEDURE sp_GiaoTacTraSachThuNghiem
    @p_MaCuonSach VARCHAR(10),
    @p_CoLoiHoacHuy BIT = 1 -- 1: Khách hủy/Lỗi phạt -> Rollback
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Tạm thời chuyển trạng thái sách sang CoSan
        UPDATE CuonSach 
        SET TrangThai = 'CoSan' 
        WHERE MaCuonSach = @p_MaCuonSach;

        -- CSDL tự động giữ Transaction trong 10 giây (mô phỏng thời gian chờ khách nộp tiền)
        WAITFOR DELAY '00:00:10';

        IF @p_CoLoiHoacHuy = 1
        BEGIN
            -- Khách không đủ tiền nộp phạt -> Hủy giao tác
            ROLLBACK TRANSACTION;
            SELECT N'Giao dịch trả sách đã bị HỦY (Rollback về DangMuon) do khách không đủ tiền nộp phạt!' AS ThongBao;
        END
        ELSE
        BEGIN
            COMMIT TRANSACTION;
            SELECT N'Giao dịch trả sách đã hoàn tất thành công!' AS ThongBao;
        END
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 2.2. Phía Độc giả: Tra cứu thông tin sách với mức cô lập READ UNCOMMITTED (Đọc rác)
CREATE OR ALTER PROCEDURE sp_TraCuuSach_DirtyRead
    @p_MaCuonSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    -- Thiết lập mức cô lập cho phép đọc dữ liệu chưa commit
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    SELECT 
        c.MaCuonSach, 
        s.TenSach, 
        c.TrangThai, 
        c.TinhTrang,
        c.ViTriKe
    FROM CuonSach c
    JOIN Sach s ON c.MaSach = s.MaSach
    WHERE c.MaCuonSach = @p_MaCuonSach;
END
GO

-- GIAIPHAP CHO KICHBAN2
CREATE OR ALTER PROCEDURE sp_TraCuuSach_DirtyRead
    @p_MaCuonSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    -- Thiết lập mức cô lập cho phép đọc dữ liệu chưa commit
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT 
        c.MaCuonSach, 
        s.TenSach, 
        c.TrangThai, 
        c.TinhTrang,
        c.ViTriKe
    FROM CuonSach c
    JOIN Sach s ON c.MaSach = s.MaSach
    WHERE c.MaCuonSach = @p_MaCuonSach;
END
GO
-- =========================================================================
-- 3. KỊCH BẢN 3: NON-REPEATABLE READ (ĐỌC KHÔNG NHẤT QUÁN / ĐỌC KHÔNG LẶP LẠI)
-- =========================================================================
-- Phía Quản lý: Lập báo cáo kiểm kê số lượng sách (Mức READ COMMITTED -> Đọc lần 1 -> Đợi 10s -> Đọc lần 2 trong cùng 1 Transaction)
CREATE OR ALTER PROCEDURE sp_BaoCaoKiemKeKho
    @p_MaSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED; -- Mức cô lập mặc định
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Lần đọc 1: Đếm số cuốn sách đang có sẵn
        DECLARE @v_Lan1 INT;
        SELECT @v_Lan1 = COUNT(*) 
        FROM CuonSach 
        WHERE MaSach = @p_MaSach AND TrangThai = 'CoSan';

        -- Giữ giao tác kiểm kê trong 10 giây (trong thời gian này Thủ thư ở quầy khác cho mượn 1 cuốn)
        WAITFOR DELAY '00:00:10';

        -- Lần đọc 2: Đếm lại trong cùng một Transaction
        DECLARE @v_Lan2 INT;
        SELECT @v_Lan2 = COUNT(*) 
        FROM CuonSach 
        WHERE MaSach = @p_MaSach AND TrangThai = 'CoSan';

        COMMIT TRANSACTION;

        -- Trả về bảng kết quả so sánh
        SELECT 
            @p_MaSach AS [MaSach],
            @v_Lan1 AS [SoLuong_DocLan1],
            @v_Lan2 AS [SoLuong_DocLan2],
            CASE 
                WHEN @v_Lan1 <> @v_Lan2 THEN N'Phát hiện lỗi Non-Repeatable Read (Số lượng thay đổi giữa 2 lần đọc trong cùng 1 giao dịch!)'
                ELSE N'Dữ liệu nhất quán (Số lượng không đổi)'
            END AS [KetQuaPhanTich];
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- =========================================================================
-- GIẢI PHÁP CHO KỊCH BẢN 3: NÂNG MỨC CÔ LẬP LÊN REPEATABLE READ
-- (Bôi đen đoạn mã bên dưới và nhấn Execute/F5 trên SSMS để biểu diễn giải pháp)
-- =========================================================================
/*
CREATE OR ALTER PROCEDURE sp_BaoCaoKiemKeKho
    @p_MaSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL REPEATABLE READ; 
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Lần đọc 1: Đếm số cuốn sách đang có sẵn
        DECLARE @v_Lan1 INT;
        SELECT @v_Lan1 = COUNT(*) 
        FROM CuonSach 
        WHERE MaSach = @p_MaSach AND TrangThai = 'CoSan';

        -- Giữ giao tác kiểm kê trong 10 giây (khóa S-Lock được giữ suốt giao dịch)
        WAITFOR DELAY '00:00:10';

        -- Lần đọc 2: Đếm lại trong cùng một Transaction
        DECLARE @v_Lan2 INT;
        SELECT @v_Lan2 = COUNT(*) 
        FROM CuonSach 
        WHERE MaSach = @p_MaSach AND TrangThai = 'CoSan';

        COMMIT TRANSACTION;

        -- Trả về bảng kết quả so sánh
        SELECT 
            @p_MaSach AS [MaSach],
            @v_Lan1 AS [SoLuong_DocLan1],
            @v_Lan2 AS [SoLuong_DocLan2],
            CASE 
                WHEN @v_Lan1 <> @v_Lan2 THEN N'Phát hiện lỗi Non-Repeatable Read (Số lượng thay đổi giữa 2 lần đọc trong cùng 1 giao dịch!)'
                ELSE N'Dữ liệu nhất quán (Số lượng không đổi)'
            END AS [KetQuaPhanTich];
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
*/

-- =========================================================================
-- 4. KỊCH BẢN 4: PHANTOM READ (ĐỌC BÓNG MA)
-- =========================================================================
-- 4.1. Phía Quản lý/Kế toán: Lập báo cáo tổng số phiếu phạt (Mức REPEATABLE READ -> Đếm lần 1 -> Đợi 10s -> Đếm lần 2)
CREATE OR ALTER PROCEDURE sp_BaoCaoTongHopPhieuPhat
AS
BEGIN
    SET NOCOUNT ON;
    -- REPEATABLE READ: Khóa các dòng hiện hữu nhưng KHÔNG khóa khoảng trống (không chặn INSERT mới)
    SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Lần đếm 1: Tổng số phiếu phạt hiện tại
        DECLARE @v_Count1 INT;
        SELECT @v_Count1 = COUNT(*) FROM PhieuPhat;

        -- Chờ 10 giây xuất báo cáo (trong thời gian này có phiếu phạt mới được chèn)
        WAITFOR DELAY '00:00:10';

        -- Lần đếm 2: Đếm lại trong cùng một Transaction
        DECLARE @v_Count2 INT;
        SELECT @v_Count2 = COUNT(*) FROM PhieuPhat;

        COMMIT TRANSACTION;

        SELECT 
            @v_Count1 AS [TongPhieu_Lan1],
            @v_Count2 AS [TongPhieu_Lan2],
            CASE 
                WHEN @v_Count2 > @v_Count1 THEN N'Phát hiện dòng bóng ma (Phantom Read) do có bản ghi mới chèn vào dải dữ liệu!'
                ELSE N'Số lượng bản ghi không đổi'
            END AS [KetQuaPhanTich];
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 4.2. Phía Thủ thư: Lập phiếu phạt nhanh (Chèn bản ghi bóng ma PP999)
CREATE OR ALTER PROCEDURE sp_TaoPhieuPhatNhanh
    @p_MaPhieuPhat VARCHAR(10) = 'PP999',
    @p_SoTienPhat  DECIMAL(18,0) = 50000
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @v_MaPM VARCHAR(10);
        SELECT TOP 1 @v_MaPM = MaPhieuMuon FROM PhieuMuon;

        IF EXISTS (SELECT 1 FROM PhieuPhat WHERE MaPhieuPhat = @p_MaPhieuPhat)
            DELETE FROM PhieuPhat WHERE MaPhieuPhat = @p_MaPhieuPhat;

        INSERT INTO PhieuPhat (MaPhieuPhat, MaPhieuMuon, LyDoPhat, SoTienPhat, NgayLap, TrangThaiThanhToan)
        VALUES (@p_MaPhieuPhat, @v_MaPM, N'Phạt hư hỏng nhẹ', @p_SoTienPhat, CAST(GETDATE() AS DATE), 'ChuaThanhToan');

        COMMIT TRANSACTION;
        SELECT N'Đã phát sinh phiếu phạt mới: ' + @p_MaPhieuPhat AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- =========================================================================
-- GIẢI PHÁP CHO KỊCH BẢN 4: NÂNG MỨC CÔ LẬP LÊN SERIALIZABLE
-- (Bôi đen đoạn mã bên dưới và nhấn Execute/F5 trên SSMS để biểu diễn giải pháp)
-- =========================================================================
/*
CREATE OR ALTER PROCEDURE sp_BaoCaoTongHopPhieuPhat
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Lần đếm 1: Tổng số phiếu phạt hiện tại
        DECLARE @v_Count1 INT;
        SELECT @v_Count1 = COUNT(*) FROM PhieuPhat;

        -- Chờ 10 giây xuất báo cáo (khóa RangeS-S được kích hoạt để chặn INSERT mới)
        WAITFOR DELAY '00:00:10';

        -- Lần đếm 2: Đếm lại trong cùng một Transaction
        DECLARE @v_Count2 INT;
        SELECT @v_Count2 = COUNT(*) FROM PhieuPhat;

        COMMIT TRANSACTION;

        SELECT 
            @v_Count1 AS [TongPhieu_Lan1],
            @v_Count2 AS [TongPhieu_Lan2],
            CASE 
                WHEN @v_Count2 > @v_Count1 THEN N'Phát hiện dòng bóng ma (Phantom Read) do có bản ghi mới chèn vào dải dữ liệu!'
                ELSE N'Số lượng bản ghi không đổi'
            END AS [KetQuaPhanTich];
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
*/
-- =========================================================================
-- DEADLOCK DEMO (TỰ NHIÊN VỚI LIST SÁCH & TVP)
-- =========================================================================

-- SP 1: Gây lỗi Deadlock (Mặc định duyệt theo thứ tự truyền vào)
CREATE OR ALTER PROCEDURE [dbo].[sp_LapPhieuMuon_Deadlock_Demo]
    @p_DanhSachCuonSach DanhSachCuonSachType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRAN;

    DECLARE @MaCuonSach VARCHAR(10);
    
    DECLARE cur CURSOR LOCAL FOR
        SELECT MaCuonSach FROM @p_DanhSachCuonSach;
        
    OPEN cur;
    FETCH NEXT FROM cur INTO @MaCuonSach;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Khóa tài nguyên (Update trạng thái cuốn sách)
        UPDATE CuonSach
        SET TrangThai = 'DangMuon'
        WHERE MaCuonSach = @MaCuonSach;
        
        -- Giả lập độ trễ hệ thống (5 giây)
        WAITFOR DELAY '00:00:05';
        
        FETCH NEXT FROM cur INTO @MaCuonSach;
    END
    
    CLOSE cur;
    DEALLOCATE cur;
    
    COMMIT TRAN;
END
GO

-- SP 2: An toàn (Phòng ngừa bằng Ordering Protocol - Sắp xếp theo MaCuonSach ASC)
CREATE OR ALTER PROCEDURE [dbo].[sp_LapPhieuMuon_Deadlock_Safe_Demo]
    @p_DanhSachCuonSach DanhSachCuonSachType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRAN;

    DECLARE @MaCuonSach VARCHAR(10);
    
    -- Khác biệt: Ép buộc ORDER BY MaCuonSach ASC để tránh vòng lặp Wait
    DECLARE cur CURSOR LOCAL FOR
        SELECT MaCuonSach FROM @p_DanhSachCuonSach
        ORDER BY MaCuonSach ASC;
        
    OPEN cur;
    FETCH NEXT FROM cur INTO @MaCuonSach;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Khóa tài nguyên (Update trạng thái cuốn sách)
        UPDATE CuonSach
        SET TrangThai = 'DangMuon'
        WHERE MaCuonSach = @MaCuonSach;
        
        -- Giả lập độ trễ hệ thống (5 giây)
        WAITFOR DELAY '00:00:05';
        
        FETCH NEXT FROM cur INTO @MaCuonSach;
    END
    
    CLOSE cur;
    DEALLOCATE cur;
    
    COMMIT TRAN;
END
GO
