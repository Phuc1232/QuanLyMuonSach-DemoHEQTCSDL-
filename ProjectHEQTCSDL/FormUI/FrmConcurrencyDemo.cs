using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ProjectHEQTCSDL.Core;

namespace ProjectHEQTCSDL.FormUI
{
    public class FrmConcurrencyDemo : Form
    {
        // Top Controls
        private ComboBox cboScenario = null!;
        private Button btnReset = null!;
        private Button btnRefreshDB = null!;

        // Layout Containers
        private Panel pnlUser1Content = null!;
        private Panel pnlUser2Content = null!;
        private TextBox txtUser1Result = null!;
        private TextBox txtUser2Result = null!;
        private DataGridView dgvDBState = null!;
        private TextBox txtExplanation = null!;

        // Interactive Transaction State
        private SqlConnection? activeConnUser1 = null;
        private SqlTransaction? activeTranUser1 = null;
        private SqlConnection? activeConnUser2 = null;
        private SqlTransaction? activeTranUser2 = null;

        public FrmConcurrencyDemo()
        {
            InitializeComponent();
            this.Load += (s, e) => LoadScenario(0);
        }

        private void InitializeComponent()
        {
            this.Text = "Module Mô Phỏng Điều Khiển Tương Tranh Đa Luồng (Interactive GUI)";
            this.Size = new Size(1150, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F);
            this.FormClosing += FrmConcurrencyDemo_FormClosing;

            // 1. TOOLBAR
            var pnlToolbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(15, 10, 15, 10) };
            
            var lblScenario = new Label { Text = "Chọn Kịch bản:", AutoSize = true, Location = new Point(15, 18), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            cboScenario = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(130, 15),
                Width = 350,
                Font = new Font("Segoe UI", 10F)
            };
            cboScenario.Items.AddRange(new object[] {
                "1. Lost Update (Mất cập nhật)",
                "2. Dirty Read (Đọc dữ liệu rác)",
                "3. Non-Repeatable Read (Đọc không nhất quán)",
                "4. Phantom Read (Bóng ma)",
                "5. Deadlock (Bế tắc tương hỗ)"
            });
            cboScenario.SelectedIndexChanged += (s, e) => LoadScenario(cboScenario.SelectedIndex);

            btnReset = new Button { Text = "↺ Khôi Phục Dữ Liệu Demo", Location = new Point(500, 13), Width = 180, Height = 32, BackColor = Color.FromArgb(71, 85, 105), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnReset.Click += (s, e) => ResetDemoData(true);

            btnRefreshDB = new Button { Text = "🔄 Tải Lại Dữ Liệu CSDL", Location = new Point(690, 13), Width = 170, Height = 32, BackColor = Color.FromArgb(14, 165, 233), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnRefreshDB.Click += (s, e) => RefreshDatabaseInspector();

            pnlToolbar.Controls.AddRange(new Control[] { lblScenario, cboScenario, btnReset, btnRefreshDB });


            // 2. MAIN SPLIT (User 1 & User 2)
            var pnlMain = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 350,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15, 10, 15, 10)
            };
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pnlMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // User 1 Column
            var grpUser1 = new GroupBox { Text = "👤 CỘT TRÁI: NGƯỜI DÙNG 1", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 58, 138), Padding = new Padding(10) };
            pnlUser1Content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            txtUser1Result = new TextBox { Dock = DockStyle.Bottom, Height = 80, Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(226, 232, 240), Font = new Font("Consolas", 10F) };
            grpUser1.Controls.Add(pnlUser1Content);
            grpUser1.Controls.Add(new Label { Text = "📋 Nhật ký & Trạng thái User 1:", Dock = DockStyle.Bottom, Height = 25, Padding = new Padding(0, 5, 0, 0) });
            grpUser1.Controls.Add(txtUser1Result);

            // User 2 Column
            var grpUser2 = new GroupBox { Text = "👤 CỘT PHẢI: NGƯỜI DÙNG 2", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(180, 83, 9), Padding = new Padding(10) };
            pnlUser2Content = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            txtUser2Result = new TextBox { Dock = DockStyle.Bottom, Height = 80, Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.FromArgb(226, 232, 240), Font = new Font("Consolas", 10F) };
            grpUser2.Controls.Add(pnlUser2Content);
            grpUser2.Controls.Add(new Label { Text = "📋 Nhật ký & Trạng thái User 2:", Dock = DockStyle.Bottom, Height = 25, Padding = new Padding(0, 5, 0, 0) });
            grpUser2.Controls.Add(txtUser2Result);

            pnlMain.Controls.Add(grpUser1, 0, 0);
            pnlMain.Controls.Add(grpUser2, 1, 0);

