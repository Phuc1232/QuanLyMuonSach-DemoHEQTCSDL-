using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ProjectHEQTCSDL.Core;
using ProjectHEQTCSDL.Models;

namespace ProjectHEQTCSDL.FormUI
{
    public class FrmMainAdmin : Form
    {
        // Navigation & Layout
        private Panel pnlSidebar = null!;
        private Label lblUser = null!;
        private Button btnMenuBaoCao = null!;
        private Button btnMenuTaiKhoan = null!;
        private Button btnMenuSach = null!;
        private Button btnMenuDemo = null!;
        private bool isSidebarExpanded = true;
        private TabControl tabAdmin = null!;
        
        // Tab 1: Bao Cao
        private ComboBox cboLoaiBaoCao = null!;
        private DataGridView dgvBaoCao = null!;
        private Panel pnlFilterDT = null!;
        private DateTimePicker dtpTuNgay = null!;
        private DateTimePicker dtpDenNgay = null!;
        private Label lblTongTienPhat = null!;
        
        // Tab 2: Tai Khoan
        private DataGridView dgvTaiKhoan = null!;

        // Tab 3: Sach
        private DataGridView dgvSach = null!;

        public FrmMainAdmin()
        {
            InitializeComponent();
            this.Load += (s, e) => LoadAllAdminData();
        }

        private void InitializeComponent()
        {
            this.Text = "Bảng Điều Khiển Quản Trị Viên (Admin) - Thư Viện";
            this.Size = new Size(1280, 840);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);
            this.BackColor = Color.FromArgb(248, 250, 252);

            // 1. Sidebar Panel
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = Color.FromArgb(15, 23, 42) // Slate 900
            };

