USE [master]
GO
/****** Object:  Database [QuanLyMuonSach]    Script Date: 8/23/2026 12:24:04 PM ******/
CREATE DATABASE [QuanLyMuonSach]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'QuanLyMuonSach', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\QuanLyMuonSach.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'QuanLyMuonSach_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\QuanLyMuonSach_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [QuanLyMuonSach] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [QuanLyMuonSach].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [QuanLyMuonSach] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET ARITHABORT OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [QuanLyMuonSach] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [QuanLyMuonSach] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET  ENABLE_BROKER 
GO
ALTER DATABASE [QuanLyMuonSach] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [QuanLyMuonSach] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET RECOVERY FULL 
GO
ALTER DATABASE [QuanLyMuonSach] SET  MULTI_USER 
GO
ALTER DATABASE [QuanLyMuonSach] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [QuanLyMuonSach] SET DB_CHAINING OFF 
GO
ALTER DATABASE [QuanLyMuonSach] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [QuanLyMuonSach] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [QuanLyMuonSach] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [QuanLyMuonSach] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'QuanLyMuonSach', N'ON'
GO
ALTER DATABASE [QuanLyMuonSach] SET QUERY_STORE = ON
GO
ALTER DATABASE [QuanLyMuonSach] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [QuanLyMuonSach]
GO
/****** Object:  UserDefinedTableType [dbo].[DanhSachCuonSachType]    Script Date: 8/23/2026 12:24:04 PM ******/
CREATE TYPE [dbo].[DanhSachCuonSachType] AS TABLE(
	[MaCuonSach] [varchar](10) NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[DanhSachTraSachType]    Script Date: 8/23/2026 12:24:04 PM ******/
CREATE TYPE [dbo].[DanhSachTraSachType] AS TABLE(
	[MaCuonSach] [varchar](10) NOT NULL,
	[TinhTrang] [varchar](20) NOT NULL
)
GO
/****** Object:  UserDefinedFunction [dbo].[fn_DemSachDangMuon]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   FUNCTION [dbo].[fn_DemSachDangMuon] (@p_MaDG VARCHAR(10))
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
    RETURN @v_SoLuong;
END
GO
/****** Object:  UserDefinedFunction [dbo].[fn_KiemTraDuDieuKienMuon]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   FUNCTION [dbo].[fn_KiemTraDuDieuKienMuon] (@p_MaDG VARCHAR(10))
RETURNS NVARCHAR(100)
AS
BEGIN
    DECLARE @v_TrangThai VARCHAR(20);
    DECLARE @v_SoSachDangMuon INT;
    DECLARE @v_GioiHan INT = 3;
    SELECT @v_TrangThai = TrangThai FROM DocGia WHERE MaDG = @p_MaDG;
    SET @v_SoSachDangMuon = dbo.fn_DemSachDangMuon(@p_MaDG);
    RETURN CASE 
        WHEN @v_TrangThai <> 'ConHan' THEN N'Không đủ điều kiện: thẻ không còn hạn sử dụng'
        WHEN @v_SoSachDangMuon >= @v_GioiHan THEN N'Không đủ điều kiện: đã mượn tối đa số sách cho phép'
        ELSE N'Đủ điều kiện mượn sách'
    END;
END
GO
/****** Object:  UserDefinedFunction [dbo].[fn_TinhSoNgayTre]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Function 3: Tính số ngày trễ hạn của 1 phiếu mượn (ĐÃ SỬA LỖI)
CREATE   FUNCTION [dbo].[fn_TinhSoNgayTre] (@p_MaPhieuMuon VARCHAR(10))
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
/****** Object:  UserDefinedFunction [dbo].[fn_TinhTienPhatTreHan]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Function 4: Tính tiền phạt trễ hạn
CREATE   FUNCTION [dbo].[fn_TinhTienPhatTreHan] (@p_SoNgayTre INT)
RETURNS DECIMAL(18,0)
AS
BEGIN
    RETURN @p_SoNgayTre * 5000;
END
GO
/****** Object:  Table [dbo].[Sach]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Sach](
	[MaSach] [varchar](10) NOT NULL,
	[TenSach] [nvarchar](200) NOT NULL,
	[MaNXB] [varchar](10) NOT NULL,
	[MaTheLoai] [varchar](10) NOT NULL,
	[NamXB] [int] NULL,
	[MoTa] [nvarchar](max) NULL,
 CONSTRAINT [PK_Sach] PRIMARY KEY CLUSTERED 
(
	[MaSach] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CuonSach]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CuonSach](
	[MaCuonSach] [varchar](10) NOT NULL,
	[MaSach] [varchar](10) NOT NULL,
	[MaVach] [varchar](50) NULL,
	[TinhTrang] [varchar](20) NOT NULL,
	[TrangThai] [varchar](20) NOT NULL,
	[ViTriKe] [nvarchar](50) NULL,
 CONSTRAINT [PK_CuonSach] PRIMARY KEY CLUSTERED 
(
	[MaCuonSach] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[View_SachConTrong]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- View 2: Số lượng bản sao còn trống theo từng đầu sách
CREATE   VIEW [dbo].[View_SachConTrong] AS
SELECT 
    s.MaSach, s.TenSach, 
    COUNT(cs.MaCuonSach) AS TongSoBanSao,
    SUM(CASE WHEN cs.TrangThai = 'CoSan' THEN 1 ELSE 0 END) AS SoBanConTrong
FROM Sach s
LEFT JOIN CuonSach cs ON s.MaSach = cs.MaSach
GROUP BY s.MaSach, s.TenSach;
GO
/****** Object:  Table [dbo].[DocGia]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DocGia](
	[MaDG] [varchar](10) NOT NULL,
	[MaTaiKhoan] [varchar](10) NOT NULL,
	[HoTen] [nvarchar](100) NOT NULL,
	[NgaySinh] [date] NULL,
	[DiaChi] [nvarchar](200) NULL,
	[SDT] [varchar](15) NULL,
	[Email] [varchar](100) NULL,
	[LoaiDocGia] [nvarchar](50) NOT NULL,
	[NgayDangKy] [date] NOT NULL,
	[TrangThai] [varchar](20) NOT NULL,
 CONSTRAINT [PK_DocGia] PRIMARY KEY CLUSTERED 
(
	[MaDG] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_DocGia_MaTaiKhoan] UNIQUE NONCLUSTERED 
(
	[MaTaiKhoan] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhieuMuon]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhieuMuon](
	[MaPhieuMuon] [varchar](10) NOT NULL,
	[MaDG] [varchar](10) NOT NULL,
	[MaNV] [varchar](10) NOT NULL,
	[NgayMuon] [date] NOT NULL,
	[NgayHenTra] [date] NOT NULL,
	[TrangThai] [varchar](20) NOT NULL,
 CONSTRAINT [PK_PhieuMuon] PRIMARY KEY CLUSTERED 
(
	[MaPhieuMuon] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CT_PhieuMuon]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CT_PhieuMuon](
	[MaPhieuMuon] [varchar](10) NOT NULL,
	[MaCuonSach] [varchar](10) NOT NULL,
	[NgayTraThucTe] [date] NULL,
	[TinhTrangSachKhiTra] [varchar](20) NOT NULL,
 CONSTRAINT [PK_CT_PhieuMuon] PRIMARY KEY CLUSTERED 
(
	[MaPhieuMuon] ASC,
	[MaCuonSach] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[View_PhieuQuaHan]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- View 3: Danh sách phiếu mượn quá hạn kèm thông tin độc giả
CREATE   VIEW [dbo].[View_PhieuQuaHan] AS
SELECT 
    pm.MaPhieuMuon, dg.HoTen, dg.SDT, dg.Email, s.TenSach, pm.NgayHenTra,
    DATEDIFF(DAY, pm.NgayHenTra, CAST(GETDATE() AS DATE)) AS SoNgayTre
FROM PhieuMuon pm
JOIN DocGia dg ON pm.MaDG = dg.MaDG
JOIN CT_PhieuMuon ct ON pm.MaPhieuMuon = ct.MaPhieuMuon
JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach
JOIN Sach s ON cs.MaSach = s.MaSach
WHERE pm.TrangThai IN ('DangMuon', 'QuaHan') AND ct.TinhTrangSachKhiTra = 'ChuaTra' AND pm.NgayHenTra < CAST(GETDATE() AS DATE);
GO
/****** Object:  Table [dbo].[TheLoai]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TheLoai](
	[MaTheLoai] [varchar](10) NOT NULL,
	[TenTheLoai] [nvarchar](100) NOT NULL,
	[MoTa] [nvarchar](max) NULL,
 CONSTRAINT [PK_TheLoai] PRIMARY KEY CLUSTERED 
(
	[MaTheLoai] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  View [dbo].[View_ThongKeTheoTheLoai]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- View 4: Thông kê số lượt mượn theo thể loại
CREATE   VIEW [dbo].[View_ThongKeTheoTheLoai] AS
SELECT 
    tl.TenTheLoai, COUNT(pm.MaPhieuMuon) AS SoLuotMuon
FROM TheLoai tl
JOIN Sach s ON tl.MaTheLoai = s.MaTheLoai
JOIN CuonSach cs ON s.MaSach = cs.MaSach
JOIN CT_PhieuMuon ct ON cs.MaCuonSach = ct.MaCuonSach
JOIN PhieuMuon pm ON ct.MaPhieuMuon = pm.MaPhieuMuon
GROUP BY tl.MaTheLoai, tl.TenTheLoai;
GO
/****** Object:  Table [dbo].[NhanVien]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NhanVien](
	[MaNV] [varchar](10) NOT NULL,
	[MaTaiKhoan] [varchar](10) NOT NULL,
	[HoTen] [nvarchar](100) NOT NULL,
	[ChucVu] [nvarchar](50) NULL,
	[SDT] [varchar](15) NULL,
	[Email] [varchar](100) NULL,
 CONSTRAINT [PK_NhanVien] PRIMARY KEY CLUSTERED 
(
	[MaNV] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_NhanVien_MaTaiKhoan] UNIQUE NONCLUSTERED 
(
	[MaTaiKhoan] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[View_ThongTinMuonSach]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================
-- 9. VIEW (KHUNG NHÌN)
-- ==========================================================
-- View 1: Thông tin mượn sách đầy đủ (ĐÃ SỬA MA_NV VÀ NGÀY TRẢ THỰC TẾ)
CREATE   VIEW [dbo].[View_ThongTinMuonSach] AS
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
/****** Object:  View [dbo].[View_ChiTietSachMuon]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[View_ChiTietSachMuon] AS
SELECT ct.MaCuonSach, s.TenSach, 'ConTot' AS TinhTrang, ct.MaPhieuMuon, ct.TinhTrangSachKhiTra
FROM CT_PhieuMuon ct 
JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach 
JOIN Sach s ON cs.MaSach = s.MaSach;
GO
/****** Object:  View [dbo].[View_DanhSachDatTruoc_ThuThu]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[View_DanhSachDatTruoc_ThuThu] AS
SELECT pdt.MaPhieuDatTruoc, pdt.MaDG, dg.HoTen AS TenDocGia, s.MaSach, s.TenSach, pdt.NgayDat, pdt.TrangThai 
FROM PhieuDatTruoc pdt 
JOIN DocGia dg ON pdt.MaDG = dg.MaDG 
JOIN Sach s ON pdt.MaSach = s.MaSach;
GO
/****** Object:  View [dbo].[View_SachCoSan]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[View_SachCoSan] AS
SELECT cs.MaCuonSach, CONCAT(cs.MaCuonSach, ' - ', s.TenSach, N' (Vị trí: ', ISNULL(cs.ViTriKe, N'Chưa rõ'), ')') AS DisplayText,
       s.MaSach, s.TenSach, cs.ViTriKe, cs.TrangThai, cs.TinhTrang
FROM CuonSach cs 
JOIN Sach s ON cs.MaSach = s.MaSach;
GO
/****** Object:  View [dbo].[View_TaiKhoan_Role]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[View_TaiKhoan_Role] AS
SELECT tk.MaTaiKhoan, tk.TenDangNhap, r.MaRole, r.TenRole, tk.TrangThai, tk.NgayTao 
FROM TaiKhoan tk 
JOIN Role r ON tk.MaRole = r.MaRole;
GO
/****** Object:  View [dbo].[View_DanhSachDauSach]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[View_DanhSachDauSach] AS
SELECT s.MaSach, s.TenSach, nxb.TenNXB, tl.TenTheLoai, s.NamXB, s.MaNXB, s.MaTheLoai
FROM Sach s 
JOIN NhaXuatBan nxb ON s.MaNXB = nxb.MaNXB 
JOIN TheLoai tl ON s.MaTheLoai = tl.MaTheLoai;
GO
/****** Object:  View [dbo].[View_LichSuDatTruoc_DocGia]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[View_LichSuDatTruoc_DocGia] AS
SELECT pdt.MaPhieuDatTruoc, pdt.MaDG, s.TenSach, pdt.NgayDat, pdt.TrangThai 
FROM PhieuDatTruoc pdt 
JOIN Sach s ON pdt.MaSach = s.MaSach;
GO
/****** Object:  View [dbo].[View_LichSuMuon_DocGia]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[View_LichSuMuon_DocGia] AS
SELECT pm.MaPhieuMuon, pm.MaDG, s.TenSach, cs.MaCuonSach, pm.NgayMuon, pm.NgayHenTra, ct.NgayTraThucTe, pm.TrangThai, ct.TinhTrangSachKhiTra
FROM PhieuMuon pm 
JOIN CT_PhieuMuon ct ON pm.MaPhieuMuon = ct.MaPhieuMuon
JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach 
JOIN Sach s ON cs.MaSach = s.MaSach;
GO
/****** Object:  Table [dbo].[NhaXuatBan]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NhaXuatBan](
	[MaNXB] [varchar](10) NOT NULL,
	[TenNXB] [nvarchar](150) NOT NULL,
	[DiaChi] [nvarchar](200) NULL,
	[SDT] [varchar](15) NULL,
 CONSTRAINT [PK_NhaXuatBan] PRIMARY KEY CLUSTERED 
(
	[MaNXB] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhieuDatTruoc]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhieuDatTruoc](
	[MaPhieuDatTruoc] [varchar](10) NOT NULL,
	[MaDG] [varchar](10) NOT NULL,
	[MaSach] [varchar](10) NOT NULL,
	[NgayDat] [date] NOT NULL,
	[TrangThai] [varchar](20) NOT NULL,
 CONSTRAINT [PK_PhieuDatTruoc] PRIMARY KEY CLUSTERED 
(
	[MaPhieuDatTruoc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PhieuPhat]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PhieuPhat](
	[MaPhieuPhat] [varchar](10) NOT NULL,
	[MaPhieuMuon] [varchar](10) NOT NULL,
	[LyDoPhat] [varchar](20) NOT NULL,
	[SoTienPhat] [decimal](18, 0) NOT NULL,
	[NgayLap] [date] NOT NULL,
	[TrangThaiThanhToan] [varchar](20) NOT NULL,
 CONSTRAINT [PK_PhieuPhat] PRIMARY KEY CLUSTERED 
(
	[MaPhieuPhat] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Role]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Role](
	[MaRole] [varchar](10) NOT NULL,
	[TenRole] [nvarchar](50) NOT NULL,
	[MoTa] [nvarchar](255) NULL,
 CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED 
(
	[MaRole] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Role_TenRole] UNIQUE NONCLUSTERED 
(
	[TenRole] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Sach_TacGia]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Sach_TacGia](
	[MaSach] [varchar](10) NOT NULL,
	[MaTacGia] [varchar](10) NOT NULL,
	[VaiTro] [nvarchar](50) NULL,
 CONSTRAINT [PK_Sach_TacGia] PRIMARY KEY CLUSTERED 
(
	[MaSach] ASC,
	[MaTacGia] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TacGia]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TacGia](
	[MaTacGia] [varchar](10) NOT NULL,
	[TenTacGia] [nvarchar](100) NOT NULL,
	[QuocTich] [nvarchar](50) NULL,
	[TieuSu] [nvarchar](max) NULL,
 CONSTRAINT [PK_TacGia] PRIMARY KEY CLUSTERED 
(
	[MaTacGia] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaiKhoan]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaiKhoan](
	[MaTaiKhoan] [varchar](10) NOT NULL,
	[TenDangNhap] [varchar](50) NOT NULL,
	[MatKhau] [varchar](255) NOT NULL,
	[MaRole] [varchar](10) NOT NULL,
	[TrangThai] [varchar](20) NOT NULL,
	[NgayTao] [date] NOT NULL,
 CONSTRAINT [PK_TaiKhoan] PRIMARY KEY CLUSTERED 
(
	[MaTaiKhoan] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_TaiKhoan_TenDangNhap] UNIQUE NONCLUSTERED 
(
	[TenDangNhap] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[CT_PhieuMuon] ADD  DEFAULT ('ChuaTra') FOR [TinhTrangSachKhiTra]
GO
ALTER TABLE [dbo].[CuonSach] ADD  DEFAULT ('ConTot') FOR [TinhTrang]
GO
ALTER TABLE [dbo].[CuonSach] ADD  DEFAULT ('CoSan') FOR [TrangThai]
GO
ALTER TABLE [dbo].[DocGia] ADD  DEFAULT (N'SinhVien') FOR [LoaiDocGia]
GO
ALTER TABLE [dbo].[DocGia] ADD  DEFAULT (CONVERT([date],getdate())) FOR [NgayDangKy]
GO
ALTER TABLE [dbo].[DocGia] ADD  DEFAULT ('ConHan') FOR [TrangThai]
GO
ALTER TABLE [dbo].[PhieuDatTruoc] ADD  DEFAULT (CONVERT([date],getdate())) FOR [NgayDat]
GO
ALTER TABLE [dbo].[PhieuDatTruoc] ADD  DEFAULT ('DangCho') FOR [TrangThai]
GO
ALTER TABLE [dbo].[PhieuMuon] ADD  DEFAULT (CONVERT([date],getdate())) FOR [NgayMuon]
GO
ALTER TABLE [dbo].[PhieuMuon] ADD  DEFAULT ('DangMuon') FOR [TrangThai]
GO
ALTER TABLE [dbo].[PhieuPhat] ADD  DEFAULT ((0)) FOR [SoTienPhat]
GO
ALTER TABLE [dbo].[PhieuPhat] ADD  DEFAULT (CONVERT([date],getdate())) FOR [NgayLap]
GO
ALTER TABLE [dbo].[PhieuPhat] ADD  DEFAULT ('ChuaThanhToan') FOR [TrangThaiThanhToan]
GO
ALTER TABLE [dbo].[Sach_TacGia] ADD  DEFAULT (N'Đồng tác giả') FOR [VaiTro]
GO
ALTER TABLE [dbo].[TaiKhoan] ADD  DEFAULT ('HoatDong') FOR [TrangThai]
GO
ALTER TABLE [dbo].[TaiKhoan] ADD  DEFAULT (CONVERT([date],getdate())) FOR [NgayTao]
GO
ALTER TABLE [dbo].[CT_PhieuMuon]  WITH CHECK ADD  CONSTRAINT [FK_CTPhieuMuon_CuonSach] FOREIGN KEY([MaCuonSach])
REFERENCES [dbo].[CuonSach] ([MaCuonSach])
GO
ALTER TABLE [dbo].[CT_PhieuMuon] CHECK CONSTRAINT [FK_CTPhieuMuon_CuonSach]
GO
ALTER TABLE [dbo].[CT_PhieuMuon]  WITH CHECK ADD  CONSTRAINT [FK_CTPhieuMuon_PhieuMuon] FOREIGN KEY([MaPhieuMuon])
REFERENCES [dbo].[PhieuMuon] ([MaPhieuMuon])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[CT_PhieuMuon] CHECK CONSTRAINT [FK_CTPhieuMuon_PhieuMuon]
GO
ALTER TABLE [dbo].[CuonSach]  WITH CHECK ADD  CONSTRAINT [FK_CuonSach_Sach] FOREIGN KEY([MaSach])
REFERENCES [dbo].[Sach] ([MaSach])
GO
ALTER TABLE [dbo].[CuonSach] CHECK CONSTRAINT [FK_CuonSach_Sach]
GO
ALTER TABLE [dbo].[DocGia]  WITH CHECK ADD  CONSTRAINT [FK_DocGia_TaiKhoan] FOREIGN KEY([MaTaiKhoan])
REFERENCES [dbo].[TaiKhoan] ([MaTaiKhoan])
GO
ALTER TABLE [dbo].[DocGia] CHECK CONSTRAINT [FK_DocGia_TaiKhoan]
GO
ALTER TABLE [dbo].[NhanVien]  WITH CHECK ADD  CONSTRAINT [FK_NhanVien_TaiKhoan] FOREIGN KEY([MaTaiKhoan])
REFERENCES [dbo].[TaiKhoan] ([MaTaiKhoan])
GO
ALTER TABLE [dbo].[NhanVien] CHECK CONSTRAINT [FK_NhanVien_TaiKhoan]
GO
ALTER TABLE [dbo].[PhieuDatTruoc]  WITH CHECK ADD  CONSTRAINT [FK_PhieuDatTruoc_DocGia] FOREIGN KEY([MaDG])
REFERENCES [dbo].[DocGia] ([MaDG])
GO
ALTER TABLE [dbo].[PhieuDatTruoc] CHECK CONSTRAINT [FK_PhieuDatTruoc_DocGia]
GO
ALTER TABLE [dbo].[PhieuDatTruoc]  WITH CHECK ADD  CONSTRAINT [FK_PhieuDatTruoc_Sach] FOREIGN KEY([MaSach])
REFERENCES [dbo].[Sach] ([MaSach])
GO
ALTER TABLE [dbo].[PhieuDatTruoc] CHECK CONSTRAINT [FK_PhieuDatTruoc_Sach]
GO
ALTER TABLE [dbo].[PhieuMuon]  WITH CHECK ADD  CONSTRAINT [FK_PhieuMuon_DocGia] FOREIGN KEY([MaDG])
REFERENCES [dbo].[DocGia] ([MaDG])
GO
ALTER TABLE [dbo].[PhieuMuon] CHECK CONSTRAINT [FK_PhieuMuon_DocGia]
GO
ALTER TABLE [dbo].[PhieuMuon]  WITH CHECK ADD  CONSTRAINT [FK_PhieuMuon_NhanVien] FOREIGN KEY([MaNV])
REFERENCES [dbo].[NhanVien] ([MaNV])
GO
ALTER TABLE [dbo].[PhieuMuon] CHECK CONSTRAINT [FK_PhieuMuon_NhanVien]
GO
ALTER TABLE [dbo].[PhieuPhat]  WITH CHECK ADD  CONSTRAINT [FK_PhieuPhat_PhieuMuon] FOREIGN KEY([MaPhieuMuon])
REFERENCES [dbo].[PhieuMuon] ([MaPhieuMuon])
GO
ALTER TABLE [dbo].[PhieuPhat] CHECK CONSTRAINT [FK_PhieuPhat_PhieuMuon]
GO
ALTER TABLE [dbo].[Sach]  WITH CHECK ADD  CONSTRAINT [FK_Sach_NhaXuatBan] FOREIGN KEY([MaNXB])
REFERENCES [dbo].[NhaXuatBan] ([MaNXB])
GO
ALTER TABLE [dbo].[Sach] CHECK CONSTRAINT [FK_Sach_NhaXuatBan]
GO
ALTER TABLE [dbo].[Sach]  WITH CHECK ADD  CONSTRAINT [FK_Sach_TheLoai] FOREIGN KEY([MaTheLoai])
REFERENCES [dbo].[TheLoai] ([MaTheLoai])
GO
ALTER TABLE [dbo].[Sach] CHECK CONSTRAINT [FK_Sach_TheLoai]
GO
ALTER TABLE [dbo].[Sach_TacGia]  WITH CHECK ADD  CONSTRAINT [FK_SachTacGia_Sach] FOREIGN KEY([MaSach])
REFERENCES [dbo].[Sach] ([MaSach])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Sach_TacGia] CHECK CONSTRAINT [FK_SachTacGia_Sach]
GO
ALTER TABLE [dbo].[Sach_TacGia]  WITH CHECK ADD  CONSTRAINT [FK_SachTacGia_TacGia] FOREIGN KEY([MaTacGia])
REFERENCES [dbo].[TacGia] ([MaTacGia])
GO
ALTER TABLE [dbo].[Sach_TacGia] CHECK CONSTRAINT [FK_SachTacGia_TacGia]
GO
ALTER TABLE [dbo].[TaiKhoan]  WITH CHECK ADD  CONSTRAINT [FK_TaiKhoan_Role] FOREIGN KEY([MaRole])
REFERENCES [dbo].[Role] ([MaRole])
GO
ALTER TABLE [dbo].[TaiKhoan] CHECK CONSTRAINT [FK_TaiKhoan_Role]
GO
ALTER TABLE [dbo].[PhieuMuon]  WITH CHECK ADD  CONSTRAINT [CK_PhieuMuon_NgayTra] CHECK  (([NgayHenTra]>=[NgayMuon]))
GO
ALTER TABLE [dbo].[PhieuMuon] CHECK CONSTRAINT [CK_PhieuMuon_NgayTra]
GO
ALTER TABLE [dbo].[PhieuPhat]  WITH CHECK ADD  CONSTRAINT [CK_PhieuPhat_SoTien] CHECK  (([SoTienPhat]>=(0)))
GO
ALTER TABLE [dbo].[PhieuPhat] CHECK CONSTRAINT [CK_PhieuPhat_SoTien]
GO
ALTER TABLE [dbo].[Sach]  WITH CHECK ADD  CONSTRAINT [CK_Sach_NamXB] CHECK  (([NamXB]<=datepart(year,getdate())))
GO
ALTER TABLE [dbo].[Sach] CHECK CONSTRAINT [CK_Sach_NamXB]
GO
/****** Object:  StoredProcedure [dbo].[sp_BaoCaoTienPhat]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- SP 3: Báo cáo doanh thu tiền phạt
CREATE   PROCEDURE [dbo].[sp_BaoCaoTienPhat]
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
/****** Object:  StoredProcedure [dbo].[sp_DatTruocSach]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_DatTruocSach]
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
        -- 1. Kiểm tra trạng thái thẻ độc giả
        SELECT @v_TrangThaiThe = TrangThai FROM DocGia WHERE MaDG = @p_MaDG;
        IF @v_TrangThaiThe <> 'ConHan'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Thẻ độc giả không còn hạn sử dụng hoặc đang bị khóa' AS ThongBao;
            RETURN;
        END
        -- 2. Kiểm tra xem độc giả đã có phiếu đặt trước nào cho đầu sách này đang chờ chưa
        IF EXISTS (SELECT 1 FROM PhieuDatTruoc WHERE MaDG = @p_MaDG AND MaSach = @p_MaSach AND TrangThai IN ('DangCho', 'ChoNhan'))
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Bạn đã đặt trước đầu sách này rồi, vui lòng không đặt trùng lặp' AS ThongBao;
            RETURN;
        END
        -- 3. Kiểm tra xem trong kho còn cuốn nào 'CoSan' không
        SELECT @v_SoLuongCoSan = COUNT(*)
        FROM CuonSach WITH (UPDLOCK, ROWLOCK)
        WHERE MaSach = @p_MaSach AND TrangThai = 'CoSan';
        IF @v_SoLuongCoSan > 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Sách hiện vẫn còn bản sao có sẵn trên kệ, vui lòng mượn trực tiếp' AS ThongBao;
            RETURN;
        END
        -- 4. Tạo phiếu đặt trước đưa vào hàng chờ
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
/****** Object:  StoredProcedure [dbo].[sp_HuyGiuChoHetHan]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_HuyGiuChoHetHan]
    @p_MaPhieuDatTruoc VARCHAR(10),
    @p_MaCuonSach      VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_MaSach VARCHAR(10);
    BEGIN TRY
        BEGIN TRANSACTION;
        SELECT @v_MaSach = MaSach FROM PhieuDatTruoc WHERE MaPhieuDatTruoc = @p_MaPhieuDatTruoc;
        -- 1. Đánh dấu phiếu đặt trước này là 'DaHuy'
        UPDATE PhieuDatTruoc 
        SET TrangThai = 'DaHuy' 
        WHERE MaPhieuDatTruoc = @p_MaPhieuDatTruoc AND TrangThai = 'ChoNhan';
        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Phiếu đặt trước không ở trạng thái Chờ nhận hoặc không tồn tại' AS ThongBao;
            RETURN;
        END
        -- 2. Tái điều chuyển cuốn sách vừa bị nhả ra: Chuyển cho người tiếp theo hoặc trả về kệ
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
/****** Object:  StoredProcedure [dbo].[sp_LapPhieuMuon]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================
-- 8. GIAO TÁC (TRANSACTION STORED PROCEDURE)
-- ==========================================================
-- Giao tác 1: Lập phiếu mượn sách (Sử dụng MaNV)
CREATE   PROCEDURE [dbo].[sp_LapPhieuMuon]
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
        SET @v_DieuKien = dbo.fn_KiemTraDuDieuKienMuon(@p_MaDG);
        IF @v_DieuKien <> N'Đủ điều kiện mượn sách'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT @v_DieuKien AS ThongBao;
            RETURN;
        END
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

        INSERT INTO PhieuMuon (MaPhieuMuon, MaDG, MaNV, NgayMuon, NgayHenTra, TrangThai)
        VALUES (@p_MaPhieuMuon, @p_MaDG, @p_MaNV, CAST(GETDATE() AS DATE), @p_NgayHenTra, 'DangMuon');
        INSERT INTO CT_PhieuMuon (MaPhieuMuon, MaCuonSach, TinhTrangSachKhiTra)
        SELECT @p_MaPhieuMuon, d.MaCuonSach, 'ChuaTra'
        FROM @p_DanhSachCuonSach d;
        COMMIT TRANSACTION;
        SELECT N'Lập phiếu mượn thành công' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Giao tác lập phiếu mượn thất bại, đã rollback' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[sp_NhanSachDatTruoc]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_NhanSachDatTruoc]
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
        -- 1. Kiểm tra phiếu đặt trước có hợp lệ ở trạng thái 'ChoNhan' không
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
        -- 3. Kiểm tra cuốn sách có đang ở trạng thái 'GiuCho' không
        IF NOT EXISTS (SELECT 1 FROM CuonSach WHERE MaCuonSach = @p_MaCuonSach AND MaSach = @v_MaSach AND TrangThai = 'GiuCho')
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Cuốn sách này không ở trạng thái Giữ chỗ hoặc không thuộc đầu sách đã đặt' AS ThongBao;
            RETURN;
        END
        -- 4. Lập Phiếu mượn chính thức (Lúc này mới bắt đầu tính ngày mượn)
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
        SELECT N'Độc giả đã nhận sách và lập phiếu mượn thành công!' AS ThongBao;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Nhận sách đặt trước thất bại' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[sp_ThanhToanPhat]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Giao tác 5: Thanh toán phiếu phạt
CREATE   PROCEDURE [dbo].[sp_ThanhToanPhat]
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
/****** Object:  StoredProcedure [dbo].[sp_Top5SachMuonNhieu]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- SP 2: Top 5 sách được mượn nhiều nhất
CREATE   PROCEDURE [dbo].[sp_Top5SachMuonNhieu]
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
/****** Object:  StoredProcedure [dbo].[sp_TraCuuSach]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================
-- 10. STORED PROCEDURE BÁO CÁO & TRA CỨU
-- ==========================================================
-- SP 1: Tra cứu sách theo từ khóa (ĐÃ SỬA LỖI JOIN BẢNG)
CREATE   PROCEDURE [dbo].[sp_TraCuuSach]
    @p_TuKhoa VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.MaSach, s.TenSach, tg.TenTacGia, tl.TenTheLoai, v.SoBanConTrong
    FROM Sach s
    JOIN Sach_TacGia stg ON s.MaSach = stg.MaSach     
    JOIN TacGia tg ON stg.MaTacGia = tg.MaTacGia      
    JOIN TheLoai tl ON s.MaTheLoai = tl.MaTheLoai
    LEFT JOIN View_SachConTrong v ON s.MaSach = v.MaSach
    WHERE s.TenSach LIKE '%' + @p_TuKhoa + '%' 
       OR tg.TenTacGia LIKE '%' + @p_TuKhoa + '%';
END
GO
/****** Object:  StoredProcedure [dbo].[sp_TraSach]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Giao tác 2: Trả sách kèm phát sinh phạt (ĐÃ SỬA LỖI CẬP NHẬT NGÀY TRẢ)
CREATE   PROCEDURE [dbo].[sp_TraSach]
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
        -- Chỉ cập nhật trạng thái ở PhieuMuon
        UPDATE PhieuMuon
        SET TrangThai = 'DaTra'
        WHERE MaPhieuMuon = @p_MaPhieuMuon;
        -- Cập nhật cả tình trạng VÀ ngày trả ở CT_PhieuMuon
        UPDATE ct
        SET ct.TinhTrangSachKhiTra = d.TinhTrang,
            ct.NgayTraThucTe = CAST(GETDATE() AS DATE)
        FROM CT_PhieuMuon ct
        JOIN @p_DanhSachTraSach d ON ct.MaCuonSach = d.MaCuonSach
        WHERE ct.MaPhieuMuon = @p_MaPhieuMuon;
        UPDATE cs
        SET cs.TinhTrang = d.TinhTrang,
            cs.TrangThai = CASE WHEN d.TinhTrang = 'ConTot' THEN 'CoSan' ELSE 'DangSua' END
        FROM CuonSach cs
        JOIN @p_DanhSachTraSach d ON cs.MaCuonSach = d.MaCuonSach;
        IF @v_SoNgayTre > 0
        BEGIN
            SET @v_MaPhieuPhat = CONCAT('PP', RIGHT('0000' + CAST(FLOOR(RAND()*9999) AS VARCHAR(4)), 4));
            
            INSERT INTO PhieuPhat (MaPhieuPhat, MaPhieuMuon, LyDoPhat, SoTienPhat, NgayLap, TrangThaiThanhToan)
            VALUES (@v_MaPhieuPhat, @p_MaPhieuMuon, 'TreHan', 
                    dbo.fn_TinhTienPhatTreHan(@v_SoNgayTre), CAST(GETDATE() AS DATE), 'ChuaThanhToan');
        END
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
/****** Object:  StoredProcedure [dbo].[sp_XuLyDatTruocKhiCoSach]    Script Date: 8/23/2026 12:24:04 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_XuLyDatTruocKhiCoSach]
    @p_MaCuonSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_MaSach          VARCHAR(10);
    DECLARE @v_MaPhieuDatTruoc VARCHAR(10);
    DECLARE @v_MaDG            VARCHAR(10);
    BEGIN TRY
        BEGIN TRANSACTION;
        -- 1. Xác định đầu sách của cuốn sách này
        SELECT @v_MaSach = MaSach
        FROM CuonSach WITH (UPDLOCK, ROWLOCK)
        WHERE MaCuonSach = @p_MaCuonSach;
        -- 2. Tìm người đặt trước sớm nhất đang ở trạng thái 'DangCho'
        SELECT TOP 1 
            @v_MaPhieuDatTruoc = MaPhieuDatTruoc,
            @v_MaDG = MaDG
        FROM PhieuDatTruoc WITH (UPDLOCK, ROWLOCK)
        WHERE MaSach = @v_MaSach AND TrangThai = 'DangCho'
        ORDER BY NgayDat ASC;
        IF @v_MaPhieuDatTruoc IS NULL
        BEGIN
            -- Không có ai đặt trước -> Chuyển sách về trạng thái có sẵn bình thường
            UPDATE CuonSach SET TrangThai = 'CoSan' WHERE MaCuonSach = @p_MaCuonSach;
            COMMIT TRANSACTION;
            SELECT N'Không có ai đặt trước cuốn này. Sách đã được trả về kệ có sẵn.' AS ThongBao;
        END
        ELSE
        BEGIN
            -- Chuyển cuốn sách sang trạng thái 'GiuCho' (Cất riêng tại quầy dịch vụ)
            UPDATE CuonSach SET TrangThai = 'GiuCho' WHERE MaCuonSach = @p_MaCuonSach;
            -- Chuyển phiếu đặt trước sang 'ChoNhan' (Hẹn độc giả đến lấy)
            UPDATE PhieuDatTruoc 
            SET TrangThai = 'ChoNhan'
            WHERE MaPhieuDatTruoc = @v_MaPhieuDatTruoc;
            COMMIT TRANSACTION;
            SELECT CONCAT(N'Đã chuyển sách ', @p_MaCuonSach, N' sang trạng thái GIỮ CHỖ cho độc giả ', @v_MaDG) AS ThongBao;
        END
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'Lỗi: Xử lý giữ chỗ thất bại' AS ThongBao, ERROR_MESSAGE() AS ChiTietLoi;
    END CATCH
END
GO
USE [master]
GO
ALTER DATABASE [QuanLyMuonSach] SET  READ_WRITE 
GO
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

---------------------------------------------------------------------
-- 4. KỊCH BẢN 4: TRANH CHẤP MƯỢN BẢN SAO CUỐI CÙNG (RACE CONDITION / ROW LOCK)
---------------------------------------------------------------------
-- SP Mượn sách đơn lẻ an toàn bằng ROWLOCK (Mô phỏng giống sp_LapPhieuMuon thực tế)
CREATE OR ALTER PROCEDURE [dbo].[sp_Demo_MuonSachDon]
    @p_MaPM VARCHAR(10),
    @p_MaDG VARCHAR(10),
    @p_MaCuonSach VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @v_TrangThai VARCHAR(20);

    BEGIN TRY
        BEGIN TRANSACTION;

        -- KIỂM TRA & KHÓA HÀNG ĐỂ CHỐNG RACE CONDITION
        SELECT @v_TrangThai = TrangThai 
        FROM CuonSach WITH (UPDLOCK, ROWLOCK) 
        WHERE MaCuonSach = @p_MaCuonSach;

        IF @v_TrangThai IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'Lỗi: Không tìm thấy sách!' AS ThongBao, 0 AS IsSuccess;
            RETURN;
        END

        IF @v_TrangThai <> 'CoSan'
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'❌ Lỗi: Cuốn sách này vừa được người khác mượn hoặc không còn sẵn có!' AS ThongBao, 0 AS IsSuccess;
            RETURN;
        END

        -- Chèn Phiếu Mượn giả lập (Không cần validate thẻ DG trong Demo)
        -- Nếu phiếu đã tồn tại (do demo bấm nhiều lần), ta thử xóa rồi chèn lại, hoặc dùng MERGE
        IF NOT EXISTS(SELECT 1 FROM PhieuMuon WHERE MaPhieuMuon = @p_MaPM)
        BEGIN
            INSERT INTO PhieuMuon (MaPhieuMuon, MaDG, MaNV, NgayMuon, NgayHenTra, TrangThai)
            VALUES (@p_MaPM, @p_MaDG, 'NV001', CAST(GETDATE() AS DATE), DATEADD(DAY, 14, CAST(GETDATE() AS DATE)), 'DangMuon');
        END

        -- Chèn CT_PhieuMuon
        IF NOT EXISTS(SELECT 1 FROM CT_PhieuMuon WHERE MaPhieuMuon = @p_MaPM AND MaCuonSach = @p_MaCuonSach)
        BEGIN
            INSERT INTO CT_PhieuMuon (MaPhieuMuon, MaCuonSach, TinhTrangSachKhiTra)
            VALUES (@p_MaPM, @p_MaCuonSach, 'ChuaTra');
        END

        -- Cập nhật sách
        UPDATE CuonSach 
        SET TrangThai = 'DangMuon' 
        WHERE MaCuonSach = @p_MaCuonSach;

        COMMIT TRANSACTION;
        SELECT N'✅ Mượn thành công! Đã tạo phiếu mượn.' AS ThongBao, 1 AS IsSuccess;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT N'❌ Lỗi hệ thống: ' + ERROR_MESSAGE() AS ThongBao, 0 AS IsSuccess;
    END CATCH
END
GO