            // 3. BOTTOM PANEL (DB Inspector)
            var grpDB = new GroupBox { Text = "🔍 BẢNG GIÁM SÁT CSDL THỰC TẾ (DATABASE INSPECTOR)", Dock = DockStyle.Fill, Padding = new Padding(15, 10, 15, 10), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42) };
            
            var pnlDBLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            pnlDBLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            pnlDBLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

            dgvDBState = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, BackgroundColor = Color.White, RowHeadersVisible = false, Font = new Font("Segoe UI", 9F) };
            
            var pnlExp = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 0, 0) };
            txtExplanation = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(254, 252, 232), ForeColor = Color.FromArgb(63, 39, 0), Font = new Font("Segoe UI", 9.5F), ScrollBars = ScrollBars.Vertical };
            pnlExp.Controls.Add(txtExplanation);
            pnlExp.Controls.Add(new Label { Text = "💡 Giải thích hiện tượng & Cơ chế:", Dock = DockStyle.Top, Height = 25 });

            pnlDBLayout.Controls.Add(dgvDBState, 0, 0);
            pnlDBLayout.Controls.Add(pnlExp, 1, 0);
            grpDB.Controls.Add(pnlDBLayout);

            // Correct Z-order for docking (Fill must be added first, then Top)
            this.Controls.Add(grpDB);
            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlToolbar);
            
            // Set initial scenario AFTER all controls are added
            cboScenario.SelectedIndex = 0;
        }

        private void LogUser1(string message) => txtUser1Result.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        private void LogUser2(string message) => txtUser2Result.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");

        private void DisposeTransactions()
        {
            if (activeTranUser1 != null) { try { activeTranUser1.Rollback(); } catch { } activeTranUser1.Dispose(); activeTranUser1 = null; }
            if (activeConnUser1 != null) { try { activeConnUser1.Close(); } catch { } activeConnUser1.Dispose(); activeConnUser1 = null; }
            if (activeTranUser2 != null) { try { activeTranUser2.Rollback(); } catch { } activeTranUser2.Dispose(); activeTranUser2 = null; }
            if (activeConnUser2 != null) { try { activeConnUser2.Close(); } catch { } activeConnUser2.Dispose(); activeConnUser2 = null; }
        }

        private void FrmConcurrencyDemo_FormClosing(object? sender, FormClosingEventArgs e)
        {
            DisposeTransactions();
        }

        private void LoadScenario(int index)
        {
            DisposeTransactions();
            pnlUser1Content.Controls.Clear();
            pnlUser2Content.Controls.Clear();
            txtUser1Result.Clear();
            txtUser2Result.Clear();
            ResetDemoData(false);

            switch (index)
            {
                case 0: BuildLostUpdateUI(); break;
                case 1: BuildDirtyReadUI(); break;
                case 2: BuildNonRepeatableReadUI(); break;
                case 3: BuildPhantomReadUI(); break;
                case 4: BuildDeadlockUI(); break;
            }
        }

        private void RefreshDatabaseInspector()
        {
            string query = "";
            int scenario = cboScenario.SelectedIndex;
            if (scenario == 0 || scenario == 1)
                query = "SELECT MaCuonSach, TrangThai, TinhTrang FROM CuonSach WITH (NOLOCK) WHERE MaCuonSach = 'CS001'";
            else if (scenario == 2)
                query = "SELECT c.MaCuonSach, s.TenSach, c.TrangThai FROM CuonSach c WITH (NOLOCK) JOIN Sach s WITH (NOLOCK) ON c.MaSach = s.MaSach WHERE c.MaSach = 'S001'";
            else if (scenario == 3)
                query = "SELECT MaPhieuPhat, MaPhieuMuon, LyDoPhat, SoTienPhat FROM PhieuPhat WITH (NOLOCK)";
            else if (scenario == 4)
                query = "SELECT c.MaCuonSach, s.TenSach, c.TinhTrang, c.TrangThai FROM CuonSach c WITH (NOLOCK) JOIN Sach s WITH (NOLOCK) ON c.MaSach = s.MaSach WHERE c.MaCuonSach IN ('CS001', 'CS002')";

            if (!string.IsNullOrEmpty(query))
            {
                try
                {
                    var dt = DatabaseHelper.ExecuteQuery(query);
                    dgvDBState.DataSource = dt;
                }
                catch (Exception ex)
                {
                    LogUser1("Lỗi tải CSDL: " + ex.Message);
                }
            }
        }

        private void ResetDemoData(bool showMessage)
        {
            DisposeTransactions();
            try
            {
                DatabaseHelper.ExecuteNonQuery(@"
                    UPDATE CuonSach SET TinhTrang = 'ConTot', TrangThai = 'DangMuon' WHERE MaCuonSach IN ('CS001', 'CS002', 'CS003');
                    DELETE FROM CT_PhieuMuon WHERE MaCuonSach = 'CS003' AND MaPhieuMuon LIKE 'PM_DEMO%';
                    DELETE FROM PhieuPhat WHERE MaPhieuPhat = 'PP999';
                    DELETE FROM PhieuMuon WHERE MaPhieuMuon LIKE 'PM_DEMO%';
                ");
                RefreshDatabaseInspector();
                if (showMessage) MessageBox.Show("Khôi phục dữ liệu mẫu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { if (showMessage) MessageBox.Show("Lỗi: " + ex.Message); }
        }

        // ==========================================
        // 1. DEMO LOST UPDATE
        // ==========================================
        private void BuildLostUpdateUI()
        {
            txtExplanation.Text = "KỊCH BẢN LOST UPDATE:\r\n- Thủ thư B lưu thay đổi (Mất đĩa CD).\r\n- Thủ thư A (cũng đang mở form với dữ liệu cũ) lưu thay đổi (Rách bìa) sau đó.\r\n- Kết quả: Thao tác của A ghi đè hoàn toàn B mà không có cảnh báo nào!\r\n- Giải pháp: Cần áp dụng Optimistic Locking (rowversion) để cảnh báo sửa đổi chồng chéo.";

            // User 1
            var lbl1 = new Label { Text = "Sách CS001 - Tình trạng cũ: ConTot", AutoSize = true, Location = new Point(10, 20), ForeColor = Color.Black };
            var cbo1 = new ComboBox { Location = new Point(10, 50), Width = 250 };
            cbo1.Items.AddRange(new object[] { "Rách bìa ngoài", "Bị ướt" }); cbo1.SelectedIndex = 0;
            var btnA = new Button { Text = "💾 Bước 2: Thủ thư A Bấm Lưu", Location = new Point(10, 90), Width = 250, Height = 35, BackColor = Color.Crimson, ForeColor = Color.White };
            
            btnA.Click += (s, e) =>
            {
                try
                {
                    var p = new SqlParameter[] { new SqlParameter("@p_MaCuonSach", "CS001"), new SqlParameter("@p_TinhTrangMoi", cbo1.Text) };
                    var dt = DatabaseHelper.ExecuteProcedure("sp_Demo_LostUpdate_CapNhat", p);
                    LogUser1("Thủ thư A ĐÃ LƯU: " + dt.Rows[0]["ThongBao"].ToString());
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser1("Lỗi: " + ex.Message); }
            };
            pnlUser1Content.Controls.AddRange(new Control[] { lbl1, cbo1, btnA });

            // User 2
            var lbl2 = new Label { Text = "Sách CS001 - Tình trạng cũ: ConTot", AutoSize = true, Location = new Point(10, 20), ForeColor = Color.Black };
            var cbo2 = new ComboBox { Location = new Point(10, 50), Width = 250 };
            cbo2.Items.AddRange(new object[] { "Mất đĩa CD kèm theo", "Rách trang" }); cbo2.SelectedIndex = 0;
            var btnB = new Button { Text = "💾 Bước 1: Thủ thư B Bấm Lưu", Location = new Point(10, 90), Width = 250, Height = 35, BackColor = Color.SteelBlue, ForeColor = Color.White };
            
            btnB.Click += (s, e) =>
            {
                try
                {
                    var p = new SqlParameter[] { new SqlParameter("@p_MaCuonSach", "CS001"), new SqlParameter("@p_TinhTrangMoi", cbo2.Text) };
                    var dt = DatabaseHelper.ExecuteProcedure("sp_Demo_LostUpdate_CapNhat", p);
                    LogUser2("Thủ thư B ĐÃ LƯU: " + dt.Rows[0]["ThongBao"].ToString());
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser2("Lỗi: " + ex.Message); }
            };
            pnlUser2Content.Controls.AddRange(new Control[] { lbl2, cbo2, btnB });
        }

        // ==========================================
        // 2. DEMO DIRTY READ
        // ==========================================
        private void BuildDirtyReadUI()
        {
            txtExplanation.Text = "KỊCH BẢN DIRTY READ (ĐỌC RÁC):\r\n- Thủ thư mở giao dịch chuyển CS001 sang 'CoSan' nhưng CHƯA COMMIT.\r\n- Độc giả tra cứu với READ UNCOMMITTED sẽ đọc được 'CoSan'.\r\n- Sau đó Thủ thư Hủy (Rollback). Độc giả đã đọc phải dữ liệu rác không có thực!\r\n- Giải pháp: Độc giả nên dùng READ COMMITTED (mặc định) để bị chặn chờ đến khi Thủ thư xong.";

            // User 1
            var btnStart = new Button { Text = "1. Bắt đầu thủ tục trả (Mở Transaction đổi sang 'CoSan')", Location = new Point(10, 20), Width = 400, Height = 35 };
            var btnCommit = new Button { Text = "2a. ✅ Khách nộp đủ tiền -> Xác nhận Trả", Location = new Point(10, 70), Width = 400, Height = 35, Enabled = false };
            var btnRollback = new Button { Text = "2b. ❌ Khách nợ tiền -> Hủy thủ tục (Rollback)", Location = new Point(10, 120), Width = 400, Height = 35, Enabled = false, BackColor = Color.LightCoral };

            btnStart.Click += (s, e) =>
            {
                try
                {
                    activeConnUser1 = DatabaseHelper.GetConnection();
                    activeConnUser1.Open();
                    activeTranUser1 = activeConnUser1.BeginTransaction();
                    using var cmd = new SqlCommand("UPDATE CuonSach SET TrangThai = 'CoSan' WHERE MaCuonSach = 'CS001'", activeConnUser1, activeTranUser1);
                    cmd.ExecuteNonQuery();
                    LogUser1("Đã mở Transaction: Tạm đổi CS001 -> 'CoSan' (CHƯA COMMIT).");
                    btnStart.Enabled = false; btnCommit.Enabled = true; btnRollback.Enabled = true;
                }
                catch (Exception ex) { LogUser1("Lỗi: " + ex.Message); }
            };

            btnCommit.Click += (s, e) =>
            {
                activeTranUser1?.Commit(); DisposeTransactions();
                LogUser1("ĐÃ COMMIT. Giao dịch hoàn tất.");
                btnStart.Enabled = true; btnCommit.Enabled = false; btnRollback.Enabled = false; RefreshDatabaseInspector();
            };

            btnRollback.Click += (s, e) =>
            {
                activeTranUser1?.Rollback(); DisposeTransactions();
                LogUser1("ĐÃ ROLLBACK. Hủy toàn bộ thay đổi!");
                btnStart.Enabled = true; btnCommit.Enabled = false; btnRollback.Enabled = false; RefreshDatabaseInspector();
            };
            pnlUser1Content.Controls.AddRange(new Control[] { btnStart, btnCommit, btnRollback });

            // User 2
            var btnSearch = new Button { Text = "🔍 Tra Cứu Tình Trạng Sách (CS001)", Location = new Point(10, 20), Width = 300, Height = 45, BackColor = Color.SeaGreen, ForeColor = Color.White };
            btnSearch.Click += (s, e) =>
            {
                try
                {
                    var p = new SqlParameter[] { new SqlParameter("@p_MaCuonSach", "CS001") };
                    var dt = DatabaseHelper.ExecuteProcedure("sp_Demo_DirtyRead_TraCuu", p);
                    if (dt.Rows.Count > 0)
                        LogUser2($"Đọc được dữ liệu rác: TrangThai = '{dt.Rows[0]["TrangThai"]}'");
                }
                catch (Exception ex) { LogUser2("Lỗi tra cứu: " + ex.Message); }
            };
            pnlUser2Content.Controls.Add(btnSearch);
        }

        // ==========================================
        // 3. DEMO NON-REPEATABLE READ
        // ==========================================
        private void BuildNonRepeatableReadUI()
        {
            txtExplanation.Text = "KỊCH BẢN NON-REPEATABLE READ:\r\n- Quản lý mở phiên kiểm kê bằng READ COMMITTED đếm được X cuốn.\r\n- Thủ thư xen ngang cho mượn 1 cuốn.\r\n- Quản lý đếm lại lần 2 thấy còn X-1 cuốn. Dữ liệu không nhất quán trong cùng 1 phiên!\r\n- Giải pháp: Quản lý cần dùng REPEATABLE READ để giữ khóa chặn thao tác của thủ thư.";

            // User 1
            var btnCount1 = new Button { Text = "1. Mở phiên kiểm kê & Đếm lần 1 (Read Committed)", Location = new Point(10, 20), Width = 400, Height = 35, BackColor = Color.MediumPurple, ForeColor = Color.White };
            var btnCount2 = new Button { Text = "3. Đếm lần 2 & Đóng phiên", Location = new Point(10, 70), Width = 400, Height = 35, Enabled = false };
            
            btnCount1.Click += (s, e) =>
            {
                try
                {
                    activeConnUser1 = DatabaseHelper.GetConnection();
                    activeConnUser1.Open();
                    // READ COMMITTED is default, but we set explicitly for demo
                    using var cmdSet = new SqlCommand("SET TRANSACTION ISOLATION LEVEL READ COMMITTED;", activeConnUser1);
                    cmdSet.ExecuteNonQuery();
                    activeTranUser1 = activeConnUser1.BeginTransaction();

                    using var cmd = new SqlCommand("SELECT COUNT(*) FROM CuonSach WHERE MaSach = 'S001' AND TrangThai = 'CoSan'", activeConnUser1, activeTranUser1);
                    int count = (int)cmd.ExecuteScalar();
                    LogUser1($"Đếm lần 1: Có {count} cuốn 'CoSan'. (Đang giữ Transaction mở...)");
                    
                    btnCount1.Enabled = false; btnCount2.Enabled = true;
                }
                catch (Exception ex) { LogUser1("Lỗi: " + ex.Message); }
            };

            btnCount2.Click += (s, e) =>
            {
                try
                {
                    using var cmd = new SqlCommand("SELECT COUNT(*) FROM CuonSach WHERE MaSach = 'S001' AND TrangThai = 'CoSan'", activeConnUser1, activeTranUser1);
                    int count = (int)cmd.ExecuteScalar();
                    LogUser1($"Đếm lần 2: Đột ngột chỉ còn {count} cuốn 'CoSan'!");
                    activeTranUser1?.Commit(); DisposeTransactions();
                    btnCount1.Enabled = true; btnCount2.Enabled = false;
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser1("Lỗi: " + ex.Message); }
            };
            pnlUser1Content.Controls.AddRange(new Control[] { btnCount1, btnCount2 });

            // User 2
            var btnBorrow = new Button { Text = "2. Cho độc giả mượn 1 cuốn (CS002 sang DangMuon)", Location = new Point(10, 45), Width = 400, Height = 40, BackColor = Color.DarkOrange, ForeColor = Color.White };
            btnBorrow.Click += (s, e) =>
            {
                try
                {
                    var p = new SqlParameter[] { new SqlParameter("@p_MaCuonSach", "CS002") };
                    var dt = DatabaseHelper.ExecuteProcedure("sp_Demo_NonRepeatableRead_ChoMuon", p);
                    LogUser2(dt.Rows[0]["ThongBao"].ToString() ?? "");
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser2("Lỗi: " + ex.Message); }
            };
            pnlUser2Content.Controls.Add(btnBorrow);
        }

        // ==========================================
        // 4. DEMO PHANTOM READ
        // ==========================================
        private void BuildPhantomReadUI()
        {
            txtExplanation.Text = "KỊCH BẢN PHANTOM READ (ĐỌC BÓNG MA):\r\n- Quản lý đọc danh sách Phiếu phạt với mức cô lập REPEATABLE READ.\r\n- REPEATABLE READ khóa cập nhật các dòng hiện có, nhưng KHÔNG khóa thêm mới (INSERT).\r\n- Hệ thống tự động phát sinh thêm 1 Phiếu phạt mới (PP999) xen ngang.\r\n- Kết quả: Quản lý đếm lại thấy số lượng tăng lên (Bóng ma xuất hiện)!\r\n- Giải pháp: Sử dụng SERIALIZABLE để khóa toàn bộ dải dữ liệu (Range Locks).";

            // User 1
            var btnCount1 = new Button { Text = "1. Bắt đầu phiên tổng hợp & Đếm lần 1 (Repeatable Read)", Location = new Point(10, 20), Width = 400, Height = 35, BackColor = Color.MediumPurple, ForeColor = Color.White };
            var btnCount2 = new Button { Text = "3. F5 Đếm lần 2 & Đóng phiên", Location = new Point(10, 70), Width = 400, Height = 35, Enabled = false };
            
            btnCount1.Click += (s, e) =>
            {
                try
                {
                    activeConnUser1 = DatabaseHelper.GetConnection();
                    activeConnUser1.Open();
                    using var cmdSet = new SqlCommand("SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;", activeConnUser1);
                    cmdSet.ExecuteNonQuery();
                    activeTranUser1 = activeConnUser1.BeginTransaction();

                    using var cmd = new SqlCommand("SELECT COUNT(*) FROM PhieuPhat", activeConnUser1, activeTranUser1);
                    int count = (int)cmd.ExecuteScalar();
                    LogUser1($"Lần 1: Tổng số phiếu phạt = {count}. Đang in báo cáo...");
                    
                    btnCount1.Enabled = false; btnCount2.Enabled = true;
                }
                catch (Exception ex) { LogUser1("Lỗi: " + ex.Message); }
            };

            btnCount2.Click += (s, e) =>
            {
                try
                {
                    using var cmd = new SqlCommand("SELECT COUNT(*) FROM PhieuPhat", activeConnUser1, activeTranUser1);
                    int count = (int)cmd.ExecuteScalar();
                    LogUser1($"Lần 2: Tổng số phiếu phạt đã bị tăng lên = {count} (Bóng ma)!");
                    activeTranUser1?.Commit(); DisposeTransactions();
                    btnCount1.Enabled = true; btnCount2.Enabled = false;
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser1("Lỗi: " + ex.Message); }
            };
            pnlUser1Content.Controls.AddRange(new Control[] { btnCount1, btnCount2 });

            // User 2
            var btnInsert = new Button { Text = "2. Hệ thống tạo phiếu phạt mới (Insert PP999)", Location = new Point(10, 45), Width = 400, Height = 40, BackColor = Color.Magenta, ForeColor = Color.White };
            btnInsert.Click += (s, e) =>
            {
                try
                {
                    string sql = "INSERT INTO PhieuPhat (MaPhieuPhat, MaPhieuMuon, LyDoPhat, SoTienPhat, NgayLap, TrangThaiThanhToan) VALUES ('PP999', (SELECT TOP 1 MaPhieuMuon FROM PhieuMuon), 'TreHan', 50000, CAST(GETDATE() AS DATE), 'ChuaThanhToan')";
                    DatabaseHelper.ExecuteNonQuery(sql);
                    LogUser2("Đã phát sinh phiếu phạt bóng ma PP999 thành công!");
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser2("Lỗi: " + ex.Message); }
            };
            pnlUser2Content.Controls.Add(btnInsert);
        }

        // ==========================================
        // 5. DEMO DEADLOCK (BẾ TẮC TƯƠNG HỖ)
        // ==========================================
        private void BuildDeadlockUI()
        {
            txtExplanation.Text = "KỊCH BẢN DEADLOCK (BẾ TẮC TƯƠNG HỖ):\r\n" +
                "- Giao tác 1 (Thủ thư 1) khóa độc quyền CS001, sau đó yêu cầu khóa CS002.\r\n" +
                "- Giao tác 2 (Thủ thư 2) khóa độc quyền CS002, sau đó yêu cầu khóa CS001.\r\n" +
                "- Cả hai rơi vào chu trình chờ đợi lẫn nhau (Circular Wait - Deadlock).\r\n" +
                "- SQL Server Deadlock Monitor phát hiện chu trình, tự động chọn 1 giao tác làm Nạn nhân (Victim - Mã lỗi 1205) và Rollback để giao tác còn lại tiếp tục thành công.\r\n\r\n" +
                "💡 GIẢI PHÁP PHÒNG TRÁNH TRONG THỰC TẾ:\r\n" +
                "1. Chuẩn hóa thứ tự truy cập tài nguyên (Access Ordering): Luôn khóa theo thứ tự ID tăng dần (CS001 trước CS002).\r\n" +
                "2. Deadlock Retry Pattern: Bắt lỗi 1205 và tự động thực hiện lại giao dịch sau một khoảng thời gian chờ ngẫu nhiên.\r\n" +
                "3. SET DEADLOCK_PRIORITY: Đặt mức ưu tiên LOW cho các giao dịch nền/báo cáo.";

            // --- USER 1 (Thủ thư 1) ---
            var lblT1Title = new Label
            {
                Text = "📌 Nghiệp vụ T1: Cập nhật sách [CS001] -> [CS002]",
                AutoSize = true,
                Location = new Point(10, 10),
                ForeColor = Color.FromArgb(30, 58, 138),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            var btnT1_Step1 = new Button
            {
                Text = "1. Bắt đầu T1 & Khóa CS001 (X-Lock)",
                Location = new Point(10, 40),
                Width = 430,
                Height = 35,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnT1_Step3 = new Button
            {
                Text = "3. T1 yêu cầu Khóa tiếp CS002 (Bị chặn/Chờ)",
                Location = new Point(10, 85),
                Width = 430,
                Height = 35,
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            var btnT1_Commit = new Button
            {
                Text = "✅ T1 Commit",
                Location = new Point(10, 130),
                Width = 210,
                Height = 32,
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            var btnT1_Rollback = new Button
            {
                Text = "❌ T1 Rollback",
                Location = new Point(230, 130),
                Width = 210,
                Height = 32,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            // --- USER 2 (Thủ thư 2) ---
            var lblT2Title = new Label
            {
                Text = "📌 Nghiệp vụ T2: Cập nhật sách [CS002] -> [CS001]",
                AutoSize = true,
                Location = new Point(10, 10),
                ForeColor = Color.FromArgb(180, 83, 9),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            var btnT2_Step2 = new Button
            {
                Text = "2. Bắt đầu T2 & Khóa CS002 (X-Lock)",
                Location = new Point(10, 40),
                Width = 430,
                Height = 35,
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            var btnT2_Step4 = new Button
            {
                Text = "4. T2 yêu cầu Khóa tiếp CS001 (Kích nổ Deadlock)",
                Location = new Point(10, 85),
                Width = 430,
                Height = 35,
                BackColor = Color.FromArgb(190, 24, 93),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            var btnT2_Commit = new Button
            {
                Text = "✅ T2 Commit",
                Location = new Point(10, 130),
                Width = 210,
                Height = 32,
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            var btnT2_Rollback = new Button
            {
                Text = "❌ T2 Rollback",
                Location = new Point(230, 130),
                Width = 210,
                Height = 32,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };

            // --- SỰ KIỆN USER 1 ---
            btnT1_Step1.Click += (s, e) =>
            {
                try
                {
                    activeConnUser1 = DatabaseHelper.GetConnection();
                    activeConnUser1.Open();
                    activeTranUser1 = activeConnUser1.BeginTransaction();

                    using var cmd = new SqlCommand("UPDATE CuonSach SET TinhTrang = 'CapNhat_T1' WHERE MaCuonSach = 'CS001'", activeConnUser1, activeTranUser1);
                    cmd.ExecuteNonQuery();

                    LogUser1("Đã mở Giao tác 1 & Khóa độc quyền (X-Lock) cuốn CS001 thành công.");
                    btnT1_Step1.Enabled = false;
                    btnT1_Step3.Enabled = true;
                    btnT1_Commit.Enabled = true;
                    btnT1_Rollback.Enabled = true;
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser1("Lỗi T1: " + ex.Message); }
            };

            btnT1_Step3.Click += (s, e) =>
            {
                btnT1_Step3.Enabled = false;
                btnT1_Step3.Text = "⏳ Đang chờ CS002 giải phóng...";
                LogUser1("T1 yêu cầu khóa CS002 -> Đang bị SQL Server chặn chờ do T2 đang nắm giữ...");

                Task.Run(() =>
                {
                    try
                    {
                        if (activeConnUser1 != null && activeTranUser1 != null)
                        {
                            using var cmd = new SqlCommand("UPDATE CuonSach SET TinhTrang = 'HoanTat_T1' WHERE MaCuonSach = 'CS002'", activeConnUser1, activeTranUser1);
                            cmd.ExecuteNonQuery();

                            this.Invoke(() =>
                            {
                                LogUser1("🎉 T1 ĐÃ NHẬN KHÓA CS002 & THỰC THI THÀNH CÔNG!");
                                btnT1_Step3.Text = "✅ T1 đã khóa xong CS002";
                                RefreshDatabaseInspector();
                            });
                        }
                    }
                    catch (SqlException ex)
                    {
                        this.Invoke(() =>
                        {
                            if (ex.Number == 1205)
                            {
                                LogUser1("💥 DEADLOCK VICTIM (Lỗi 1205): SQL Server chọn T1 làm nạn nhân và tự động ROLLBACK!");
                                activeTranUser1 = null;
                                btnT1_Step3.Text = "❌ T1 bị hủy (Deadlock Victim)";
                                btnT1_Commit.Enabled = false;
                                btnT1_Rollback.Enabled = false;
                                btnT1_Step1.Enabled = true;
                            }
                            else
                            {
                                LogUser1("Lỗi T1: " + ex.Message);
                                btnT1_Step3.Text = "3. T1 yêu cầu Khóa tiếp CS002";
                                btnT1_Step3.Enabled = true;
                            }
                            RefreshDatabaseInspector();
                        });
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(() => LogUser1("Lỗi T1: " + ex.Message));
                    }
                });
            };

            btnT1_Commit.Click += (s, e) =>
            {
                try
                {
                    activeTranUser1?.Commit();
                    DisposeTransactions();
                    LogUser1("✅ T1 đã COMMIT thành công!");
                    btnT1_Step1.Enabled = true;
                    btnT1_Step3.Enabled = false;
                    btnT1_Step3.Text = "3. T1 yêu cầu Khóa tiếp CS002 (Bị chặn/Chờ)";
                    btnT1_Commit.Enabled = false;
                    btnT1_Rollback.Enabled = false;
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser1("Lỗi Commit T1: " + ex.Message); }
            };

            btnT1_Rollback.Click += (s, e) =>
            {
                try
                {
                    activeTranUser1?.Rollback();
                    DisposeTransactions();
                    LogUser1("❌ T1 đã ROLLBACK!");
                    btnT1_Step1.Enabled = true;
                    btnT1_Step3.Enabled = false;
                    btnT1_Step3.Text = "3. T1 yêu cầu Khóa tiếp CS002 (Bị chặn/Chờ)";
                    btnT1_Commit.Enabled = false;
                    btnT1_Rollback.Enabled = false;
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser1("Lỗi Rollback T1: " + ex.Message); }
            };

            pnlUser1Content.Controls.AddRange(new Control[] { lblT1Title, btnT1_Step1, btnT1_Step3, btnT1_Commit, btnT1_Rollback });

            // --- SỰ KIỆN USER 2 ---
            btnT2_Step2.Click += (s, e) =>
            {
                try
                {
                    activeConnUser2 = DatabaseHelper.GetConnection();
                    activeConnUser2.Open();
                    activeTranUser2 = activeConnUser2.BeginTransaction();

                    using var cmd = new SqlCommand("UPDATE CuonSach SET TinhTrang = 'CapNhat_T2' WHERE MaCuonSach = 'CS002'", activeConnUser2, activeTranUser2);
                    cmd.ExecuteNonQuery();

                    LogUser2("Đã mở Giao tác 2 & Khóa độc quyền (X-Lock) cuốn CS002 thành công.");
                    btnT2_Step2.Enabled = false;
                    btnT2_Step4.Enabled = true;
                    btnT2_Commit.Enabled = true;
                    btnT2_Rollback.Enabled = true;
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser2("Lỗi T2: " + ex.Message); }
            };

            btnT2_Step4.Click += (s, e) =>
            {
                btnT2_Step4.Enabled = false;
                btnT2_Step4.Text = "⏳ Đang chờ CS001 giải phóng...";
                LogUser2("T2 yêu cầu khóa CS001 -> Đụng độ chu trình Deadlock với T1!");

                Task.Run(() =>
                {
                    try
                    {
                        if (activeConnUser2 != null && activeTranUser2 != null)
                        {
                            using var cmd = new SqlCommand("UPDATE CuonSach SET TinhTrang = 'HoanTat_T2' WHERE MaCuonSach = 'CS001'", activeConnUser2, activeTranUser2);
                            cmd.ExecuteNonQuery();

                            this.Invoke(() =>
                            {
                                LogUser2("🎉 T2 ĐÃ NHẬN KHÓA CS001 & THỰC THI THÀNH CÔNG!");
                                btnT2_Step4.Text = "✅ T2 đã khóa xong CS001";
                                RefreshDatabaseInspector();
                            });
                        }
                    }
                    catch (SqlException ex)
                    {
                        this.Invoke(() =>
                        {
                            if (ex.Number == 1205)
                            {
                                LogUser2("💥 DEADLOCK VICTIM (Lỗi 1205): SQL Server chọn T2 làm nạn nhân và tự động ROLLBACK!");
                                activeTranUser2 = null;
                                btnT2_Step4.Text = "❌ T2 bị hủy (Deadlock Victim)";
                                btnT2_Commit.Enabled = false;
                                btnT2_Rollback.Enabled = false;
                                btnT2_Step2.Enabled = true;
                            }
                            else
                            {
                                LogUser2("Lỗi T2: " + ex.Message);
                                btnT2_Step4.Text = "4. T2 yêu cầu Khóa tiếp CS001 (Kích nổ Deadlock)";
                                btnT2_Step4.Enabled = true;
                            }
                            RefreshDatabaseInspector();
                        });
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(() => LogUser2("Lỗi T2: " + ex.Message));
                    }
                });
            };

            btnT2_Commit.Click += (s, e) =>
            {
                try
                {
                    activeTranUser2?.Commit();
                    DisposeTransactions();
                    LogUser2("✅ T2 đã COMMIT thành công!");
                    btnT2_Step2.Enabled = true;
                    btnT2_Step4.Enabled = false;
                    btnT2_Step4.Text = "4. T2 yêu cầu Khóa tiếp CS001 (Kích nổ Deadlock)";
                    btnT2_Commit.Enabled = false;
                    btnT2_Rollback.Enabled = false;
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser2("Lỗi Commit T2: " + ex.Message); }
            };

            btnT2_Rollback.Click += (s, e) =>
            {
                try
                {
                    activeTranUser2?.Rollback();
                    DisposeTransactions();
                    LogUser2("❌ T2 đã ROLLBACK!");
                    btnT2_Step2.Enabled = true;
                    btnT2_Step4.Enabled = false;
                    btnT2_Step4.Text = "4. T2 yêu cầu Khóa tiếp CS001 (Kích nổ Deadlock)";
                    btnT2_Commit.Enabled = false;
                    btnT2_Rollback.Enabled = false;
                    RefreshDatabaseInspector();
                }
                catch (Exception ex) { LogUser2("Lỗi Rollback T2: " + ex.Message); }
            };

            pnlUser2Content.Controls.AddRange(new Control[] { lblT2Title, btnT2_Step2, btnT2_Step4, btnT2_Commit, btnT2_Rollback });
        }
    }
}

