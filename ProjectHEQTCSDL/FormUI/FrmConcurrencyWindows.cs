using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using ProjectHEQTCSDL.Core;

namespace ProjectHEQTCSDL.FormUI
{
    public enum AlertType
    {
        Info,
        Success,
        Warning,
        Danger
    }

    // =========================================================================
    // BASE FORM CHO CÁC CỬA SỔ DEMO CON (GIAO DIỆN NGHIỆP VỤ THỰC TẾ)
    // =========================================================================
    public class FrmConcurrencyBase : Form
    {
        protected Panel pnlHeader = null!;
        protected Label lblHeaderTitle = null!;
        protected Label lblHeaderSub = null!;
        protected Panel pnlBody = null!;
        
        // Status Alert Card (Thay thế ô log đen)
        protected Panel pnlStatusAlert = null!;
        protected Label lblAlertIcon = null!;
        protected Label lblAlertTitle = null!;
        protected Label lblAlertDetail = null!;
        protected ProgressBar prgTimer = null!;
        protected Label lblTimerStatus = null!;
        protected System.Windows.Forms.Timer? countdownTimer = null;
        protected int remainingSeconds = 0;
        private Color currentBorderColor = Color.FromArgb(226, 232, 240);

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action? OnDataChanged { get; set; }

        public FrmConcurrencyBase(string roleTitle, string scenarioSubtitle, Color themeColor)
        {
            this.Size = new Size(580, 650);
            this.StartPosition = FormStartPosition.Manual;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(248, 250, 252);

            // 1. Header Panel
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = themeColor,
                Padding = new Padding(15, 10, 15, 10)
            };

            lblHeaderTitle = new Label
            {
                Text = roleTitle,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 30
            };

            lblHeaderSub = new Label
            {
                Text = scenarioSubtitle,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(241, 245, 249),
                Dock = DockStyle.Bottom,
                Height = 25
            };

            pnlHeader.Controls.Add(lblHeaderSub);
            pnlHeader.Controls.Add(lblHeaderTitle);

