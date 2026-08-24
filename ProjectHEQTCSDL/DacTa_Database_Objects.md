# ĐẶC TẢ CHI TIẾT CÁC ĐỐI TƯỢNG CƠ SỞ DỮ LIỆU (DATABASE OBJECTS SPECIFICATION)
## HỆ THỐNG QUẢN LÝ ĐỘC GIẢ MƯỢN SÁCH (`QuanLyMuonSach`)


---

## MỤC LỤC
1. [Table-Valued Types (Kiểu dữ liệu bảng TVP)](#1-table-valued-types-tvp)
2. [Functions (Hàm tính toán & Kiểm tra điều kiện)](#2-functions-hàm-tính-toán--kiểm-tra)
3. [Giao tác Nghiệp vụ Lõi (Core Transaction Stored Procedures)](#3-giao-tác-nghiệp-vụ-lõi-transactions)
4. [Stored Procedures Tra cứu & Báo cáo Thống kê](#4-stored-procedures-tra-cứu--báo-cáo)
5. [Views (Khung nhìn Báo cáo Tổng hợp)](#5-views-khung-nhìn-tổng-hợp)
6. [Triggers (Ràng buộc Toàn vẹn Tầng CSDL)](#6-triggers-ràng-buộc-toàn-vẹn)
7. [Stored Procedures Mô phỏng Tương tranh](#7-stored-procedures-mô-phỏng-tương-tranh)

---

## 1. TABLE-VALUED TYPES (TVP)

Dùng làm tham số đầu vào dạng danh sách cho các Stored Procedure (Lập phiếu mượn nhiều cuốn, Trả nhiều cuốn cùng lúc).

### 1.1. `dbo.DanhSachCuonSachType`
* **Mục đích:** Truyền danh sách mã cuốn sách khi Lập phiếu mượn.
```sql
CREATE TYPE dbo.DanhSachCuonSachType AS TABLE (
    MaCuonSach VARCHAR(10) NOT NULL
);
GO
```

### 1.2. `dbo.DanhSachTraSachType`
* **Mục đích:** Truyền danh sách mã cuốn sách kèm tình trạng thực tế khi Trả sách (`ConTot`, `HuHong`, `Mat`).
```sql
CREATE TYPE dbo.DanhSachTraSachType AS TABLE (
    MaCuonSach VARCHAR(10) NOT NULL,
    TinhTrang  VARCHAR(20) NOT NULL
);
GO
```

---

## 2. FUNCTIONS (HÀM TÍNH TOÁN & KIỂM TRA)

### 2.1. `fn_DemSachDangMuon`
* **Vai trò:** Đếm chính xác số lượng cuốn sách vật lý mà một độc giả đang mượn chưa trả.
* **Tham số:** `@p_MaDG VARCHAR(10)`
* **Giá trị trả về:** `INT`
```sql
CREATE OR ALTER FUNCTION fn_DemSachDangMuon (@p_MaDG VARCHAR(10))
RETURNS INT
AS
BEGIN
    DECLARE @v_SoLuong INT;
    SELECT @v_SoLuong = COUNT(ct.MaCuonSach)
    FROM PhieuMuon pm
    JOIN CT_PhieuMuon ct ON pm.MaPhieuMuon = ct.MaPhieuMuon
    WHERE pm.MaDG = @p_MaDG
      AND pm.TrangThai IN ('DangMuon', 'QuaHan')
      AND ct.TinhTrangSachKhiTra = 'ChuaTra';
    RETURN ISNULL(@v_SoLuong, 0);
END
GO
```

### 2.2. `fn_KiemTraDuDieuKienMuon`
* **Vai trò:** Kiểm tra xem thẻ độc giả có còn hạn và số sách đang mượn có vượt quá hạn mức (tối đa 3 cuốn) hay không.
* **Tham số:** `@p_MaDG VARCHAR(10)`
* **Giá trị trả về:** `NVARCHAR(100)`
```sql
CREATE OR ALTER FUNCTION fn_KiemTraDuDieuKienMuon (@p_MaDG VARCHAR(10))
RETURNS NVARCHAR(100)
AS
BEGIN
    DECLARE @v_TrangThai VARCHAR(20);
    DECLARE @v_SoSachDangMuon INT;
    DECLARE @v_GioiHan INT = 3;

    SELECT @v_TrangThai = TrangThai FROM DocGia WHERE MaDG = @p_MaDG;
    SET @v_SoSachDangMuon = dbo.fn_DemSachDangMuon(@p_MaDG);

    RETURN CASE 
        WHEN @v_TrangThai IS NULL THEN N'Không tìm thấy độc giả'
        WHEN @v_TrangThai <> 'ConHan' THEN N'Không đủ điều kiện: thẻ không còn hạn sử dụng hoặc đang bị khóa'
        WHEN @v_SoSachDangMuon >= @v_GioiHan THEN N'Không đủ điều kiện: đã mượn tối đa số sách cho phép (3 cuốn)'
        ELSE N'Đủ điều kiện mượn sách'
    END;
END
GO
```

### 2.3. `fn_TinhSoNgayTre`
* **Vai trò:** Tính số ngày trễ hạn của 1 phiếu mượn dựa trên ngày hẹn trả và ngày trả thực tế trong chi tiết phiếu mượn (nếu chưa trả thì so sánh với ngày hiện tại).
* **Tham số:** `@p_MaPhieuMuon VARCHAR(10)`
* **Giá trị trả về:** `INT`
```sql
CREATE OR ALTER FUNCTION fn_TinhSoNgayTre (@p_MaPhieuMuon VARCHAR(10))
RETURNS INT
AS
BEGIN
    DECLARE @v_NgayHenTra DATE;
    DECLARE @v_NgaySoSanh DATE;
    DECLARE @v_SoNgayTre INT;

    SELECT TOP 1
        @v_NgayHenTra = pm.NgayHenTra,
        @v_NgaySoSanh = ISNULL(ct.NgayTraThucTe, CAST(GETDATE() AS DATE))
    FROM PhieuMuon pm
    JOIN CT_PhieuMuon ct ON pm.MaPhieuMuon = ct.MaPhieuMuon
    WHERE pm.MaPhieuMuon = @p_MaPhieuMuon
    ORDER BY ct.NgayTraThucTe DESC;

    SET @v_SoNgayTre = DATEDIFF(DAY, @v_NgayHenTra, @v_NgaySoSanh);

    RETURN CASE WHEN @v_SoNgayTre > 0 THEN @v_SoNgayTre ELSE 0 END;
END
GO
```

### 2.4. `fn_TinhTienPhatTreHan`
* **Vai trò:** Tính tiền phạt quá hạn theo công thức: `5.000 VNĐ x Số ngày trễ`.
* **Tham số:** `@p_SoNgayTre INT`
* **Giá trị trả về:** `DECIMAL(18,0)`
```sql
CREATE OR ALTER FUNCTION fn_TinhTienPhatTreHan (@p_SoNgayTre INT)
RETURNS DECIMAL(18,0)
AS
BEGIN
    IF @p_SoNgayTre <= 0 RETURN 0;
    RETURN @p_SoNgayTre * 5000;
END
GO
```

---

## 3. GIAO TÁC NGHIỆP VỤ LÕI (TRANSACTIONS)

### 3.1. Giao tác Lập phiếu mượn sách (`sp_LapPhieuMuon`)
* **Mô tả:** Kiểm tra điều kiện thẻ độc giả, kiểm tra các cuốn sách chọn mượn có đang `CoSan` hay không (dùng khóa `UPDLOCK, ROWLOCK`), tạo bản ghi `PhieuMuon`, tạo các dòng `CT_PhieuMuon`, và cập nhật trạng thái `CuonSach` thành `DangMuon`.
```sql
CREATE OR ALTER PROCEDURE sp_LapPhieuMuon
    @p_MaPhieuMuon      VARCHAR(10),
    @p_MaDG             VARCHAR(10),
    @p_MaNV             VARCHAR(10), 
    @p_NgayHenTra       DATE,
    @p_DanhSachCuonSach dbo.DanhSachCuonSachType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_DieuKien NVARCHAR(200);
    DECLARE @v_SoLuongKhongSan INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- B1: Kiểm tra điều kiện mượn của độc giả
        SET @v_DieuKien = dbo.fn_KiemTraDuDieuKienMuon(@p_MaDG);
        IF @v_DieuKien <> N'Đủ điều kiện mượn sách'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @v_DieuKien AS ThongBao;
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
            SELECT N'Lỗi: có ít nhất 1 cuốn sách trong danh sách hiện không sẵn có' AS ThongBao;
            RETURN;
        END

        -- Kiểm tra mượn trùng đầu sách trong danh sách yêu cầu
        IF EXISTS (
            SELECT cs.MaSach
            FROM CuonSach cs WITH (UPDLOCK, ROWLOCK)
            JOIN @p_DanhSachCuonSach d ON cs.MaCuonSach = d.MaCuonSach
            GROUP BY cs.MaSach
            HAVING COUNT(*) > 1
        )
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Không được mượn nhiều bản sao của cùng một đầu sách trong một lần mượn' AS ThongBao;
            RETURN;
        END

        -- Kiểm tra mượn trùng đầu sách với các sách ĐANG MƯỢN của độc giả
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
            SELECT N'Lỗi: Độc giả đang mượn một bản sao của đầu sách này rồi, không được mượn thêm' AS ThongBao;
            RETURN;
        END


        -- B3: Tạo phiếu mượn
        INSERT INTO PhieuMuon (MaPhieuMuon, MaDG, MaNV, NgayMuon, NgayHenTra, TrangThai)
        VALUES (@p_MaPhieuMuon, @p_MaDG, @p_MaNV, CAST(GETDATE() AS DATE), @p_NgayHenTra, 'DangMuon');

        -- B4: Tạo chi tiết phiếu mượn cho từng cuốn sách
        INSERT INTO CT_PhieuMuon (MaPhieuMuon, MaCuonSach, TinhTrangSachKhiTra)
        SELECT @p_MaPhieuMuon, d.MaCuonSach, 'ChuaTra'
        FROM @p_DanhSachCuonSach d;

        -- B5: Cập nhật trạng thái từng cuốn sách sang DangMuon
        UPDATE cs
        SET cs.TrangThai = 'DangMuon'
        FROM CuonSach cs
        JOIN @p_DanhSachCuonSach d ON cs.MaCuonSach = d.MaCuonSach;

        COMMIT TRANSACTION;
        SELECT N'Lập phiếu mượn thành công' AS ThongBao;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Giao tác lập phiếu mượn thất bại, đã rollback' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
```

### 3.2. Giao tác Trả sách & Phát sinh phạt (`sp_TraSach`)
* **Mô tả:** Đóng trạng thái `PhieuMuon` thành `DaTra`, cập nhật ngày trả thực tế và tình trạng vào `CT_PhieuMuon`, cập nhật trạng thái kho của `CuonSach`, tự động sinh `PhieuPhat` trễ hạn và phạt hư hỏng/mất sách nếu có.
```sql
CREATE OR ALTER PROCEDURE sp_TraSach
    @p_MaPhieuMuon      VARCHAR(10),
    @p_DanhSachTraSach  dbo.DanhSachTraSachType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_NgayHenTra DATE;
    DECLARE @v_SoNgayTre INT;
    DECLARE @v_MaPhieuPhat VARCHAR(10);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @v_NgayHenTra = NgayHenTra
        FROM PhieuMuon WITH (UPDLOCK, ROWLOCK)
        WHERE MaPhieuMuon = @p_MaPhieuMuon;

        SET @v_SoNgayTre = DATEDIFF(DAY, @v_NgayHenTra, CAST(GETDATE() AS DATE));

        -- B1: Cập nhật trạng thái phiếu mượn 
        UPDATE PhieuMuon
        SET TrangThai = 'DaTra'
        WHERE MaPhieuMuon = @p_MaPhieuMuon;

        -- B2: Cập nhật tình trạng & ngày trả vào CT_PhieuMuon
        UPDATE ct
        SET ct.TinhTrangSachKhiTra = d.TinhTrang,
            ct.NgayTraThucTe = CAST(GETDATE() AS DATE)
        FROM CT_PhieuMuon ct
        JOIN @p_DanhSachTraSach d ON ct.MaCuonSach = d.MaCuonSach
        WHERE ct.MaPhieuMuon = @p_MaPhieuMuon;

        -- B3: Cập nhật trạng thái từng cuốn sách vật lý
        UPDATE cs
        SET cs.TinhTrang = d.TinhTrang,
            cs.TrangThai = CASE WHEN d.TinhTrang = 'ConTot' THEN 'CoSan' ELSE 'DangMuon' END
        FROM CuonSach cs
        JOIN @p_DanhSachTraSach d ON cs.MaCuonSach = d.MaCuonSach;

        -- B4: Phát sinh phiếu phạt trễ hạn (nếu có)
        IF @v_SoNgayTre > 0
        BEGIN
            SET @v_MaPhieuPhat = CONCAT('PP', RIGHT('0000' + CAST(FLOOR(RAND()*9999) AS VARCHAR(4)), 4));
            
            INSERT INTO PhieuPhat (MaPhieuPhat, MaPhieuMuon, LyDoPhat, SoTienPhat, NgayLap, TrangThaiThanhToan)
            VALUES (@v_MaPhieuPhat, @p_MaPhieuMuon, 'TreHan', 
                    dbo.fn_TinhTienPhatTreHan(@v_SoNgayTre), CAST(GETDATE() AS DATE), 'ChuaThanhToan');
        END

        -- B5: Phát sinh phiếu phạt cho từng cuốn sách hư hỏng/mất
        INSERT INTO PhieuPhat (MaPhieuPhat, MaPhieuMuon, LyDoPhat, SoTienPhat, NgayLap, TrangThaiThanhToan)
        SELECT 
            CONCAT('PP', RIGHT('0000' + CAST(FLOOR(RAND()*9999) AS VARCHAR(4)), 4)),
            @p_MaPhieuMuon,
            CASE WHEN d.TinhTrang = 'Mat' THEN 'MatSach' ELSE 'HuHong' END,
            CASE WHEN d.TinhTrang = 'Mat' THEN 100000 ELSE 30000 END,
            CAST(GETDATE() AS DATE),
            'ChuaThanhToan'
        FROM @p_DanhSachTraSach d
        WHERE d.TinhTrang IN ('HuHong', 'Mat');

        COMMIT TRANSACTION;
        SELECT N'Trả sách thành công' AS ThongBao;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Giao tác trả sách thất bại, đã rollback' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
```

### 3.3. Giao tác Đặt trước đầu sách (`sp_DatTruocSach`)
* **Mô tả:** Độc giả đặt trước khi kho sách đã hết bản sao sẵn có (`CoSan = 0`). Xếp độc giả vào hàng chờ `DangCho`.
```sql
CREATE OR ALTER PROCEDURE sp_DatTruocSach
    @p_MaPhieuDatTruoc VARCHAR(10),
    @p_MaDG            VARCHAR(10),
    @p_MaSach          VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_SoLuongCoSan INT;
    DECLARE @v_TrangThaiThe VARCHAR(20);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @v_TrangThaiThe = TrangThai FROM DocGia WHERE MaDG = @p_MaDG;
        IF @v_TrangThaiThe <> 'ConHan'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Thẻ độc giả không còn hạn sử dụng hoặc đang bị khóa' AS ThongBao;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM PhieuDatTruoc WHERE MaDG = @p_MaDG AND MaSach = @p_MaSach AND TrangThai IN ('DangCho', 'ChoNhan'))
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Bạn đã đặt trước đầu sách này rồi, vui lòng không đặt trùng lặp' AS ThongBao;
            RETURN;
        END

        SELECT @v_SoLuongCoSan = COUNT(*)
        FROM CuonSach WITH (UPDLOCK, ROWLOCK)
        WHERE MaSach = @p_MaSach AND TrangThai = 'CoSan';

        IF @v_SoLuongCoSan > 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Sách hiện vẫn còn bản sao có sẵn trên kệ, vui lòng mượn trực tiếp' AS ThongBao;
            RETURN;
        END

        INSERT INTO PhieuDatTruoc (MaPhieuDatTruoc, MaDG, MaSach, NgayDat, TrangThai)
        VALUES (@p_MaPhieuDatTruoc, @p_MaDG, @p_MaSach, CAST(GETDATE() AS DATE), 'DangCho');

        COMMIT TRANSACTION;
        SELECT N'Đặt trước thành công! Bạn đã được xếp vào hàng chờ.' AS ThongBao;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Giao tác đặt trước thất bại' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
```

### 3.4. Giao tác Xử lý Giữ chỗ 48h khi có sách trả về (`sp_XuLyDatTruocKhiCoSach`)
* **Mô tả:** Khi có sách trả về, quét tìm người đặt trước sớm nhất. Chuyển sách thành `GiuCho` (cất tại quầy) và phiếu đặt trước thành `ChoNhan` (hẹn trong 48h), KHÔNG tạo phiếu mượn ngay.
```sql
CREATE OR ALTER PROCEDURE sp_XuLyDatTruocKhiCoSach
    @p_MaCuonSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_MaSach          VARCHAR(10);
    DECLARE @v_MaPhieuDatTruoc VARCHAR(10);
    DECLARE @v_MaDG            VARCHAR(10);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @v_MaSach = MaSach
        FROM CuonSach WITH (UPDLOCK, ROWLOCK)
        WHERE MaCuonSach = @p_MaCuonSach;

        SELECT TOP 1 
            @v_MaPhieuDatTruoc = MaPhieuDatTruoc,
            @v_MaDG = MaDG
        FROM PhieuDatTruoc WITH (UPDLOCK, ROWLOCK)
        WHERE MaSach = @v_MaSach AND TrangThai = 'DangCho'
        ORDER BY NgayDat ASC;

        IF @v_MaPhieuDatTruoc IS NULL
        BEGIN
            UPDATE CuonSach SET TrangThai = 'CoSan' WHERE MaCuonSach = @p_MaCuonSach;
            COMMIT TRANSACTION;
            SELECT N'Không có ai đặt trước cuốn này. Sách đã được trả về kệ có sẵn.' AS ThongBao;
        END
        ELSE
        BEGIN
            UPDATE CuonSach SET TrangThai = 'GiuCho' WHERE MaCuonSach = @p_MaCuonSach;

            UPDATE PhieuDatTruoc 
            SET TrangThai = 'ChoNhan'
            WHERE MaPhieuDatTruoc = @v_MaPhieuDatTruoc;

            COMMIT TRANSACTION;
            SELECT CONCAT(N'Đã chuyển sách ', @p_MaCuonSach, N' sang trạng thái GIỮ CHỖ (48h) cho độc giả ', @v_MaDG) AS ThongBao;
        END

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Xử lý giữ chỗ thất bại' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
```

### 3.5. Giao tác Độc giả đến quầy nhận sách giữ chỗ (`sp_NhanSachDatTruoc`)
* **Mô tả:** Độc giả đến xuất trình thẻ và mã đặt trước $\rightarrow$ Lúc này mới chính thức tạo `PhieuMuon`, `CT_PhieuMuon`, chuyển sách sang `DangMuon` và hoàn tất phiếu đặt trước `DaXuLy`.
```sql
CREATE OR ALTER PROCEDURE sp_NhanSachDatTruoc
    @p_MaPhieuDatTruoc VARCHAR(10),
    @p_MaCuonSach      VARCHAR(10),
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

        SELECT @v_MaDG = MaDG, @v_MaSach = MaSach, @v_TrangThai = TrangThai
        FROM PhieuDatTruoc WITH (UPDLOCK, ROWLOCK)
        WHERE MaPhieuDatTruoc = @p_MaPhieuDatTruoc;

        IF @v_TrangThai IS NULL OR @v_TrangThai <> 'ChoNhan'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Phiếu đặt trước không tồn tại hoặc chưa có sách sẵn sàng để nhận' AS ThongBao;
            RETURN;
        END

        SET @v_DieuKien = dbo.fn_KiemTraDuDieuKienMuon(@v_MaDG);
        IF @v_DieuKien <> N'Đủ điều kiện mượn sách'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @v_DieuKien AS ThongBao;
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM CuonSach WHERE MaCuonSach = @p_MaCuonSach AND MaSach = @v_MaSach AND TrangThai = 'GiuCho')
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Cuốn sách này không ở trạng thái Giữ chỗ hoặc không thuộc đầu sách đã đặt' AS ThongBao;
            RETURN;
        END

        INSERT INTO PhieuMuon (MaPhieuMuon, MaDG, MaNV, NgayMuon, NgayHenTra, TrangThai)
        VALUES (@p_MaPhieuMuon, @v_MaDG, @p_MaNV, CAST(GETDATE() AS DATE), @p_NgayHenTra, 'DangMuon');

        INSERT INTO CT_PhieuMuon (MaPhieuMuon, MaCuonSach, TinhTrangSachKhiTra)
        VALUES (@p_MaPhieuMuon, @p_MaCuonSach, 'ChuaTra');

        UPDATE CuonSach SET TrangThai = 'DangMuon' WHERE MaCuonSach = @p_MaCuonSach;
        UPDATE PhieuDatTruoc SET TrangThai = 'DaXuLy' WHERE MaPhieuDatTruoc = @p_MaPhieuDatTruoc;

        COMMIT TRANSACTION;
        SELECT N'Độc giả đã nhận sách và lập phiếu mượn thành công!' AS ThongBao;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Nhận sách đặt trước thất bại' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
```

### 3.6. Giao tác Hủy giữ chỗ quá hạn 48h (`sp_HuyGiuChoHetHan`)
* **Mô tả:** Hủy đặt trước quá hạn 48h (`DaHuy`), tự động chuyển cuốn sách đó cho người tiếp theo trong hàng chờ hoặc trả về kệ `CoSan`.
```sql
CREATE OR ALTER PROCEDURE sp_HuyGiuChoHetHan
    @p_MaPhieuDatTruoc VARCHAR(10),
    @p_MaCuonSach      VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE PhieuDatTruoc 
        SET TrangThai = 'DaHuy' 
        WHERE MaPhieuDatTruoc = @p_MaPhieuDatTruoc AND TrangThai = 'ChoNhan';

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Phiếu đặt trước không ở trạng thái Chờ nhận hoặc không tồn tại' AS ThongBao;
            RETURN;
        END

        -- Tái điều chuyển cuốn sách vừa nhả ra
        EXEC sp_XuLyDatTruocKhiCoSach @p_MaCuonSach = @p_MaCuonSach;

        COMMIT TRANSACTION;
        SELECT N'Đã hủy giữ chỗ quá hạn và điều chuyển sách thành công!' AS ThongBao;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Hủy giữ chỗ thất bại' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
```

### 3.7. Giao tác Thanh toán phiếu phạt (`sp_ThanhToanPhat`)
* **Mô tả:** Thu tiền phạt và chuyển trạng thái từ `ChuaThanhToan` $\rightarrow$ `DaThanhToan`.
```sql
CREATE OR ALTER PROCEDURE sp_ThanhToanPhat
    @p_MaPhieuPhat VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        UPDATE PhieuPhat
        SET TrangThaiThanhToan = 'DaThanhToan'
        WHERE MaPhieuPhat = @p_MaPhieuPhat AND TrangThaiThanhToan = 'ChuaThanhToan';

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Phiếu phạt không tồn tại hoặc đã thanh toán rồi' AS ThongBao;
        END
        ELSE
        BEGIN
            COMMIT TRANSACTION;
            SELECT N'Thanh toán thành công' AS ThongBao;
        END
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Thanh toán thất bại' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
```

### 3.8. Giao tác Cập nhật thông tin độc giả (`sp_CapNhatThongTinDocGia`)
* **Mô tả:** Cập nhật thông tin cá nhân của độc giả và cập nhật mật khẩu bảng `TaiKhoan` (nếu có nhập mật khẩu mới).
```sql
CREATE OR ALTER PROCEDURE sp_CapNhatThongTinDocGia
    @p_MaDG       VARCHAR(10),
    @p_HoTen      NVARCHAR(100),
    @p_NgaySinh   DATE,
    @p_DiaChi     NVARCHAR(200),
    @p_SDT        VARCHAR(15),
    @p_Email      VARCHAR(100),
    @p_MatKhauMoi VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        UPDATE DocGia
        SET HoTen = @p_HoTen,
            NgaySinh = @p_NgaySinh,
            DiaChi = @p_DiaChi,
            SDT = @p_SDT,
            Email = @p_Email
        WHERE MaDG = @p_MaDG;
        
        IF @p_MatKhauMoi IS NOT NULL AND LTRIM(RTRIM(@p_MatKhauMoi)) <> ''
        BEGIN
            UPDATE tk
            SET tk.MatKhau = @p_MatKhauMoi
            FROM TaiKhoan tk
            JOIN DocGia dg ON tk.MaTaiKhoan = dg.MaTaiKhoan
            WHERE dg.MaDG = @p_MaDG;
        END

        COMMIT TRANSACTION;
        SELECT N'Cập nhật thông tin cá nhân thành công!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Cập nhật thông tin thất bại' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
```

### 3.9. Giao tác Cập nhật thông tin nhân viên (`sp_CapNhatThongTinNhanVien`)
* **Mô tả:** Cập nhật thông tin cá nhân của nhân viên/thủ thư và cập nhật mật khẩu bảng `TaiKhoan`.
```sql
CREATE OR ALTER PROCEDURE sp_CapNhatThongTinNhanVien
    @p_MaNV       VARCHAR(10),
    @p_HoTen      NVARCHAR(100),
    @p_SDT        VARCHAR(15),
    @p_Email      VARCHAR(100),
    @p_MatKhauMoi VARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        
        UPDATE NhanVien
        SET HoTen = @p_HoTen,
            SDT = @p_SDT,
            Email = @p_Email
        WHERE MaNV = @p_MaNV;
        
        IF @p_MatKhauMoi IS NOT NULL AND LTRIM(RTRIM(@p_MatKhauMoi)) <> ''
        BEGIN
            UPDATE tk
            SET tk.MatKhau = @p_MatKhauMoi
            FROM TaiKhoan tk
            JOIN NhanVien nv ON tk.MaTaiKhoan = nv.MaTaiKhoan
            WHERE nv.MaNV = @p_MaNV;
        END

        COMMIT TRANSACTION;
        SELECT N'Cập nhật thông tin cá nhân thành công!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Cập nhật thông tin thất bại' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
```

---

## 4. STORED PROCEDURES TRA CỨU & BÁO CÁO

### 4.1. `sp_TraCuuSach`
* **Mô tả:** Tra cứu sách theo từ khóa (tên sách hoặc tên tác giả), JOIN đúng qua bảng trung gian `Sach_TacGia`.
```sql
CREATE OR ALTER PROCEDURE sp_TraCuuSach
    @p_TuKhoa NVARCHAR(100) = N''
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.MaSach, s.TenSach, 
           ISNULL(tg.TenTacGia, N'Chưa rõ') AS TenTacGia, 
           ISNULL(tl.TenTheLoai, N'Khác') AS TenTheLoai, 
           ISNULL(v.SoBanConTrong, 0) AS SoBanConTrong
    FROM Sach s
    LEFT JOIN Sach_TacGia stg ON s.MaSach = stg.MaSach     
    LEFT JOIN TacGia tg ON stg.MaTacGia = tg.MaTacGia      
    LEFT JOIN TheLoai tl ON s.MaTheLoai = tl.MaTheLoai
    LEFT JOIN View_SachConTrong v ON s.MaSach = v.MaSach
    WHERE @p_TuKhoa IS NULL OR @p_TuKhoa = ''
       OR s.MaSach LIKE N'%' + @p_TuKhoa + N'%'
       OR s.TenSach LIKE N'%' + @p_TuKhoa + N'%' 
       OR tg.TenTacGia LIKE N'%' + @p_TuKhoa + N'%'
       OR tl.TenTheLoai LIKE N'%' + @p_TuKhoa + N'%';
END
GO
```

### 4.2. `sp_Top5SachMuonNhieu`
* **Mô tả:** Thống kê 5 đầu sách có số lượt mượn nhiều nhất.
```sql
CREATE OR ALTER PROCEDURE sp_Top5SachMuonNhieu
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 5 s.TenSach, COUNT(ct.MaCuonSach) AS SoLuotMuon
    FROM PhieuMuon pm
    JOIN CT_PhieuMuon ct ON pm.MaPhieuMuon = ct.MaPhieuMuon
    JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach
    JOIN Sach s ON cs.MaSach = s.MaSach
    GROUP BY s.MaSach, s.TenSach
    ORDER BY SoLuotMuon DESC;
END
GO
```

### 4.3. `sp_BaoCaoTienPhat`
* **Mô tả:** Báo cáo tổng tiền phạt thu được theo từng lý do phạt trong khoảng thời gian.
```sql
CREATE OR ALTER PROCEDURE sp_BaoCaoTienPhat
    @p_TuNgay DATE,
    @p_DenNgay DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT LyDoPhat, COUNT(*) AS SoLuong, SUM(SoTienPhat) AS TongTien
    FROM PhieuPhat
    WHERE NgayLap BETWEEN @p_TuNgay AND @p_DenNgay 
      AND TrangThaiThanhToan = 'DaThanhToan'
    GROUP BY LyDoPhat;
END
GO
```

---

## 5. VIEWS (KHUNG NHÌN TỔNG HỢP)

### 5.1. `View_ThongTinMuonSach`
* **Mô tả:** Xem toàn cảnh thông tin mượn sách, hiển thị tên độc giả, tên sách, tên nhân viên lập phiếu và ngày trả thực tế.
```sql
CREATE OR ALTER VIEW View_ThongTinMuonSach AS
SELECT 
    pm.MaPhieuMuon, dg.HoTen AS TenDocGia, s.TenSach, cs.MaCuonSach, 
    nv.HoTen AS NhanVienLapPhieu, 
    pm.NgayMuon, pm.NgayHenTra, ct.NgayTraThucTe, pm.TrangThai 
FROM PhieuMuon pm
JOIN DocGia dg ON pm.MaDG = dg.MaDG
JOIN CT_PhieuMuon ct ON pm.MaPhieuMuon = ct.MaPhieuMuon
JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach
JOIN Sach s ON cs.MaSach = s.MaSach
JOIN NhanVien nv ON pm.MaNV = nv.MaNV; 
GO
```

### 5.2. `View_SachConTrong`
* **Mô tả:** Đếm tổng số bản in và số lượng bản sao đang `CoSan` trên kệ theo từng đầu sách.
```sql
CREATE OR ALTER VIEW View_SachConTrong AS
SELECT 
    s.MaSach, s.TenSach, 
    COUNT(cs.MaCuonSach) AS TongSoBanSao,
    SUM(CASE WHEN cs.TrangThai = 'CoSan' THEN 1 ELSE 0 END) AS SoBanConTrong
FROM Sach s
LEFT JOIN CuonSach cs ON s.MaSach = cs.MaSach
GROUP BY s.MaSach, s.TenSach;
GO
```

### 5.3. `View_PhieuQuaHan`
* **Mô tả:** Danh sách các phiếu mượn đã quá hạn kèm thông tin liên lạc của độc giả để thủ thư gọi điện đôn đốc.
```sql
CREATE OR ALTER VIEW View_PhieuQuaHan AS
SELECT 
    pm.MaPhieuMuon, dg.HoTen, dg.SDT, dg.Email, s.TenSach, pm.NgayHenTra,
    DATEDIFF(DAY, pm.NgayHenTra, CAST(GETDATE() AS DATE)) AS SoNgayTre
FROM PhieuMuon pm
JOIN DocGia dg ON pm.MaDG = dg.MaDG
JOIN CT_PhieuMuon ct ON pm.MaPhieuMuon = ct.MaPhieuMuon
JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach
JOIN Sach s ON cs.MaSach = s.MaSach
WHERE pm.TrangThai IN ('DangMuon', 'QuaHan') 
  AND ct.TinhTrangSachKhiTra = 'ChuaTra' 
  AND pm.NgayHenTra < CAST(GETDATE() AS DATE);
GO
```

### 5.4. `View_ThongKeTheoTheLoai`
* **Mô tả:** Thống kê tổng số lượt mượn theo từng thể loại sách.
```sql
CREATE OR ALTER VIEW View_ThongKeTheoTheLoai AS
SELECT 
    tl.TenTheLoai, COUNT(pm.MaPhieuMuon) AS SoLuotMuon
FROM TheLoai tl
JOIN Sach s ON tl.MaTheLoai = s.MaTheLoai
JOIN CuonSach cs ON s.MaSach = cs.MaSach
JOIN CT_PhieuMuon ct ON cs.MaCuonSach = ct.MaCuonSach
JOIN PhieuMuon pm ON ct.MaPhieuMuon = pm.MaPhieuMuon
GROUP BY tl.MaTheLoai, tl.TenTheLoai;
GO
```


### 5.5. `View_ChiTietSachMuon`
* **Mô tả:** Lấy chi tiết sách đang mượn (chưa trả) của một phiếu mượn.
```sql
CREATE OR ALTER VIEW View_ChiTietSachMuon AS
SELECT ct.MaCuonSach, s.TenSach, 'ConTot' AS TinhTrang, ct.MaPhieuMuon, ct.TinhTrangSachKhiTra
FROM CT_PhieuMuon ct 
JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach 
JOIN Sach s ON cs.MaSach = s.MaSach;
GO
```

### 5.6. `View_DanhSachDatTruoc_ThuThu`
* **Mô tả:** Lấy thông tin phiếu đặt trước dành cho Thủ thư xử lý.
```sql
CREATE OR ALTER VIEW View_DanhSachDatTruoc_ThuThu AS
SELECT pdt.MaPhieuDatTruoc, pdt.MaDG, dg.HoTen AS TenDocGia, s.MaSach, s.TenSach, pdt.NgayDat, pdt.TrangThai 
FROM PhieuDatTruoc pdt 
JOIN DocGia dg ON pdt.MaDG = dg.MaDG 
JOIN Sach s ON pdt.MaSach = s.MaSach;
GO
```

### 5.7. `View_SachCoSan`
* **Mô tả:** Xem danh sách sách có sẵn để mượn.
```sql
CREATE OR ALTER VIEW View_SachCoSan AS
SELECT cs.MaCuonSach, CONCAT(cs.MaCuonSach, ' - ', s.TenSach, N' (Vị trí: ', ISNULL(cs.ViTriKe, N'Chưa rõ'), ')') AS DisplayText,
       s.MaSach, s.TenSach, cs.ViTriKe, cs.TrangThai, cs.TinhTrang
FROM CuonSach cs 
JOIN Sach s ON cs.MaSach = s.MaSach;
GO
```

### 5.8. `View_TaiKhoan_Role`
* **Mô tả:** Lấy danh sách tài khoản kèm tên Role.
```sql
CREATE OR ALTER VIEW View_TaiKhoan_Role AS
SELECT tk.MaTaiKhoan, tk.TenDangNhap, r.MaRole, r.TenRole, tk.TrangThai, tk.NgayTao 
FROM TaiKhoan tk 
JOIN Role r ON tk.MaRole = r.MaRole;
GO
```

### 5.9. `View_DanhSachDauSach`
* **Mô tả:** Lấy danh sách đầu sách kèm Thể loại và Nhà xuất bản.
```sql
CREATE OR ALTER VIEW View_DanhSachDauSach AS
SELECT s.MaSach, s.TenSach, nxb.TenNXB, tl.TenTheLoai, s.NamXB, s.MaNXB, s.MaTheLoai
FROM Sach s 
JOIN NhaXuatBan nxb ON s.MaNXB = nxb.MaNXB 
JOIN TheLoai tl ON s.MaTheLoai = tl.MaTheLoai;
GO
```

### 5.10. `View_LichSuDatTruoc_DocGia`
* **Mô tả:** Lịch sử đặt trước cho độc giả xem.
```sql
CREATE OR ALTER VIEW View_LichSuDatTruoc_DocGia AS
SELECT pdt.MaPhieuDatTruoc, pdt.MaDG, s.TenSach, pdt.NgayDat, pdt.TrangThai 
FROM PhieuDatTruoc pdt 
JOIN Sach s ON pdt.MaSach = s.MaSach;
GO
```

### 5.11. `View_LichSuMuon_DocGia`
* **Mô tả:** Lịch sử mượn sách cho độc giả xem.
```sql
CREATE OR ALTER VIEW View_LichSuMuon_DocGia AS
SELECT pm.MaPhieuMuon, pm.MaDG, s.TenSach, cs.MaCuonSach, pm.NgayMuon, pm.NgayHenTra, ct.NgayTraThucTe, pm.TrangThai, ct.TinhTrangSachKhiTra
FROM PhieuMuon pm 
JOIN CT_PhieuMuon ct ON pm.MaPhieuMuon = ct.MaPhieuMuon
JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach 
JOIN Sach s ON cs.MaSach = s.MaSach;
GO
```

---

## 6. TRIGGERS (RÀNG BUỘC TOÀN VẸN)

### 6.1. `trg_KiemTraTruocKhiMuon`
* **Sự kiện:** `INSTEAD OF INSERT` trên bảng `CT_PhieuMuon`.
* **Vai trò:** Ngăn chặn tuyệt đối việc mượn một cuốn sách đang không ở trạng thái `CoSan` hoặc `GiuCho`.
```sql
CREATE OR ALTER TRIGGER trg_KiemTraTruocKhiMuon
ON CT_PhieuMuon
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN CuonSach cs ON cs.MaCuonSach = i.MaCuonSach
        WHERE cs.TrangThai NOT IN ('CoSan', 'GiuCho')
    )
    BEGIN
        RAISERROR(N'Cuốn sách này hiện không sẵn có để mượn', 16, 1);
        RETURN;
    END

    INSERT INTO CT_PhieuMuon (MaPhieuMuon, MaCuonSach, TinhTrangSachKhiTra)
    SELECT MaPhieuMuon, MaCuonSach, TinhTrangSachKhiTra
    FROM inserted;
END
GO
```

### 6.2. `trg_CapNhatCuonSachSauKhiMuon`
* **Sự kiện:** `AFTER INSERT` trên bảng `CT_PhieuMuon`.
* **Vai trò:** Tự động đồng bộ trạng thái cuốn sách thành `DangMuon` ngay sau khi được đưa vào chi tiết phiếu mượn.
```sql
CREATE OR ALTER TRIGGER trg_CapNhatCuonSachSauKhiMuon
ON CT_PhieuMuon
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE cs
    SET cs.TrangThai = 'DangMuon'
    FROM CuonSach cs
    JOIN inserted i ON cs.MaCuonSach = i.MaCuonSach;
END
GO
```

### 6.3. `trg_NganXoaSachDangMuon`
* **Sự kiện:** `INSTEAD OF DELETE` trên bảng `Sach`.
* **Vai trò:** Ngăn chặn việc xóa đầu sách khi vẫn còn ít nhất một bản sao vật lý đang được độc giả mượn.
```sql
CREATE OR ALTER TRIGGER trg_NganXoaSachDangMuon
ON Sach
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM deleted d
        JOIN CuonSach cs ON cs.MaSach = d.MaSach
        WHERE cs.TrangThai = 'DangMuon'
    )
    BEGIN
        RAISERROR(N'Không thể xóa: vẫn còn bản sao của sách này đang được mượn', 16, 1);
        RETURN;
    END

    DELETE s
    FROM Sach s
    JOIN deleted d ON s.MaSach = d.MaSach;
END
GO
```

### 6.4. `trg_TuDongKhoaThe`
* **Sự kiện:** `AFTER INSERT` trên bảng `PhieuPhat`.
* **Vai trò:** Tự động khóa thẻ độc giả (`DocGia.TrangThai = 'TamKhoa'`) ngay khi độc giả đó tích lũy từ 3 phiếu phạt chưa thanh toán trở lên.
```sql
CREATE OR ALTER TRIGGER trg_TuDongKhoaThe
ON PhieuPhat
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    ;WITH DocGiaBiAnhHuong AS (
        SELECT DISTINCT dg.MaDG
        FROM inserted i
        JOIN PhieuMuon pm ON pm.MaPhieuMuon = i.MaPhieuMuon
        JOIN DocGia dg ON dg.MaDG = pm.MaDG
    ),
    SoPhieuPhatChuaTra AS (
        SELECT pm.MaDG, COUNT(*) AS SoLuong
        FROM PhieuPhat pp
        JOIN PhieuMuon pm ON pp.MaPhieuMuon = pm.MaPhieuMuon
        WHERE pp.TrangThaiThanhToan = 'ChuaThanhToan'
          AND pm.MaDG IN (SELECT MaDG FROM DocGiaBiAnhHuong)
        GROUP BY pm.MaDG
    )
    UPDATE dg
    SET dg.TrangThai = 'TamKhoa'
    FROM DocGia dg
    JOIN SoPhieuPhatChuaTra s ON dg.MaDG = s.MaDG
    WHERE s.SoLuong >= 3;
END
GO
```

---

## 7. STORED PROCEDURES MÔ PHỎNG TƯƠNG TRANH

Các Stored Procedures phục vụ module demo Interactive GUI cho 4 kịch bản tương tranh.

### 7.1. Kịch bản 1: Mất bản cập nhật (Lost Update)
*   **Tên SP:** `sp_Demo_LostUpdate_CapNhat`
*   **Mục đích:** Cập nhật trực tiếp trạng thái vật lý của cuốn sách mà không sử dụng khóa hay kiểm tra giá trị cũ, dẫn đến thao tác ghi đè mất bản cập nhật khi có 2 luồng cùng lúc.
*   **Cơ chế:** Dùng `UPDATE CuonSach SET TinhTrang = @p_TinhTrangMoi WHERE MaCuonSach = @p_MaCuonSach` không bắt điều kiện tình trạng cũ.

### 7.2. Kịch bản 2: Đọc dữ liệu rác (Dirty Read)
*   **Tên SP:** `sp_Demo_DirtyRead_TraCuu`
*   **Mục đích:** Mô phỏng độc giả tra cứu dữ liệu chưa chốt (Uncommitted) trong khi thủ thư đang mở Transaction xử lý sách.
*   **Cơ chế:** Sử dụng mức cô lập `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;` để vượt qua Shared Lock và đọc dữ liệu đang bị khóa.

### 7.3. Kịch bản 3: Đọc không nhất quán (Non-Repeatable Read)
*   **Tên SP 1 (Quản lý Kiểm kê):** `sp_Demo_NonRepeatableRead_KiemKe`
    *   **Mục đích:** Đếm số sách đang có sẵn bằng mức cô lập mặc định `READ COMMITTED`. Mức cô lập này nhả Shared Lock ngay sau khi đọc nên cho phép luồng khác sửa dữ liệu.
*   **Tên SP 2 (Thủ thư Cho Mượn):** `sp_Demo_NonRepeatableRead_ChoMuon`
    *   **Mục đích:** Cập nhật cuốn sách sang trạng thái `DangMuon` để xen vào giữa giao dịch đếm lần 1 và lần 2 của Quản lý, làm thay đổi tập kết quả.

### 7.4. Kịch bản 4: Đọc Bóng ma (Phantom Read)
*   **Mục đích:** Mô phỏng hiện tượng Phantom Read khi mức cô lập REPEATABLE READ chỉ khóa cập nhật/xóa các dòng đã đọc, nhưng không chặn luồng khác chèn thêm dòng mới (INSERT) vào phạm vi dữ liệu đang truy vấn.
*   **Cơ chế (Thực hiện trực tiếp trên Form):** 
    *   **User 1 (Quản lý):** Thực thi `SELECT COUNT(*) FROM PhieuPhat` với `SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;` và giữ nguyên Transaction.
    *   **User 2 (Hệ thống):** Gửi lệnh `INSERT INTO PhieuPhat (MaPhieuPhat, ...)` để chèn thêm phiếu phạt. Lệnh này không bị chặn.
    *   Khi User 1 chạy lại lệnh SELECT COUNT(*), số lượng trả về sẽ lớn hơn lần 1 (xuất hiện dữ liệu Bóng ma).

