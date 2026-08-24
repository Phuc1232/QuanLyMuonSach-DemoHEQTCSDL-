using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ProjectHEQTCSDL.Core;
using ProjectHEQTCSDL.Models;

namespace ProjectHEQTCSDL.FormUI
{
    public class FrmMainDocGia : Form
    {
        private TabControl tabDocGia = null!;

        // Navigation & Layout
        private Panel pnlSidebar = null!;
        private Label lblUser = null!;
        private Button btnMenuTraCuu = null!;
        private Button btnMenuDatTruoc = null!;
        private Button btnMenuLichSu = null!;
        private Button btnMenuCaNhan = null!;
        private Button btnMenuReload = null!;
        private bool isSidebarExpanded = true;
        private Panel pnlBanner = null!;
        private Label lblBannerMessage = null!;

        // Tab 1: Tra Cứu
        private TextBox txtTuKhoa = null!;
        private DataGridView dgvTraCuu = null!;
        private Label lblSelectedBookInfo = null!;
        private Button btnQuickDatTruoc = null!;
        private Button btnXemViTriKe = null!;

        // Tab 2: Đặt Trước
        private TextBox txtMaPDT = null!;
        private ComboBox cboSachDatTruoc = null!;
        private DataGridView dgvLichSuDatTruoc = null!;

        // Tab 3: Lịch Sử Cá Nhân
        private DataGridView dgvLichSuMuon = null!;
        private DataGridView dgvLichSuPhat = null!;
        private Label lblTongQuanCaNhan = null!;

        // Tab 4: Thông Tin Cá Nhân
        private TextBox txtMaDG_Profile = null!;
        private TextBox txtTenDangNhap_Profile = null!;
        private TextBox txtLoaiDocGia_Profile = null!;
        private TextBox txtNgayDangKy_Profile = null!;
        private TextBox txtTrangThaiThe_Profile = null!;
        private TextBox txtHoTen_Profile = null!;
        private DateTimePicker dtpNgaySinh_Profile = null!;
        private TextBox txtSDT_Profile = null!;
        private TextBox txtEmail_Profile = null!;
        private TextBox txtDiaChi_Profile = null!;
        private TextBox txtMatKhauMoi_Profile = null!;
        private TextBox txtXacNhanMatKhau_Profile = null!;

        public FrmMainDocGia()
        {
            InitializeComponent();
            this.Load += (s, e) => LoadAllDocGiaData();
        }

        private void InitializeComponent()
        {
            this.Text = "Cổng Thông Tin Độc Giả - Thư Viện";
            this.Size = new Size(1180, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);
            this.BackColor = Color.FromArgb(248, 250, 252);

            // 1. Banner Notification (Global)
            pnlBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(254, 240, 138), // Yellow 200
                Visible = false // Hidden by default
            };
            lblBannerMessage = new Label
            {
                AutoSize = true,
                Location = new Point(20, 10),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(161, 98, 7) // Yellow 700
            };
            pnlBanner.Controls.Add(lblBannerMessage);
            this.Controls.Add(pnlBanner);

            // 2. Sidebar Panel
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
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
                Text = "READER PORTAL",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(55, 25),
                AutoSize = true
            };
            pnlSidebarHeader.Controls.AddRange(new Control[] { btnToggle, lblLogo });

            lblUser = new Label
            {
                Text = $"👤 {SessionContext.TenDocGia}\n(Thẻ: {SessionContext.MaDG})",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(15, 85),
                AutoSize = true
            };

            btnMenuTraCuu = CreateNavButton("🔍 Tra Cứu & Mượn", 130);
            btnMenuTraCuu.Click += (s, e) => SwitchTab(0, btnMenuTraCuu);

            btnMenuDatTruoc = CreateNavButton("🔖 Đặt Trước Sách", 180);
            btnMenuDatTruoc.Click += (s, e) => SwitchTab(1, btnMenuDatTruoc);

            btnMenuLichSu = CreateNavButton("📑 Lịch Sử & Phạt", 230);
            btnMenuLichSu.Click += (s, e) => SwitchTab(2, btnMenuLichSu);

            btnMenuCaNhan = CreateNavButton("⚙️ Thông Tin Cá Nhân", 280);
            btnMenuCaNhan.Click += (s, e) => SwitchTab(3, btnMenuCaNhan);