            var pnlSidebarHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(2, 6, 23) };
            var btnToggle = new Button
            {
                Text = "☰",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(10, 15),
                Cursor = Cursors.Hand
            };
            btnToggle.FlatAppearance.BorderSize = 0;
            btnToggle.Click += (s, e) => ToggleSidebar();

            var lblLogo = new Label
            {
                Text = "ADMIN DASHBOARD",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(55, 22),
                AutoSize = true
            };
            pnlSidebarHeader.Controls.AddRange(new Control[] { btnToggle, lblLogo });

            lblUser = new Label
            {
                Text = $"👤 {SessionContext.TenNhanVien}\n(Vai trò: {SessionContext.TenRole})",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(15, 85),
                AutoSize = true
            };

            btnMenuBaoCao = CreateNavButton("📊 Báo Cáo & Thống Kê", 140);
            btnMenuBaoCao.Click += (s, e) => SwitchTab(0, btnMenuBaoCao);

            btnMenuTaiKhoan = CreateNavButton("👥 Quản Lý Tài Khoản", 190);
            btnMenuTaiKhoan.Click += (s, e) => SwitchTab(1, btnMenuTaiKhoan);

            btnMenuSach = CreateNavButton("📚 Quản Lý Sách", 240);
            btnMenuSach.Click += (s, e) => SwitchTab(2, btnMenuSach);

            btnMenuDemo = CreateNavButton("⚡ Demo Tương Tranh", 290);
            btnMenuDemo.ForeColor = Color.FromArgb(252, 165, 165); // Nổi bật đỏ nhạt
            btnMenuDemo.Click += (s, e) => new FrmConcurrencyDemo().ShowDialog();

            var btnLogout = new Button
            {
                Text = "🚪 Đăng Xuất",
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) => this.Close();

            pnlSidebar.Controls.AddRange(new Control[] { pnlSidebarHeader, lblUser, btnMenuBaoCao, btnMenuTaiKhoan, btnMenuSach, btnMenuDemo, btnLogout });

            // 2. Main Content TabControl (Headers hidden)
            tabAdmin = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                Appearance = TabAppearance.FlatButtons,
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed
            };

            var tabBaoCao = new TabPage();
            var tabTaiKhoan = new TabPage();
            var tabSach = new TabPage();

            tabAdmin.TabPages.AddRange(new TabPage[] { tabBaoCao, tabTaiKhoan, tabSach });

            BuildTabBaoCao(tabBaoCao);
            BuildTabTaiKhoan(tabTaiKhoan);
            BuildTabSach(tabSach);

            this.Controls.Add(tabAdmin);
            this.Controls.Add(pnlSidebar);
            
            tabAdmin.BringToFront();
            SwitchTab(0, btnMenuBaoCao);
        }

        // =========================================================================
        // SIDEBAR HELPERS
        // =========================================================================
        private Button CreateNavButton(string text, int yPos)
        {
            var btn = new Button
            {
                Text = "  " + text,
                Location = new Point(0, yPos),
                Size = new Size(260, 50),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(203, 213, 225),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SwitchTab(int index, Button activeBtn)
        {
            tabAdmin.SelectedIndex = index;
            
            btnMenuBaoCao.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuTaiKhoan.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuSach.BackColor = Color.FromArgb(15, 23, 42);

            btnMenuBaoCao.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuTaiKhoan.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuSach.ForeColor = Color.FromArgb(203, 213, 225);

            activeBtn.BackColor = Color.FromArgb(51, 65, 85);
            activeBtn.ForeColor = Color.White;
        }

        private void ToggleSidebar()
        {
            isSidebarExpanded = !isSidebarExpanded;
            if (isSidebarExpanded)
            {
                pnlSidebar.Width = 260;
                btnMenuBaoCao.Text = "  📊 Báo Cáo & Thống Kê";
                btnMenuTaiKhoan.Text = "  👥 Quản Lý Tài Khoản";
                btnMenuSach.Text = "  📚 Quản Lý Sách";
                btnMenuDemo.Text = "  ⚡ Demo Tương Tranh";
            }
            else
            {
                pnlSidebar.Width = 60;
                btnMenuBaoCao.Text = "  📊";
                btnMenuTaiKhoan.Text = "  👥";
                btnMenuSach.Text = "  📚";
                btnMenuDemo.Text = "  ⚡";
            }
        }

        // =========================================================================
        // TAB 1: BÁO CÁO & THỐNG KÊ
        // =========================================================================
        private void BuildTabBaoCao(TabPage tab)
        {
            tab.BackColor = Color.White;

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(15) };
            
            var lblSelect = new Label { Text = "📌 Chọn Loại Báo Cáo:", Location = new Point(15, 20), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            cboLoaiBaoCao = new ComboBox
            {
                Location = new Point(220, 16),
                Size = new Size(350, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            cboLoaiBaoCao.Items.AddRange(new string[] {
                "1. Sách Quá Hạn Chưa Trả (View_PhieuQuaHan)",
                "2. Tồn Kho Bản Sao (View_SachConTrong)",
                "3. Lượt Mượn Theo Thể Loại (View_ThongKeTheoTheLoai)",
                "4. Top 5 Sách Mượn Nhiều Nhất (sp_Top5SachMuonNhieu)",
                "5. Doanh Thu Tiền Phạt (sp_BaoCaoTienPhat)"
            });
            cboLoaiBaoCao.SelectedIndexChanged += CboLoaiBaoCao_SelectedIndexChanged;

            var btnRefresh = new Button { Text = "↺ Làm Mới Dữ Liệu", Location = new Point(590, 14), AutoSize = true, MinimumSize = new Size(160, 32), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRefresh.Click += (s, e) => CboLoaiBaoCao_SelectedIndexChanged(null, EventArgs.Empty);

            pnlTop.Controls.AddRange(new Control[] { lblSelect, cboLoaiBaoCao, btnRefresh });

            // Panel bộ lọc cho Doanh thu tiền phạt
            pnlFilterDT = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.White, Padding = new Padding(15), Visible = false };
            var lblTu = new Label { Text = "Từ ngày:", Location = new Point(15, 20), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            dtpTuNgay = new DateTimePicker { Location = new Point(90, 16), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            var lblDen = new Label { Text = "Đến ngày:", Location = new Point(220, 20), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            dtpDenNgay = new DateTimePicker { Location = new Point(300, 16), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            var btnXemDT = new Button { Text = "Thống Kê", Location = new Point(440, 14), Size = new Size(100, 32), BackColor = Color.FromArgb(22, 163, 74), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnXemDT.Click += (s, e) => LoadSPBaoCaoTienPhat();
            lblTongTienPhat = new Label { Text = "Tổng thu: 0 VNĐ", Location = new Point(570, 18), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(220, 38, 38) };
            
            pnlFilterDT.Controls.AddRange(new Control[] { lblTu, dtpTuNgay, lblDen, dtpDenNgay, btnXemDT, lblTongTienPhat });

            dgvBaoCao = CreateStandardGrid();
            
            tab.Controls.Add(dgvBaoCao);
            tab.Controls.Add(pnlFilterDT);
            tab.Controls.Add(pnlTop);

            pnlTop.BringToFront();
            pnlFilterDT.BringToFront();
            dgvBaoCao.BringToFront();

            cboLoaiBaoCao.SelectedIndex = 0;
        }

        private void CboLoaiBaoCao_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboLoaiBaoCao.SelectedIndex == -1) return;

            int index = cboLoaiBaoCao.SelectedIndex;
            pnlFilterDT.Visible = (index == 4);

            dgvBaoCao.DataSource = null;
            dgvBaoCao.Columns.Clear();

            switch (index)
            {
                case 0: LoadViewQuaHan(); break;
                case 1: LoadViewSachConTrong(); break;
                case 2: LoadViewTheLoai(); break;
                case 3: LoadSPTop5(); break;
                case 4: LoadSPBaoCaoTienPhat(); break;
            }
        }

        // =========================================================================
        // TAB 2: QUẢN LÝ TÀI KHOẢN NGƯỜI DÙNG
        // =========================================================================
        private void BuildTabTaiKhoan(TabPage tab)
        {
            tab.BackColor = Color.White;

            var pnlAccToolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(15, 10, 10, 5) };

            var btnLock = new Button { Text = "🔒 Khóa Tài Khoản", Size = new Size(160, 35), BackColor = Color.FromArgb(220, 38, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 10, 0), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            var btnUnlock = new Button { Text = "🔓 Kích Hoạt (Mở)", Size = new Size(160, 35), BackColor = Color.FromArgb(22, 163, 74), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 10, 0), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            var btnResetPass = new Button { Text = "🔑 Reset Mật Khẩu (123456)", Size = new Size(240, 35), BackColor = Color.FromArgb(71, 85, 105), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 10, 0), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            var btnRefreshAcc = new Button { Text = "↺ Làm Mới", Size = new Size(120, 35), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };

            btnLock.Click += (s, e) => ChangeAccountStatus("Khoa");
            btnUnlock.Click += (s, e) => ChangeAccountStatus("HoatDong");
            btnResetPass.Click += (s, e) => ResetPassword();
            btnRefreshAcc.Click += (s, e) => LoadTaiKhoan();

            pnlAccToolbar.Controls.AddRange(new Control[] { btnLock, btnUnlock, btnResetPass, btnRefreshAcc });
            dgvTaiKhoan = CreateStandardGrid();

            var lblAccHeader = new Label { Text = "DANH SÁCH TÀI KHOẢN NGƯỜI DÙNG TẠI HỆ THỐNG:", Dock = DockStyle.Top, Height = 35, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Padding = new Padding(10, 8, 0, 0), ForeColor = Color.FromArgb(30, 58, 138) };

            tab.Controls.Add(dgvTaiKhoan);
            tab.Controls.Add(pnlAccToolbar);
            tab.Controls.Add(lblAccHeader);

            lblAccHeader.BringToFront();
            pnlAccToolbar.BringToFront();
            dgvTaiKhoan.BringToFront();
        }

        // =========================================================================
        // TAB 3: QUẢN LÝ SÁCH & TEST TRIGGER 3
        // =========================================================================
        private void BuildTabSach(TabPage tab)
        {
            tab.BackColor = Color.White;

            var pnlToolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 55, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(15, 10, 10, 5) };

            var btnDeleteSach = new Button
            {
                Text = "❌ Xóa Đầu Sách Được Chọn",
                Size = new Size(340, 35),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnDeleteSach.Click += BtnDeleteSach_Click;

            var btnRefreshSach = new Button { Text = "↺ Làm Mới", Size = new Size(120, 35), BackColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnRefreshSach.Click += (s, e) => LoadSach();

            var lblHelp = new Label
            {
                Text = "💡 Trigger 3 (trg_NganXoaSachDangMuon) sẽ chặn lệnh DELETE nếu sách đang cho mượn!",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(71, 85, 105),
                Margin = new Padding(20, 8, 0, 0)
            };

            pnlToolbar.Controls.AddRange(new Control[] { btnDeleteSach, btnRefreshSach, lblHelp });

            dgvSach = CreateStandardGrid();
            
            var lblSachHeader = new Label { Text = "QUẢN LÝ DANH MỤC ĐẦU SÁCH:", Dock = DockStyle.Top, Height = 35, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Padding = new Padding(10, 8, 0, 0), ForeColor = Color.FromArgb(30, 58, 138) };

            tab.Controls.Add(dgvSach);
            tab.Controls.Add(pnlToolbar);
            tab.Controls.Add(lblSachHeader);

            lblSachHeader.BringToFront();
            pnlToolbar.BringToFront();
            dgvSach.BringToFront();
        }

        // =========================================================================
        // CÁC HÀM XỬ LÝ DỮ LIỆU BÁO CÁO & THỐNG KÊ
        // =========================================================================
        private void LoadAllAdminData()
        {
            LoadTaiKhoan();
            LoadSach();
        }

        private void LoadViewQuaHan()
        {
            try 
            { 
                dgvBaoCao.DataSource = DatabaseHelper.ExecuteQuery("SELECT * FROM View_PhieuQuaHan ORDER BY SoNgayTre DESC");
                if (dgvBaoCao.Columns.Contains("MaPhieuMuon")) dgvBaoCao.Columns["MaPhieuMuon"].HeaderText = "Mã Phiếu Mượn";
                if (dgvBaoCao.Columns.Contains("HoTen")) dgvBaoCao.Columns["HoTen"].HeaderText = "Tên Độc Giả";
                if (dgvBaoCao.Columns.Contains("TenSach")) dgvBaoCao.Columns["TenSach"].HeaderText = "Tên Sách";
                if (dgvBaoCao.Columns.Contains("SoNgayTre")) dgvBaoCao.Columns["SoNgayTre"].HeaderText = "Số Ngày Trễ";
            } 
            catch { }
        }

        private void LoadViewSachConTrong()
        {
            try 
            { 
                dgvBaoCao.DataSource = DatabaseHelper.ExecuteQuery("SELECT * FROM View_SachConTrong");
                if (dgvBaoCao.Columns.Contains("MaSach")) dgvBaoCao.Columns["MaSach"].HeaderText = "Mã Sách";
                if (dgvBaoCao.Columns.Contains("TenSach")) dgvBaoCao.Columns["TenSach"].HeaderText = "Tên Sách";
                if (dgvBaoCao.Columns.Contains("TongBanSao")) dgvBaoCao.Columns["TongBanSao"].HeaderText = "Tổng Bản Sao";
                if (dgvBaoCao.Columns.Contains("SoBanConTrong")) dgvBaoCao.Columns["SoBanConTrong"].HeaderText = "Số Bản Còn Trống";
            } 
            catch { }
        }

        private void LoadViewTheLoai()
        {
            try 
            { 
                dgvBaoCao.DataSource = DatabaseHelper.ExecuteQuery("SELECT * FROM View_ThongKeTheoTheLoai");
                if (dgvBaoCao.Columns.Contains("MaTheLoai")) dgvBaoCao.Columns["MaTheLoai"].HeaderText = "Mã Thể Loại";
                if (dgvBaoCao.Columns.Contains("TenTheLoai")) dgvBaoCao.Columns["TenTheLoai"].HeaderText = "Tên Thể Loại";
                if (dgvBaoCao.Columns.Contains("TongLuotMuon")) dgvBaoCao.Columns["TongLuotMuon"].HeaderText = "Tổng Lượt Mượn";
            } 
            catch { }
        }

        private void LoadSPTop5()
        {
            try 
            { 
                dgvBaoCao.DataSource = DatabaseHelper.ExecuteProcedure("sp_Top5SachMuonNhieu");
                if (dgvBaoCao.Columns.Contains("MaSach")) dgvBaoCao.Columns["MaSach"].HeaderText = "Mã Sách";
                if (dgvBaoCao.Columns.Contains("TenSach")) dgvBaoCao.Columns["TenSach"].HeaderText = "Tên Sách";
                if (dgvBaoCao.Columns.Contains("SoLuotMuon")) dgvBaoCao.Columns["SoLuotMuon"].HeaderText = "Số Lượt Mượn";
            } 
            catch { }
        }

        private void LoadSPBaoCaoTienPhat()
        {
            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_TuNgay", dtpTuNgay.Value.Date),
                    new SqlParameter("@p_DenNgay", dtpDenNgay.Value.Date)
                };
                DataTable dt = DatabaseHelper.ExecuteProcedure("sp_BaoCaoTienPhat", pars);
                dgvBaoCao.DataSource = dt;

                if (dgvBaoCao.Columns.Contains("LyDoPhat")) dgvBaoCao.Columns["LyDoPhat"].HeaderText = "Lý Do Phạt";
                if (dgvBaoCao.Columns.Contains("SoLuong")) dgvBaoCao.Columns["SoLuong"].HeaderText = "Số Lượng Phiếu";
                if (dgvBaoCao.Columns.Contains("TongTien")) 
                {
                    dgvBaoCao.Columns["TongTien"].HeaderText = "Tổng Tiền (VNĐ)";
                    dgvBaoCao.Columns["TongTien"].DefaultCellStyle.Format = "N0";
                    dgvBaoCao.Columns["TongTien"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                decimal tongTien = 0;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["TongTien"] != DBNull.Value) tongTien += Convert.ToDecimal(r["TongTien"]);
                }
                lblTongTienPhat.Text = $"Tổng thu: {tongTien:N0} VNĐ";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load báo cáo doanh thu phạt: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================
        // CÁC HÀM XỬ LÝ QUẢN LÝ SÁCH & TÀI KHOẢN
        // =========================================================================
        private void LoadTaiKhoan()
        {
            try
            {
                string sql = @"
                    SELECT v.MaTaiKhoan, v.TenDangNhap, v.TenRole, v.TrangThai, v.NgayTao,
                           ISNULL(dg.HoTen, nv.HoTen) AS HoTenNguoiDung,
                           ISNULL(dg.SDT, nv.SDT) AS SDT
                    FROM View_TaiKhoan_Role v
                    LEFT JOIN DocGia dg ON v.MaTaiKhoan = dg.MaTaiKhoan
                    LEFT JOIN NhanVien nv ON v.MaTaiKhoan = nv.MaTaiKhoan;";
                dgvTaiKhoan.DataSource = DatabaseHelper.ExecuteQuery(sql);
                
                if (dgvTaiKhoan.Columns.Contains("MaTaiKhoan")) dgvTaiKhoan.Columns["MaTaiKhoan"].HeaderText = "Mã TK";
                if (dgvTaiKhoan.Columns.Contains("TenDangNhap")) dgvTaiKhoan.Columns["TenDangNhap"].HeaderText = "Tên Đăng Nhập";
                if (dgvTaiKhoan.Columns.Contains("TenRole")) dgvTaiKhoan.Columns["TenRole"].HeaderText = "Vai Trò";
                if (dgvTaiKhoan.Columns.Contains("TrangThai")) dgvTaiKhoan.Columns["TrangThai"].HeaderText = "Trạng Thái";
                if (dgvTaiKhoan.Columns.Contains("NgayTao")) dgvTaiKhoan.Columns["NgayTao"].HeaderText = "Ngày Tạo";
                if (dgvTaiKhoan.Columns.Contains("HoTenNguoiDung")) dgvTaiKhoan.Columns["HoTenNguoiDung"].HeaderText = "Họ Tên";
            }
            catch { }
        }

        private void LoadSach()
        {
            try
            {
                string sql = @"
                    SELECT v.MaSach, v.TenSach, v.TenNXB, v.TenTheLoai, v.NamXB,
                           COUNT(cs.MaCuonSach) AS TongCuon,
                           SUM(CASE WHEN cs.TrangThai = 'CoSan' THEN 1 ELSE 0 END) AS CoSan,
                           SUM(CASE WHEN cs.TrangThai = 'DangMuon' THEN 1 ELSE 0 END) AS DangMuon
                    FROM View_DanhSachDauSach v
                    LEFT JOIN CuonSach cs ON v.MaSach = cs.MaSach
                    GROUP BY v.MaSach, v.TenSach, v.TenNXB, v.TenTheLoai, v.NamXB;";
                dgvSach.DataSource = DatabaseHelper.ExecuteQuery(sql);

                if (dgvSach.Columns.Contains("MaSach")) dgvSach.Columns["MaSach"].HeaderText = "Mã Sách";
                if (dgvSach.Columns.Contains("TenSach")) dgvSach.Columns["TenSach"].HeaderText = "Tên Sách";
                if (dgvSach.Columns.Contains("TenNXB")) dgvSach.Columns["TenNXB"].HeaderText = "Nhà Xuất Bản";
                if (dgvSach.Columns.Contains("TenTheLoai")) dgvSach.Columns["TenTheLoai"].HeaderText = "Thể Loại";
                if (dgvSach.Columns.Contains("NamXB")) dgvSach.Columns["NamXB"].HeaderText = "Năm XB";
                if (dgvSach.Columns.Contains("TongCuon")) dgvSach.Columns["TongCuon"].HeaderText = "Tổng Bản Sao";
                if (dgvSach.Columns.Contains("CoSan")) dgvSach.Columns["CoSan"].HeaderText = "Sẵn Có";
                if (dgvSach.Columns.Contains("DangMuon")) dgvSach.Columns["DangMuon"].HeaderText = "Đang Mượn";
            }
            catch { }
        }

        private void ChangeAccountStatus(string newStatus)
        {
            if (dgvTaiKhoan.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn 1 tài khoản trong danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string maTK = dgvTaiKhoan.CurrentRow.Cells["MaTaiKhoan"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(maTK)) return;

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@status", newStatus),
                    new SqlParameter("@id", maTK)
                };
                int rows = DatabaseHelper.ExecuteNonQuery("UPDATE TaiKhoan SET TrangThai = @status WHERE MaTaiKhoan = @id", pars);

                if (rows > 0)
                {
                    MessageBox.Show($"Đã cập nhật trạng thái tài khoản '{maTK}' thành '{newStatus}'!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadTaiKhoan();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật trạng thái: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetPassword()
        {
            if (dgvTaiKhoan.CurrentRow == null) return;
            string maTK = dgvTaiKhoan.CurrentRow.Cells["MaTaiKhoan"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(maTK)) return;

            if (MessageBox.Show($"Bạn có chắc chắn muốn đặt lại mật khẩu của tài khoản '{maTK}' về '123456'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    DatabaseHelper.ExecuteNonQuery("UPDATE TaiKhoan SET MatKhau = '123456' WHERE MaTaiKhoan = @id", new SqlParameter[] { new SqlParameter("@id", maTK) });
                    MessageBox.Show("Đã reset mật khẩu thành công về '123456'!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi reset mật khẩu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDeleteSach_Click(object? sender, EventArgs e)
        {
            if (dgvSach.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn 1 đầu sách trong danh sách cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string maSach = dgvSach.CurrentRow.Cells["MaSach"].Value?.ToString() ?? "";
            string tenSach = dgvSach.CurrentRow.Cells["TenSach"].Value?.ToString() ?? "";

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa đầu sách: '{tenSach}' ({maSach})?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var pars = new SqlParameter[] { new SqlParameter("@id", maSach) };
                DatabaseHelper.ExecuteNonQuery("DELETE FROM Sach WHERE MaSach = @id", pars);

                MessageBox.Show($"Đã xóa đầu sách '{tenSach}' thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadSach();
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"❌ TRIGGER NGĂN CHẶN THAO TÁC XÓA:\n\n{sqlEx.Message}", "Cảnh Báo Toàn Vẹn CSDL", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================
        // HELPER CONTROLS
        // =========================================================================
        private DataGridView CreateStandardGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9.5F)
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
        }
    }
}