            // 2. Status Alert Panel at Bottom (Modern Card)
            var pnlBottomContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 115,
                Padding = new Padding(15, 5, 15, 10),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            pnlStatusAlert = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(12)
            };
            pnlStatusAlert.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlStatusAlert.ClientRectangle, currentBorderColor, ButtonBorderStyle.Solid);
            };

            lblAlertIcon = new Label
            {
                Text = "[i]",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(10, 10),
                Size = new Size(40, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblAlertTitle = new Label
            {
                Text = "Sẵn sàng thực hiện nghiệp vụ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(55, 10),
                Size = new Size(480, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblAlertDetail = new Label
            {
                Text = "Vui lòng chọn thao tác nghiệp vụ ở phía trên để bắt đầu giao tác.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(55, 30),
                Size = new Size(480, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblTimerStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(55, 64),
                Size = new Size(480, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = false,
                Visible = false
            };

            prgTimer = new ProgressBar
            {
                Location = new Point(55, 82),
                Size = new Size(475, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Style = ProgressBarStyle.Continuous,
                Value = 0,
                Visible = false
            };

            pnlStatusAlert.Controls.AddRange(new Control[] { lblAlertIcon, lblAlertTitle, lblAlertDetail, lblTimerStatus, prgTimer });
            pnlBottomContainer.Controls.Add(pnlStatusAlert);

            // 3. Main Body Panel
            pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 8),
                AutoScroll = false
            };

            // BẮT BUỘC trong WinForms: Thêm pnlBody (DockStyle.Fill) ĐẦU TIÊN vào Controls collection,
            // sau đó mới thêm pnlBottomContainer và pnlHeader.
            // Điều này đảm bảo pnlHeader và pnlBottomContainer chiếm chỗ trước, và pnlBody nhận phần diện tích
            // khả dụng ở giữa (bắt đầu từ Y = 75), KHÔNG bao giờ bị pnlHeader đè khuất phần trên!
            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlBottomContainer);
            this.Controls.Add(pnlHeader);
        }

        public void SetAlert(string title, string detail, AlertType type)
        {
            if (this.IsDisposed) return;
            if (pnlStatusAlert.InvokeRequired)
            {
                pnlStatusAlert.Invoke(new Action(() => SetAlert(title, detail, type)));
                return;
            }

            switch (type)
            {
                case AlertType.Success:
                    pnlStatusAlert.BackColor = Color.FromArgb(236, 253, 245);
                    currentBorderColor = Color.FromArgb(167, 243, 208);
                    lblAlertIcon.Text = "[OK]";
                    lblAlertTitle.ForeColor = Color.FromArgb(6, 95, 70);
                    lblAlertDetail.ForeColor = Color.FromArgb(4, 120, 87);
                    break;

                case AlertType.Warning:
                    pnlStatusAlert.BackColor = Color.FromArgb(254, 252, 232);
                    currentBorderColor = Color.FromArgb(253, 224, 71);
                    lblAlertIcon.Text = "[!]";
                    lblAlertTitle.ForeColor = Color.FromArgb(133, 77, 14);
                    lblAlertDetail.ForeColor = Color.FromArgb(161, 98, 7);
                    break;

                case AlertType.Danger:
                    pnlStatusAlert.BackColor = Color.FromArgb(254, 242, 242);
                    currentBorderColor = Color.FromArgb(254, 202, 202);
                    lblAlertIcon.Text = "[X]";
                    lblAlertTitle.ForeColor = Color.FromArgb(153, 27, 27);
                    lblAlertDetail.ForeColor = Color.FromArgb(185, 28, 28);
                    break;

                default: // Info
                    pnlStatusAlert.BackColor = Color.FromArgb(239, 246, 255);
                    currentBorderColor = Color.FromArgb(191, 219, 254);
                    lblAlertIcon.Text = "[i]";
                    lblAlertTitle.ForeColor = Color.FromArgb(30, 64, 175);
                    lblAlertDetail.ForeColor = Color.FromArgb(29, 78, 216);
                    break;
            }

            lblAlertTitle.Text = title;
            lblAlertDetail.Text = detail;
            pnlStatusAlert.Invalidate();
        }

        protected void StartCountdown(int totalSeconds, string actionDescription)
        {
            remainingSeconds = totalSeconds;
            prgTimer.Maximum = totalSeconds * 10;
            prgTimer.Value = totalSeconds * 10;
            prgTimer.Visible = true;
            lblTimerStatus.Visible = true;
            lblTimerStatus.Text = $"[Còn {remainingSeconds}s] {actionDescription}...";

            SetAlert("Đang Xử Lý Giao Dịch CSDL", $"Hệ thống đang mở phiên làm việc ({totalSeconds} giây) trên SQL Server...", AlertType.Warning);

            countdownTimer?.Stop();
            countdownTimer?.Dispose();
            countdownTimer = new System.Windows.Forms.Timer { Interval = 100 };
            int ticksLeft = totalSeconds * 10;

            countdownTimer.Tick += (s, e) =>
            {
                ticksLeft--;
                if (ticksLeft >= 0 && !prgTimer.IsDisposed)
                {
                    prgTimer.Value = ticksLeft;
                    if (ticksLeft % 10 == 0)
                    {
                        remainingSeconds = ticksLeft / 10;
                        lblTimerStatus.Text = $"[Còn {remainingSeconds}s] {actionDescription}...";
                    }
                }
                else
                {
                    countdownTimer.Stop();
                    prgTimer.Visible = false;
                    lblTimerStatus.Visible = false;
                }
            };
            countdownTimer.Start();
        }

        protected void StopCountdown()
        {
            countdownTimer?.Stop();
            prgTimer.Visible = false;
            lblTimerStatus.Visible = false;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            countdownTimer?.Stop();
            countdownTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }


    // =========================================================================
    // 1. KỊCH BẢN 1: LOST UPDATE (MẤT BẢN CẬP NHẬT)
    // =========================================================================
    public class FrmLostUpdateWin1 : FrmConcurrencyBase
    {
        private ComboBox cboTinhTrang = null!;
        private Button btnSave = null!;
        private Label lblCurrentStatus = null!;

        public FrmLostUpdateWin1() : base(
            "[Quầy 1] Thủ Thư A",
            "Cập nhật tình trạng vật lý cuốn sách CS001",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "[Quầy 1] Thủ Thư A - Cập Nhật Tình Trạng Sách CS001";
            BuildUI();
            LoadCurrentBookStatus();
        }

        private void BuildUI()
        {
            // Book Details Form
            var grpBook = new GroupBox
            {
                Text = "Thông Tin Sách Đang Xử Lý",
                Dock = DockStyle.Top,
                Height = 150,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            var lblBookInfo = new Label
            {
                Text = "• Mã cuốn sách: CS001\n• Tựa sách: Lập trình C# Cơ bản từ Zero đến Hero\n• Mã đầu sách: S001 | Vị trí kệ: K01",
                Location = new Point(20, 28),
                Size = new Size(490, 55),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            lblCurrentStatus = new Label
            {
                Text = "Trạng thái trong CSDL: Đang tải...",
                Location = new Point(20, 90),
                Size = new Size(490, 30),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(2, 132, 199)
            };

            grpBook.Controls.AddRange(new Control[] { lblBookInfo, lblCurrentStatus });
            pnlBody.Controls.Add(grpBook);

            // Edit Action Box
            var grpEdit = new GroupBox
            {
                Text = "Cập Nhật Tình Trạng Cuốn Sách",
                Dock = DockStyle.Top,
                Height = 160,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            var lblSelect = new Label
            {
                Text = "Chọn tình trạng ghi nhận:",
                Location = new Point(20, 30),
                Size = new Size(200, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            cboTinhTrang = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 55),
                Size = new Size(490, 30),
                Font = new Font("Segoe UI", 10F)
            };
            cboTinhTrang.Items.AddRange(new object[] {
                "Rách bìa ngoài",
                "Ố vàng trang cuối",
                "Mất trang phụ lục",
                "ConTot"
            });
            cboTinhTrang.SelectedIndex = 0;

            btnSave = new Button
            {
                Text = "LƯU TÌNH TRẠNG VÀO CSDL",
                Location = new Point(20, 98),
                Size = new Size(490, 42),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (s, e) => await SaveBookStatus();

            grpEdit.Controls.AddRange(new Control[] { lblSelect, cboTinhTrang, btnSave });
            pnlBody.Controls.Add(grpEdit);

            grpEdit.BringToFront();
            grpBook.BringToFront();
        }

        public void LoadCurrentBookStatus()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteQuery("SELECT MaCuonSach, TinhTrang, TrangThai FROM CuonSach WITH (NOLOCK) WHERE MaCuonSach = 'CS001'");
                if (dt.Rows.Count > 0)
                {
                    string tt = dt.Rows[0]["TinhTrang"].ToString() ?? "";
                    string st = dt.Rows[0]["TrangThai"].ToString() ?? "";
                    lblCurrentStatus.Text = $"Trạng thái trong CSDL hiện tại: Tình trạng='{tt}', Trạng thái='{st}'";
                }
            }
            catch (Exception ex)
            {
                SetAlert("Lỗi Đọc Dữ Liệu", ex.Message, AlertType.Danger);
            }
        }

        private async Task SaveBookStatus()
        {
            btnSave.Enabled = false;
            string newStatus = cboTinhTrang.SelectedItem?.ToString() ?? "Rách bìa ngoài";

            StartCountdown(10, "Đang gửi giao tác cập nhật CS001");
            SetAlert("Đang Thực Hiện Giao Tác", $"[{DateTime.Now:HH:mm:ss}] Thủ thư A đang gửi cập nhật '{newStatus}' vào CSDL...", AlertType.Warning);

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaCuonSach", "CS001"),
                    new SqlParameter("@p_TinhTrang", newStatus)
                };

                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_CapNhatTinhTrangCuonSach", pars);
                StopCountdown();
                string msg = "Cập nhật thành công!";
                if (dt.Rows.Count > 0)
                {
                    if (dt.Columns.Contains("ThongBao"))
                        msg = dt.Rows[0]["ThongBao"]?.ToString() ?? msg;
                    else if (dt.Columns.Count > 0)
                        msg = dt.Rows[0][0]?.ToString() ?? msg;
                }
                
                LoadCurrentBookStatus();
                SetAlert("Cập Nhật Thành Công", $"[{DateTime.Now:HH:mm:ss}] Thủ thư A đã lưu thành công: '{newStatus}' vào CSDL.", AlertType.Success);
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Cập Nhật", $"[{DateTime.Now:HH:mm:ss}] Lỗi: {ex.Message}", AlertType.Danger);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }
    }


    public class FrmLostUpdateWin2 : FrmConcurrencyBase
    {
        private ComboBox cboTinhTrang = null!;
        private Button btnSave = null!;
        private Label lblCurrentStatus = null!;

        public FrmLostUpdateWin2() : base(
            "[Quầy 2] Thủ Thư B",
            "Cập nhật tình trạng vật lý cuốn sách CS001",
            Color.FromArgb(180, 83, 9))
        {
            this.Text = "[Quầy 2] Thủ Thư B - Cập Nhật Tình Trạng Sách CS001";
            BuildUI();
            LoadCurrentBookStatus();
        }

        private void BuildUI()
        {
            var grpBook = new GroupBox
            {
                Text = "Thông Tin Sách Đang Xử Lý",
                Dock = DockStyle.Top,
                Height = 150,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            var lblBookInfo = new Label
            {
                Text = "• Mã cuốn sách: CS001\n• Tựa sách: Lập trình C# Cơ bản từ Zero đến Hero\n• Mã đầu sách: S001 | Vị trí kệ: K01",
                Location = new Point(20, 28),
                Size = new Size(490, 55),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            lblCurrentStatus = new Label
            {
                Text = "Trạng thái trong CSDL: Đang tải...",
                Location = new Point(20, 90),
                Size = new Size(490, 30),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6)
            };

            grpBook.Controls.AddRange(new Control[] { lblBookInfo, lblCurrentStatus });
            pnlBody.Controls.Add(grpBook);

            var grpEdit = new GroupBox
            {
                Text = "Cập Nhật Tình Trạng Cuốn Sách",
                Dock = DockStyle.Top,
                Height = 160,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            var lblSelect = new Label
            {
                Text = "Chọn tình trạng ghi nhận:",
                Location = new Point(20, 30),
                Size = new Size(200, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            cboTinhTrang = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, 55),
                Size = new Size(490, 30),
                Font = new Font("Segoe UI", 10F)
            };
            cboTinhTrang.Items.AddRange(new object[] {
                "Mất đĩa CD kèm theo",
                "Gãy gáy sách",
                "Bị ướt góc trang",
                "ConTot"
            });
            cboTinhTrang.SelectedIndex = 0;

            btnSave = new Button
            {
                Text = "LƯU TÌNH TRẠNG VÀO CSDL",
                Location = new Point(20, 98),
                Size = new Size(490, 42),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (s, e) => await SaveBookStatus();

            grpEdit.Controls.AddRange(new Control[] { lblSelect, cboTinhTrang, btnSave });
            pnlBody.Controls.Add(grpEdit);

            grpEdit.BringToFront();
            grpBook.BringToFront();
        }

        public void LoadCurrentBookStatus()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteQuery("SELECT MaCuonSach, TinhTrang, TrangThai FROM CuonSach WITH (NOLOCK) WHERE MaCuonSach = 'CS001'");
                if (dt.Rows.Count > 0)
                {
                    string tt = dt.Rows[0]["TinhTrang"].ToString() ?? "";
                    string st = dt.Rows[0]["TrangThai"].ToString() ?? "";
                    lblCurrentStatus.Text = $"Trạng thái trong CSDL hiện tại: Tình trạng='{tt}', Trạng thái='{st}'";
                }
            }
            catch (Exception ex)
            {
                SetAlert("Lỗi Đọc Dữ Liệu", ex.Message, AlertType.Danger);
            }
        }

        private async Task SaveBookStatus()
        {
            btnSave.Enabled = false;
            string newStatus = cboTinhTrang.SelectedItem?.ToString() ?? "Mất đĩa CD kèm theo";

            StartCountdown(10, "Đang gửi giao tác cập nhật CS001");
            SetAlert("Đang Thực Hiện Giao Tác", $"[{DateTime.Now:HH:mm:ss}] Thủ thư B đang gửi cập nhật '{newStatus}' vào CSDL...", AlertType.Warning);

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaCuonSach", "CS001"),
                    new SqlParameter("@p_TinhTrang", newStatus)
                };

                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_CapNhatTinhTrangCuonSach", pars);
                StopCountdown();
                string msg = "Cập nhật thành công!";
                if (dt.Rows.Count > 0)
                {
                    if (dt.Columns.Contains("ThongBao"))
                        msg = dt.Rows[0]["ThongBao"]?.ToString() ?? msg;
                    else if (dt.Columns.Count > 0)
                        msg = dt.Rows[0][0]?.ToString() ?? msg;
                }
                
                LoadCurrentBookStatus();
                SetAlert("Cập Nhật Thành Công", $"[{DateTime.Now:HH:mm:ss}] Thủ thư B ĐÃ LƯU THÀNH CÔNG: '{newStatus}' vào CSDL.", AlertType.Success);
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Cập Nhật", $"[{DateTime.Now:HH:mm:ss}] Lỗi: {ex.Message}", AlertType.Danger);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }
    }


    // =========================================================================
    // 2. KỊCH BẢN 2: DIRTY READ (ĐỌC DỮ LIỆU RÁC)
    // =========================================================================
    public class FrmDirtyReadWin1 : FrmConcurrencyBase
    {
        private Button btnTraSach = null!;
        private RadioButton radRollback = null!;
        private RadioButton radCommit = null!;

        public FrmDirtyReadWin1() : base(
            "[Quầy 1] Thủ Thư Tiếp Nhận Trả Sách",
            "Tiếp nhận trả sách CS001 và xử lý vi phạm",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "[Quầy 1] Thủ Thư - Tiếp Nhận Trả Sách";
            BuildUI();
        }

        private void BuildUI()
        {
            var grpBook = new GroupBox
            {
                Text = "Hồ Sơ Mượn Sách Cần Trả",
                Dock = DockStyle.Top,
                Height = 130,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            var lblBookInfo = new Label
            {
                Text = "• Mã cuốn sách: CS001 (Lập trình C# Cơ bản)\n• Độc giả: DG001 - Nguyễn Xuân Sang\n• Tình trạng mượn: Quá hạn 3 ngày (Phạt 15,000 VNĐ)\n• Trạng thái ban đầu: DangMuon",
                Location = new Point(20, 28),
                Size = new Size(490, 85),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            grpBook.Controls.Add(lblBookInfo);

            var grpAction = new GroupBox
            {
                Text = "Quy Trình Tiếp Nhận & Xử Lý Tiền Phạt",
                Dock = DockStyle.Top,
                Height = 180,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            radRollback = new RadioButton
            {
                Text = "Khách không đủ tiền phạt -> Hủy bỏ trả sách (ROLLBACK sau 10s)",
                Location = new Point(20, 28),
                Size = new Size(490, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Checked = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 38, 38)
            };

            radCommit = new RadioButton
            {
                Text = "Khách nộp đủ tiền phạt -> Hoàn tất trả sách (COMMIT sau 10s)",
                Location = new Point(20, 55),
                Size = new Size(490, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(22, 101, 52)
            };

            btnTraSach = new Button
            {
                Text = "XÁC NHẬN TIẾP NHẬN TRẢ SÁCH",
                Location = new Point(20, 95),
                Size = new Size(490, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTraSach.FlatAppearance.BorderSize = 0;
            btnTraSach.Click += async (s, e) => await ExecuteTraSach();

            grpAction.Controls.AddRange(new Control[] { radRollback, radCommit, btnTraSach });

            // Thứ tự add: grpAction trước, grpBook sau để grpBook ở trên đỉnh (Dock = Top)
            pnlBody.Controls.Add(grpAction);
            pnlBody.Controls.Add(grpBook);
        }

        private async Task ExecuteTraSach()
        {
            btnTraSach.Enabled = false;
            int coLoiHoacHuy = radRollback.Checked ? 1 : 0;
            string outcome = coLoiHoacHuy == 1 ? "Khách thiếu tiền phạt (Sẽ Rollback)" : "Khách nộp đủ tiền (Sẽ Commit)";

            StartCountdown(10, "Đang mở giao dịch trả sách");
            SetAlert("Đang Mở Giao Tác Trả Sách", $"CSDL tạm chuyển CS001 sang 'CoSan'. Đang chờ khách nộp phạt (10 giây)...", AlertType.Warning);

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaCuonSach", "CS001"),
                    new SqlParameter("@p_CoLoiHoacHuy", coLoiHoacHuy)
                };

                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_GiaoTacTraSachThuNghiem", pars);
                StopCountdown();

                string msg = dt.Rows.Count > 0 ? (dt.Rows[0]["ThongBao"]?.ToString() ?? "") : "";
                
                if (coLoiHoacHuy == 1)
                {
                    SetAlert("Giao Tác Đã Rollback", $"Khách không đóng tiền phạt -> Đã Rollback giao dịch. Trạng thái sách CS001 vẫn là 'DangMuon'.", AlertType.Danger);
                    MessageBox.Show("Khách hàng không đủ tiền nộp phạt!\n\nHệ thống đã thực hiện ROLLBACK giao dịch.\nTrạng thái sách CS001 được khôi phục về 'DangMuon'.", "Hủy Giao Dịch Trả Sách (Rollback)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    SetAlert("Trả Sách Thành Công", "Khách nộp đủ tiền phạt -> Đã COMMIT giao dịch. CS001 chuyển sang 'CoSan'.", AlertType.Success);
                    MessageBox.Show("Khách hàng nộp phạt thành công!\n\nĐã COMMIT giao dịch hoàn tất.\nSách CS001 chính thức có sẵn trên kệ (CoSan).", "Hoàn Tất Trả Sách (Commit)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Giao Tác", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi giao tác: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTraSach.Enabled = true;
            }
        }
    }


    public class FrmDirtyReadWin2 : FrmConcurrencyBase
    {
        private TextBox txtSearch = null!;
        private ComboBox cboDanhMuc = null!;
        private Button btnTraCuu = null!;
        private Button btnMuonSach = null!;
        private DataGridView dgvResult = null!;
        
        // View Inspection Controls
        private RadioButton radViewBook = null!;
        private RadioButton radViewReader = null!;
        private Button btnRefreshView = null!;
        private DataGridView dgvView = null!;

        public FrmDirtyReadWin2() : base(
            "🌐 Cổng Tra Cứu OPAC & Đăng Ký Mượn Sách (Độc Giả)",
            "Độc giả: Trần Minh Tuấn (Mã thẻ: DG002 - Sinh viên) | Tra cứu trực tuyến & Mượn sách",
            Color.FromArgb(13, 148, 136))
        {
            this.Text = "🌐 Cổng Tra Cứu Trực Tuyến OPAC - Độc Giả Trần Minh Tuấn (DG002)";
            BuildUI();
            _ = ExecuteTraCuu();
            LoadViewData();
        }

        private void BuildUI()
        {
            // Master TableLayoutPanel cho pnlBody: Dòng 0 (Tra cứu & Đăng ký: 118px), Dòng 1 (Khu vực 2 lưới dữ liệu: 100%)
            var tableBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            tableBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tableBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 118f));
            tableBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // 1. Hộp Tra Cứu Sách Trực Tuyến
            var grpSearch = new GroupBox
            {
                Text = "Tra Cứu Sách Trực Tuyến (Cổng OPAC)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(12, 22, 12, 8)
            };

            var tableSearch = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tableSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75f));
            tableSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tableSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tableSearch.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105f));

            tableSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            tableSearch.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));

            var lblSearch = new Label 
            { 
                Text = "Mã sách:", 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleLeft, 
                Font = new Font("Segoe UI", 9F) 
            };
            txtSearch = new TextBox 
            { 
                Text = "CS001", 
                Dock = DockStyle.Fill, 
                Font = new Font("Segoe UI", 10F) 
            };
            cboDanhMuc = new ComboBox 
            { 
                Dock = DockStyle.Fill, 
                Font = new Font("Segoe UI", 9.5F), 
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            cboDanhMuc.Items.AddRange(new object[] { "Mã Cuốn Sách", "Tên Sách", "Tác Giả" });
            cboDanhMuc.SelectedIndex = 0;

            btnTraCuu = new Button
            {
                Text = "TÌM KIẾM",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTraCuu.FlatAppearance.BorderSize = 0;
            btnTraCuu.Click += async (s, e) => await ExecuteTraCuu();

            btnMuonSach = new Button
            {
                Text = "ĐĂNG KÝ MƯỢN CUỐN NÀY NGAY",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 0, 0)
            };
            btnMuonSach.FlatAppearance.BorderSize = 0;
            btnMuonSach.Click += async (s, e) => await ExecuteMuonSach();

            tableSearch.Controls.Add(lblSearch, 0, 0);
            tableSearch.Controls.Add(txtSearch, 1, 0);
            tableSearch.Controls.Add(cboDanhMuc, 2, 0);
            tableSearch.Controls.Add(btnTraCuu, 3, 0);
            tableSearch.Controls.Add(btnMuonSach, 0, 1);
            tableSearch.SetColumnSpan(btnMuonSach, 4);

            grpSearch.Controls.Add(tableSearch);
            tableBody.Controls.Add(grpSearch, 0, 0);

            // 2. Khu Vực Hiển Thị 2 Bảng Dữ Liệu (Tỷ lệ 48% / 52%)
            var tableGrids = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tableGrids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tableGrids.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));
            tableGrids.RowStyles.Add(new RowStyle(SizeType.Percent, 52f));

            // Bảng 1: Kết quả tra cứu sách
            var grpResult = new GroupBox
            {
                Text = "Kết Quả Tra Cứu Sách",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10, 20, 10, 8)
            };
            dgvResult = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F)
            };
            grpResult.Controls.Add(dgvResult);
            tableGrids.Controls.Add(grpResult, 0, 0);

            // Bảng 2: Sổ mượn cá nhân & tình trạng bản sao trong CSDL
            var grpView = new GroupBox
            {
                Text = "Sổ Mượn Cá Nhân (DG002) và Tình Trạng Bản Sao",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10, 20, 10, 8)
            };

            var pnlViewControls = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            pnlViewControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            pnlViewControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            pnlViewControls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95f));
            pnlViewControls.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            radViewBook = new RadioButton 
            { 
                Text = "Ai đang giữ CS001?", 
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F), 
                Checked = true
            };
            radViewReader = new RadioButton 
            { 
                Text = "Sổ mượn cá nhân (DG002)", 
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F)
            };
            radViewBook.CheckedChanged += (s, e) => { if (radViewBook.Checked) LoadViewData(); };
            radViewReader.CheckedChanged += (s, e) => { if (radViewReader.Checked) LoadViewData(); };

            btnRefreshView = new Button
            {
                Text = "LÀM MỚI",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefreshView.FlatAppearance.BorderSize = 0;
            btnRefreshView.Click += (s, e) => LoadViewData();

            pnlViewControls.Controls.Add(radViewBook, 0, 0);
            pnlViewControls.Controls.Add(radViewReader, 1, 0);
            pnlViewControls.Controls.Add(btnRefreshView, 2, 0);

            dgvView = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.FromArgb(248, 250, 252),
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F)
            };

            grpView.Controls.Add(dgvView);
            grpView.Controls.Add(pnlViewControls);
            pnlViewControls.BringToFront();

            tableGrids.Controls.Add(grpView, 0, 1);
            tableBody.Controls.Add(tableGrids, 0, 1);

            pnlBody.Controls.Add(tableBody);
        }

        private async Task ExecuteTraCuu()
        {
            string maCS = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(maCS)) maCS = "CS001";

            try
            {
                var pars = new SqlParameter[] { new SqlParameter("@p_MaCuonSach", maCS) };
                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_TraCuuSach_DirtyRead", pars);
                dgvResult.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tra cứu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadViewData()
        {
            try
            {
                string query = "";
                if (radViewBook.Checked)
                {
                    query = @"
                        SELECT c.MaCuonSach, c.TrangThai, 
                               ISNULL(dg.HoTen, ISNULL(v.MaDG, N'Chưa có người mượn')) AS NguoiDangGiua,
                               ISNULL(v.MaPhieuMuon, '') AS PhieuMuon
                        FROM CuonSach c WITH (NOLOCK)
                        LEFT JOIN View_LichSuMuon_DocGia v WITH (NOLOCK) ON c.MaCuonSach = v.MaCuonSach AND v.TrangThai = 'DangMuon'
                        LEFT JOIN DocGia dg WITH (NOLOCK) ON v.MaDG = dg.MaDG
                        WHERE c.MaCuonSach = 'CS001'";
                }
                else
                {
                    query = @"
                        SELECT v.MaPhieuMuon, v.MaCuonSach, v.TenSach, v.NgayMuon, v.NgayHenTra
                        FROM View_LichSuMuon_DocGia v WITH (NOLOCK)
                        WHERE v.MaDG = 'DG002' AND v.TrangThai = 'DangMuon'";
                }

                var dt = DatabaseHelper.ExecuteQuery(query);
                dgvView.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải view: " + ex.Message, "Lỗi CSDL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ExecuteMuonSach()
        {
            string maCS = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(maCS)) return;

            btnMuonSach.Enabled = false;
            btnTraCuu.Enabled = false;
            StartCountdown(10, "Đang gửi yêu cầu mượn lên CSDL - Chờ CSDL kiểm tra khóa tài nguyên...");

            try
            {
                string maPM = "PM" + DateTime.Now.ToString("HHmmss");
                string maDG = "DG002"; // Trần Minh Tuấn

                var dtBooks = new DataTable();
                dtBooks.Columns.Add("MaCuonSach", typeof(string));
                dtBooks.Rows.Add(maCS);

                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaPhieuMuon", maPM),
                    new SqlParameter("@p_MaDG", maDG),
                    new SqlParameter("@p_MaNV", "NV001"),
                    new SqlParameter("@p_NgayHenTra", DateTime.Now.AddDays(14)),
                    new SqlParameter("@p_DanhSachCuonSach", SqlDbType.Structured)
                    {
                        TypeName = "dbo.DanhSachCuonSachType",
                        Value = dtBooks
                    }
                };

                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_LapPhieuMuon", pars);
                StopCountdown();

                int isSuccess = 0;
                string msg = "Không có phản hồi từ máy chủ!";
                if (dt != null && dt.Rows.Count > 0)
                {
                    msg = dt.Rows[0]["ThongBao"]?.ToString() ?? msg;
                    isSuccess = msg.StartsWith("Lỗi", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                }

                if (isSuccess == 1)
                {
                    SetAlert("Mượn Sách Thành Công", $"[{DateTime.Now:HH:mm:ss}] {msg}", AlertType.Success);
                    radViewReader.Checked = true;
                }
                else
                {
                    SetAlert("Không Thể Mượn Sách", $"[{DateTime.Now:HH:mm:ss}] {msg}", AlertType.Danger);
                    radViewBook.Checked = true;
                }

                await ExecuteTraCuu();
                LoadViewData();
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Xử Lý Giao Tác", ex.Message, AlertType.Danger);
            }
            finally
            {
                btnMuonSach.Enabled = true;
                btnTraCuu.Enabled = true;
            }
        }
    }


    // =========================================================================
    // 3. KỊCH BẢN 3: NON-REPEATABLE READ (ĐỌC KHÔNG NHẤT QUÁN)
    // =========================================================================
    public class FrmNonRepeatableWin1 : FrmConcurrencyBase
    {
        private Button btnKiemKe = null!;
        private DataGridView dgvResult = null!;

        public FrmNonRepeatableWin1() : base(
            "[Quầy 1] Quản Lý Kho Thư Viện",
            "Kiểm kê số lượng bản sao có sẵn của đầu sách S001",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "[Quầy 1] Quản Lý Kho - Báo Cáo Kiểm Kê Đầu Sách";
            BuildUI();
        }

        private void BuildUI()
        {
            var grpAction = new GroupBox
            {
                Text = "Nghiệp Vụ Kiểm Kê Đầu Sách S001",
                Dock = DockStyle.Top,
                Height = 110,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            btnKiemKe = new Button
            {
                Text = "XUẤT BÁO CÁO KIỂM KÊ ĐẦU SÁCH",
                Location = new Point(20, 30),
                Size = new Size(490, 50),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnKiemKe.FlatAppearance.BorderSize = 0;
            btnKiemKe.Click += async (s, e) => await ExecuteKiemKe();

            grpAction.Controls.Add(btnKiemKe);
            pnlBody.Controls.Add(grpAction);

            var grpResult = new GroupBox
            {
                Text = "Bảng Đối Soát 2 Lần Đọc Trong Cùng 1 Giao Dịch",
                Dock = DockStyle.Top,
                Height = 160,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10)
            };

            dgvResult = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F)
            };
            grpResult.Controls.Add(dgvResult);
            pnlBody.Controls.Add(grpResult);

            grpResult.BringToFront();
            grpAction.BringToFront();
        }

        private async Task ExecuteKiemKe()
        {
            btnKiemKe.Enabled = false;
            StartCountdown(10, "Đang thực hiện giao tác kiểm kê kho");
            SetAlert("Đang Kiểm Kê Kho Sách", "Đã đếm Lần 1. Đang giữ Transaction kiểm kê trong 10 giây để đếm Lần 2...", AlertType.Warning);

            try
            {
                var pars = new SqlParameter[] { new SqlParameter("@p_MaSach", "S001") };
                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_BaoCaoKiemKeKho", pars);
                StopCountdown();
                dgvResult.DataSource = dt;

                if (dt.Rows.Count > 0)
                {
                    var lan1 = dt.Rows[0]["SoLuong_DocLan1"];
                    var lan2 = dt.Rows[0]["SoLuong_DocLan2"];
                    var ketQua = dt.Rows[0]["KetQuaPhanTich"]?.ToString() ?? "";

                    if (lan1?.ToString() != lan2?.ToString())
                    {
                        SetAlert("Phát Hiện Lỗi Đọc Không Nhất Quán", $"Lần 1 đếm: {lan1} cuốn | Lần 2 đếm: {lan2} cuốn. Số lượng bị thay đổi giữa chừng!", AlertType.Danger);
                        MessageBox.Show($"Báo cáo kiểm kê đầu sách S001 hoàn tất:\n\n• Số lượng đếm Lần 1: {lan1} cuốn\n• Số lượng đếm Lần 2: {lan2} cuốn\n\nPHÁT HIỆN LỖI NON-REPEATABLE READ:\nTrong lúc bạn đang kiểm kê, một thủ thư ở quầy khác đã cho mượn 1 cuốn sách làm thay đổi số lượng giữa 2 lần đếm trong cùng 1 phiên làm việc!", "Kết Quả Kiểm Kê Kho", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        SetAlert("Kiểm Kê Nhất Quán", $"Số lượng 2 lần đếm đều bằng {lan1} cuốn (Dữ liệu nhất quán).", AlertType.Success);
                        MessageBox.Show($"Báo cáo kiểm kê đầu sách S001 hoàn tất!\nSố lượng sách có sẵn: {lan1} cuốn (Nhất quán giữa 2 lần đếm).", "Kết Quả Kiểm Kê Kho", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Kiểm Kê", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi kiểm kê: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnKiemKe.Enabled = true;
            }
        }
    }


    public class FrmNonRepeatableWin2 : FrmConcurrencyBase
    {
        private Button btnChoMuon = null!;
        private Label lblCuonSach = null!;

        public FrmNonRepeatableWin2() : base(
            "[Quầy 2] Quầy Thủ Thư",
            "Nghiệp vụ lập phiếu mượn sách cho độc giả",
            Color.FromArgb(180, 83, 9))
        {
            this.Text = "[Quầy 2] Quầy Thủ Thư - Lập Phiếu Cho Mượn Sách";
            BuildUI();
        }

        private void BuildUI()
        {
            var grpAction = new GroupBox
            {
                Text = "Quầy Thủ Thư - Lập Phiếu Cho Mượn Sách",
                Dock = DockStyle.Top,
                Height = 190,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            lblCuonSach = new Label
            {
                Text = "• Cuốn sách: CS002 (Thuộc đầu sách S001 - Lập trình C#)\n• Độc giả: DG002 - Trần Minh Tuấn (Thẻ: Còn hạn)\n• Thao tác: Lập phiếu mượn qua Stored Procedure sp_LapPhieuMuon",
                Location = new Point(20, 30),
                Size = new Size(490, 65),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            btnChoMuon = new Button
            {
                Text = "XÁC NHẬN CHO MƯỢN SÁCH (CS002)",
                Location = new Point(20, 105),
                Size = new Size(490, 50),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnChoMuon.FlatAppearance.BorderSize = 0;
            btnChoMuon.Click += async (s, e) => await ExecuteChoMuon();

            grpAction.Controls.AddRange(new Control[] { lblCuonSach, btnChoMuon });
            pnlBody.Controls.Add(grpAction);

            grpAction.BringToFront();
        }

        private async Task ExecuteChoMuon()
        {
            btnChoMuon.Enabled = false;

            try
            {
                var dtBooks = new DataTable();
                dtBooks.Columns.Add("MaCuonSach", typeof(string));
                dtBooks.Rows.Add("CS002");

                var pars = new SqlParameter[] { 
                    new SqlParameter("@p_MaPhieuMuon", "PM" + DateTime.Now.ToString("ddHHmmss")),
                    new SqlParameter("@p_MaDG", "DG002"),
                    new SqlParameter("@p_MaNV", "NV001"),
                    new SqlParameter("@p_NgayHenTra", DateTime.Now.AddDays(14)),
                    DatabaseHelper.CreateStructuredParameter("@p_DanhSachCuonSach", "dbo.DanhSachCuonSachType", dtBooks)
                };
                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_LapPhieuMuon", pars);

                string msg = dt.Rows.Count > 0 && dt.Columns.Contains("ThongBao") 
                    ? dt.Rows[0]["ThongBao"].ToString() ?? "" 
                    : "";

                bool isError = msg.Contains("Lỗi") || msg.Contains("Không đủ điều kiện") || msg.Contains("thất bại");

                if (!isError && !string.IsNullOrEmpty(msg))
                {
                    SetAlert("Cho Mượn Thành Công", "Đã lập phiếu mượn và chuyển cuốn sách CS002 sang 'DangMuon' qua sp_LapPhieuMuon thành công.", AlertType.Success);
                    MessageBox.Show("Thủ thư đã xác nhận cho mượn cuốn sách CS002 thành công qua Stored Procedure sp_LapPhieuMuon!\nTrạng thái cuốn sách đã chuyển thành 'DangMuon'.", "Cho Mượn Sách Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SetAlert("Cảnh Báo Nghiệp Vụ", !string.IsNullOrEmpty(msg) ? msg : "Không thể lập phiếu mượn sách. Vui lòng kiểm tra lại tình trạng sách trên Form Thủ thư.", AlertType.Warning);
                    MessageBox.Show(!string.IsNullOrEmpty(msg) ? msg : "Không thể lập phiếu mượn sách. Vui lòng kiểm tra lại tình trạng sách trên Form Thủ thư.", "Thông Báo Nghiệp Vụ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                SetAlert("Lỗi Cập Nhật", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi Nghiệp Vụ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnChoMuon.Enabled = true;
            }
        }
    }


    // =========================================================================
    // 4. KỊCH BẢN 4: PHANTOM READ (ĐỌC BÓNG MA)
    // =========================================================================
    public class FrmPhantomWin1 : FrmConcurrencyBase
    {
        private Button btnBaoCao = null!;
        private DataGridView dgvResult = null!;

        public FrmPhantomWin1() : base(
            "[Phòng Kế Toán] Kế Toán Thư Viện",
            "Tổng hợp báo cáo các phiếu phạt vi phạm thư viện",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "[Phòng Kế Toán] Kế toán Thư viện - Báo cáo Tổng hợp Phạt";
            BuildUI();
        }

        private void BuildUI()
        {
            var grpAction = new GroupBox
            {
                Text = "Nghiệp Vụ Kế Toán - Tổng Hợp Phiếu Phạt",
                Dock = DockStyle.Top,
                Height = 110,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            btnBaoCao = new Button
            {
                Text = "XUẤT BÁO CÁO TỔNG HỢP TIỀN PHẠT",
                Location = new Point(20, 30),
                Size = new Size(490, 50),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBaoCao.FlatAppearance.BorderSize = 0;
            btnBaoCao.Click += async (s, e) => await ExecuteBaoCao();

            grpAction.Controls.Add(btnBaoCao);
            pnlBody.Controls.Add(grpAction);

            var grpResult = new GroupBox
            {
                Text = "Bảng Kết Quả Đối Soát & Thống Kê Phiếu Phạt",
                Dock = DockStyle.Top,
                Height = 220,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(10)
            };

            dgvResult = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F)
            };
            grpResult.Controls.Add(dgvResult);
            pnlBody.Controls.Add(grpResult);

            grpResult.BringToFront();
            grpAction.BringToFront();
        }

        private async Task ExecuteBaoCao()
        {
            btnBaoCao.Enabled = false;
            StartCountdown(10, "Đang chạy giao tác tổng hợp phiếu phạt");
            SetAlert("Đang Tổng Hợp Phiếu Phạt", "Đã đếm tổng số phiếu Lần 1. Đang giữ giao tác REPEATABLE READ 10s để đếm Lần 2...", AlertType.Warning);

            try
            {
                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_BaoCaoTongHopPhieuPhat");
                StopCountdown();
                dgvResult.DataSource = dt;

                if (dt.Rows.Count > 0)
                {
                    var lan1 = dt.Rows[0]["TongPhieu_Lan1"];
                    var lan2 = dt.Rows[0]["TongPhieu_Lan2"];
                    var ketQua = dt.Rows[0]["KetQuaPhanTich"]?.ToString() ?? "";

                    if (Convert.ToInt32(lan2) > Convert.ToInt32(lan1))
                    {
                        SetAlert("Phát Hiện Dòng Bóng Ma (Phantom Read)", $"Lần 1: {lan1} phiếu | Lần 2: {lan2} phiếu. Xuất hiện bản ghi mới chèn xen ngang!", AlertType.Danger);
                        MessageBox.Show($"Báo cáo tổng hợp tiền phạt hoàn tất:\n\n• Tổng số phiếu Lần 1: {lan1} phiếu\n• Tổng số phiếu Lần 2: {lan2} phiếu\n\nPHÁT HIỆN DÒNG BÓNG MA (PHANTOM READ):\nMặc dù dùng REPEATABLE READ (khóa các dòng hiện hữu), nhưng do không khóa khoảng trắng (Range Lock), một thủ thư đã chèn thêm 1 phiếu phạt mới làm tăng số lượng ở Lần 2!", "Kết Quả Tổng Hợp Phiếu Phạt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        SetAlert("Tổng Hợp Nhất Quán", $"Tổng số phiếu cả 2 lần đều là {lan1} phiếu.", AlertType.Success);
                        MessageBox.Show($"Báo cáo tổng hợp tiền phạt hoàn tất!\nTổng số phiếu phạt: {lan1} phiếu.", "Kết Quả Báo Cáo Phạt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Báo Cáo", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi báo cáo: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBaoCao.Enabled = true;
            }
        }
    }


    public class FrmPhantomWin2 : FrmConcurrencyBase
    {
        private TextBox txtMaPP = null!;
        private TextBox txtSoTien = null!;
        private Button btnTaoPhieu = null!;

        public FrmPhantomWin2() : base(
            "[Quầy 2] Quầy Thủ Thư",
            "Lập phiếu ghi nhận vi phạm và tiền phạt mới",
            Color.FromArgb(180, 83, 9))
        {
            this.Text = "[Quầy 2] Quầy Thủ Thư - Lập Phiếu Phạt Mới";
            BuildUI();
        }

        private void BuildUI()
        {
            var grpAction = new GroupBox
            {
                Text = "Thông Tin Phiếu Phạt Mới",
                Dock = DockStyle.Top,
                Height = 190,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            var lblMa = new Label { Text = "Mã phiếu phạt:", Location = new Point(20, 28), Size = new Size(150, 22), Font = new Font("Segoe UI", 9F, FontStyle.Regular) };
            txtMaPP = new TextBox { Text = "PP999", Location = new Point(20, 50), Size = new Size(180, 28), Font = new Font("Segoe UI", 10F) };

            var lblTien = new Label { Text = "Số tiền phạt (VNĐ):", Location = new Point(230, 28), Size = new Size(150, 22), Font = new Font("Segoe UI", 9F, FontStyle.Regular) };
            txtSoTien = new TextBox { Text = "50000", Location = new Point(230, 50), Size = new Size(180, 28), Font = new Font("Segoe UI", 10F) };

            btnTaoPhieu = new Button
            {
                Text = "XÁC NHẬN GHI NHẬN PHIẾU PHẠT",
                Location = new Point(20, 100),
                Size = new Size(490, 50),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTaoPhieu.FlatAppearance.BorderSize = 0;
            btnTaoPhieu.Click += async (s, e) => await ExecuteTaoPhieu();

            grpAction.Controls.AddRange(new Control[] { lblMa, txtMaPP, lblTien, txtSoTien, btnTaoPhieu });
            pnlBody.Controls.Add(grpAction);

            grpAction.BringToFront();
        }

        private async Task ExecuteTaoPhieu()
        {
            btnTaoPhieu.Enabled = false;
            string maPP = txtMaPP.Text.Trim();
            if (string.IsNullOrEmpty(maPP)) maPP = "PP999";
            decimal.TryParse(txtSoTien.Text.Trim(), out decimal soTien);
            if (soTien <= 0) soTien = 50000;

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaPhieuPhat", maPP),
                    new SqlParameter("@p_SoTienPhat", soTien)
                };

                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_TaoPhieuPhatNhanh", pars);
                SetAlert("Tạo Phiếu Phạt Thành Công", $"Đã ghi nhận phiếu phạt {maPP} ({soTien:N0} VNĐ) vào cơ sở dữ liệu.", AlertType.Success);
                MessageBox.Show($"Đã tạo mới phiếu phạt {maPP} ({soTien:N0} VNĐ) thành công vào hệ thống!", "Lập Phiếu Phạt Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                SetAlert("Lỗi Tạo Phiếu", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi tạo phiếu phạt: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTaoPhieu.Enabled = true;
            }
        }
    }


    // =========================================================================
    // 5. KỊCH BẢN 5: DEADLOCK (BẾ TẮC TƯƠNG HỖ)
    // =========================================================================
    public class FrmDeadlockWin1 : FrmConcurrencyBase
    {
        private TextBox txtBooks = null!;
        private Button btnConfirm = null!;
        private Label lblBookStatus = null!;

        public FrmDeadlockWin1() : base(
            "[Quầy 1] Thủ Thư 1",
            "Lập phiếu mượn sách cho Độc giả DG002 (Trần Minh Tuấn)",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "[Quầy 1] Thủ Thư 1 - Lập Phiếu Mượn Sách";
            pnlBody.AutoScroll = false;
            pnlBody.Padding = new Padding(12, 8, 12, 8);
            BuildUI();
            _ = LoadBookStatus();
        }

        private void BuildUI()
        {
            var tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(0)
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // 1. Thông tin độc giả và phiếu mượn
            var grpReader = new GroupBox
            {
                Text = "THÔNG TIN ĐỘC GIẢ VÀ PHIẾU MƯỢN",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 22, 12, 8)
            };
            var lblReaderInfo = new Label
            {
                Text = "• Độc giả: DG002 - Trần Minh Tuấn (Sinh viên | Thẻ: Còn hạn)\n• Mã phiếu: PM1_AUTO   |   Hạn trả: 14 ngày   |   Nhân viên: NV001",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            grpReader.Controls.Add(lblReaderInfo);

            // 2. Máy quét mã vạch và danh sách sách
            var grpScanner = new GroupBox
            {
                Text = "QUÉT MÃ VẠCH SÁCH MƯỢN (BARCODE SCANNER)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 22, 12, 8)
            };
            var tlpScanner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            tlpScanner.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpScanner.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tlpScanner.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));

            var lblPrompt = new Label
            {
                Text = "Thứ tự quét mã vạch các cuốn sách:",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            txtBooks = new TextBox
            {
                Text = "CS001, CS004",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 138)
            };
            var lblRecognized = new Label
            {
                Text = "Sách nhận diện: [CS001] Clean Code  |  [CS004] Đắc Nhân Tâm",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            tlpScanner.Controls.Add(lblPrompt, 0, 0);
            tlpScanner.Controls.Add(txtBooks, 0, 1);
            tlpScanner.Controls.Add(lblRecognized, 0, 2);
            grpScanner.Controls.Add(tlpScanner);

            // 3. Nút xác nhận lập phiếu mượn
            var grpAction = new GroupBox
            {
                Text = "THAO TÁC NGHIỆP VỤ",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 22, 12, 10)
            };
            btnConfirm = new Button
            {
                Text = "XÁC NHẬN LẬP PHIẾU MƯỢN",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += async (s, e) => await ExecuteT1();
            grpAction.Controls.Add(btnConfirm);

            // 4. Bảng giám sát trạng thái sách
            var grpStatus = new GroupBox
            {
                Text = "TRẠNG THÁI SÁCH TRONG KHO (THEO DÕI THỜI GIAN THỰC)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 22, 12, 8)
            };
            lblBookStatus = new Label
            {
                Text = "Đang đồng bộ trạng thái sách từ CSDL...",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(2, 132, 199)
            };
            grpStatus.Controls.Add(lblBookStatus);

            // Cấu hình chiều cao cho các dòng trong TableLayoutPanel:
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));   // grpReader
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 136F));  // grpScanner
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));   // grpAction
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 106F));  // grpStatus
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Flexible bottom buffer

            tlpMain.Controls.Add(grpReader, 0, 0);
            tlpMain.Controls.Add(grpScanner, 0, 1);
            tlpMain.Controls.Add(grpAction, 0, 2);
            tlpMain.Controls.Add(grpStatus, 0, 3);

            pnlBody.Controls.Add(tlpMain);
        }

        public async Task LoadBookStatus()
        {
            try
            {
                var dt = await DatabaseHelper.ExecuteQueryAsync(
                    "SELECT cs.MaCuonSach, s.TenSach, cs.TrangThai, cs.TinhTrang FROM CuonSach cs JOIN Sach s ON cs.MaSach = s.MaSach WHERE cs.MaCuonSach IN ('CS001', 'CS004') ORDER BY cs.MaCuonSach ASC");
                if (dt.Rows.Count > 0)
                {
                    var lines = new List<string>();
                    foreach (DataRow row in dt.Rows)
                    {
                        lines.Add($"• [{row["MaCuonSach"]}] {row["TenSach"]}: {row["TrangThai"]} ({row["TinhTrang"]})");
                    }
                    lines.Add("Tự động đồng bộ qua Database Trigger & Stored Procedure");
                    lblBookStatus.Text = string.Join("\n", lines);
                }
            }
            catch
            {
                lblBookStatus.Text = "Không thể lấy trạng thái sách từ CSDL.";
            }
        }

        private async Task ExecuteT1()
        {
            var books = txtBooks.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(b => b.Trim()).ToList();
            if (books.Count < 2)
            {
                MessageBox.Show("Vui lòng nhập ít nhất 2 mã sách để thực nghiệm (vd: CS001, CS004).", "Lỗi Nhập Liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string book1 = books[0];

            btnConfirm.Enabled = false;
            StartCountdown(5, $"Đang quét mã & xin khóa cuốn {book1}...");
            SetAlert("Đang Lập Phiếu Mượn", $"Đang quét mã {book1}, xin chờ 5s để mô phỏng hệ thống xử lý...", AlertType.Warning);

            try
            {
                var dtBooks = new System.Data.DataTable();
                dtBooks.Columns.Add("MaCuonSach", typeof(string));
                foreach (var b in books)
                {
                    dtBooks.Rows.Add(b);
                }

                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaPhieuMuon", "PM1_" + DateTime.Now.ToString("HHmmss")),
                    new SqlParameter("@p_MaDG", "DG002"),
                    new SqlParameter("@p_MaNV", "NV001"),
                    new SqlParameter("@p_NgayHenTra", DateTime.Now.AddDays(14)),
                    new SqlParameter
                    {
                        ParameterName = "@p_DanhSachCuonSach",
                        SqlDbType = System.Data.SqlDbType.Structured,
                        TypeName = "dbo.DanhSachCuonSachType",
                        Value = dtBooks
                    },
                    new SqlParameter("@p_DelayGiay", 5)
                };

                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_LapPhieuMuon_Deadlock", pars);
                StopCountdown();

                if (dt != null && dt.Rows.Count > 0)
                {
                    string msg = dt.Rows[0]["ThongBao"]?.ToString() ?? "";
                    int code = Convert.ToInt32(dt.Rows[0]["ErrorCode"]);
                    if (code == 1205)
                    {
                        SetAlert("Xung Đột Deadlock (Lỗi 1205)", "Giao tác Quầy 1 bị SQL Server chọn làm nạn nhân (Deadlock Victim) và tự động Rollback!", AlertType.Danger);
                        MessageBox.Show("GIAO DỊCH QUẦY 1 BỊ HỦY DO DEADLOCK (LỖI 1205)!\n\nSQL Server phát hiện chu trình bế tắc tài nguyên (Circular Wait) giữa Quầy 1 và Quầy 2:\n• Quầy 1 giữ khóa CS001 và chờ CS004\n• Quầy 2 giữ khóa CS004 và chờ CS001\n\nSQL Server đã tự động ROLLBACK giao dịch của Quầy 1 để giải phóng tài nguyên. Dữ liệu sách được bảo toàn an toàn!", "Xung Đột Bế Tắc (Deadlock Victim)", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                    else if (code != 0)
                    {
                        SetAlert("Lỗi Nghiệp Vụ", msg, AlertType.Danger);
                        MessageBox.Show("Thông báo: " + msg, "Lỗi Giao Tác", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        SetAlert("Lập Phiếu Mượn Thành Công", msg, AlertType.Success);
                        MessageBox.Show("Giao dịch Quầy 1 đã hoàn thành thành công!\nPhiếu mượn đã được ghi nhận vào hệ thống.", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    SetAlert("Lập Phiếu Mượn Thành Công", "Đã cập nhật trạng thái các cuốn sách.", AlertType.Success);
                    MessageBox.Show("Giao dịch Quầy 1 đã hoàn thành thành công!", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (SqlException ex) when (ex.Number == 1205)
            {
                StopCountdown();
                SetAlert("Xung Đột Deadlock (Lỗi 1205)", "Giao tác Quầy 1 bị SQL Server chọn làm nạn nhân (Deadlock Victim) và tự động Rollback!", AlertType.Danger);
                MessageBox.Show("GIAO DỊCH QUẦY 1 BỊ HỦY DO DEADLOCK (LỖI 1205)!\n\nSQL Server phát hiện chu trình bế tắc tài nguyên (Circular Wait) giữa Quầy 1 và Quầy 2:\n• Quầy 1 giữ khóa CS001 và chờ CS004\n• Quầy 2 giữ khóa CS004 và chờ CS001\n\nSQL Server đã tự động ROLLBACK giao dịch của Quầy 1 để giải phóng tài nguyên. Dữ liệu sách được bảo toàn an toàn!", "Xung Đột Bế Tắc (Deadlock Victim)", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Thực Thi", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi Giao Tác", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConfirm.Enabled = true;
                await LoadBookStatus();
                OnDataChanged?.Invoke();
            }
        }
    }


    public class FrmDeadlockWin2 : FrmConcurrencyBase
    {
        private TextBox txtBooks = null!;
        private Button btnConfirm = null!;
        private Label lblBookStatus = null!;

        public FrmDeadlockWin2() : base(
            "[Quầy 2] Thủ Thư 2",
            "Lập phiếu mượn sách cho Độc giả DG003 (Hoàng Thị Mai)",
            Color.FromArgb(180, 83, 9))
        {
            this.Text = "[Quầy 2] Thủ Thư 2 - Lập Phiếu Mượn Sách";
            pnlBody.AutoScroll = false;
            pnlBody.Padding = new Padding(12, 8, 12, 8);
            BuildUI();
            _ = LoadBookStatus();
        }

        private void BuildUI()
        {
            var tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(0)
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // 1. Thông tin độc giả và phiếu mượn
            var grpReader = new GroupBox
            {
                Text = "THÔNG TIN ĐỘC GIẢ VÀ PHIẾU MƯỢN",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 22, 12, 8)
            };
            var lblReaderInfo = new Label
            {
                Text = "• Độc giả: DG003 - Hoàng Thị Mai (Giảng viên | Thẻ: Còn hạn)\n• Mã phiếu: PM2_AUTO   |   Hạn trả: 14 ngày   |   Nhân viên: NV001",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            grpReader.Controls.Add(lblReaderInfo);

            // 2. Máy quét mã vạch và danh sách sách
            var grpScanner = new GroupBox
            {
                Text = "QUÉT MÃ VẠCH SÁCH MƯỢN (BARCODE SCANNER)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 22, 12, 8)
            };
            var tlpScanner = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            tlpScanner.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tlpScanner.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tlpScanner.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));

            var lblPrompt = new Label
            {
                Text = "Thứ tự quét mã vạch các cuốn sách (quét ngược lại so với Quầy 1):",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            txtBooks = new TextBox
            {
                Text = "CS004, CS001",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 83, 9)
            };
            var lblRecognized = new Label
            {
                Text = "Sách nhận diện: [CS004] Đắc Nhân Tâm  |  [CS001] Clean Code",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            tlpScanner.Controls.Add(lblPrompt, 0, 0);
            tlpScanner.Controls.Add(txtBooks, 0, 1);
            tlpScanner.Controls.Add(lblRecognized, 0, 2);
            grpScanner.Controls.Add(tlpScanner);

            // 3. Nút xác nhận lập phiếu mượn
            var grpAction = new GroupBox
            {
                Text = "THAO TÁC NGHIỆP VỤ",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 22, 12, 10)
            };
            btnConfirm = new Button
            {
                Text = "XÁC NHẬN LẬP PHIẾU MƯỢN",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += async (s, e) => await ExecuteT2();
            grpAction.Controls.Add(btnConfirm);

            // 4. Bảng giám sát trạng thái sách
            var grpStatus = new GroupBox
            {
                Text = "TRẠNG THÁI SÁCH TRONG KHO (THEO DÕI THỜI GIAN THỰC)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(12, 22, 12, 8)
            };
            lblBookStatus = new Label
            {
                Text = "Đang đồng bộ trạng thái sách từ CSDL...",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(2, 132, 199)
            };
            grpStatus.Controls.Add(lblBookStatus);

            // Cấu hình chiều cao cho các dòng trong TableLayoutPanel:
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));   // grpReader
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 136F));  // grpScanner
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));   // grpAction
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 106F));  // grpStatus
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // Flexible bottom buffer

            tlpMain.Controls.Add(grpReader, 0, 0);
            tlpMain.Controls.Add(grpScanner, 0, 1);
            tlpMain.Controls.Add(grpAction, 0, 2);
            tlpMain.Controls.Add(grpStatus, 0, 3);

            pnlBody.Controls.Add(tlpMain);
        }

        public async Task LoadBookStatus()
        {
            try
            {
                var dt = await DatabaseHelper.ExecuteQueryAsync(
                    "SELECT cs.MaCuonSach, s.TenSach, cs.TrangThai, cs.TinhTrang FROM CuonSach cs JOIN Sach s ON cs.MaSach = s.MaSach WHERE cs.MaCuonSach IN ('CS001', 'CS004') ORDER BY cs.MaCuonSach ASC");
                if (dt.Rows.Count > 0)
                {
                    var lines = new List<string>();
                    foreach (DataRow row in dt.Rows)
                    {
                        lines.Add($"• [{row["MaCuonSach"]}] {row["TenSach"]}: {row["TrangThai"]} ({row["TinhTrang"]})");
                    }
                    lines.Add("Tự động đồng bộ qua Database Trigger & Stored Procedure");
                    lblBookStatus.Text = string.Join("\n", lines);
                }
            }
            catch
            {
                lblBookStatus.Text = "Không thể lấy trạng thái sách từ CSDL.";
            }
        }

        private async Task ExecuteT2()
        {
            var books = txtBooks.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(b => b.Trim()).ToList();
            if (books.Count < 2)
            {
                MessageBox.Show("Vui lòng nhập ít nhất 2 mã sách để thực nghiệm (vd: CS004, CS001).", "Lỗi Nhập Liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string book1 = books[0];

            btnConfirm.Enabled = false;
            StartCountdown(5, $"Đang quét mã & xin khóa cuốn {book1}...");
            SetAlert("Đang Lập Phiếu Mượn", $"Đang quét mã {book1}, xin chờ 5s để mô phỏng hệ thống xử lý...", AlertType.Warning);

            try
            {
                var dtBooks = new System.Data.DataTable();
                dtBooks.Columns.Add("MaCuonSach", typeof(string));
                foreach (var b in books)
                {
                    dtBooks.Rows.Add(b);
                }

                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaPhieuMuon", "PM2_" + DateTime.Now.ToString("HHmmss")),
                    new SqlParameter("@p_MaDG", "DG003"),
                    new SqlParameter("@p_MaNV", "NV001"),
                    new SqlParameter("@p_NgayHenTra", DateTime.Now.AddDays(14)),
                    new SqlParameter
                    {
                        ParameterName = "@p_DanhSachCuonSach",
                        SqlDbType = System.Data.SqlDbType.Structured,
                        TypeName = "dbo.DanhSachCuonSachType",
                        Value = dtBooks
                    },
                    new SqlParameter("@p_DelayGiay", 5)
                };

                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_LapPhieuMuon_Deadlock", pars);
                StopCountdown();

                if (dt != null && dt.Rows.Count > 0)
                {
                    string msg = dt.Rows[0]["ThongBao"]?.ToString() ?? "";
                    int code = Convert.ToInt32(dt.Rows[0]["ErrorCode"]);
                    if (code == 1205)
                    {
                        SetAlert("Xung Đột Deadlock (Lỗi 1205)", "Giao tác Quầy 2 bị SQL Server chọn làm nạn nhân (Deadlock Victim) và tự động Rollback!", AlertType.Danger);
                        MessageBox.Show("GIAO DỊCH QUẦY 2 BỊ HỦY DO DEADLOCK (LỖI 1205)!\n\nSQL Server phát hiện chu trình bế tắc tài nguyên (Circular Wait) giữa Quầy 1 và Quầy 2:\n• Quầy 1 giữ khóa CS001 và chờ CS004\n• Quầy 2 giữ khóa CS004 và chờ CS001\n\nSQL Server đã tự động ROLLBACK giao dịch của Quầy 2 để giải phóng tài nguyên. Dữ liệu sách được bảo toàn an toàn!", "Xung Đột Bế Tắc (Deadlock Victim)", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                    else if (code != 0)
                    {
                        SetAlert("Lỗi Nghiệp Vụ", msg, AlertType.Danger);
                        MessageBox.Show("Thông báo: " + msg, "Lỗi Giao Tác", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        SetAlert("Lập Phiếu Mượn Thành Công", msg, AlertType.Success);
                        MessageBox.Show("Giao dịch Quầy 2 đã hoàn thành thành công!\nPhiếu mượn đã được ghi nhận vào hệ thống.", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    SetAlert("Lập Phiếu Mượn Thành Công", "Đã cập nhật trạng thái các cuốn sách.", AlertType.Success);
                    MessageBox.Show("Giao dịch Quầy 2 đã hoàn thành thành công!", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (SqlException ex) when (ex.Number == 1205)
            {
                StopCountdown();
                SetAlert("Xung Đột Deadlock (Lỗi 1205)", "Giao tác Quầy 2 bị SQL Server chọn làm nạn nhân (Deadlock Victim) và tự động Rollback!", AlertType.Danger);
                MessageBox.Show("GIAO DỊCH QUẦY 2 BỊ HỦY DO DEADLOCK (LỖI 1205)!\n\nSQL Server phát hiện chu trình bế tắc tài nguyên (Circular Wait) giữa Quầy 1 và Quầy 2:\n• Quầy 1 giữ khóa CS001 và chờ CS004\n• Quầy 2 giữ khóa CS004 và chờ CS001\n\nSQL Server đã tự động ROLLBACK giao dịch của Quầy 2 để giải phóng tài nguyên. Dữ liệu sách được bảo toàn an toàn!", "Xung Đột Bế Tắc (Deadlock Victim)", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Thực Thi", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi Giao Tác", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConfirm.Enabled = true;
                await LoadBookStatus();
                OnDataChanged?.Invoke();
            }
        }
    }
}
