using System;

namespace ProjectHEQTCSDL.Models
{
    public static class SessionContext
    {
        public static string MaTaiKhoan { get; set; } = string.Empty;
        public static string TenDangNhap { get; set; } = string.Empty;
        public static string MaRole { get; set; } = string.Empty;
        public static string TenRole { get; set; } = string.Empty;
        public static string MaDG { get; set; } = string.Empty;
        public static string TenDocGia { get; set; } = string.Empty;
        public static string MaNV { get; set; } = string.Empty;
        public static string TenNhanVien { get; set; } = string.Empty;

        public static void Clear()
        {
            MaTaiKhoan = string.Empty;
            TenDangNhap = string.Empty;
            MaRole = string.Empty;
            TenRole = string.Empty;
            MaDG = string.Empty;
            TenDocGia = string.Empty;
            MaNV = string.Empty;
            TenNhanVien = string.Empty;
        }
    }
}
