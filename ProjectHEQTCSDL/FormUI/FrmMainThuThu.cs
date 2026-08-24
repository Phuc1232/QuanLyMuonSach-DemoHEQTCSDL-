using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ProjectHEQTCSDL.Core;
using ProjectHEQTCSDL.Models;

namespace ProjectHEQTCSDL.FormUI
{
    public class FrmMainThuThu : Form
    {
        private TabControl tabThuThu = null!;

        // Navigation & Layout
        private Panel pnlSidebar = null!;
        private Label lblUser = null!;
        private Button btnMenuLapPM = null!;
        private Button btnMenuTraSach = null!;
        private Button btnMenuDatTruoc = null!;
        private Button btnMenuThuPhat = null!;
        private Button btnMenuQuaHan = null!;
        private Button btnMenuCaNhan = null!;
        private bool isSidebarExpanded = true;

        // Tab 1: Lập Phiếu Mượn
        private TextBox txtMaPM = null!;
        private ComboBox cboDocGia = null!;
        private Label lblKiemTraThe = null!;
        private Label lblSoSachDangMuon = null!;
        private DateTimePicker dtpNgayHenTra = null!;
        private ComboBox cboCuonSachCoSan = null!;
        private DataGridView dgvSachMuon = null!;
        private DataTable dtDanhSachCuonMuon = null!;

        // Tab 2: Trả Sách
        private DataGridView dgvPhieuMuonDangMuon = null!;
        private DataGridView dgvChiTietTra = null!;
        private Label lblThongTinTreHan = null!;

        // Tab 6: Thông Tin Cá Nhân
        private TextBox txtMaNV_Profile = null!;
        private TextBox txtTenDangNhap_Profile = null!;
        private TextBox txtChucVu_Profile = null!;
        private TextBox txtHoTen_Profile = null!;
        private TextBox txtSDT_Profile = null!;
        private TextBox txtEmail_Profile = null!;
        private TextBox txtMatKhauMoi_Profile = null!;
        private TextBox txtXacNhanMatKhau_Profile = null!;

        // Tab 3: Xử Lý Đặt Trước
        private DataGridView dgvDatTruoc = null!;
        private ComboBox cboCuonSachTraVe = null!;
        private TextBox txtMaPMMoi = null!;
        private DateTimePicker dtpNgayHenTraDT = null!;

        // Tab 4: Thu Tiền Phạt
        private DataGridView dgvPhieuPhat = null!;

        // Tab 5: Danh Sách Quá Hạn
        private DataGridView dgvQuaHan = null!;

        public FrmMainThuThu()
        {
            InitializeComponent();
            this.Load += (s, e) => LoadAllThuThuData();
        }

        private void InitializeComponent()
        {
            this.Text = "Hệ Thống Thủ Thư - Quản Lý Mượn Trả Sách";
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
                Text = "LIBRARIAN",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(60, 22),
                AutoSize = true
            };
            pnlSidebarHeader.Controls.AddRange(new Control[] { btnToggle, lblLogo });

            lblUser = new Label
            {
                Text = $"👤 {SessionContext.TenNhanVien}\n(NV: {SessionContext.MaNV})",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(15, 85),
                AutoSize = true
            };

            btnMenuLapPM = CreateNavButton("📖 Lập Phiếu Mượn", 130);
            btnMenuLapPM.Click += (s, e) => SwitchTab(0, btnMenuLapPM);

            btnMenuTraSach = CreateNavButton("↩️ Trả Sách & Phạt", 180);
            btnMenuTraSach.Click += (s, e) => SwitchTab(1, btnMenuTraSach);

            btnMenuDatTruoc = CreateNavButton("⏳ Xử Lý Đặt Trước", 230);
            btnMenuDatTruoc.Click += (s, e) => SwitchTab(2, btnMenuDatTruoc);

            btnMenuThuPhat = CreateNavButton("💵 Thu Tiền Phạt", 280);
            btnMenuThuPhat.Click += (s, e) => SwitchTab(3, btnMenuThuPhat);

            btnMenuQuaHan = CreateNavButton("📞 Liên Hệ Quá Hạn", 330);
            btnMenuQuaHan.Click += (s, e) => SwitchTab(4, btnMenuQuaHan);

            btnMenuCaNhan = CreateNavButton("⚙️ Thông Tin Cá Nhân", 380);
            btnMenuCaNhan.Click += (s, e) => SwitchTab(5, btnMenuCaNhan);

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

            pnlSidebar.Controls.AddRange(new Control[] { pnlSidebarHeader, lblUser, btnMenuLapPM, btnMenuTraSach, btnMenuDatTruoc, btnMenuThuPhat, btnMenuQuaHan, btnMenuCaNhan, btnLogout });

            // 2. Main Content TabControl (Headers hidden)
            tabThuThu = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                Appearance = TabAppearance.FlatButtons,
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed
            };

            var tabLapPhieu = new TabPage();
            var tabTraSach = new TabPage();
            var tabDatTruoc = new TabPage();
            var tabThuPhat = new TabPage();
            var tabQuaHan = new TabPage();
            var tabCaNhan = new TabPage();

            tabThuThu.TabPages.AddRange(new TabPage[] { tabLapPhieu, tabTraSach, tabDatTruoc, tabThuPhat, tabQuaHan, tabCaNhan });

            BuildTabLapPhieu(tabLapPhieu);
            BuildTabTraSach(tabTraSach);
            BuildTabDatTruoc(tabDatTruoc);
            BuildTabThuPhat(tabThuPhat);
            BuildTabQuaHan(tabQuaHan);
            BuildTabCaNhan(tabCaNhan);

            this.Controls.Add(tabThuThu);
            this.Controls.Add(pnlSidebar);
            
            tabThuThu.BringToFront();
            SwitchTab(0, btnMenuLapPM);
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
            tabThuThu.SelectedIndex = index;
            
            btnMenuLapPM.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuTraSach.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuDatTruoc.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuThuPhat.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuQuaHan.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuCaNhan.BackColor = Color.FromArgb(15, 23, 42);

            btnMenuLapPM.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuTraSach.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuDatTruoc.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuThuPhat.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuQuaHan.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuCaNhan.ForeColor = Color.FromArgb(203, 213, 225);

