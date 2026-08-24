USE [QuanLyMuonSach]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

---------------------------------------------------------------------
-- 1. KỊCH BẢN 1: MẤT BẢN CẬP NHẬT (LOST UPDATE)
---------------------------------------------------------------------
-- SP cập nhật tình trạng sách trực tiếp không dùng khóa
CREATE OR ALTER PROCEDURE [dbo].[sp_Demo_LostUpdate_CapNhat]
    @p_MaCuonSach VARCHAR(10),
    @p_TinhTrangMoi NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Cập nhật trực tiếp không kiểm tra điều kiện dữ liệu cũ
        UPDATE CuonSach 
        SET TinhTrang = @p_TinhTrangMoi 
        WHERE MaCuonSach = @p_MaCuonSach;

        COMMIT TRANSACTION;
        SELECT N'Cập nhật tình trạng sách thành công!' AS ThongBao, 1 AS IsSuccess;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi cập nhật: ' + ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess;
    END CATCH
END
GO

---------------------------------------------------------------------
-- 2. KỊCH BẢN 2: ĐỌC DỮ LIỆU RÁC (DIRTY READ)
---------------------------------------------------------------------
-- SP Độc giả tra cứu (READ UNCOMMITTED) - Đọc rác
CREATE OR ALTER PROCEDURE [dbo].[sp_Demo_DirtyRead_TraCuu]
    @p_MaCuonSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    -- Thiết lập mức cô lập READ UNCOMMITTED để cố tình đọc dữ liệu chưa chốt
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
    
    SELECT MaCuonSach, TrangThai, TinhTrang 
    FROM CuonSach 
    WHERE MaCuonSach = @p_MaCuonSach;
END
GO

---------------------------------------------------------------------
-- 3. KỊCH BẢN 3: ĐỌC KHÔNG NHẤT QUÁN (NON-REPEATABLE READ)
---------------------------------------------------------------------
-- SP Quản lý kiểm kê (READ COMMITTED) - Bị thay đổi kết quả giữa chừng
CREATE OR ALTER PROCEDURE [dbo].[sp_Demo_NonRepeatableRead_KiemKe]
    @p_MaSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    -- Mức cô lập mặc định READ COMMITTED không giữ Shared Lock sau khi đọc xong câu lệnh
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    
    SELECT COUNT(*) AS SoLuongCoSan
    FROM CuonSach 
    WHERE MaSach = @p_MaSach AND TrangThai = 'CoSan';
END
GO

-- SP Thủ thư cho mượn (để tác động thay đổi dữ liệu khi Quản lý đang kiểm kê)
CREATE OR ALTER PROCEDURE [dbo].[sp_Demo_NonRepeatableRead_ChoMuon]
    @p_MaCuonSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        UPDATE CuonSach 
        SET TrangThai = 'DangMuon' 
        WHERE MaCuonSach = @p_MaCuonSach AND TrangThai = 'CoSan';
        
        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Cuốn sách không sẵn có để mượn!' AS ThongBao, 0 AS IsSuccess;
            RETURN;
        END

        COMMIT TRANSACTION;
        SELECT N'Đã chuyển trạng thái cuốn sách thành DangMuon!' AS ThongBao, 1 AS IsSuccess;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: ' + ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess;
    END CATCH
---------------------------------------------------------------------
-- 4. KỊCH BẢN 4: BẾ TẮC (DEADLOCK - CROSS DEPENDENCY)
---------------------------------------------------------------------
-- Giao tác T1: Khóa CS001 trước -> Chờ -> Đòi khóa tiếp CS002
CREATE OR ALTER PROCEDURE [dbo].[sp_Demo_Deadlock_T1]
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
        
        PRINT N'[T1] Đã khóa thành công CS001. Đang giữ khóa và chờ ' + CAST(@p_DelaySeconds AS NVARCHAR(10)) + N' giây...';
        
        -- Giữ giao tác mở và tạm dừng để T2 kịp chiếm khóa CS002
        WAITFOR DELAY @delayStr;
        
        -- Bước 2: T1 cố gắng chiếm khóa tiếp cuốn sách CS002 (lúc này đang bị T2 nắm giữ)
        PRINT N'[T1] Đang yêu cầu khóa CS002...';
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

-- Giao tác T2: Khóa CS002 trước -> Chờ -> Đòi khóa tiếp CS001
CREATE OR ALTER PROCEDURE [dbo].[sp_Demo_Deadlock_T2]
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
        
        PRINT N'[T2] Đã khóa thành công CS002. Đang giữ khóa và chờ ' + CAST(@p_DelaySeconds AS NVARCHAR(10)) + N' giây...';
        
        -- Giữ giao tác mở và tạm dừng để T1 kịp chiếm khóa CS001
        WAITFOR DELAY @delayStr;
        
        -- Bước 2: T2 cố gắng chiếm khóa tiếp cuốn sách CS001 (lúc này đang bị T1 nắm giữ)
        PRINT N'[T2] Đang yêu cầu khóa CS001...';
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


