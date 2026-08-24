using System;

namespace ProjectHEQTCSDL
{
    public static class App_Config
    {
        public static string ServerName { get; set; } = "THIEN-PHUC";
        public static string DatabaseName { get; set; } = "QuanLyMuonSach";
        public static bool UseTrustedConnection { get; set; } = true;
        public static bool TrustServerCertificate { get; set; } = true;
        public static string UserId { get; set; } = "";
        public static string Password { get; set; } = "";

        public static string ConnectionString
        {
            get
            {
                if (UseTrustedConnection)
                {
                    return $"Server={ServerName};Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate={TrustServerCertificate};";
                }
                else
                {
                    return $"Server={ServerName};Database={DatabaseName};User Id={UserId};Password={Password};TrustServerCertificate={TrustServerCertificate};";
                }
            }
        }
    }
}
