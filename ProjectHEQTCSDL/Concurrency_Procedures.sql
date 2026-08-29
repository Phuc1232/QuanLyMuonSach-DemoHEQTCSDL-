USE [QuanLyMuonSach]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =========================================================================
-- 0. STORED PROCEDURE RESET DỮ LIỆU DEMO VỀ TRẠNG THÁI CHUẨN & CHUẨN HÓA CSDL
-- =========================================================================
-- Đảm bảo độ dài cột TinhTrang tối thiểu NVARCHAR(100) để không bị lỗi truncate
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CuonSach' AND COLUMN_NAME = 'TinhTrang' AND (CHARACTER_MAXIMUM_LENGTH < 100 OR DATA_TYPE = 'varchar'))
BEGIN
    ALTER TABLE CuonSach ALTER COLUMN TinhTrang NVARCHAR(100) NOT NULL;
END
GO

CREATE OR ALTER PROCEDURE sp_ResetDuLieuDemoTuongTranh
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Đảm bảo cột TinhTrang có độ dài NVARCHAR(100)
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CuonSach' AND COLUMN_NAME = 'TinhTrang' AND (CHARACTER_MAXIMUM_LENGTH < 100 OR DATA_TYPE = 'varchar'))
    BEGIN
        ALTER TABLE CuonSach ALTER COLUMN TinhTrang NVARCHAR(100) NOT NULL;
    END

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

--GIAI PHAP CHO KICHBAN3
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

-- GIAI PHAP CHO KICHBAN 4
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
-- =========================================================================
-- 5. KỊCH BẢN 5: DEADLOCK (BẾ TẮC TƯƠNG HỖ - CROSS DEPENDENCY)
-- =========================================================================
-- 5.1. Giao tác T1: Khóa CS001 trước -> Chờ 5s -> Đòi khóa tiếp CS002
CREATE OR ALTER PROCEDURE sp_Demo_Deadlock_T1
    @p_DelaySeconds INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @delayStr VARCHAR(10) = '00:00:' + RIGHT('00' + CAST(@p_DelaySeconds AS VARCHAR(2)), 2);
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Bước 1: T1 chiếm giữ độc quyền (X-Lock) cuốn sách CS001
        UPDATE CuonSach 
        SET TinhTrang = 'CapNhat_T1' 
        WHERE MaCuonSach = 'CS001';
        
        -- Giữ giao tác mở và tạm dừng để T2 kịp chiếm khóa CS002
        WAITFOR DELAY @delayStr;
        
        -- Bước 2: T1 cố gắng chiếm khóa tiếp cuốn sách CS002 (lúc này đang bị T2 nắm giữ)
        UPDATE CuonSach 
        SET TinhTrang = 'HoanTat_T1' 
        WHERE MaCuonSach = 'CS002';
        
        COMMIT TRANSACTION;
        SELECT N'Giao dịch T1 hoàn thành thành công!' AS ThongBao, 1 AS IsSuccess, 0 AS ErrorCode;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        IF ERROR_NUMBER() = 1205
        BEGIN
            SELECT N'Giao dịch T1 bị SQL Server chọn làm NẠN NHÂN DEADLOCK (Victim - Error 1205) và tự động Rollback!' AS ThongBao, 0 AS IsSuccess, 1205 AS ErrorCode;
        END
        ELSE
        BEGIN
            SELECT N'Lỗi T1: ' + ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess, ERROR_NUMBER() AS ErrorCode;
        END
    END CATCH
END
GO

-- 5.2. Giao tác T2: Khóa CS002 trước -> Chờ 5s -> Đòi khóa tiếp CS001
CREATE OR ALTER PROCEDURE sp_Demo_Deadlock_T2
    @p_DelaySeconds INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @delayStr VARCHAR(10) = '00:00:' + RIGHT('00' + CAST(@p_DelaySeconds AS VARCHAR(2)), 2);
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Bước 1: T2 chiếm giữ độc quyền (X-Lock) cuốn sách CS002
        UPDATE CuonSach 
        SET TinhTrang = 'CapNhat_T2' 
        WHERE MaCuonSach = 'CS002';
        
        -- Giữ giao tác mở và tạm dừng để T1 kịp chiếm khóa CS001
        WAITFOR DELAY @delayStr;
        
        -- Bước 2: T2 cố gắng chiếm khóa tiếp cuốn sách CS001 (lúc này đang bị T1 nắm giữ)
        UPDATE CuonSach 
        SET TinhTrang = 'HoanTat_T2' 
        WHERE MaCuonSach = 'CS001';
        
        COMMIT TRANSACTION;
        SELECT N'Giao dịch T2 hoàn thành thành công!' AS ThongBao, 1 AS IsSuccess, 0 AS ErrorCode;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        
        IF ERROR_NUMBER() = 1205
        BEGIN
            SELECT N'Giao dịch T2 bị SQL Server chọn làm NẠN NHÂN DEADLOCK (Victim - Error 1205) và tự động Rollback!' AS ThongBao, 0 AS IsSuccess, 1205 AS ErrorCode;
        END
        ELSE
        BEGIN
            SELECT N'Lỗi T2: ' + ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess, ERROR_NUMBER() AS ErrorCode;
        END
    END CATCH
END
GO