            activeBtn.BackColor = Color.FromArgb(51, 65, 85);
            activeBtn.ForeColor = Color.White;
        }

        private void ToggleSidebar()
        {
            isSidebarExpanded = !isSidebarExpanded;
            if (isSidebarExpanded)
            {
                pnlSidebar.Width = 260;
                btnMenuLapPM.Text = "  📖 Lập Phiếu Mượn";
                btnMenuTraSach.Text = "  ↩️ Trả Sách & Phạt";
                btnMenuDatTruoc.Text = "  ⏳ Xử Lý Đặt Trước";
                btnMenuThuPhat.Text = "  💵 Thu Tiền Phạt";
                btnMenuQuaHan.Text = "  📞 Liên Hệ Quá Hạn";
                btnMenuCaNhan.Text = "  ⚙️ Thông Tin Cá Nhân";
            }
            else
            {
                pnlSidebar.Width = 60;
                btnMenuLapPM.Text = "  📖";
                btnMenuTraSach.Text = "  ↩️";
                btnMenuDatTruoc.Text = "  ⏳";
                btnMenuThuPhat.Text = "  💵";
                btnMenuQuaHan.Text = "  📞";
                btnMenuCaNhan.Text = "  ⚙️";
            }
        }

        // =========================================================================
        // TAB 1: LẬP PHIẾU MƯỢN (sp_LapPhieuMuon + TVP + Trigger 1 & 2)
        // =========================================================================
        private void BuildTabLapPhieu(TabPage tab)
        {
            tab.Font = new Font("Segoe UI", 9.5F);
            tab.BackColor = Color.White;

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 220,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(15)
            };

            // Row 1: Mã Phiếu Mượn & Độc Giả
            var lblMaPM = new Label { Text = "Mã Phiếu Mượn:", Location = new Point(15, 18), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtMaPM = new TextBox { Location = new Point(160, 14), Size = new Size(160, 26), Text = "PM" + DateTime.Now.ToString("HHmmss") };

            var btnGenMaPM = new Button { Text = "Tạo mã", Location = new Point(330, 13), Size = new Size(70, 28), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnGenMaPM.Click += (s, e) => txtMaPM.Text = "PM" + DateTime.Now.ToString("HHmmss");

            var lblDG = new Label { Text = "Chọn Độc Giả:", Location = new Point(410, 18), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            cboDocGia = new ComboBox { Location = new Point(620, 14), Size = new Size(330, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cboDocGia.SelectedIndexChanged += (s, e) => CheckDocGiaConditions();

            // Row 2: Condition check from functions
            lblKiemTraThe = new Label
            {
                Text = "Tình trạng thẻ: (Chưa chọn độc giả)",
                Location = new Point(15, 55),
                Size = new Size(480, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };

            lblSoSachDangMuon = new Label
            {
                Text = "Số sách đang mượn: 0 / 3 cuốn (Tối đa)",
                Location = new Point(620, 55),
                Size = new Size(330, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            // Row 3: Ngày Hẹn Trả
            var lblNgayTra = new Label { Text = "Ngày Hẹn Trả:", Location = new Point(15, 95), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            dtpNgayHenTra = new DateTimePicker { Location = new Point(160, 92), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(14) };

            var lblCuonSach = new Label { Text = "Chọn Cuốn Sách Có Sẵn:", Location = new Point(410, 95), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            cboCuonSachCoSan = new ComboBox { Location = new Point(620, 92), Size = new Size(300, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var btnAddSach = new Button
            {
                Text = "➕ Thêm Vào Phiếu",
                Location = new Point(930, 89),
                Size = new Size(180, 32),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAddSach.Click += BtnAddSach_Click;

            // Row 4: Main confirm button
            var btnConfirmMuon = new Button
            {
                Text = "✔ XÁC NHẬN LẬP PHIẾU MƯỢN (Gọi sp_LapPhieuMuon TVP)",
                Location = new Point(15, 145),
                Size = new Size(500, 42),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirmMuon.Click += BtnConfirmMuon_Click;

            var btnRemoveSach = new Button
            {
                Text = "➖ Xóa Cuốn Đang Chọn Khỏi Lưới",
                Location = new Point(530, 145),
                Size = new Size(300, 42),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRemoveSach.Click += BtnRemoveSach_Click;

            pnlTop.Controls.AddRange(new Control[] {
                lblMaPM, txtMaPM, btnGenMaPM, lblDG, cboDocGia,
                lblKiemTraThe, lblSoSachDangMuon,
                lblNgayTra, dtpNgayHenTra, lblCuonSach, cboCuonSachCoSan, btnAddSach,
                btnConfirmMuon, btnRemoveSach
            });

            // Grid for selected books
            dtDanhSachCuonMuon = new DataTable();
            dtDanhSachCuonMuon.Columns.Add("MaCuonSach", typeof(string));
            dtDanhSachCuonMuon.Columns.Add("MaSach", typeof(string));
            dtDanhSachCuonMuon.Columns.Add("TenSach", typeof(string));
            dtDanhSachCuonMuon.Columns.Add("ViTriKe", typeof(string));
            dtDanhSachCuonMuon.Columns.Add("TrangThai", typeof(string));

            dgvSachMuon = CreateStandardGrid();
            dgvSachMuon.DataSource = dtDanhSachCuonMuon;

            var lblGridHeader = new Label
            {
                Text = "DANH SÁCH CUỐN SÁCH CHUẨN BỊ MƯỢN (Sẽ truyền vào SP qua Table-Valued Parameter):",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10, 5, 0, 0)
            };

            tab.Controls.Add(dgvSachMuon);
            tab.Controls.Add(lblGridHeader);
            tab.Controls.Add(pnlTop);

            pnlTop.BringToFront();
            lblGridHeader.BringToFront();
            dgvSachMuon.BringToFront();
        }

        // =========================================================================
        // TAB 2: TRẢ SÁCH & XỬ LÝ PHẠT (sp_TraSach + TVP + Tính ngày trễ & phạt)
        // =========================================================================
        private void BuildTabTraSach(TabPage tab)
        {
            tab.Font = new Font("Segoe UI", 9.5F);
            tab.BackColor = Color.White;

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 550
            };

            // Left Panel: Danh sách Phiếu Mượn đang mượn
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var lblLeftTitle = new Label { Text = "1. CHỌN PHIẾU MƯỢN CẦN TRẢ (Đang mượn / Quá hạn):", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            
            var pnlLeftToolbar = new Panel { Dock = DockStyle.Top, Height = 40 };
            var btnRefreshPM = new Button { Text = "↺ Tải Lại Danh Sách Phiếu Mượn", Location = new Point(0, 5), Size = new Size(250, 30), BackColor = Color.FromArgb(241, 245, 249), FlatStyle = FlatStyle.Flat };
            btnRefreshPM.Click += (s, e) => LoadPhieuMuonDangMuon();
            pnlLeftToolbar.Controls.Add(btnRefreshPM);

            dgvPhieuMuonDangMuon = CreateStandardGrid();
            dgvPhieuMuonDangMuon.SelectionChanged += DgvPhieuMuonDangMuon_SelectionChanged;

            pnlLeft.Controls.Add(dgvPhieuMuonDangMuon);
            pnlLeft.Controls.Add(pnlLeftToolbar);
            pnlLeft.Controls.Add(lblLeftTitle);

            lblLeftTitle.BringToFront();
            pnlLeftToolbar.BringToFront();
            dgvPhieuMuonDangMuon.BringToFront();

            split.Panel1.Controls.Add(pnlLeft);

            // Right Panel: Chi tiết cuốn sách & Tình trạng trả
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var lblRightTitle = new Label { Text = "2. DANH SÁCH CUỐN SÁCH & CHỌN TÌNH TRẠNG KHI TRẢ:", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };

            lblThongTinTreHan = new Label
            {
                Text = "Thông tin hạn trả: (Chọn phiếu mượn bên trái)",
                Dock = DockStyle.Top,
                Height = 35,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38)
            };

            dgvChiTietTra = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9.5F)
            };

            var pnlActionTra = new Panel { Dock = DockStyle.Bottom, Height = 65, Padding = new Padding(0, 10, 0, 0) };
            var btnConfirmTra = new Button
            {
                Text = "✔ XÁC NHẬN TRẢ SÁCH (Gọi sp_TraSach TVP & Tính Phạt)",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirmTra.Click += BtnConfirmTra_Click;
            pnlActionTra.Controls.Add(btnConfirmTra);

            pnlRight.Controls.Add(dgvChiTietTra);
            pnlRight.Controls.Add(pnlActionTra);
            pnlRight.Controls.Add(lblThongTinTreHan);
            pnlRight.Controls.Add(lblRightTitle);

            lblRightTitle.BringToFront();
            lblThongTinTreHan.BringToFront();
            pnlActionTra.BringToFront();
            dgvChiTietTra.BringToFront();

            split.Panel2.Controls.Add(pnlRight);

            tab.Controls.Add(split);
        }

        // =========================================================================
        // TAB 3: XỬ LÝ ĐẶT TRƯỚC (sp_XuLyDatTruoc)
        // =========================================================================
        private void BuildTabDatTruoc(TabPage tab)
        {
            tab.Font = new Font("Segoe UI", 9.5F);
            tab.BackColor = Color.White;

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 120
            };

            // Top: Khu vực 1 (sp_XuLyDatTruocKhiCoSach)
            var pnlKhuVuc1 = new GroupBox { Text = "1. KÍCH HOẠT GIỮ CHỖ (Khi thu hồi bản sao vật lý vào kho)", Dock = DockStyle.Fill, Padding = new Padding(15) };
            
            var lblCuon = new Label { Text = "Sách Vừa Trả Về (Có Sẵn):", Location = new Point(15, 45), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            cboCuonSachTraVe = new ComboBox { Location = new Point(220, 42), Size = new Size(350, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            
            var btnXuLyKhuVuc1 = new Button { Text = "Giữ Chỗ Cho Độc Giả Chờ Sớm Nhất", Location = new Point(600, 38), Size = new Size(500, 38), BackColor = Color.FromArgb(5, 150, 105), ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnXuLyKhuVuc1.Click += BtnKhuVuc1_Click;

            pnlKhuVuc1.Controls.AddRange(new Control[] { lblCuon, cboCuonSachTraVe, btnXuLyKhuVuc1 });
            split.Panel1.Controls.Add(pnlKhuVuc1);

            // Bottom: Khu vực 2 & 3
            var pnlKhuVuc2 = new GroupBox { Text = "2. QUẢN LÝ DANH SÁCH CHỜ NHẬN SÁCH", Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            var pnlToolbar2 = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(241, 245, 249) };
            
            var lblPM = new Label { Text = "Mã PM Mới:", Location = new Point(15, 15), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtMaPMMoi = new TextBox { Location = new Point(120, 12), Size = new Size(160, 26), Text = "PM" + DateTime.Now.ToString("HHmmss") };
            
            var lblNgay = new Label { Text = "Ngày Trả:", Location = new Point(15, 45), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            dtpNgayHenTraDT = new DateTimePicker { Location = new Point(120, 42), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(14) };

            var btnNhanSach = new Button { Text = "Giao Sách & Lập Phiếu Mượn", Location = new Point(320, 12), Size = new Size(380, 56), BackColor = Color.FromArgb(37, 99, 235), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnNhanSach.Click += BtnKhuVuc2_Click;

            var btnHuy = new Button { Text = "Hủy Giữ Chỗ Quá Hạn 48h", Location = new Point(720, 12), Size = new Size(380, 56), BackColor = Color.FromArgb(220, 38, 38), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnHuy.Click += BtnKhuVuc3_Click;

            pnlToolbar2.Controls.AddRange(new Control[] { lblPM, txtMaPMMoi, lblNgay, dtpNgayHenTraDT, btnNhanSach, btnHuy });

            dgvDatTruoc = CreateStandardGrid();
            
            pnlKhuVuc2.Controls.Add(dgvDatTruoc);
            pnlKhuVuc2.Controls.Add(pnlToolbar2);
            dgvDatTruoc.BringToFront();

            split.Panel2.Controls.Add(pnlKhuVuc2);
            tab.Controls.Add(split);
        }

        // =========================================================================
        // TAB 4: THU TIỀN PHẠT (sp_ThanhToanPhat + Trigger 4)
        // =========================================================================
        private void BuildTabThuPhat(TabPage tab)
        {
            tab.Font = new Font("Segoe UI", 9.5F);
            tab.BackColor = Color.White;

            var pnlToolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(10, 12, 10, 5) };

            var btnPay = new Button
            {
                Text = "💵 Thu Tiền / Xác Nhận Thanh Toán",
                Size = new Size(420, 36),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 15, 0)
            };
            btnPay.Click += BtnPay_Click;

            var btnRefreshPhat = new Button { Text = "↺ Làm Mới", Size = new Size(110, 36), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRefreshPhat.Click += (s, e) => LoadPhieuPhat();

            var lblNote = new Label
            {
                Text = "Nếu độc giả có từ 3 phiếu phạt chưa trả, tài khoản sẽ tự động chuyển 'TamKhoa'!",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(71, 85, 105),
                Margin = new Padding(15, 10, 0, 0)
            };

            pnlToolbar.Controls.AddRange(new Control[] { btnPay, btnRefreshPhat, lblNote });

            dgvPhieuPhat = CreateStandardGrid();

            var lblHeader = new Label
            {
                Text = "DANH SÁCH TẤT CẢ PHIẾU PHẠT (Chọn 1 phiếu để thanh toán):",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10, 5, 0, 0)
            };

            tab.Controls.Add(dgvPhieuPhat);
            tab.Controls.Add(lblHeader);
            tab.Controls.Add(pnlToolbar);

            pnlToolbar.BringToFront();
            lblHeader.BringToFront();
            dgvPhieuPhat.BringToFront();
        }

        // =========================================================================
        // TAB 5: QUẢN LÝ QUÁ HẠN (View_PhieuQuaHan)
        // =========================================================================
        private void BuildTabQuaHan(TabPage tab)
        {
            tab.Font = new Font("Segoe UI", 9.5F);
            tab.BackColor = Color.White;
            
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(241, 245, 249) };
            var btnRefresh = new Button { Text = "↺ Làm Mới Danh Sách Quá Hạn", Location = new Point(10, 10), Size = new Size(250, 30), BackColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRefresh.Click += (s, e) => LoadPhieuQuaHan();
            pnlTop.Controls.Add(btnRefresh);

            dgvQuaHan = CreateStandardGrid();
            
            var lblHeader = new Label
            {
                Text = "DANH SÁCH ĐỘC GIẢ MƯỢN SÁCH QUÁ HẠN CẦN LIÊN HỆ:",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10, 5, 0, 0),
                ForeColor = Color.FromArgb(220, 38, 38)
            };

            tab.Controls.Add(dgvQuaHan);
            tab.Controls.Add(lblHeader);
            tab.Controls.Add(pnlTop);
            pnlTop.BringToFront();
            lblHeader.BringToFront();
            dgvQuaHan.BringToFront();
        }

        // =========================================================================
        // TAB 6: THÔNG TIN CÁ NHÂN THỦ THƯ
        // =========================================================================
        private void BuildTabCaNhan(TabPage tab)
        {
            tab.Font = new Font("Segoe UI", 9.5F);
            tab.BackColor = Color.FromArgb(248, 250, 252);
            tab.AutoScroll = true;

            var pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            // Group 1: Thông tin hệ thống (ReadOnly)
            var grpSystem = new GroupBox
            {
                Text = "1. THÔNG TIN NHÂN SỰ & TÀI KHOẢN HỆ THỐNG",
                Location = new Point(20, 15),
                Size = new Size(850, 95),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138)
            };

            var lblMa = new Label { Text = "Mã Nhân Viên:", Location = new Point(20, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtMaNV_Profile = new TextBox { Location = new Point(130, 32), Size = new Size(130, 25), ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            var lblUser = new Label { Text = "Tên Đăng Nhập:", Location = new Point(290, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtTenDangNhap_Profile = new TextBox { Location = new Point(410, 32), Size = new Size(150, 25), ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            var lblChucVu = new Label { Text = "Chức Vụ:", Location = new Point(590, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtChucVu_Profile = new TextBox { Location = new Point(670, 32), Size = new Size(150, 25), ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            grpSystem.Controls.AddRange(new Control[] {
                lblMa, txtMaNV_Profile, lblUser, txtTenDangNhap_Profile, lblChucVu, txtChucVu_Profile
            });

            // Group 2: Thông tin cá nhân (Editable)
            var grpProfile = new GroupBox
            {
                Text = "2. THÔNG TIN CÁ NHÂN LIÊN HỆ",
                Location = new Point(20, 125),
                Size = new Size(850, 140),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 149, 193)
            };

            var lblHoTen = new Label { Text = "Họ và Tên (*):", Location = new Point(20, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            txtHoTen_Profile = new TextBox { Location = new Point(130, 32), Size = new Size(300, 25), Font = new Font("Segoe UI", 9F) };

            var lblSDT = new Label { Text = "Số Điện Thoại:", Location = new Point(20, 80), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            txtSDT_Profile = new TextBox { Location = new Point(130, 77), Size = new Size(300, 25), Font = new Font("Segoe UI", 9F) };

            var lblEmail = new Label { Text = "Email:", Location = new Point(460, 80), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtEmail_Profile = new TextBox { Location = new Point(520, 77), Size = new Size(300, 25), Font = new Font("Segoe UI", 9F) };

            grpProfile.Controls.AddRange(new Control[] {
                lblHoTen, txtHoTen_Profile,
                lblSDT, txtSDT_Profile, lblEmail, txtEmail_Profile
            });

            // Group 3: Đổi mật khẩu
            var grpPassword = new GroupBox
            {
                Text = "3. ĐỔI MẬT KHẨU ĐĂNG NHẬP (Bỏ trống nếu không thay đổi)",
                Location = new Point(20, 280),
                Size = new Size(850, 100),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38)
            };

            var lblPass = new Label { Text = "Mật Khẩu Mới:", Location = new Point(20, 40), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtMatKhauMoi_Profile = new TextBox { Location = new Point(150, 37), Size = new Size(240, 25), PasswordChar = '●', Font = new Font("Segoe UI", 9F) };

            var lblPassConfirm = new Label { Text = "Xác Nhận Mật Khẩu:", Location = new Point(440, 40), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtXacNhanMatKhau_Profile = new TextBox { Location = new Point(580, 37), Size = new Size(240, 25), PasswordChar = '●', Font = new Font("Segoe UI", 9F) };

            grpPassword.Controls.AddRange(new Control[] { lblPass, txtMatKhauMoi_Profile, lblPassConfirm, txtXacNhanMatKhau_Profile });

            // Action Buttons
            var btnSave = new Button
            {
                Text = "💾 LƯU THAY ĐỔI THÔNG TIN",
                Location = new Point(20, 400),
                Size = new Size(320, 44),
                BackColor = Color.FromArgb(22, 163, 74), // Green 600
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => BtnLuuThongTinNV_Click();

            var btnReset = new Button
            {
                Text = "↺ Khôi Phục Ban Đầu",
                Location = new Point(360, 400),
                Size = new Size(200, 44),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReset.Click += (s, e) => LoadThongTinCaNhanNV();

            pnlContainer.Controls.AddRange(new Control[] { grpSystem, grpProfile, grpPassword, btnSave, btnReset });
            tab.Controls.Add(pnlContainer);
        }

        // =========================================================================
        // DATA LOADERS & EVENT HANDLERS
        // =========================================================================
        private void LoadAllThuThuData()
        {
            LoadDocGiaComboBox();
            LoadCuonSachCoSan();
            LoadCuonSachTraVeChoDatTruoc();
            LoadPhieuMuonDangMuon();
            LoadDatTruoc();
            LoadPhieuPhat();
            LoadPhieuQuaHan();
            LoadThongTinCaNhanNV();
        }

        private void LoadThongTinCaNhanNV()
        {
            string maNV = !string.IsNullOrEmpty(SessionContext.MaNV) ? SessionContext.MaNV : "NV001";
            try
            {
                string sql = @"
                    SELECT nv.MaNV, nv.HoTen, nv.ChucVu, nv.SDT, nv.Email, v.TenDangNhap
                    FROM NhanVien nv
                    JOIN View_TaiKhoan_Role v ON nv.MaTaiKhoan = v.MaTaiKhoan
                    WHERE nv.MaNV = @id;";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql, new SqlParameter[] { new SqlParameter("@id", maNV) });
                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    txtMaNV_Profile.Text = r["MaNV"].ToString() ?? "";
                    txtTenDangNhap_Profile.Text = r["TenDangNhap"].ToString() ?? "";
                    txtChucVu_Profile.Text = r["ChucVu"].ToString() ?? "";

                    txtHoTen_Profile.Text = r["HoTen"].ToString() ?? "";
                    txtSDT_Profile.Text = r["SDT"] != DBNull.Value ? r["SDT"].ToString() ?? "" : "";
                    txtEmail_Profile.Text = r["Email"] != DBNull.Value ? r["Email"].ToString() ?? "" : "";

                    txtMatKhauMoi_Profile.Text = "";
                    txtXacNhanMatKhau_Profile.Text = "";
                }
            }
            catch { }
        }

        private void BtnLuuThongTinNV_Click()
        {
            string maNV = !string.IsNullOrEmpty(SessionContext.MaNV) ? SessionContext.MaNV : "NV001";
            string hoTen = txtHoTen_Profile.Text.Trim();
            string sdt = txtSDT_Profile.Text.Trim();
            string email = txtEmail_Profile.Text.Trim();
            string passMoi = txtMatKhauMoi_Profile.Text.Trim();
            string passConfirm = txtXacNhanMatKhau_Profile.Text.Trim();

            if (string.IsNullOrEmpty(hoTen))
            {
                MessageBox.Show("Họ và tên không được để trống!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(passMoi))
            {
                if (passMoi.Length < 4)
                {
                    MessageBox.Show("Mật khẩu mới phải có ít nhất 4 ký tự!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (passMoi != passConfirm)
                {
                    MessageBox.Show("Xác nhận mật khẩu không khớp với mật khẩu mới!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaNV", maNV),
                    new SqlParameter("@p_HoTen", hoTen),
                    new SqlParameter("@p_SDT", sdt),
                    new SqlParameter("@p_Email", email),
                    new SqlParameter("@p_MatKhauMoi", string.IsNullOrEmpty(passMoi) ? (object)DBNull.Value : passMoi)
                };

                DataTable dt = DatabaseHelper.ExecuteProcedure("sp_CapNhatThongTinNhanVien", pars);
                string msg = dt.Rows.Count > 0 && dt.Columns.Contains("ThongBao") ? dt.Rows[0]["ThongBao"].ToString() ?? "" : "Cập nhật thành công!";

                MessageBox.Show(msg, "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Update session
                SessionContext.TenNhanVien = hoTen;
                lblUser.Text = $"👤 {SessionContext.TenNhanVien}\n(NV: {SessionContext.MaNV})";
                LoadThongTinCaNhanNV();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadDocGiaComboBox()
        {
            try
            {
                string sql = "SELECT MaDG, CONCAT(MaDG, ' - ', HoTen, ' (', TrangThai, ')') AS DisplayText FROM DocGia";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql);
                cboDocGia.DataSource = dt;
                cboDocGia.DisplayMember = "DisplayText";
                cboDocGia.ValueMember = "MaDG";
            }
            catch { }
        }

        private void CheckDocGiaConditions()
        {
            if (cboDocGia.SelectedValue == null) return;
            string maDG = cboDocGia.SelectedValue.ToString() ?? "";
            if (string.IsNullOrEmpty(maDG)) return;

            try
            {
                // Call Function 1: fn_DemSachDangMuon
                var countObj = DatabaseHelper.ExecuteScalar($"SELECT dbo.fn_DemSachDangMuon('{maDG}')");
                int count = Convert.ToInt32(countObj ?? 0);
                lblSoSachDangMuon.Text = $"Số sách đang mượn: {count} / 3 cuốn (Tối đa)";

                // Call Function 2: fn_KiemTraDuDieuKienMuon
                var checkObj = DatabaseHelper.ExecuteScalar($"SELECT dbo.fn_KiemTraDuDieuKienMuon('{maDG}')");
                string msg = checkObj?.ToString() ?? "";
                lblKiemTraThe.Text = $"Tình trạng thẻ: {msg}";
                lblKiemTraThe.ForeColor = msg.Contains("Đủ điều kiện") ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
            }
            catch { }
        }

        private void LoadCuonSachCoSan()
        {
            try 
            {
                string sql = @"
                    SELECT MaCuonSach, DisplayText, MaSach, TenSach, ViTriKe, TrangThai
                    FROM View_SachCoSan
                    WHERE TrangThai = 'CoSan' AND TinhTrang = 'ConTot';";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql);
                cboCuonSachCoSan.DataSource = dt;
                cboCuonSachCoSan.DisplayMember = "DisplayText";
                cboCuonSachCoSan.ValueMember = "MaCuonSach";
            }
            catch { }
        }

        private void LoadCuonSachTraVeChoDatTruoc()
        {
            try
            {
                string sql = @"
                    SELECT DISTINCT v.MaCuonSach, v.DisplayText
                    FROM View_SachCoSan v
                    JOIN PhieuDatTruoc pdt ON v.MaSach = pdt.MaSach
                    WHERE v.TrangThai = 'CoSan' AND v.TinhTrang = 'ConTot' AND pdt.TrangThai = 'DangCho';";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql);
                cboCuonSachTraVe.DataSource = dt;
                cboCuonSachTraVe.DisplayMember = "DisplayText";
                cboCuonSachTraVe.ValueMember = "MaCuonSach";
            }
            catch { }
        }

        private void BtnAddSach_Click(object? sender, EventArgs e)
        {
            if (cboCuonSachCoSan.SelectedItem is not DataRowView rowView) return;
            string maCS = rowView["MaCuonSach"]?.ToString() ?? "";
            string maSach = rowView["MaSach"]?.ToString() ?? "";
            string tenSach = rowView["TenSach"]?.ToString() ?? "";
            string viTriKe = rowView["ViTriKe"]?.ToString() ?? "";
            string trangThai = rowView["TrangThai"]?.ToString() ?? "";

            // Check if already in list
            foreach (DataRow r in dtDanhSachCuonMuon.Rows)
            {
                if (r["MaSach"].ToString() == maSach)
                {
                    MessageBox.Show("Đầu sách này đã có trong danh sách chuẩn bị mượn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (dtDanhSachCuonMuon.Rows.Count >= 3)
            {
                MessageBox.Show("Mỗi phiếu mượn chỉ được tối đa 3 cuốn sách!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if reader is already borrowing this book title
            string maDG = cboDocGia.SelectedValue?.ToString() ?? "";
            if (!string.IsNullOrEmpty(maDG))
            {
                string checkSql = @"
                    SELECT COUNT(*)
                    FROM CT_PhieuMuon ct
                    JOIN PhieuMuon pm ON ct.MaPhieuMuon = pm.MaPhieuMuon
                    JOIN CuonSach cs ON ct.MaCuonSach = cs.MaCuonSach
                    WHERE pm.MaDG = @MaDG AND cs.MaSach = @MaSach AND ct.TinhTrangSachKhiTra = 'ChuaTra'";
                
                var pars = new[] {
                    new SqlParameter("@MaDG", maDG),
                    new SqlParameter("@MaSach", maSach)
                };
                
                int countDangMuon = Convert.ToInt32(DatabaseHelper.ExecuteScalar(checkSql, pars) ?? 0);
                if (countDangMuon > 0)
                {
                    MessageBox.Show("Độc giả này đang giữ 1 cuốn thuộc đầu sách này rồi, không được mượn thêm bản sao khác!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            dtDanhSachCuonMuon.Rows.Add(maCS, maSach, tenSach, viTriKe, trangThai);
        }

        private void BtnRemoveSach_Click(object? sender, EventArgs e)
        {
            if (dgvSachMuon.CurrentRow != null)
            {
                int idx = dgvSachMuon.CurrentRow.Index;
                if (idx >= 0 && idx < dtDanhSachCuonMuon.Rows.Count)
                {
                    dtDanhSachCuonMuon.Rows.RemoveAt(idx);
                }
            }
        }

        private void BtnConfirmMuon_Click(object? sender, EventArgs e)
        {
            string maPM = txtMaPM.Text.Trim();
            if (string.IsNullOrEmpty(maPM))
            {
                MessageBox.Show("Vui lòng nhập Mã phiếu mượn!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboDocGia.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Độc giả!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dtDanhSachCuonMuon.Rows.Count == 0)
            {
                MessageBox.Show("Vui lòng thêm ít nhất 1 cuốn sách vào danh sách mượn!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string maDG = cboDocGia.SelectedValue.ToString() ?? "";
            string maNV = !string.IsNullOrEmpty(SessionContext.MaNV) ? SessionContext.MaNV : "NV001";
            DateTime ngayHenTra = dtpNgayHenTra.Value.Date;

            // Prepare TVP Table
            DataTable dtTVP = new DataTable();
            dtTVP.Columns.Add("MaCuonSach", typeof(string));
            foreach (DataRow r in dtDanhSachCuonMuon.Rows)
            {
                dtTVP.Rows.Add(r["MaCuonSach"].ToString());
            }

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaPhieuMuon", maPM),
                    new SqlParameter("@p_MaDG", maDG),
                    new SqlParameter("@p_MaNV", maNV),
                    new SqlParameter("@p_NgayHenTra", ngayHenTra),
                    DatabaseHelper.CreateStructuredParameter("@p_DanhSachCuonSach", "dbo.DanhSachCuonSachType", dtTVP)
                };

                DataTable res = DatabaseHelper.ExecuteProcedure("sp_LapPhieuMuon", pars);

                string thongBao = "Thực thi hoàn tất!";
                if (res.Rows.Count > 0 && res.Columns.Contains("ThongBao"))
                {
                    thongBao = res.Rows[0]["ThongBao"].ToString() ?? "";
                }

                if (thongBao.Contains("Lỗi") || thongBao.Contains("Không đủ điều kiện"))
                {
                    MessageBox.Show(thongBao, "Thông Báo Từ Giao Tác", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(thongBao, "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dtDanhSachCuonMuon.Clear();
                    txtMaPM.Text = "PM" + DateTime.Now.ToString("HHmmss");
                    LoadAllThuThuData();
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"❌ LỖI RÀNG BUỘC CSDL (TRIGGER/SP):\n\n{sqlEx.Message}", "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================
        // TAB 2 HANDLERS: TRẢ SÁCH
        // =========================================================================
        private void LoadPhieuMuonDangMuon()
        {
            try
            {
                string sql = @"
                    SELECT pm.MaPhieuMuon, dg.MaDG, dg.HoTen AS TenDocGia, pm.NgayMuon, pm.NgayHenTra, pm.TrangThai,
                           dbo.fn_TinhSoNgayTre(pm.MaPhieuMuon) AS SoNgayTre
                    FROM PhieuMuon pm
                    JOIN DocGia dg ON pm.MaDG = dg.MaDG
                    WHERE pm.TrangThai IN ('DangMuon', 'QuaHan')
                    ORDER BY pm.NgayHenTra ASC;";
                dgvPhieuMuonDangMuon.DataSource = DatabaseHelper.ExecuteQuery(sql);
            }
            catch { }
        }

        private void DgvPhieuMuonDangMuon_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvPhieuMuonDangMuon.CurrentRow == null) return;
            string maPM = dgvPhieuMuonDangMuon.CurrentRow.Cells["MaPhieuMuon"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(maPM)) return;

            try
            {
                // Calculate late days via fn_TinhSoNgayTre
                var lateDaysObj = DatabaseHelper.ExecuteScalar($"SELECT dbo.fn_TinhSoNgayTre('{maPM}')");
                int lateDays = Convert.ToInt32(lateDaysObj ?? 0);

                if (lateDays > 0)
                {
                    decimal tienPhat = lateDays * 5000;
                    lblThongTinTreHan.Text = $"⚠️ Phiếu {maPM} ĐÃ QUÁ HẠN {lateDays} ngày! Dự kiến phạt trễ: {tienPhat:N0} VNĐ";
                    lblThongTinTreHan.ForeColor = Color.FromArgb(220, 38, 38);
                }
                else
                {
                    lblThongTinTreHan.Text = $"✅ Phiếu {maPM} trả đúng hạn (0 ngày trễ).";
                    lblThongTinTreHan.ForeColor = Color.FromArgb(22, 163, 74);
                }

                // Load detail books in this loan
                string sql = @"
                    SELECT MaCuonSach, TenSach, TinhTrang
                    FROM View_ChiTietSachMuon
                    WHERE MaPhieuMuon = @MaPM AND TinhTrangSachKhiTra = 'ChuaTra'";

                DataTable dt = DatabaseHelper.ExecuteQuery(sql, new SqlParameter[] { new SqlParameter("@MaPM", maPM) });
                
                // Bind to grid with ComboBox column for TinhTrang
                dgvChiTietTra.Columns.Clear();
                dgvChiTietTra.AutoGenerateColumns = false;

                dgvChiTietTra.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "MaCuonSach", HeaderText = "Mã Cuốn Sách", ReadOnly = true, Width = 130 });
                dgvChiTietTra.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TenSach", HeaderText = "Tên Sách", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

                var colCombo = new DataGridViewComboBoxColumn
                {
                    DataPropertyName = "TinhTrang",
                    HeaderText = "Tình Trạng Sách Khi Trả",
                    Width = 180
                };
                colCombo.Items.AddRange("ConTot", "HuHong", "Mat");
                dgvChiTietTra.Columns.Add(colCombo);

                dgvChiTietTra.DataSource = dt;
            }
            catch { }
        }

        private void BtnConfirmTra_Click(object? sender, EventArgs e)
        {
            if (dgvPhieuMuonDangMuon.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn 1 phiếu mượn bên trái cần trả!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string maPM = dgvPhieuMuonDangMuon.CurrentRow.Cells["MaPhieuMuon"].Value?.ToString() ?? "";
            DataTable? dt = dgvChiTietTra.DataSource as DataTable;

            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show("Không có cuốn sách nào cần trả cho phiếu này!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build TVP for TraSach
            DataTable dtTVPTra = new DataTable();
            dtTVPTra.Columns.Add("MaCuonSach", typeof(string));
            dtTVPTra.Columns.Add("TinhTrang", typeof(string));

            foreach (DataRow r in dt.Rows)
            {
                string maCS = r["MaCuonSach"].ToString() ?? "";
                string tinhTrang = r["TinhTrang"].ToString() ?? "ConTot";
                dtTVPTra.Rows.Add(maCS, tinhTrang);
            }

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaPhieuMuon", maPM),
                    DatabaseHelper.CreateStructuredParameter("@p_DanhSachTraSach", "dbo.DanhSachTraSachType", dtTVPTra)
                };

                DataTable res = DatabaseHelper.ExecuteProcedure("sp_TraSach", pars);

                string msg = "Trả sách thành công!";
                if (res.Rows.Count > 0 && res.Columns.Contains("ThongBao"))
                {
                    msg = res.Rows[0]["ThongBao"].ToString() ?? "";
                }

                // Check generated fines
                string sqlFines = "SELECT MaPhieuPhat, LyDoPhat, SoTienPhat FROM PhieuPhat WHERE MaPhieuMuon = @id AND TrangThaiThanhToan = 'ChuaThanhToan'";
                DataTable dtFines = DatabaseHelper.ExecuteQuery(sqlFines, new SqlParameter[] { new SqlParameter("@id", maPM) });

                string finesInfo = "";
                if (dtFines.Rows.Count > 0)
                {
                    finesInfo = "\n\n⚠️ CÁC PHIẾU PHẠT ĐƯỢC PHÁT SINH TỰ ĐỘNG:";
                    foreach (DataRow fr in dtFines.Rows)
                    {
                        finesInfo += $"\n- Mã phạt: {fr["MaPhieuPhat"]} | Lý do: {fr["LyDoPhat"]} | Tiền phạt: {Convert.ToDecimal(fr["SoTienPhat"]):N0} VNĐ";
                    }
                }

                MessageBox.Show($"{msg}{finesInfo}", "Kết Quả Trả Sách", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadAllThuThuData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi giao tác trả sách: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================
        // TAB 3 HANDLERS: XỬ LÝ ĐẶT TRƯỚC
        // =========================================================================
        private void LoadDatTruoc()
        {
            try
            {
                string sql = @"
                    SELECT * 
                    FROM View_DanhSachDatTruoc_ThuThu
                    WHERE TrangThai IN ('DangCho', 'ChoNhan')
                    ORDER BY NgayDat ASC;";
                dgvDatTruoc.DataSource = DatabaseHelper.ExecuteQuery(sql);
            }
            catch { }
        }

        private void BtnKhuVuc1_Click(object? sender, EventArgs e)
        {
            if (cboCuonSachTraVe.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn 1 cuốn sách có sẵn vừa trả về!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string maCS = cboCuonSachTraVe.SelectedValue.ToString() ?? "";
            try
            {
                var pars = new SqlParameter[] { new SqlParameter("@p_MaCuonSach", maCS) };
                DataTable dt = DatabaseHelper.ExecuteProcedure("sp_XuLyDatTruocKhiCoSach", pars);
                string msg = dt.Rows.Count > 0 && dt.Columns.Contains("ThongBao") ? dt.Rows[0]["ThongBao"].ToString() ?? "" : "Xử lý xong!";
                MessageBox.Show(msg, "Kết Quả Kích Hoạt Giữ Chỗ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAllThuThuData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnKhuVuc2_Click(object? sender, EventArgs e)
        {
            if (dgvDatTruoc.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu đặt trước trong danh sách (Trạng thái phải là ChoNhan)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string trangThai = dgvDatTruoc.CurrentRow.Cells["TrangThai"].Value?.ToString() ?? "";
            if (trangThai != "ChoNhan")
            {
                MessageBox.Show("Chỉ có thể giao sách cho phiếu đặt trước ở trạng thái 'ChoNhan'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string maPDT = dgvDatTruoc.CurrentRow.Cells["MaPhieuDatTruoc"].Value?.ToString() ?? "";
            string maPM = txtMaPMMoi.Text.Trim();
            string maNV = !string.IsNullOrEmpty(SessionContext.MaNV) ? SessionContext.MaNV : "NV001";
            DateTime ngayHenTra = dtpNgayHenTraDT.Value.Date;
            
            string maSach = dgvDatTruoc.CurrentRow.Cells["MaSach"].Value?.ToString() ?? "";
            try
            {
                var obj = DatabaseHelper.ExecuteScalar("SELECT TOP 1 MaCuonSach FROM CuonSach WHERE MaSach = @ms AND TrangThai = 'GiuCho'", new SqlParameter[] { new SqlParameter("@ms", maSach) });
                if (obj == null)
                {
                    MessageBox.Show("Không tìm thấy cuốn sách vật lý nào đang được Giữ chỗ cho đầu sách này!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string maCuonSach = obj.ToString() ?? "";
                
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaPhieuDatTruoc", maPDT),
                    new SqlParameter("@p_MaCuonSach", maCuonSach),
                    new SqlParameter("@p_MaPhieuMuon", maPM),
                    new SqlParameter("@p_MaNV", maNV),
                    new SqlParameter("@p_NgayHenTra", ngayHenTra)
                };
                DataTable dt = DatabaseHelper.ExecuteProcedure("sp_NhanSachDatTruoc", pars);
                string msg = dt.Rows.Count > 0 && dt.Columns.Contains("ThongBao") ? dt.Rows[0]["ThongBao"].ToString() ?? "" : "Lập phiếu thành công!";
                MessageBox.Show(msg, "Kết Quả Giao Sách", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtMaPMMoi.Text = "PM" + DateTime.Now.ToString("HHmmss");
                LoadAllThuThuData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnKhuVuc3_Click(object? sender, EventArgs e)
        {
            if (dgvDatTruoc.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu đặt trước cần hủy!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string trangThai = dgvDatTruoc.CurrentRow.Cells["TrangThai"].Value?.ToString() ?? "";
            if (trangThai != "ChoNhan")
            {
                MessageBox.Show("Chỉ có thể hủy giữ chỗ cho phiếu đặt trước ở trạng thái 'ChoNhan'!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string maPDT = dgvDatTruoc.CurrentRow.Cells["MaPhieuDatTruoc"].Value?.ToString() ?? "";
            string maSach = dgvDatTruoc.CurrentRow.Cells["MaSach"].Value?.ToString() ?? "";
            
            try
            {
                var obj = DatabaseHelper.ExecuteScalar("SELECT TOP 1 MaCuonSach FROM CuonSach WHERE MaSach = @ms AND TrangThai = 'GiuCho'", new SqlParameter[] { new SqlParameter("@ms", maSach) });
                string maCuonSach = obj?.ToString() ?? "";
                if (string.IsNullOrEmpty(maCuonSach))
                {
                    MessageBox.Show("Lỗi: Không tìm thấy sách vật lý đang giữ chỗ để hủy!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaPhieuDatTruoc", maPDT),
                    new SqlParameter("@p_MaCuonSach", maCuonSach)
                };
                DataTable dt = DatabaseHelper.ExecuteProcedure("sp_HuyGiuChoHetHan", pars);
                string msg = dt.Rows.Count > 0 && dt.Columns.Contains("ThongBao") ? dt.Rows[0]["ThongBao"].ToString() ?? "" : "Hủy thành công!";
                MessageBox.Show(msg, "Kết Quả Hủy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAllThuThuData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadPhieuQuaHan()
        {
            try
            {
                dgvQuaHan.DataSource = DatabaseHelper.ExecuteQuery("SELECT * FROM View_PhieuQuaHan ORDER BY SoNgayTre DESC");
            }
            catch { }
        }

        // =========================================================================
        // TAB 4 HANDLERS: THU PHẠT
        // =========================================================================
        private void LoadPhieuPhat()
        {
            try
            {
                string sql = @"
                    SELECT pp.MaPhieuPhat, pp.MaPhieuMuon, dg.MaDG, dg.HoTen AS TenDocGia,
                           pp.LyDoPhat, pp.SoTienPhat, pp.NgayLap, pp.TrangThaiThanhToan
                    FROM PhieuPhat pp
                    JOIN PhieuMuon pm ON pp.MaPhieuMuon = pm.MaPhieuMuon
                    JOIN DocGia dg ON pm.MaDG = dg.MaDG
                    ORDER BY CASE WHEN pp.TrangThaiThanhToan = 'ChuaThanhToan' THEN 0 ELSE 1 END, pp.NgayLap DESC;";
                dgvPhieuPhat.DataSource = DatabaseHelper.ExecuteQuery(sql);
            }
            catch { }
        }

        private void BtnPay_Click(object? sender, EventArgs e)
        {
            if (dgvPhieuPhat.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn 1 phiếu phạt trong danh sách!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string maPP = dgvPhieuPhat.CurrentRow.Cells["MaPhieuPhat"].Value?.ToString() ?? "";
            string tt = dgvPhieuPhat.CurrentRow.Cells["TrangThaiThanhToan"].Value?.ToString() ?? "";

            if (tt == "DaThanhToan")
            {
                MessageBox.Show("Phiếu phạt này đã được thanh toán trước đó rồi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var pars = new SqlParameter[] { new SqlParameter("@p_MaPhieuPhat", maPP) };
                DataTable dt = DatabaseHelper.ExecuteProcedure("sp_ThanhToanPhat", pars);

                string msg = dt.Rows.Count > 0 && dt.Columns.Contains("ThongBao") ? dt.Rows[0]["ThongBao"].ToString() ?? "" : "Thanh toán thành công!";
                MessageBox.Show(msg, "Kết Quả", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadPhieuPhat();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi thanh toán: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
        }
    }
}
