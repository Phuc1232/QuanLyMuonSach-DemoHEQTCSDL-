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


-- =========================================================================
-- PHÒNG NGỪA DEADLOCK - ORDERING PROTOCOL (CS001 -> CS002)
-- =========================================================================
CREATE OR ALTER PROCEDURE sp_Demo_Deadlock_Prevention_T1
    @p_DelaySeconds INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @delayStr VARCHAR(10) = '00:00:' + RIGHT('00' + CAST(@p_DelaySeconds AS VARCHAR(2)), 2);
    BEGIN TRY
        BEGIN TRANSACTION;
        UPDATE CuonSach SET TinhTrang = 'CapNhat_T1_Safe' WHERE MaCuonSach = 'CS001';
        WAITFOR DELAY @delayStr;
        UPDATE CuonSach SET TinhTrang = 'HoanTat_T1_Safe' WHERE MaCuonSach = 'CS002';
        COMMIT TRANSACTION;
        SELECT N'Giao dịch T1 (Phòng ngừa) hoàn thành thành công!' AS ThongBao, 1 AS IsSuccess, 0 AS ErrorCode;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi T1: ' + ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess, ERROR_NUMBER() AS ErrorCode;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE sp_Demo_Deadlock_Prevention_T2
    @p_DelaySeconds INT = 5
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @delayStr VARCHAR(10) = '00:00:' + RIGHT('00' + CAST(@p_DelaySeconds AS VARCHAR(2)), 2);
    BEGIN TRY
        BEGIN TRANSACTION;
        -- Đổi thứ tự: Thay vì khóa CS002 trước, ta ép T2 khóa CS001 trước (Tuân thủ Ordering Protocol)
        UPDATE CuonSach SET TinhTrang = 'CapNhat_T2_Safe' WHERE MaCuonSach = 'CS001';
        WAITFOR DELAY @delayStr;
        UPDATE CuonSach SET TinhTrang = 'HoanTat_T2_Safe' WHERE MaCuonSach = 'CS002';
        COMMIT TRANSACTION;
        SELECT N'Giao dịch T2 (Phòng ngừa) hoàn thành thành công!' AS ThongBao, 1 AS IsSuccess, 0 AS ErrorCode;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi T2: ' + ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess, ERROR_NUMBER() AS ErrorCode;
    END CATCH
END
GO

-- =========================================================================
-- KỊCH BẢN DEADLOCK THỰC TẾ: LẬP PHIẾU MƯỢN SÁCH
-- =========================================================================
-- 1. Phiên bản MẶC ĐỊNH (Gây Lỗi): Duyệt sách theo đúng thứ tự thủ thư quét mã
CREATE OR ALTER PROCEDURE sp_LapPhieuMuon_Deadlock_Demo
    @p_Book1 VARCHAR(10),
    @p_Book2 VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Khóa cuốn thứ nhất
        UPDATE CuonSach SET TinhTrang = 'DangMuon' WHERE MaCuonSach = @p_Book1;
        
        -- Giả lập hệ thống xử lý lâu hoặc in phiếu (5 giây)
        WAITFOR DELAY '00:00:05';
        
        -- Khóa cuốn thứ hai
        UPDATE CuonSach SET TinhTrang = 'DangMuon' WHERE MaCuonSach = @p_Book2;
        
        COMMIT TRANSACTION;
        SELECT N'Lập phiếu mượn thành công (' + @p_Book1 + ', ' + @p_Book2 + ')!' AS ThongBao, 1 AS IsSuccess, 0 AS ErrorCode;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: ' + ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess, ERROR_NUMBER() AS ErrorCode;
    END CATCH
END
GO

