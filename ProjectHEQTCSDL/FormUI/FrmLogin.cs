using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ProjectHEQTCSDL.Core;
using ProjectHEQTCSDL.Models;

namespace ProjectHEQTCSDL.FormUI
{
    public class FrmLogin : Form
    {
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private CheckBox chkShowPassword = null!;
        private Button btnLogin = null!;
        private Button btnExit = null!;
        private Label lblServerInfo = null!;
        private Button btnConfigServer = null!;

        public FrmLogin()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Đăng Nhập - Hệ Thống Quản Lý Mượn Sách";
            this.Size = new Size(520, 530);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            // Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.FromArgb(30, 58, 138) // Deep Royal Blue
            };

            var lblTitle = new Label
            {
                Text = "QUẢN LÝ MƯỢN TRẢ SÁCH",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 55
            };

            var lblSubTitle = new Label
            {
                Text = "Đồ Án Hệ Quản Trị CSDL - Demo Tương Tranh & Phân Quyền",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(219, 234, 254),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Bottom,
                Height = 40
            };

            pnlHeader.Controls.Add(lblSubTitle);
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // Main Content Panel
            var pnlBody = new Panel
            {
                Location = new Point(35, 120),
                Size = new Size(435, 345),
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            pnlBody.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlBody.ClientRectangle, Color.FromArgb(226, 232, 240), ButtonBorderStyle.Solid);
            };

            // Server connection info
            lblServerInfo = new Label
            {
                Text = $"Server: {App_Config.ServerName} | DB: {App_Config.DatabaseName}",
                Location = new Point(20, 15),
                Size = new Size(270, 25),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            btnConfigServer = new Button
            {
                Text = "Cấu hình Server",
                Location = new Point(295, 12),
                Size = new Size(120, 28),
                Font = new Font("Segoe UI", 8.5F),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfigServer.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnConfigServer.Click += BtnConfigServer_Click;

            // Username
            var lblUser = new Label
            {
                Text = "Tên đăng nhập:",
                Location = new Point(20, 55),
                Size = new Size(395, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txtUsername = new TextBox
            {
                Location = new Point(20, 80),
                Size = new Size(395, 29),
                Text = ""
            };

            // Password
            var lblPass = new Label
            {
                Text = "Mật khẩu:",
                Location = new Point(20, 120),
                Size = new Size(395, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txtPassword = new TextBox
            {
                Location = new Point(20, 145),
                Size = new Size(395, 29),
                UseSystemPasswordChar = true,
                Text = ""
            };

            // Show password checkbox
            chkShowPassword = new CheckBox
            {
                Text = "Hiển thị mật khẩu",
                Location = new Point(20, 185),
                Size = new Size(200, 24),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            chkShowPassword.CheckedChanged += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
            };

            // Login Button
            btnLogin = new Button
            {
                Text = "ĐĂNG NHẬP",
                Location = new Point(20, 220),
                Size = new Size(250, 42),
                BackColor = Color.FromArgb(37, 99, 235), // Royal Blue
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Exit Button
            btnExit = new Button
            {
                Text = "Thoát",
                Location = new Point(280, 220),
                Size = new Size(135, 42),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnExit.Click += (s, e) => Application.Exit();

            // Concurrency Demo Entry Button
            var btnDemoTuongTranh = new Button
            {
                Text = "⚡ CHẾ ĐỘ DEMO TƯƠNG TRANH (HQTCSDL)",
                Location = new Point(20, 275),
                Size = new Size(395, 42),
                BackColor = Color.FromArgb(124, 58, 237), // Vibrant Purple
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDemoTuongTranh.FlatAppearance.BorderSize = 0;
            btnDemoTuongTranh.Click += (s, e) =>
            {
                var frmDemo = new FrmConcurrencyDemo();
                frmDemo.Show();
            };

            pnlBody.Controls.Add(lblServerInfo);
            pnlBody.Controls.Add(btnConfigServer);
            pnlBody.Controls.Add(lblUser);
            pnlBody.Controls.Add(txtUsername);
            pnlBody.Controls.Add(lblPass);
            pnlBody.Controls.Add(txtPassword);
            pnlBody.Controls.Add(chkShowPassword);
            pnlBody.Controls.Add(btnLogin);
            pnlBody.Controls.Add(btnExit);
            pnlBody.Controls.Add(btnDemoTuongTranh);

            this.Controls.Add(pnlBody);
            this.AcceptButton = btnLogin;
        }

        private void BtnConfigServer_Click(object? sender, EventArgs e)
        {
            using var dlg = new Form
            {
                Text = "Cấu Hình Chuỗi Kết Nối SQL Server",
                Size = new Size(450, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Font = new Font("Segoe UI", 9.5F)
            };

            var lblSrv = new Label { Text = "Tên Server (VD: THIEN-PHUC hoặc .\\SQLEXPRESS):", Location = new Point(20, 20), Size = new Size(390, 22) };
            var txtSrv = new TextBox { Text = App_Config.ServerName, Location = new Point(20, 45), Size = new Size(390, 26) };

            var lblDb = new Label { Text = "Tên Database:", Location = new Point(20, 80), Size = new Size(390, 22) };
            var txtDb = new TextBox { Text = App_Config.DatabaseName, Location = new Point(20, 105), Size = new Size(390, 26) };

            var btnTest = new Button { Text = "Kiểm Tra Kết Nối", Location = new Point(20, 150), Size = new Size(150, 35), BackColor = Color.FromArgb(241, 245, 249) };
            var btnSave = new Button { Text = "Lưu Cấu Hình", Location = new Point(180, 150), Size = new Size(130, 35), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(320, 150), Size = new Size(90, 35) };

            btnTest.Click += (ts, te) =>
            {
                var oldSrv = App_Config.ServerName;
                var oldDb = App_Config.DatabaseName;
                App_Config.ServerName = txtSrv.Text.Trim();
                App_Config.DatabaseName = txtDb.Text.Trim();

                if (DatabaseHelper.TestConnection(out string error))
                {
                    MessageBox.Show("Kết nối đến SQL Server thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Kết nối thất bại!\n\nChi tiết lỗi: {error}", "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    App_Config.ServerName = oldSrv;
                    App_Config.DatabaseName = oldDb;
                }
            };

            btnSave.Click += (ss, se) =>
            {
                App_Config.ServerName = txtSrv.Text.Trim();
                App_Config.DatabaseName = txtDb.Text.Trim();
                lblServerInfo.Text = $"Server: {App_Config.ServerName} | DB: {App_Config.DatabaseName}";
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            btnCancel.Click += (cs, ce) => dlg.Close();

            dlg.Controls.AddRange(new Control[] { lblSrv, txtSrv, lblDb, txtDb, btnTest, btnSave, btnCancel });
            dlg.ShowDialog();
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string sql = @"
                    SELECT v.MaTaiKhoan, v.TenDangNhap, v.MaRole, v.TenRole, v.TrangThai,
                           dg.MaDG, dg.HoTen AS TenDocGia,
                           nv.MaNV, nv.HoTen AS TenNhanVien
                    FROM View_TaiKhoan_Role v
                    JOIN TaiKhoan tk ON v.MaTaiKhoan = tk.MaTaiKhoan
                    LEFT JOIN DocGia dg ON v.MaTaiKhoan = dg.MaTaiKhoan
                    LEFT JOIN NhanVien nv ON v.MaTaiKhoan = nv.MaTaiKhoan
                    WHERE v.TenDangNhap = @user AND tk.MatKhau = @pass;";

                var pars = new SqlParameter[]
                {
                    new SqlParameter("@user", username),
                    new SqlParameter("@pass", password)
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(sql, pars);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Tên đăng nhập hoặc mật khẩu không chính xác!", "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DataRow row = dt.Rows[0];
                string trangThai = row["TrangThai"].ToString() ?? "";

                if (trangThai.Equals("Khoa", StringComparison.OrdinalIgnoreCase) || trangThai.Equals("TamKhoa", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Tài khoản của bạn đang ở trạng thái '{trangThai}' (bị khóa/tạm khóa).\nVui lòng liên hệ Thủ thư hoặc Quản trị viên để được mở khóa!", "Tài khoản bị khóa", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                // Save session
                SessionContext.MaTaiKhoan = row["MaTaiKhoan"].ToString() ?? "";
                SessionContext.TenDangNhap = row["TenDangNhap"].ToString() ?? "";
                SessionContext.MaRole = row["MaRole"].ToString() ?? "";
                SessionContext.TenRole = row["TenRole"].ToString() ?? "";
                SessionContext.MaDG = row["MaDG"] != DBNull.Value ? row["MaDG"].ToString() ?? "" : "";
                SessionContext.TenDocGia = row["TenDocGia"] != DBNull.Value ? row["TenDocGia"].ToString() ?? "" : "";
                SessionContext.MaNV = row["MaNV"] != DBNull.Value ? row["MaNV"].ToString() ?? "" : "";
                SessionContext.TenNhanVien = row["TenNhanVien"] != DBNull.Value ? row["TenNhanVien"].ToString() ?? "" : "";

                this.Hide();

                // Route according to Role
                Form mainForm;
                if (SessionContext.MaRole == "R001") // Admin
                {
                    mainForm = new FrmMainAdmin();
                }
                else if (SessionContext.MaRole == "R002") // ThuThu
                {
                    mainForm = new FrmMainThuThu();
                }
                else // DocGia
                {
                    mainForm = new FrmMainDocGia();
                }

                mainForm.FormClosed += (fs, fe) =>
                {
                    SessionContext.Clear();
                    this.Show();
                    txtPassword.Clear();
                };

                mainForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối đến CSDL hoặc có lỗi xảy ra:\n\n{ex.Message}\n\nVui lòng kiểm tra lại cấu hình tên Server '{App_Config.ServerName}'.", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