            btnMenuReload = CreateNavButton("🔄 Làm Mới Dữ Liệu", 330);
            btnMenuReload.BackColor = Color.FromArgb(30, 41, 59);
            btnMenuReload.Click += (s, e) => BtnReload_Click();

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

            pnlSidebar.Controls.AddRange(new Control[] { pnlSidebarHeader, lblUser, btnMenuTraCuu, btnMenuDatTruoc, btnMenuLichSu, btnMenuCaNhan, btnMenuReload, btnLogout });

            // 3. Main Content TabControl (Headers hidden)
            tabDocGia = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F),
                Appearance = TabAppearance.FlatButtons,
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed
            };

            var tabTraCuu = new TabPage();
            var tabDatTruoc = new TabPage();
            var tabLichSu = new TabPage();
            var tabCaNhan = new TabPage();

            tabDocGia.TabPages.AddRange(new TabPage[] { tabTraCuu, tabDatTruoc, tabLichSu, tabCaNhan });

            BuildTabTraCuu(tabTraCuu);
            BuildTabDatTruoc(tabDatTruoc);
            BuildTabLichSu(tabLichSu);
            BuildTabCaNhan(tabCaNhan);

            this.Controls.Add(tabDocGia);
            this.Controls.Add(pnlSidebar);
            
            tabDocGia.BringToFront();
            SwitchTab(0, btnMenuTraCuu);
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
                Size = new Size(250, 50),
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
            tabDocGia.SelectedIndex = index;
            
            btnMenuTraCuu.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuDatTruoc.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuLichSu.BackColor = Color.FromArgb(15, 23, 42);
            btnMenuCaNhan.BackColor = Color.FromArgb(15, 23, 42);

            btnMenuTraCuu.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuDatTruoc.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuLichSu.ForeColor = Color.FromArgb(203, 213, 225);
            btnMenuCaNhan.ForeColor = Color.FromArgb(203, 213, 225);

            activeBtn.BackColor = Color.FromArgb(51, 65, 85);
            activeBtn.ForeColor = Color.White;
        }

        private void ToggleSidebar()
        {
            isSidebarExpanded = !isSidebarExpanded;
            if (isSidebarExpanded)
            {
                pnlSidebar.Width = 250;
                btnMenuTraCuu.Text = "  🔍 Tra Cứu & Mượn";
                btnMenuDatTruoc.Text = "  🔖 Đặt Trước Sách";
                btnMenuLichSu.Text = "  📑 Lịch Sử & Phạt";
                btnMenuCaNhan.Text = "  ⚙️ Thông Tin Cá Nhân";
                btnMenuReload.Text = "  🔄 Làm Mới Dữ Liệu";
            }
            else
            {
                pnlSidebar.Width = 60;
                btnMenuTraCuu.Text = "  🔍";
                btnMenuDatTruoc.Text = "  🔖";
                btnMenuLichSu.Text = "  📑";
                btnMenuCaNhan.Text = "  ⚙️";
                btnMenuReload.Text = "  🔄";
            }
        }

        // =========================================================================
        // TAB 1: TRA CỨU SÁCH & THAO TÁC TRỰC TIẾP
        // =========================================================================
        private void BuildTabTraCuu(TabPage tab)
        {
            tab.Font = new Font("Segoe UI", 9.5F);
            tab.BackColor = Color.White;

            // Search Bar (Top)
            var pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(15, 12, 15, 5)
            };

            var lblSearch = new Label { Text = "Nhập tên sách hoặc tên tác giả:", Location = new Point(15, 18), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtTuKhoa = new TextBox { Location = new Point(240, 18), Size = new Size(350, 26) };

            var btnSearch = new Button
            {
                Text = "🔍 Tìm Kiếm",
                Location = new Point(600, 18),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSearch.Click += (s, e) => SearchSach();

            var btnShowAll = new Button
            {
                Text = "Xem Tất Cả",
                Location = new Point(730, 18),
                Size = new Size(110, 32),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnShowAll.Click += (s, e) => { txtTuKhoa.Clear(); SearchSach(); };

            pnlSearch.Controls.AddRange(new Control[] { lblSearch, txtTuKhoa, btnSearch, btnShowAll });

            // Bottom Action Panel: Direct interaction when a row is selected
            var pnlBottomAction = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(15, 10, 15, 10)
            };
            pnlBottomAction.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240), 1), 0, 0, pnlBottomAction.Width, 0);
            };

            lblSelectedBookInfo = new Label
            {
                Text = "👉 Hãy chọn một cuốn sách trong danh sách phía trên để thực hiện thao tác.",
                Location = new Point(15, 12),
                Size = new Size(1100, 24),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            btnQuickDatTruoc = new Button
            {
                Text = "🔖 ĐẶT TRƯỚC CUỐN SÁCH ĐANG CHỌN (Dành Cho Sách Hết Bản Sao)",
                Location = new Point(15, 42),
                Size = new Size(500, 36),
                BackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnQuickDatTruoc.Click += BtnQuickDatTruoc_Click;

            btnXemViTriKe = new Button
            {
                Text = "📍 Xem Vị Trí Kệ Sách Tại Thư Viện",
                Location = new Point(525, 42),
                Size = new Size(260, 36),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnXemViTriKe.Click += BtnXemViTriKe_Click;

            pnlBottomAction.Controls.AddRange(new Control[] { lblSelectedBookInfo, btnQuickDatTruoc, btnXemViTriKe });

            // DataGridView (Fill)
            dgvTraCuu = CreateStandardGrid();
            dgvTraCuu.SelectionChanged += DgvTraCuu_SelectionChanged;

            // Add in correct order
            tab.Controls.Add(dgvTraCuu);
            tab.Controls.Add(pnlBottomAction);
            tab.Controls.Add(pnlSearch);

            pnlSearch.BringToFront();
            pnlBottomAction.BringToFront();
            dgvTraCuu.BringToFront();
        }

        private void DgvTraCuu_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvTraCuu.CurrentRow == null)
            {
                lblSelectedBookInfo.Text = "👉 Hãy chọn một cuốn sách trong danh sách phía trên để thực hiện thao tác.";
                btnQuickDatTruoc.Enabled = false;
                btnXemViTriKe.Enabled = false;
                return;
            }

            string maSach = dgvTraCuu.CurrentRow.Cells["MaSach"].Value?.ToString() ?? "";
            string tenSach = dgvTraCuu.CurrentRow.Cells["TenSach"].Value?.ToString() ?? "";
            string tacGia = dgvTraCuu.CurrentRow.Cells["TenTacGia"].Value?.ToString() ?? "";
            int soBanCon = 0;
            if (dgvTraCuu.CurrentRow.Cells["SoBanConTrong"].Value != DBNull.Value)
            {
                int.TryParse(dgvTraCuu.CurrentRow.Cells["SoBanConTrong"].Value?.ToString(), out soBanCon);
            }

            btnXemViTriKe.Enabled = true;

            if (soBanCon == 0)
            {
                lblSelectedBookInfo.Text = $"📚 Đang chọn: [{maSach}] {tenSach} | Tác giả: {tacGia}  --->  🔴 HẾT BẢN SAO (Có thể đặt trước)";
                lblSelectedBookInfo.ForeColor = Color.FromArgb(220, 38, 38);
                btnQuickDatTruoc.Enabled = true;
                btnQuickDatTruoc.BackColor = Color.FromArgb(5, 150, 105);
            }
            else
            {
                lblSelectedBookInfo.Text = $"📚 Đang chọn: [{maSach}] {tenSach} | Tác giả: {tacGia}  --->  🟢 CÒN {soBanCon} BẢN CÓ SẴN (Vui lòng mượn trực tiếp tại quầy)";
                lblSelectedBookInfo.ForeColor = Color.FromArgb(22, 163, 74);
                btnQuickDatTruoc.Enabled = false;
                btnQuickDatTruoc.BackColor = Color.FromArgb(156, 163, 175);
            }
        }

        private void BtnQuickDatTruoc_Click(object? sender, EventArgs e)
        {
            if (dgvTraCuu.CurrentRow == null) return;
            string maSach = dgvTraCuu.CurrentRow.Cells["MaSach"].Value?.ToString() ?? "";
            string tenSach = dgvTraCuu.CurrentRow.Cells["TenSach"].Value?.ToString() ?? "";
            string maPDT = "PDT" + DateTime.Now.ToString("HHmmss");
            string maDG = !string.IsNullOrEmpty(SessionContext.MaDG) ? SessionContext.MaDG : "DG001";

            if (MessageBox.Show($"Bạn có muốn đặt trước đầu sách:\n\n[{maSach}] {tenSach}\nMã phiếu đặt: {maPDT}\n\nKhi có sách trả về, Thủ thư sẽ ưu tiên mượn cho bạn.", "Xác Nhận Đặt Trước", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    var pars = new SqlParameter[]
                    {
                        new SqlParameter("@p_MaPhieuDatTruoc", maPDT),
                        new SqlParameter("@p_MaDG", maDG),
                        new SqlParameter("@p_MaSach", maSach)
                    };

                    DataTable dt = DatabaseHelper.ExecuteProcedure("sp_DatTruocSach", pars);
                    string msg = dt.Rows.Count > 0 && dt.Columns.Contains("ThongBao") ? dt.Rows[0]["ThongBao"].ToString() ?? "" : "Hoàn tất!";

                    if (msg.Contains("còn bản sao"))
                    {
                        MessageBox.Show(msg, "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(msg, "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadLichSuDatTruoc();
                        tabDocGia.SelectedIndex = 1; // Switch to Tab 2 to see the reservation
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi đặt trước: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnXemViTriKe_Click(object? sender, EventArgs e)
        {
            if (dgvTraCuu.CurrentRow == null) return;
            string maSach = dgvTraCuu.CurrentRow.Cells["MaSach"].Value?.ToString() ?? "";
            string tenSach = dgvTraCuu.CurrentRow.Cells["TenSach"].Value?.ToString() ?? "";

            try
            {
                string sql = "SELECT MaCuonSach, TinhTrang, TrangThai, ViTriKe FROM CuonSach WHERE MaSach = @id";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql, new SqlParameter[] { new SqlParameter("@id", maSach) });

                string info = $"📍 VỊ TRÍ CÁC CUỐN SÁCH [{maSach}] {tenSach}:\n\n";
                if (dt.Rows.Count == 0)
                {
                    info += "Chưa có bản sao vật lý nào trong kho.";
                }
                else
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        info += $"• Mã cuốn: {r["MaCuonSach"]} | Trạng thái: {r["TrangThai"]} | Tình trạng: {r["TinhTrang"]} | Kệ: {r["ViTriKe"]}\n";
                    }
                }

                MessageBox.Show(info, "Thông Tin Vị Trí Kệ Sách", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================
        // TAB 2: ĐẶT TRƯỚC SÁCH (sp_DatTruocSach)
        // =========================================================================
        private void BuildTabDatTruoc(TabPage tab)
        {
            tab.Font = new Font("Segoe UI", 9.5F);
            tab.BackColor = Color.White;

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(15)
            };

            var lblPDT = new Label { Text = "Mã Phiếu Đặt Trước:", Location = new Point(15, 18), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            txtMaPDT = new TextBox { Location = new Point(200, 15), Size = new Size(140, 26), Text = "PDT" + DateTime.Now.ToString("HHmmss") };

            var lblSach = new Label { Text = "Chọn Sách Muốn Đặt Trước:", Location = new Point(360, 18), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            cboSachDatTruoc = new ComboBox { Location = new Point(600, 15), Size = new Size(380, 28), DropDownStyle = ComboBoxStyle.DropDownList };

            var btnConfirmDat = new Button
            {
                Text = "GỬI YÊU CẦU ĐẶT TRƯỚC SÁCH",
                Location = new Point(15, 60),
                Size = new Size(500, 40),
                BackColor = Color.FromArgb(5, 150, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirmDat.Click += BtnConfirmDat_Click;

            var lblNote = new Label
            {
                Text = "💡 Giao tác sp_DatTruocSach sẽ tự động kiểm tra: Nếu sách vẫn còn bản sao có sẵn, hệ thống sẽ yêu cầu mượn trực tiếp!",
                Location = new Point(530, 60),
                AutoSize = true,
                MaximumSize = new Size(450, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            pnlTop.Controls.AddRange(new Control[] { lblPDT, txtMaPDT, lblSach, cboSachDatTruoc, btnConfirmDat, lblNote });

            dgvLichSuDatTruoc = CreateStandardGrid();

            var lblHistoryHeader = new Label
            {
                Text = "DANH SÁCH YÊU CẦU ĐẶT TRƯỚC CỦA BẠN",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10, 5, 0, 0)
            };

            tab.Controls.Add(dgvLichSuDatTruoc);
            tab.Controls.Add(lblHistoryHeader);
            tab.Controls.Add(pnlTop);

            pnlTop.BringToFront();
            lblHistoryHeader.BringToFront();
            dgvLichSuDatTruoc.BringToFront();
        }

        // =========================================================================
        // TAB 3: LỊCH SỬ MƯỢN & PHẠT CÁ NHÂN
        // =========================================================================
        private void BuildTabLichSu(TabPage tab)
        {
            tab.Font = new Font("Segoe UI", 9.5F);
            tab.BackColor = Color.White;

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 330
            };

            // Top: Lịch sử phiếu mượn
            var pnlTop = new Panel { Dock = DockStyle.Fill };
            var lblMuonHeader = new Label { Text = "📖 LỊCH SỬ MƯỢN SÁCH CỦA BẠN:", Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Padding = new Padding(10, 5, 0, 0) };
            dgvLichSuMuon = CreateStandardGrid();
            dgvLichSuMuon.CellFormatting += DgvLichSuMuon_CellFormatting;

            pnlTop.Controls.Add(dgvLichSuMuon);
            pnlTop.Controls.Add(lblMuonHeader);
            lblMuonHeader.BringToFront();
            dgvLichSuMuon.BringToFront();
            split.Panel1.Controls.Add(pnlTop);

            // Bottom: Lịch sử phiếu phạt
            var pnlBottom = new Panel { Dock = DockStyle.Fill };
            var lblPhatHeader = new Label { Text = "⚠️ DANH SÁCH PHIẾU PHẠT PHÁT SINH CỦA BẠN:", Dock = DockStyle.Top, Height = 28, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(220, 38, 38), Padding = new Padding(10, 5, 0, 0) };
            dgvLichSuPhat = CreateStandardGrid();

            lblTongQuanCaNhan = new Label
            {
                Text = "Tổng kết cá nhân: Đang load...",
                Dock = DockStyle.Bottom,
                Height = 35,
                BackColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10, 8, 0, 0)
            };

            pnlBottom.Controls.Add(dgvLichSuPhat);
            pnlBottom.Controls.Add(lblPhatHeader);
            pnlBottom.Controls.Add(lblTongQuanCaNhan);
            lblPhatHeader.BringToFront();
            lblTongQuanCaNhan.BringToFront();
            dgvLichSuPhat.BringToFront();
            split.Panel2.Controls.Add(pnlBottom);

            tab.Controls.Add(split);
        }

        // =========================================================================
        // TAB 4: THÔNG TIN CÁ NHÂN & TÀI KHOẢN
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
                Text = "1. THÔNG TIN THẺ & TÀI KHOẢN HỆ THỐNG",
                Location = new Point(20, 15),
                Size = new Size(850, 140),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138)
            };

            var lblMa = new Label { Text = "Mã Độc Giả:", Location = new Point(20, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtMaDG_Profile = new TextBox { Location = new Point(130, 32), Size = new Size(130, 25), ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            var lblUser = new Label { Text = "Tên Đăng Nhập:", Location = new Point(290, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtTenDangNhap_Profile = new TextBox { Location = new Point(410, 32), Size = new Size(150, 25), ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            var lblLoai = new Label { Text = "Loại Độc Giả:", Location = new Point(590, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtLoaiDocGia_Profile = new TextBox { Location = new Point(690, 32), Size = new Size(130, 25), ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            var lblNgayDK = new Label { Text = "Ngày Đăng Ký:", Location = new Point(20, 80), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtNgayDangKy_Profile = new TextBox { Location = new Point(130, 77), Size = new Size(130, 25), ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F) };

            var lblTrangThai = new Label { Text = "Trạng Thái Thẻ:", Location = new Point(290, 80), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtTrangThaiThe_Profile = new TextBox { Location = new Point(410, 77), Size = new Size(150, 25), ReadOnly = true, BackColor = Color.FromArgb(241, 245, 249), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            grpSystem.Controls.AddRange(new Control[] {
                lblMa, txtMaDG_Profile, lblUser, txtTenDangNhap_Profile, lblLoai, txtLoaiDocGia_Profile,
                lblNgayDK, txtNgayDangKy_Profile, lblTrangThai, txtTrangThaiThe_Profile
            });

            // Group 2: Thông tin cá nhân (Editable)
            var grpProfile = new GroupBox
            {
                Text = "2. THÔNG TIN CÁ NHÂN LIÊN HỆ",
                Location = new Point(20, 170),
                Size = new Size(850, 185),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(16, 149, 193)
            };

            var lblHoTen = new Label { Text = "Họ và Tên (*):", Location = new Point(20, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            txtHoTen_Profile = new TextBox { Location = new Point(130, 32), Size = new Size(260, 25), Font = new Font("Segoe UI", 9F) };

            var lblNgaySinh = new Label { Text = "Ngày Sinh:", Location = new Point(440, 35), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            dtpNgaySinh_Profile = new DateTimePicker { Location = new Point(540, 32), Size = new Size(150, 25), Format = DateTimePickerFormat.Short, Font = new Font("Segoe UI", 9F) };

            var lblSDT = new Label { Text = "Số Điện Thoại:", Location = new Point(20, 80), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            txtSDT_Profile = new TextBox { Location = new Point(130, 77), Size = new Size(260, 25), Font = new Font("Segoe UI", 9F) };

            var lblEmail = new Label { Text = "Email:", Location = new Point(440, 80), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtEmail_Profile = new TextBox { Location = new Point(540, 77), Size = new Size(280, 25), Font = new Font("Segoe UI", 9F) };

            var lblDiaChi = new Label { Text = "Địa Chỉ:", Location = new Point(20, 125), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Regular), ForeColor = Color.FromArgb(30, 41, 59) };
            txtDiaChi_Profile = new TextBox { Location = new Point(130, 122), Size = new Size(690, 25), Font = new Font("Segoe UI", 9F) };

            grpProfile.Controls.AddRange(new Control[] {
                lblHoTen, txtHoTen_Profile, lblNgaySinh, dtpNgaySinh_Profile,
                lblSDT, txtSDT_Profile, lblEmail, txtEmail_Profile,
                lblDiaChi, txtDiaChi_Profile
            });

            // Group 3: Đổi mật khẩu
            var grpPassword = new GroupBox
            {
                Text = "3. ĐỔI MẬT KHẨU ĐĂNG NHẬP (Bỏ trống nếu không thay đổi)",
                Location = new Point(20, 370),
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
                Location = new Point(20, 490),
                Size = new Size(320, 44),
                BackColor = Color.FromArgb(22, 163, 74), // Green 600
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => BtnLuuThongTin_Click();

            var btnReset = new Button
            {
                Text = "↺ Khôi Phục Ban Đầu",
                Location = new Point(360, 490),
                Size = new Size(200, 44),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReset.Click += (s, e) => LoadThongTinCaNhan();

            pnlContainer.Controls.AddRange(new Control[] { grpSystem, grpProfile, grpPassword, btnSave, btnReset });
            tab.Controls.Add(pnlContainer);
        }

        // =========================================================================
        // DATA LOADERS & EVENT HANDLERS
        // =========================================================================
        private void DgvLichSuMuon_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvLichSuMuon.Columns.Contains("TrangThai"))
            {
                string status = dgvLichSuMuon.Rows[e.RowIndex].Cells["TrangThai"].Value?.ToString() ?? "";
                if (status == "QuaHan")
                {
                    dgvLichSuMuon.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226); // Red 100
                    dgvLichSuMuon.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(153, 27, 27); // Red 800
                }
            }
        }

        private void LoadAllDocGiaData()
        {
            CheckSanSangNhan();
            SearchSach();
            LoadSachDatTruocCombo();
            LoadLichSuDatTruoc();
            LoadLichSuMuon();
            LoadLichSuPhat();
            LoadThongTinCaNhan();
        }

        private void BtnReload_Click()
        {
            LoadAllDocGiaData();
            MessageBox.Show("✅ Đã làm mới toàn bộ dữ liệu từ CSDL thành công!", "Làm Mới Dữ Liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadThongTinCaNhan()
        {
            string maDG = !string.IsNullOrEmpty(SessionContext.MaDG) ? SessionContext.MaDG : "DG001";
            try
            {
                string sql = @"
                    SELECT dg.MaDG, dg.HoTen, dg.NgaySinh, dg.DiaChi, dg.SDT, dg.Email,
                           dg.LoaiDocGia, dg.NgayDangKy, dg.TrangThai, tk.TenDangNhap
                    FROM DocGia dg
                    JOIN TaiKhoan tk ON dg.MaTaiKhoan = tk.MaTaiKhoan
                    WHERE dg.MaDG = @id;";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql, new SqlParameter[] { new SqlParameter("@id", maDG) });
                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    txtMaDG_Profile.Text = r["MaDG"].ToString() ?? "";
                    txtTenDangNhap_Profile.Text = r["TenDangNhap"].ToString() ?? "";
                    txtLoaiDocGia_Profile.Text = r["LoaiDocGia"].ToString() ?? "";
                    txtNgayDangKy_Profile.Text = r["NgayDangKy"] != DBNull.Value ? Convert.ToDateTime(r["NgayDangKy"]).ToString("dd/MM/yyyy") : "";
                    txtTrangThaiThe_Profile.Text = r["TrangThai"].ToString() ?? "";

                    txtHoTen_Profile.Text = r["HoTen"].ToString() ?? "";
                    if (r["NgaySinh"] != DBNull.Value) dtpNgaySinh_Profile.Value = Convert.ToDateTime(r["NgaySinh"]);
                    txtSDT_Profile.Text = r["SDT"] != DBNull.Value ? r["SDT"].ToString() ?? "" : "";
                    txtEmail_Profile.Text = r["Email"] != DBNull.Value ? r["Email"].ToString() ?? "" : "";
                    txtDiaChi_Profile.Text = r["DiaChi"] != DBNull.Value ? r["DiaChi"].ToString() ?? "" : "";

                    txtMatKhauMoi_Profile.Text = "";
                    txtXacNhanMatKhau_Profile.Text = "";
                }
            }
            catch { }
        }

        private void BtnLuuThongTin_Click()
        {
            string maDG = !string.IsNullOrEmpty(SessionContext.MaDG) ? SessionContext.MaDG : "DG001";
            string hoTen = txtHoTen_Profile.Text.Trim();
            string sdt = txtSDT_Profile.Text.Trim();
            string email = txtEmail_Profile.Text.Trim();
            string diaChi = txtDiaChi_Profile.Text.Trim();
            DateTime ngaySinh = dtpNgaySinh_Profile.Value.Date;
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
                    new SqlParameter("@p_MaDG", maDG),
                    new SqlParameter("@p_HoTen", hoTen),
                    new SqlParameter("@p_NgaySinh", ngaySinh),
                    new SqlParameter("@p_DiaChi", diaChi),
                    new SqlParameter("@p_SDT", sdt),
                    new SqlParameter("@p_Email", email),
                    new SqlParameter("@p_MatKhauMoi", string.IsNullOrEmpty(passMoi) ? (object)DBNull.Value : passMoi)
                };

                DataTable dt = DatabaseHelper.ExecuteProcedure("sp_CapNhatThongTinDocGia", pars);
                string msg = dt.Rows.Count > 0 && dt.Columns.Contains("ThongBao") ? dt.Rows[0]["ThongBao"].ToString() ?? "" : "Cập nhật thành công!";

                MessageBox.Show(msg, "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Update session
                SessionContext.TenDocGia = hoTen;
                lblUser.Text = $"👤 {SessionContext.TenDocGia}\n(Thẻ: {SessionContext.MaDG})";
                LoadThongTinCaNhan();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SearchSach()
        {
            string tuKhoa = txtTuKhoa.Text.Trim();
            try
            {
                var pars = new SqlParameter[] { new SqlParameter("@p_TuKhoa", tuKhoa) };
                DataTable dt = DatabaseHelper.ExecuteProcedure("sp_TraCuuSach", pars);
                dgvTraCuu.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tra cứu sách: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSachDatTruocCombo()
        {
            try
            {
                string sql = "SELECT MaSach, CONCAT(MaSach, ' - ', TenSach) AS DisplayText FROM Sach";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql);
                cboSachDatTruoc.DataSource = dt;
                cboSachDatTruoc.DisplayMember = "DisplayText";
                cboSachDatTruoc.ValueMember = "MaSach";
            }
            catch { }
        }

        private void BtnConfirmDat_Click(object? sender, EventArgs e)
        {
            string maPDT = txtMaPDT.Text.Trim();
            if (string.IsNullOrEmpty(maPDT))
            {
                MessageBox.Show("Vui lòng nhập mã phiếu đặt trước!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboSachDatTruoc.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn đầu sách muốn đặt trước!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string maSach = cboSachDatTruoc.SelectedValue.ToString() ?? "";
            string maDG = !string.IsNullOrEmpty(SessionContext.MaDG) ? SessionContext.MaDG : "DG001";

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaPhieuDatTruoc", maPDT),
                    new SqlParameter("@p_MaDG", maDG),
                    new SqlParameter("@p_MaSach", maSach)
                };

                DataTable dt = DatabaseHelper.ExecuteProcedure("sp_DatTruocSach", pars);
                string msg = dt.Rows.Count > 0 && dt.Columns.Contains("ThongBao") ? dt.Rows[0]["ThongBao"].ToString() ?? "" : "Giao tác đặt trước hoàn tất!";

                if (msg.Contains("còn bản sao trống") || msg.Contains("Lỗi"))
                {
                    MessageBox.Show(msg, "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(msg, "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtMaPDT.Text = "PDT" + DateTime.Now.ToString("HHmmss");
                    LoadLichSuDatTruoc();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi giao tác đặt trước: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLichSuDatTruoc()
        {
            string maDG = !string.IsNullOrEmpty(SessionContext.MaDG) ? SessionContext.MaDG : "DG001";
            try
            {
                string sql = @"
                    SELECT MaPhieuDatTruoc, TenSach, NgayDat, TrangThai
                    FROM View_LichSuDatTruoc_DocGia
                    WHERE MaDG = @id
                    ORDER BY NgayDat DESC;";
                dgvLichSuDatTruoc.DataSource = DatabaseHelper.ExecuteQuery(sql, new SqlParameter[] { new SqlParameter("@id", maDG) });
                CheckSanSangNhan(); // Update banner if a reservation changed
            }
            catch { }
        }

        private void CheckSanSangNhan()
        {
            string maDG = !string.IsNullOrEmpty(SessionContext.MaDG) ? SessionContext.MaDG : "DG001";
            try
            {
                string sql = "SELECT COUNT(*) FROM PhieuDatTruoc WHERE MaDG = @id AND TrangThai = 'ChoNhan'";
                var count = Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new SqlParameter[] { new SqlParameter("@id", maDG) }));
                if (count > 0)
                {
                    lblBannerMessage.Text = $"⚠️ BẠN CÓ {count} YÊU CẦU ĐẶT TRƯỚC ĐÃ SẴN SÀNG! Vui lòng đến thư viện nhận sách trong vòng 48h tới, nếu không hệ thống sẽ tự động hủy giữ chỗ.";
                    pnlBanner.Visible = true;
                }
                else
                {
                    pnlBanner.Visible = false;
                }
            }
            catch { }
        }

        private void LoadLichSuMuon()
        {
            string maDG = !string.IsNullOrEmpty(SessionContext.MaDG) ? SessionContext.MaDG : "DG001";
            try
            {
                string sql = @"
                    SELECT MaPhieuMuon, TenSach, MaCuonSach, NgayMuon, NgayHenTra,
                           NgayTraThucTe, TinhTrangSachKhiTra, TrangThai
                    FROM View_LichSuMuon_DocGia
                    WHERE MaDG = @id
                    ORDER BY NgayMuon DESC;";
                dgvLichSuMuon.DataSource = DatabaseHelper.ExecuteQuery(sql, new SqlParameter[] { new SqlParameter("@id", maDG) });
            }
            catch { }
        }

        private void LoadLichSuPhat()
        {
            string maDG = !string.IsNullOrEmpty(SessionContext.MaDG) ? SessionContext.MaDG : "DG001";
            try
            {
                string sql = @"
                    SELECT pp.MaPhieuPhat, pp.MaPhieuMuon, pp.LyDoPhat, pp.SoTienPhat, pp.NgayLap, pp.TrangThaiThanhToan
                    FROM PhieuPhat pp
                    JOIN PhieuMuon pm ON pp.MaPhieuMuon = pm.MaPhieuMuon
                    WHERE pm.MaDG = @id
                    ORDER BY pp.NgayLap DESC;";
                DataTable dt = DatabaseHelper.ExecuteQuery(sql, new SqlParameter[] { new SqlParameter("@id", maDG) });
                dgvLichSuPhat.DataSource = dt;

                int unpaidCount = 0;
                decimal unpaidMoney = 0;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["TrangThaiThanhToan"].ToString() == "ChuaThanhToan")
                    {
                        unpaidCount++;
                        unpaidMoney += Convert.ToDecimal(r["SoTienPhat"]);
                    }
                }

                lblTongQuanCaNhan.Text = $"📊 Bạn có {unpaidCount} phiếu phạt chưa nộp (Tổng: {unpaidMoney:N0} VNĐ). {(unpaidCount >= 3 ? "⚠️ Thẻ đang bị TẠM KHÓA do có từ 3 phiếu phạt chưa nộp!" : "")}";
                lblTongQuanCaNhan.ForeColor = unpaidCount > 0 ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 163, 74);
            }
            catch { }
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