-- 2. Phiên bản AN TOÀN (Phòng ngừa Deadlock): Ép duyệt sách theo thứ tự mã sách (Ordering Protocol)
CREATE OR ALTER PROCEDURE sp_LapPhieuMuon_Deadlock_Safe_Demo
    @p_Book1 VARCHAR(10),
    @p_Book2 VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @FirstBook VARCHAR(10) = CASE WHEN @p_Book1 < @p_Book2 THEN @p_Book1 ELSE @p_Book2 END;
    DECLARE @SecondBook VARCHAR(10) = CASE WHEN @p_Book1 > @p_Book2 THEN @p_Book1 ELSE @p_Book2 END;

    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Luôn khóa cuốn có mã nhỏ hơn trước (Tuân thủ Ordering Protocol)
        UPDATE CuonSach SET TinhTrang = 'DangMuon' WHERE MaCuonSach = @FirstBook;
        
        -- Giả lập hệ thống xử lý lâu (5 giây)
        WAITFOR DELAY '00:00:05';
        
        -- Khóa cuốn có mã lớn hơn sau
        UPDATE CuonSach SET TinhTrang = 'DangMuon' WHERE MaCuonSach = @SecondBook;
        
        COMMIT TRANSACTION;
        SELECT N'Lập phiếu mượn (An toàn) thành công (' + @p_Book1 + ', ' + @p_Book2 + ')!' AS ThongBao, 1 AS IsSuccess, 0 AS ErrorCode;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: ' + ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess, ERROR_NUMBER() AS ErrorCode;
    END CATCH
END
GO

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


