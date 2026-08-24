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
END
GO


