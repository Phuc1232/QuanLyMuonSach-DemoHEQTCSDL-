using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ProjectHEQTCSDL.Core;

namespace ProjectHEQTCSDL.FormUI
{
    public class FrmConcurrencyDemo : Form
    {
        // Controls
        private ComboBox cboScenario = null!;
        private Button btnLaunchWindows = null!;
        private Button btnResetData = null!;

        private TextBox txtExplanation = null!;
        private Label lblScenarioSummary = null!;

        // References to Active Sub-Windows
        private Form? activeWin1 = null;
        private Form? activeWin2 = null;

        public FrmConcurrencyDemo()
        {
            InitializeComponent();
            this.Load += (s, e) =>
            {
                cboScenario.SelectedIndex = 0;
            };
        }

        private void InitializeComponent()
        {
            this.Text = "TRUNG TÂM ĐIỀU KHIỂN & PHÂN TÍCH TƯƠNG TRANH CSDL";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.FormClosing += (s, e) => CloseActiveWindows();

            // 1. TOP HEADER BANNER
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblTitle = new Label
            {
                Text = "TRUNG TÂM ĐIỀU KHIỂN & PHÂN TÍCH TƯƠNG TRANH CSDL",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblSub = new Label
            {
                Text = "Mô hình 2 Cửa Sổ Nghiệp Vụ Độc Lập - Thực thi 100% Stored Procedures & Transaction trong SQL Server",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                Dock = DockStyle.Bottom,
                Height = 22
            };

            pnlHeader.Controls.Add(lblSub);
            pnlHeader.Controls.Add(lblTitle);
            this.Controls.Add(pnlHeader);

            // 2. TOOLBAR CONTROL PANEL
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(15, 12, 15, 12),
                AutoScroll = true
            };

            var lblSelect = new Label
            {
                Text = "Chọn Kịch Bản:",
                AutoSize = true,
                Location = new Point(15, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            cboScenario = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(135, 17),
                Width = 340,
                Font = new Font("Segoe UI", 10F)
            };
            cboScenario.Items.AddRange(new object[] {
                "1. Lost Update (Mất bản cập nhật)",
                "2. Dirty Read (Đọc dữ liệu rác)",
                "3. Non-Repeatable Read (Đọc không nhất quán)",
                "4. Phantom Read (Đọc dòng bóng ma)",
                "5. Deadlock (Bế tắc tương hỗ)"
            });
            cboScenario.SelectedIndexChanged += (s, e) => OnScenarioChanged();

            btnLaunchWindows = new Button
            {
                Text = "MỞ 2 CỬA SỔ NGHIỆP VỤ",
                Location = new Point(490, 14),
                Width = 240,
                Height = 36,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLaunchWindows.FlatAppearance.BorderSize = 0;
            btnLaunchWindows.Click += (s, e) => LaunchSubWindows();

            btnResetData = new Button
            {
                Text = "KHÔI PHỤC DỮ LIỆU GỐC (RESET)",
                Location = new Point(745, 14),
                Width = 250,
                Height = 36,
                BackColor = Color.FromArgb(13, 148, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResetData.FlatAppearance.BorderSize = 0;
            btnResetData.Click += async (s, e) =>
            {
                try
                {
                    btnResetData.Enabled = false;
                    await DatabaseHelper.ExecuteProcedureAsync("sp_ResetDuLieuDemoTuongTranh");
                    MessageBox.Show("Đã khôi phục dữ liệu demo về trạng thái chuẩn ban đầu thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi reset dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnResetData.Enabled = true;
                }
            };

            pnlToolbar.Controls.AddRange(new Control[] { lblSelect, cboScenario, btnLaunchWindows, btnResetData });
            this.Controls.Add(pnlToolbar);

            // 3. SCENARIO SUMMARY BANNER
            var pnlSummary = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(238, 242, 255),
                Padding = new Padding(20, 10, 20, 10)
            };

            lblScenarioSummary = new Label
            {
                Text = "Kịch bản: ...",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138)
            };
            pnlSummary.Controls.Add(lblScenarioSummary);
            this.Controls.Add(pnlSummary);

            // 4. MAIN WORKSPACE (Bảng Phân Tích & Cơ Chế Khóa CSDL Toàn Màn Hình)
            var pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            var grpExp = new GroupBox
            {
                Text = "PHÂN TÍCH HIỆN TƯỢNG VÀ CƠ CHẾ KHÓA TRONG CSDL (ACID & ISOLATION LEVELS)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9),
                Padding = new Padding(15)
            };

            txtExplanation = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(254, 252, 232),
                ForeColor = Color.FromArgb(67, 20, 7),
                Font = new Font("Segoe UI", 10.5F)
            };
            grpExp.Controls.Add(txtExplanation);
            pnlMain.Controls.Add(grpExp);

            this.Controls.Add(pnlMain);
            pnlMain.BringToFront();
        }

        private void CloseActiveWindows()
        {
            if (activeWin1 != null && !activeWin1.IsDisposed)
            {
                activeWin1.Close();
                activeWin1.Dispose();
                activeWin1 = null;
            }
            if (activeWin2 != null && !activeWin2.IsDisposed)
            {
                activeWin2.Close();
                activeWin2.Dispose();
                activeWin2 = null;
            }
        }

        private void OnScenarioChanged()
        {
            int idx = cboScenario.SelectedIndex;
            switch (idx)
            {
                case 0:
                    lblScenarioSummary.Text = "Kịch Bản 1: Lost Update (Mất bản cập nhật) - 2 Thủ thư cùng sửa tình trạng sách, người lưu sau ghi đè người lưu trước.";
                    txtExplanation.Text =
                        "=== 1. HIỆN TƯỢNG LOST UPDATE ===\r\n\r\n" +
                        "• Bản chất: Hai giao tác T1 và T2 cùng đọc 1 bản ghi với dữ liệu ban đầu. Sau đó cả hai cùng sửa và ghi lại vào CSDL. Giao tác ghi sau sẽ ghi đè lên kết quả của giao tác ghi trước mà không hay biết, làm mất cập nhật của giao tác trước.\r\n\r\n" +
                        "• Kịch bản mô phỏng:\r\n" +
                        "  - Ban đầu sách có TinhTrang = 'ConTot'.\r\n" +
                        "  - Thủ thư B phát hiện 'Mất đĩa CD kèm theo' và bấm Lưu vào CSDL.\r\n" +
                        "  - Thủ thư A chọn 'Rách bìa ngoài' và bấm Lưu sau.\r\n" +
                        "  - Kết quả: Tình trạng 'Mất đĩa CD' của Thủ thư B biến mất hoàn toàn, bị thay thế bằng 'Rách bìa ngoài' của Thủ thư A!\r\n\r\n" +
                        "• Cơ chế khóa & Giải pháp khắc phục:\r\n" +
                        "  - Nguyên nhân: Sử dụng giao tác thông thường không có cơ chế khóa lạc quan (Optimistic Concurrency) hoặc khóa bi quan (Pessimistic Locking).\r\n" +
                        "  - Giải pháp 1 (Optimistic Locking): Bổ sung cột Version hoặc RowVersion (Timestamp) vào bảng CuonSach. Khi Update kiểm tra WHERE Version = @OldVersion.\r\n" +
                        "  - Giải pháp 2 (Pessimistic Locking): Dùng SELECT ... WITH (UPDLOCK, ROWLOCK) khi đọc dữ liệu chuẩn bị sửa để ngăn giao tác khác cùng đọc để sửa.";
                    break;

                case 1:
                    lblScenarioSummary.Text = "Kịch Bản 2: Dirty Read (Đọc rác) - Độc giả tra cứu sách với READ UNCOMMITTED khi Thủ thư đang làm thủ tục trả chưa hoàn tất.";
                    txtExplanation.Text =
                        "=== 2. HIỆN TƯỢNG DIRTY READ (ĐỌC DỮ LIỆU RÁC) ===\r\n\r\n" +
                        "• Bản chất: Giao tác T1 sửa dữ liệu nhưng CHƯA COMMIT. Giao tác T2 đọc được dữ liệu này (do dùng mức cô lập READ UNCOMMITTED). Sau đó giao tác T1 bị ROLLBACK (hủy bỏ). Dữ liệu mà T2 vừa đọc trở thành dữ liệu rác không hề tồn tại trong CSDL.\r\n\r\n" +
                        "• Kịch bản mô phỏng:\r\n" +
                        "  - Ban đầu sách có TrangThai = 'DangMuon'.\r\n" +
                        "  - Thủ thư nhận trả sách: CSDL tạm đổi TrangThai = 'CoSan' và giữ mở giao tác 10 giây (chờ khách đóng tiền phạt).\r\n" +
                        "  - Trong 10s này, Độc giả tra cứu trực tuyến thấy sách 'CoSan' và hào hứng đến mượn.\r\n" +
                        "  - Hết 10s: Khách không đủ tiền phạt, Thủ thư Rollback -> Sách trở về 'DangMuon'.\r\n" +
                        "  - Kết quả: Trạng thái 'CoSan' mà Độc giả thấy là DỮ LIỆU RÁC!\r\n\r\n" +
                        "• Cơ chế khóa & Mức cô lập:\r\n" +
                        "  - Thủ thư nắm giữ Exclusive Lock (X-Lock) trên dòng CuonSach.\r\n" +
                        "  - Độc giả dùng mức READ UNCOMMITTED (hoặc gợi ý WITH (NOLOCK)) nên bỏ qua X-Lock và đọc thẳng dữ liệu chưa chốt.\r\n" +
                        "  - Khắc phục: Nâng mức cô lập tra cứu lên READ COMMITTED (mặc định) -> Câu lệnh đọc sẽ chờ X-Lock giải phóng mới được đọc.";
                    break;

                case 2:
                    lblScenarioSummary.Text = "Kịch Bản 3: Non-Repeatable Read (Đọc không nhất quán) - Quản lý kiểm kê kho đọc 2 lần trong 1 Transaction, bị Thủ thư xen ngang cho mượn.";
                    txtExplanation.Text =
                        "=== 3. HIỆN TƯỢNG NON-REPEATABLE READ ===\r\n\r\n" +
                        "• Bản chất: Trong cùng một giao tác T1, đọc cùng một dữ liệu 2 lần nhưng nhận về 2 kết quả khác nhau, do giao tác T2 xen vào giữa thực hiện UPDATE/DELETE và COMMIT.\r\n\r\n" +
                        "• Kịch bản mô phỏng:\r\n" +
                        "  - Quản lý chạy SP kiểm kê đầu sách (mức READ COMMITTED). Lần 1 đếm số cuốn 'CoSan'. CSDL giữ Transaction trong 10s.\r\n" +
                        "  - Trong 10s này, Thủ thư ở quầy cho mượn 1 cuốn -> Chuyển sang 'DangMuon' và Commit thành công.\r\n" +
                        "  - Quản lý đếm Lần 2 trong cùng Transaction -> Số lượng bị giảm đi 1 cuốn!\r\n\r\n" +
                        "• Cơ chế khóa & Khắc phục:\r\n" +
                        "  - READ COMMITTED chỉ giữ Shared Lock (S-Lock) trong lúc đọc lệnh, đọc xong lập tức nhả S-Lock, cho phép người khác UPDATE.\r\n" +
                        "  - Khắc phục: Nâng mức cô lập lên REPEATABLE READ hoặc SERIALIZABLE. Ở mức này, S-Lock được giữ cho đến khi toàn bộ Transaction kết thúc (COMMIT/ROLLBACK), chặn người khác UPDATE dòng đang đọc.";
                    break;

                case 3:
                    lblScenarioSummary.Text = "Kịch Bản 4: Phantom Read (Đọc dòng bóng ma) - Kế toán đếm tổng số phiếu phạt, Thủ thư xen ngang INSERT thêm phiếu phạt mới.";
                    txtExplanation.Text =
                        "=== 4. HIỆN TƯỢNG PHANTOM READ (ĐỌC BÓNG MA) ===\r\n\r\n" +
                        "• Bản chất: Giao tác T1 đọc một tập hợp các dòng thỏa mãn điều kiện WHERE. Giao tác T2 sau đó chèn thêm (INSERT) dòng mới thỏa điều kiện đó và COMMIT. Khi T1 đọc lại tập hợp đó trong cùng giao tác, xuất hiện thêm dòng mới chưa từng thấy (dòng Bóng Ma).\r\n\r\n" +
                        "• Kịch bản mô phỏng:\r\n" +
                        "  - Kế toán chạy SP tổng hợp phiếu phạt (mức REPEATABLE READ). Đếm Lần 1 và giữ giao tác trong 10s.\r\n" +
                        "  - Trong 10s, Thủ thư lập phiếu phạt mới (50,000 VNĐ) và Commit thành công.\r\n" +
                        "  - Kế toán đếm Lần 2 -> Xuất hiện thêm 1 bản ghi bóng ma mới!\r\n\r\n" +
                        "• Cơ chế khóa & Khắc phục:\r\n" +
                        "  - Mức REPEATABLE READ chỉ khóa các dòng hiện hữu (Row Locks), KHÔNG khóa khoảng trống (Gap / Key Range) giữa các dòng, nên không chặn được lệnh INSERT mới.\r\n" +
                        "  - Khắc phục: Nâng mức cô lập lên SERIALIZABLE. SQL Server sẽ dùng Key-Range Lock (RangeS-S) để khóa toàn bộ dải dữ liệu, ngăn chặn việc chèn bản ghi mới cho đến khi Transaction hoàn tất.";
                    break;

                case 4:
                    lblScenarioSummary.Text = "Kịch Bản 5: Deadlock (Bế tắc chu trình) - 2 giao tác mượn sách khóa chéo tài nguyên CS001 và CS004.";
                    txtExplanation.Text =
                        "=== 5. HIỆN TƯỢNG DEADLOCK (BẾ TẮC TƯƠNG HỖ) ===\r\n\r\n" +
                        "• Bản chất: Giao tác T1 nắm giữ khóa tài nguyên A và chờ khóa tài nguyên B do T2 nắm giữ. Đồng thời T2 nắm giữ tài nguyên B và chờ khóa tài nguyên A do T1 nắm giữ. Cả hai chờ đợi vô tận lẫn nhau.\r\n\r\n" +
                        "• Kịch bản mô phỏng:\r\n" +
                        "  - Quầy 1 (DG002): Quét CS001 -> Giữ UPDLOCK CS001 -> Chờ 5s -> Đòi khóa CS004.\r\n" +
                        "  - Quầy 2 (DG003): Quét CS004 -> Giữ UPDLOCK CS004 -> Chờ 5s -> Đòi khóa CS001.\r\n" +
                        "  - Cơ chế phát hiện của SQL Server: Deadlock Monitor định kỳ quét đồ thị chờ (Wait-For Graph). Khi phát hiện chu trình (Circular Wait), SQL Server tự động chọn 1 giao tác làm NẠN NHÂN (Deadlock Victim - Error 1205) và Rollback giao tác đó, cho phép giao tác còn lại tiếp tục thành công!\r\n\r\n" +
                        "• Giải pháp phòng tránh Deadlock:\r\n" +
                        "  1. Chuẩn hóa thứ tự truy xuất tài nguyên (Ordering Protocol: luôn khóa theo thứ tự MaCuonSach ASC trong mọi giao dịch).\r\n" +
                        "  2. Rút ngắn thời gian giao dịch, không để giao tác mở quá lâu.\r\n" +
                        "  3. Bắt lỗi 1205 ở tầng ứng dụng và thực hiện cơ chế Retry tự động.";
                    break;
            }
        }

        public void RefreshDatabaseInspector()
        {
            // Bảng giám sát bên ngoài đã được loại bỏ để tập trung vào dữ liệu trực tiếp trên 2 cửa sổ con.
        }

        private void LaunchSubWindows()
        {
            CloseActiveWindows();

            int scenario = cboScenario.SelectedIndex;
            FrmConcurrencyBase win1;
            FrmConcurrencyBase win2;

            switch (scenario)
            {
                case 0: // Lost Update
                    win1 = new FrmLostUpdateWin1();
                    win2 = new FrmLostUpdateWin2();
                    break;
                case 1: // Dirty Read
                    win1 = new FrmDirtyReadWin1();
                    win2 = new FrmDirtyReadWin2();
                    break;
                case 2: // Non-Repeatable Read
                    win1 = new FrmNonRepeatableWin1();
                    win2 = new FrmNonRepeatableWin2();
                    break;
                case 3: // Phantom Read
                    win1 = new FrmPhantomWin1();
                    win2 = new FrmPhantomWin2();
                    break;
                case 4: // Deadlock
                    win1 = new FrmDeadlockWin1();
                    win2 = new FrmDeadlockWin2();
                    break;
                default:
                    return;
            }

            // Đồng bộ trực tiếp giữa 2 cửa sổ con khi dữ liệu thay đổi
            win1.OnDataChanged = () =>
            {
                if (win2 != null && !win2.IsDisposed)
                {
                    try { win2.Invoke(new Action(() => win2.RefreshData())); } catch { }
                }
            };
            win2.OnDataChanged = () =>
            {
                if (win1 != null && !win1.IsDisposed)
                {
                    try { win1.Invoke(new Action(() => win1.RefreshData())); } catch { }
                }
            };

            // Position side by side on screen
            var screenArea = Screen.FromControl(this).WorkingArea;
            int winWidth = (screenArea.Width / 2) - 15;
            int winHeight = Math.Min(screenArea.Height - 40, 740);

            win1.StartPosition = FormStartPosition.Manual;
            win1.Location = new Point(screenArea.X + 10, screenArea.Y + 20);
            win1.Size = new Size(winWidth, winHeight);

            win2.StartPosition = FormStartPosition.Manual;
            win2.Location = new Point(screenArea.X + (screenArea.Width / 2) + 5, screenArea.Y + 20);
            win2.Size = new Size(winWidth, winHeight);

            activeWin1 = win1;
            activeWin2 = win2;

            win1.Show();
            win2.Show();
        }
    }
}