USE [QuanLyMuonSach]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =========================================================================
-- STORED PROCEDURE: sp_LapPhieuMuon_v2
-- Mục đích: Mô phỏng nghiệp vụ Lập Phiếu Mượn thực tế gây ra xung đột Deadlock
-- Cơ chế: Khóa tài nguyên theo đúng thứ tự quét mã vạch của thủ thư
--         (KHÔNG áp dụng Ordering Protocol) và giả lập thời gian trễ hệ thống.
-- =========================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_LapPhieuMuon_v2]
    @p_MaPhieuMuon      VARCHAR(10),
    @p_MaDG             VARCHAR(10),
    @p_MaNV             VARCHAR(10), 
    @p_NgayHenTra       DATE,
    @p_DanhSachCuonSach dbo.DanhSachCuonSachType READONLY,
    @p_DelayGiay        INT = 5   -- Giả lập độ trễ giữa các lần khóa sách (mặc định 5s để dễ demo)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_DieuKien NVARCHAR(200);
    DECLARE @v_SoLuongKhongSan INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- =====================================================================
        -- 1. KHÓA TÀI NGUYÊN THEO THỨ TỰ QUÉT MÃ VẠCH (NGUYÊN NHÂN GÂY DEADLOCK)
        -- Sử dụng CURSOR để duyệt tuần tự từng cuốn sách theo đúng thứ tự nhập vào
        -- =====================================================================
        DECLARE @cur_MaCuonSach VARCHAR(10);
        DECLARE cur_Books CURSOR LOCAL FOR 
            SELECT MaCuonSach FROM @p_DanhSachCuonSach;
            
        OPEN cur_Books;
        FETCH NEXT FROM cur_Books INTO @cur_MaCuonSach;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Xin khóa cập nhật độc quyền (UPDLOCK, ROWLOCK) trên từng cuốn sách
            DECLARE @dummy VARCHAR(10);
            SELECT @dummy = MaCuonSach 
            FROM CuonSach WITH (UPDLOCK, ROWLOCK)
            WHERE MaCuonSach = @cur_MaCuonSach;

            -- Giả lập độ trễ hệ thống (in phiếu / gọi API / tải nặng)
            -- Tạo khoảng trống thời gian cho quầy khác chen vào xin khóa
            IF @p_DelayGiay > 0
            BEGIN
                DECLARE @waitStr VARCHAR(8) = '00:00:' + RIGHT('0' + CAST(@p_DelayGiay AS VARCHAR(2)), 2);
                WAITFOR DELAY @waitStr;
            END

            FETCH NEXT FROM cur_Books INTO @cur_MaCuonSach;
        END
        
        CLOSE cur_Books;
        DEALLOCATE cur_Books;

        -- =====================================================================
        -- 2. CÁC BƯỚC KIỂM TRA RÀNG BUỘC NGHIỆP VỤ
        -- =====================================================================
        -- B1: Kiểm tra điều kiện mượn của độc giả
        SET @v_DieuKien = dbo.fn_KiemTraDuDieuKienMuon(@p_MaDG);
        IF @v_DieuKien <> N'Đủ điều kiện mượn sách'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @v_DieuKien AS ThongBao, 0 AS IsSuccess, 1 AS ErrorCode;
            RETURN;
        END

        -- B2: Kiểm tra toàn bộ cuốn sách trong danh sách có sẵn để mượn không
        SELECT @v_SoLuongKhongSan = COUNT(*)
        FROM CuonSach cs WITH (UPDLOCK, ROWLOCK)
        JOIN @p_DanhSachCuonSach d ON cs.MaCuonSach = d.MaCuonSach
        WHERE cs.TrangThai <> 'CoSan';

        IF @v_SoLuongKhongSan > 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: có ít nhất 1 cuốn sách trong danh sách hiện không sẵn có' AS ThongBao, 0 AS IsSuccess, 2 AS ErrorCode;
            RETURN;
        END

        -- B3: Kiểm tra mượn trùng đầu sách trong danh sách yêu cầu
        IF EXISTS (
            SELECT cs.MaSach
            FROM CuonSach cs WITH (UPDLOCK, ROWLOCK)
            JOIN @p_DanhSachCuonSach d ON cs.MaCuonSach = d.MaCuonSach
            GROUP BY cs.MaSach
            HAVING COUNT(*) > 1
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Không được mượn nhiều bản sao của cùng một đầu sách trong một lần mượn' AS ThongBao, 0 AS IsSuccess, 3 AS ErrorCode;
            RETURN;
        END

        -- B4: Kiểm tra mượn trùng đầu sách với các sách ĐANG MƯỢN của độc giả
        IF EXISTS (
            SELECT 1
            FROM CuonSach cs_new WITH (UPDLOCK, ROWLOCK)
            JOIN @p_DanhSachCuonSach d ON cs_new.MaCuonSach = d.MaCuonSach
            JOIN CT_PhieuMuon ct ON ct.TinhTrangSachKhiTra = 'ChuaTra'
            JOIN PhieuMuon pm ON pm.MaPhieuMuon = ct.MaPhieuMuon
            JOIN CuonSach cs_old ON cs_old.MaCuonSach = ct.MaCuonSach
            WHERE pm.MaDG = @p_MaDG AND cs_old.MaSach = cs_new.MaSach
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Độc giả đang mượn một bản sao của đầu sách này rồi, không được mượn thêm' AS ThongBao, 0 AS IsSuccess, 4 AS ErrorCode;
            RETURN;
        END

        -- =====================================================================
        -- 3. GHI NHẬN GIAO DỊCH VÀO CSDL
        -- =====================================================================
        INSERT INTO PhieuMuon (MaPhieuMuon, MaDG, MaNV, NgayMuon, NgayHenTra, TrangThai)
        VALUES (@p_MaPhieuMuon, @p_MaDG, @p_MaNV, CAST(GETDATE() AS DATE), @p_NgayHenTra, 'DangMuon');

        INSERT INTO CT_PhieuMuon (MaPhieuMuon, MaCuonSach, TinhTrangSachKhiTra)
        SELECT @p_MaPhieuMuon, d.MaCuonSach, 'ChuaTra'
        FROM @p_DanhSachCuonSach d;

        UPDATE cs
        SET cs.TrangThai = 'DangMuon'
        FROM CuonSach cs
        JOIN @p_DanhSachCuonSach d ON cs.MaCuonSach = d.MaCuonSach;

        COMMIT TRANSACTION;
        SELECT N'Lập phiếu mượn thành công' AS ThongBao, 1 AS IsSuccess, 0 AS ErrorCode;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess, ERROR_NUMBER() AS ErrorCode;
    END CATCH
END
GO


USE [QuanLyMuonSach]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =========================================================================
-- STORED PROCEDURE: sp_LapPhieuMuon_Deadlock
-- Mục đích: Mô phỏng nghiệp vụ Lập Phiếu Mượn thực tế gây ra xung đột Deadlock
-- Cơ chế: Khóa tài nguyên theo đúng thứ tự quét mã vạch của thủ thư
--         (KHÔNG áp dụng Ordering Protocol) và giả lập thời gian trễ hệ thống.
-- =========================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_LapPhieuMuon_Deadlock]
    @p_MaPhieuMuon      VARCHAR(10),
    @p_MaDG             VARCHAR(10),
    @p_MaNV             VARCHAR(10), 
    @p_NgayHenTra       DATE,
    @p_DanhSachCuonSach dbo.DanhSachCuonSachType READONLY,
    @p_DelayGiay        INT = 5   -- Giả lập độ trễ giữa các lần khóa sách (mặc định 5s để dễ demo)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_DieuKien NVARCHAR(200);
    DECLARE @v_SoLuongKhongSan INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- =====================================================================
        -- 1. KHÓA TÀI NGUYÊN THEO THỨ TỰ QUÉT MÃ VẠCH (NGUYÊN NHÂN GÂY DEADLOCK)
        -- Sử dụng CURSOR để duyệt tuần tự từng cuốn sách theo đúng thứ tự nhập vào
        -- =====================================================================
        DECLARE @cur_MaCuonSach VARCHAR(10);
        DECLARE cur_Books CURSOR LOCAL FOR 
            SELECT MaCuonSach FROM @p_DanhSachCuonSach;
            
        OPEN cur_Books;
        FETCH NEXT FROM cur_Books INTO @cur_MaCuonSach;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Xin khóa cập nhật độc quyền (UPDLOCK, ROWLOCK) trên từng cuốn sách
            DECLARE @dummy VARCHAR(10);
            SELECT @dummy = MaCuonSach 
            FROM CuonSach WITH (UPDLOCK, ROWLOCK)
            WHERE MaCuonSach = @cur_MaCuonSach;

            -- Giả lập độ trễ hệ thống (in phiếu / gọi API / tải nặng)
            -- Tạo khoảng trống thời gian cho quầy khác chen vào xin khóa
            IF @p_DelayGiay > 0
            BEGIN
                DECLARE @waitStr VARCHAR(8) = '00:00:' + RIGHT('0' + CAST(@p_DelayGiay AS VARCHAR(2)), 2);
                WAITFOR DELAY @waitStr;
            END

            FETCH NEXT FROM cur_Books INTO @cur_MaCuonSach;
        END
        
        CLOSE cur_Books;
        DEALLOCATE cur_Books;

        -- =====================================================================
        -- 2. CÁC BƯỚC KIỂM TRA RÀNG BUỘC NGHIỆP VỤ
        -- =====================================================================
        -- B1: Kiểm tra điều kiện mượn của độc giả
        SET @v_DieuKien = dbo.fn_KiemTraDuDieuKienMuon(@p_MaDG);
        IF @v_DieuKien <> N'Đủ điều kiện mượn sách'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @v_DieuKien AS ThongBao, 0 AS IsSuccess, 1 AS ErrorCode;
            RETURN;
        END

        -- B2: Kiểm tra toàn bộ cuốn sách trong danh sách có sẵn để mượn không
        SELECT @v_SoLuongKhongSan = COUNT(*)
        FROM CuonSach cs WITH (UPDLOCK, ROWLOCK)
        JOIN @p_DanhSachCuonSach d ON cs.MaCuonSach = d.MaCuonSach
        WHERE cs.TrangThai <> 'CoSan';

        IF @v_SoLuongKhongSan > 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: có ít nhất 1 cuốn sách trong danh sách hiện không sẵn có' AS ThongBao, 0 AS IsSuccess, 2 AS ErrorCode;
            RETURN;
        END

        -- B3: Kiểm tra mượn trùng đầu sách trong danh sách yêu cầu
        IF EXISTS (
            SELECT cs.MaSach
            FROM CuonSach cs WITH (UPDLOCK, ROWLOCK)
            JOIN @p_DanhSachCuonSach d ON cs.MaCuonSach = d.MaCuonSach
            GROUP BY cs.MaSach
            HAVING COUNT(*) > 1
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Không được mượn nhiều bản sao của cùng một đầu sách trong một lần mượn' AS ThongBao, 0 AS IsSuccess, 3 AS ErrorCode;
            RETURN;
        END

        -- B4: Kiểm tra mượn trùng đầu sách với các sách ĐANG MƯỢN của độc giả
        IF EXISTS (
            SELECT 1
            FROM CuonSach cs_new WITH (UPDLOCK, ROWLOCK)
            JOIN @p_DanhSachCuonSach d ON cs_new.MaCuonSach = d.MaCuonSach
            JOIN CT_PhieuMuon ct ON ct.TinhTrangSachKhiTra = 'ChuaTra'
            JOIN PhieuMuon pm ON pm.MaPhieuMuon = ct.MaPhieuMuon
            JOIN CuonSach cs_old ON cs_old.MaCuonSach = ct.MaCuonSach
            WHERE pm.MaDG = @p_MaDG AND cs_old.MaSach = cs_new.MaSach
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Độc giả đang mượn một bản sao của đầu sách này rồi, không được mượn thêm' AS ThongBao, 0 AS IsSuccess, 4 AS ErrorCode;
            RETURN;
        END

        -- =====================================================================
        -- 3. GHI NHẬN GIAO DỊCH VÀO CSDL
        -- =====================================================================
        INSERT INTO PhieuMuon (MaPhieuMuon, MaDG, MaNV, NgayMuon, NgayHenTra, TrangThai)
        VALUES (@p_MaPhieuMuon, @p_MaDG, @p_MaNV, CAST(GETDATE() AS DATE), @p_NgayHenTra, 'DangMuon');

        INSERT INTO CT_PhieuMuon (MaPhieuMuon, MaCuonSach, TinhTrangSachKhiTra)
        SELECT @p_MaPhieuMuon, d.MaCuonSach, 'ChuaTra'
        FROM @p_DanhSachCuonSach d;

        UPDATE cs
        SET cs.TrangThai = 'DangMuon'
        FROM CuonSach cs
        JOIN @p_DanhSachCuonSach d ON cs.MaCuonSach = d.MaCuonSach;

        COMMIT TRANSACTION;
        SELECT N'Lập phiếu mượn thành công' AS ThongBao, 1 AS IsSuccess, 0 AS ErrorCode;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess, ERROR_NUMBER() AS ErrorCode;
    END CATCH
END
GO

-- Đồng bộ alias sp_LapPhieuMuon_v2 nếu có nơi gọi
CREATE OR ALTER PROCEDURE [dbo].[sp_LapPhieuMuon_v2]
    @p_MaPhieuMuon      VARCHAR(10),
    @p_MaDG             VARCHAR(10),
    @p_MaNV             VARCHAR(10), 
    @p_NgayHenTra       DATE,
    @p_DanhSachCuonSach dbo.DanhSachCuonSachType READONLY,
    @p_DelayGiay        INT = 5
AS
BEGIN
    EXEC [dbo].[sp_LapPhieuMuon_Deadlock]
        @p_MaPhieuMuon = @p_MaPhieuMuon,
        @p_MaDG = @p_MaDG,
        @p_MaNV = @p_MaNV,
        @p_NgayHenTra = @p_NgayHenTra,
        @p_DanhSachCuonSach = @p_DanhSachCuonSach,
        @p_DelayGiay = @p_DelayGiay;
END
GO

