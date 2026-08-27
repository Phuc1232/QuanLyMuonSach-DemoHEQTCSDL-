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
            this.Controls.Add(pnlHeader);

            // 2. Status Alert Panel at Bottom (Modern Card)
            var pnlBottomContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 135,
                Padding = new Padding(15, 5, 15, 15),
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
                Text = "ℹ️",
                Font = new Font("Segoe UI", 14F),
                Location = new Point(10, 10),
                Size = new Size(35, 35),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblAlertTitle = new Label
            {
                Text = "Sẵn sàng thực hiện nghiệp vụ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(50, 10),
                Size = new Size(480, 24)
            };

            lblAlertDetail = new Label
            {
                Text = "Vui lòng chọn thao tác nghiệp vụ ở phía trên để bắt đầu giao tác.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(50, 34),
                Size = new Size(480, 40)
            };

            lblTimerStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(50, 72),
                Size = new Size(480, 18),
                Visible = false
            };

            prgTimer = new ProgressBar
            {
                Location = new Point(50, 92),
                Size = new Size(475, 10),
                Style = ProgressBarStyle.Continuous,
                Value = 0,
                Visible = false
            };

            pnlStatusAlert.Controls.AddRange(new Control[] { lblAlertIcon, lblAlertTitle, lblAlertDetail, lblTimerStatus, prgTimer });
            pnlBottomContainer.Controls.Add(pnlStatusAlert);
            this.Controls.Add(pnlBottomContainer);

            // 3. Main Body Panel
            pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                AutoScroll = true
            };
            this.Controls.Add(pnlBody);
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
                    lblAlertIcon.Text = "✅";
                    lblAlertTitle.ForeColor = Color.FromArgb(6, 95, 70);
                    lblAlertDetail.ForeColor = Color.FromArgb(4, 120, 87);
                    break;

                case AlertType.Warning:
                    pnlStatusAlert.BackColor = Color.FromArgb(254, 252, 232);
                    currentBorderColor = Color.FromArgb(253, 224, 71);
                    lblAlertIcon.Text = "⚠️";
                    lblAlertTitle.ForeColor = Color.FromArgb(133, 77, 14);
                    lblAlertDetail.ForeColor = Color.FromArgb(161, 98, 7);
                    break;

                case AlertType.Danger:
                    pnlStatusAlert.BackColor = Color.FromArgb(254, 242, 242);
                    currentBorderColor = Color.FromArgb(254, 202, 202);
                    lblAlertIcon.Text = "❌";
                    lblAlertTitle.ForeColor = Color.FromArgb(153, 27, 27);
                    lblAlertDetail.ForeColor = Color.FromArgb(185, 28, 28);
                    break;

                default: // Info
                    pnlStatusAlert.BackColor = Color.FromArgb(239, 246, 255);
                    currentBorderColor = Color.FromArgb(191, 219, 254);
                    lblAlertIcon.Text = "ℹ️";
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
            lblTimerStatus.Text = $"⏳ {actionDescription} (Đang mở giao tác CSDL: còn {remainingSeconds}s)...";

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
                        lblTimerStatus.Text = $"⏳ {actionDescription} (Đang mở giao tác CSDL: còn {remainingSeconds}s)...";
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
            "👤 [User 1] Thủ Thư A (Quầy Tiếp Nhận 1)",
            "Kịch bản 1: Sửa tình trạng cuốn sách CS001 (Lost Update Demo)",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "👤 [User 1] Thủ Thư A - Cập Nhật Tình Trạng Sách CS001";
            BuildUI();
            LoadCurrentBookStatus();
        }

        private void BuildUI()
        {
            // Instruction Box
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(238, 242, 255),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Lost Update:\n1. Cả 2 thủ thư cùng mở màn hình và thấy tình trạng ban đầu là 'ConTot'.\n2. Thủ thư B (bên phải) chọn 'Mất đĩa CD' và bấm Lưu trước.\n3. Bạn (Thủ thư A) chọn 'Rách bìa ngoài' và bấm Lưu sau -> Ghi đè mất cập nhật của Thủ thư B!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(30, 58, 138)
            });
            pnlBody.Controls.Add(pnlGuide);

            // Book Details Form
            var grpBook = new GroupBox
            {
                Text = "📖 Thông Tin Sách Đang Xử Lý",
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
                Text = "✏️ Cập Nhật Tình Trạng Cuốn Sách",
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
                Text = "💾 [Thủ Thư A] Bấm Lưu Tình Trạng Vào CSDL",
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
            pnlGuide.BringToFront();
        }

        public void LoadCurrentBookStatus()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteQuery("SELECT MaCuonSach, TinhTrang, TrangThai FROM CuonSach WHERE MaCuonSach = 'CS001'");
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

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaCuonSach", "CS001"),
                    new SqlParameter("@p_TinhTrang", newStatus)
                };

                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_CapNhatTinhTrangCuonSach", pars);
                string msg = dt.Rows.Count > 0 ? (dt.Rows[0]["ThongBao"]?.ToString() ?? "Cập nhật thành công!") : "Cập nhật thành công!";
                
                LoadCurrentBookStatus();
                SetAlert("Cập Nhật Thành Công", $"Đã lưu tình trạng '{newStatus}' cho cuốn sách CS001 vào CSDL.", AlertType.Success);
                MessageBox.Show($"Thủ thư A đã cập nhật tình trạng cuốn sách CS001 thành '{newStatus}' thành công!", "Thông Báo Nghiệp Vụ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                SetAlert("Lỗi Cập Nhật", ex.Message, AlertType.Danger);
                MessageBox.Show("Có lỗi khi cập nhật tình trạng sách:\n" + ex.Message, "Lỗi Nghiệp Vụ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            "👤 [User 2] Thủ Thư B (Quầy Tiếp Nhận 2)",
            "Kịch bản 1: Sửa tình trạng cuốn sách CS001 (Lost Update Demo)",
            Color.FromArgb(180, 83, 9))
        {
            this.Text = "👤 [User 2] Thủ Thư B - Cập Nhật Tình Trạng Sách CS001";
            BuildUI();
            LoadCurrentBookStatus();
        }

        private void BuildUI()
        {
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Lost Update:\n1. Bạn là Thủ thư B. Chọn tình trạng bạn phát hiện (VD: 'Mất đĩa CD kèm theo').\n2. Bấm [Lưu Tình Trạng] trước Thủ thư A.\n3. Quan sát Dashboard CSDL: Khi Thủ thư A bấm Lưu sau, dữ liệu của B sẽ bị biến mất!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(146, 64, 14)
            });
            pnlBody.Controls.Add(pnlGuide);

            var grpBook = new GroupBox
            {
                Text = "📖 Thông Tin Sách Đang Xử Lý",
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
                Text = "✏️ Cập Nhật Tình Trạng Cuốn Sách",
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
                Text = "💾 [Thủ Thư B] Bấm Lưu Tình Trạng Vào CSDL",
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
            pnlGuide.BringToFront();
        }

        public void LoadCurrentBookStatus()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteQuery("SELECT MaCuonSach, TinhTrang, TrangThai FROM CuonSach WHERE MaCuonSach = 'CS001'");
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

            try
            {
                var pars = new SqlParameter[]
                {
                    new SqlParameter("@p_MaCuonSach", "CS001"),
                    new SqlParameter("@p_TinhTrang", newStatus)
                };

                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_CapNhatTinhTrangCuonSach", pars);
                string msg = dt.Rows.Count > 0 ? (dt.Rows[0]["ThongBao"]?.ToString() ?? "Cập nhật thành công!") : "Cập nhật thành công!";
                
                LoadCurrentBookStatus();
                SetAlert("Cập Nhật Thành Công", $"Đã lưu tình trạng '{newStatus}' cho cuốn sách CS001 vào CSDL.", AlertType.Success);
                MessageBox.Show($"Thủ thư B đã cập nhật tình trạng cuốn sách CS001 thành '{newStatus}' thành công!", "Thông Báo Nghiệp Vụ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                SetAlert("Lỗi Cập Nhật", ex.Message, AlertType.Danger);
                MessageBox.Show("Có lỗi khi cập nhật tình trạng sách:\n" + ex.Message, "Lỗi Nghiệp Vụ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            "👤 [User 1] Quầy Tiếp Nhận Trả Sách (Thủ Thư)",
            "Kịch bản 2: Tiếp nhận trả sách CS001 - Mở Transaction chờ 10s (Dirty Read Demo)",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "👤 [User 1] Quầy Tiếp Nhận Trả Sách & Xử Lý Vi Phạm";
            BuildUI();
        }

        private void BuildUI()
        {
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(238, 242, 255),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Dirty Read:\n1. Bấm nút [Tiếp Nhận Trả Sách] -> CSDL tạm đổi CS001 thành 'CoSan' và giữ mở giao tác 10s.\n2. Trong 10 giây này, Độc giả (bên phải) bấm [Tra Cứu] -> Đọc rác được 'CoSan'.\n3. Hết 10s, Khách thiếu tiền phạt -> Rollback về 'DangMuon' làm dữ liệu Độc giả vừa đọc thành Rác!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(30, 58, 138)
            });
            pnlBody.Controls.Add(pnlGuide);

            var grpBook = new GroupBox
            {
                Text = "📋 Hồ Sơ Mượn Sách Cần Trả",
                Dock = DockStyle.Top,
                Height = 130,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            grpBook.Controls.Add(new Label
            {
                Text = "• Mã cuốn sách: CS001 (Lập trình C# Cơ bản)\n• Độc giả: DG001 - Nguyễn Văn A\n• Tình trạng mượn: Quá hạn 3 ngày (Phạt 15,000 VNĐ)\n• Trạng thái ban đầu: DangMuon",
                Location = new Point(20, 28),
                Size = new Size(490, 85),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59)
            });
            pnlBody.Controls.Add(grpBook);

            var grpAction = new GroupBox
            {
                Text = "⚡ Quy Trình Tiếp Nhận & Xử Lý Tiền Phạt",
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
                Checked = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 38, 38)
            };

            radCommit = new RadioButton
            {
                Text = "Khách nộp đủ tiền phạt -> Hoàn tất trả sách (COMMIT sau 10s)",
                Location = new Point(20, 55),
                Size = new Size(490, 24),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(22, 101, 52)
            };

            btnTraSach = new Button
            {
                Text = "📋 1. TIẾP NHẬN TRẢ SÁCH (Mở Giao Tác Chờ 10 Giây)",
                Location = new Point(20, 95),
                Size = new Size(490, 50),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTraSach.FlatAppearance.BorderSize = 0;
            btnTraSach.Click += async (s, e) => await ExecuteTraSach();

            grpAction.Controls.AddRange(new Control[] { radRollback, radCommit, btnTraSach });
            pnlBody.Controls.Add(grpAction);

            grpAction.BringToFront();
            grpBook.BringToFront();
            pnlGuide.BringToFront();
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

                string msg = dt.Rows.Count > 0 ? (dt.Rows[0]["ThongBao"]?.ToString() ?? "Giao dịch kết thúc.") : "Giao dịch kết thúc.";
                
                if (coLoiHoacHuy == 1)
                {
                    SetAlert("Giao Dịch Đã HỦY (Rollback)", "Khách không đủ tiền nộp phạt -> Giao dịch bị hủy. Cuốn sách CS001 trở về 'DangMuon'.", AlertType.Danger);
                    MessageBox.Show("Giao dịch trả sách đã bị HỦY BỎ (Rollback) do khách hàng không đủ tiền nộp phạt!\n\nTrạng thái cuốn sách CS001 được khôi phục về 'DangMuon'.", "Hủy Giao Dịch Trả Sách", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    SetAlert("Trả Sách Thành Công", "Giao dịch trả sách đã hoàn tất và Commit thành công vào CSDL.", AlertType.Success);
                    MessageBox.Show("Giao dịch trả sách CS001 hoàn tất thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        private TextBox txtMaCuonSach = null!;
        private Button btnTraCuu = null!;
        private DataGridView dgvResult = null!;

        public FrmDirtyReadWin2() : base(
            "👤 [User 2] Cổng Tra Cứu Trực Tuyến (Độc Giả)",
            "Kịch bản 2: Tra cứu tình trạng sách (Mức READ UNCOMMITTED - Dirty Read)",
            Color.FromArgb(13, 148, 136))
        {
            this.Text = "👤 [User 2] Cổng Tra Cứu Trực Tuyến - Độc Giả";
            BuildUI();
        }

        private void BuildUI()
        {
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(240, 253, 250),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Dirty Read:\n1. Bấm nút [Tra Cứu Ngay] trong khi Thủ thư bên trái đang trong 10 giây chờ.\n2. Quan sát kết quả: CSDL trả về 'CoSan' (dữ liệu rác chưa commit do dùng READ UNCOMMITTED).\n3. Bấm tra cứu lại sau khi Thủ thư Rollback -> Trạng thái thực tế vẫn là 'DangMuon'!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(15, 118, 110)
            });
            pnlBody.Controls.Add(pnlGuide);

            var grpSearch = new GroupBox
            {
                Text = "🔍 Tra Cứu Tình Trạng Cuốn Sách",
                Dock = DockStyle.Top,
                Height = 100,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            var lblMa = new Label { Text = "Mã cuốn sách:", Location = new Point(20, 25), Size = new Size(120, 22), Font = new Font("Segoe UI", 9F, FontStyle.Regular) };
            txtMaCuonSach = new TextBox { Text = "CS001", Location = new Point(20, 48), Size = new Size(150, 28), Font = new Font("Segoe UI", 10F) };

            btnTraCuu = new Button
            {
                Text = "🔍 2. TRA CỨU NGAY (READ UNCOMMITTED)",
                Location = new Point(185, 45),
                Size = new Size(325, 34),
                BackColor = Color.FromArgb(13, 148, 136),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTraCuu.FlatAppearance.BorderSize = 0;
            btnTraCuu.Click += async (s, e) => await ExecuteTraCuu();

            grpSearch.Controls.AddRange(new Control[] { lblMa, txtMaCuonSach, btnTraCuu });
            pnlBody.Controls.Add(grpSearch);

            var grpResult = new GroupBox
            {
                Text = "📊 Kết Quả Tra Cứu Từ CSDL",
                Dock = DockStyle.Top,
                Height = 150,
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
            grpSearch.BringToFront();
            pnlGuide.BringToFront();
        }

        private async Task ExecuteTraCuu()
        {
            string maCS = txtMaCuonSach.Text.Trim();
            if (string.IsNullOrEmpty(maCS)) maCS = "CS001";

            try
            {
                var pars = new SqlParameter[] { new SqlParameter("@p_MaCuonSach", maCS) };
                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_TraCuuSach_DirtyRead", pars);
                dgvResult.DataSource = dt;

                if (dt.Rows.Count > 0)
                {
                    string trangThai = dt.Rows[0]["TrangThai"].ToString() ?? "";
                    string tenSach = dt.Rows[0]["TenSach"].ToString() ?? "";

                    if (trangThai.Equals("CoSan", StringComparison.OrdinalIgnoreCase))
                    {
                        SetAlert("Phát Hiện Dữ Liệu Chưa Chốt (Dirty Read)", $"Sách '{tenSach}' hiện có TrangThai = 'CoSan'. Đây là dữ liệu chưa commit từ quầy thủ thư!", AlertType.Warning);
                        MessageBox.Show($"Tìm thấy cuốn sách: {tenSach} ({maCS})\nTrạng thái: CÓ SẴN (CoSan)\n\n⚠️ CẢNH BÁO TƯƠNG TRANH: Bạn đang đọc với mức READ UNCOMMITTED khi thủ thư đang mở giao tác. Nếu thủ thư hủy giao dịch, trạng thái này là DỮ LIỆU RÁC!", "Kết Quả Tra Cứu (READ UNCOMMITTED)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        SetAlert("Tra Cứu Thành Công", $"Sách '{tenSach}' hiện có TrangThai = '{trangThai}' (Đang mượn).", AlertType.Info);
                        MessageBox.Show($"Tìm thấy cuốn sách: {tenSach} ({maCS})\nTrạng thái: ĐANG MƯỢN (DangMuon)\n\n(Dữ liệu thực tế sau khi giao tác trả sách bị hủy).", "Kết Quả Tra Cứu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    SetAlert("Không Tìm Thấy", $"Không tìm thấy cuốn sách có mã '{maCS}'.", AlertType.Warning);
                    MessageBox.Show($"Không tìm thấy cuốn sách mã '{maCS}' trong hệ thống.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                SetAlert("Lỗi Tra Cứu", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi tra cứu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            "👤 [User 1] Quản Lý Kho Thư Viện",
            "Kịch bản 3: Kiểm kê đầu sách S001 (READ COMMITTED - Non-Repeatable Read)",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "👤 [User 1] Quản Lý Kho - Báo Cáo Kiểm Kê Đầu Sách";
            BuildUI();
        }

        private void BuildUI()
        {
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(238, 242, 255),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Non-Repeatable Read:\n1. Bấm nút [Xuất Báo Cáo Kiểm Kê] -> Quản lý đếm Lần 1, giữ giao tác và chờ 10s.\n2. Trong 10s này, Thủ thư (bên phải) bấm [Cho Mượn 1 Cuốn (CS002)].\n3. Sau 10s, Quản lý đếm Lần 2 -> Kết quả Lần 2 bị giảm so với Lần 1 ngay trong cùng 1 Transaction!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(30, 58, 138)
            });
            pnlBody.Controls.Add(pnlGuide);

            var grpAction = new GroupBox
            {
                Text = "📊 Nghiệp Vụ Kiểm Kê Đầu Sách S001",
                Dock = DockStyle.Top,
                Height = 110,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            btnKiemKe = new Button
            {
                Text = "📊 1. XUẤT BÁO CÁO KIỂM KÊ (Giao Tác READ COMMITTED 10s)",
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
                Text = "📈 Bảng Đối Soát 2 Lần Đọc Trong Cùng 1 Transaction",
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
            pnlGuide.BringToFront();
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
                        MessageBox.Show($"Báo cáo kiểm kê đầu sách S001 hoàn tất:\n\n• Số lượng đếm Lần 1: {lan1} cuốn\n• Số lượng đếm Lần 2: {lan2} cuốn\n\n🚨 PHÁT HIỆN LỖI NON-REPEATABLE READ:\nTrong lúc bạn đang kiểm kê, một thủ thư ở quầy khác đã cho mượn 1 cuốn sách làm thay đổi số lượng giữa 2 lần đếm trong cùng 1 phiên làm việc!", "Kết Quả Kiểm Kê Kho", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            "👤 [User 2] Quầy Thủ Thư (Cho Mượn Nhanh)",
            "Kịch bản 3: Lập phiếu cho mượn cuốn sách CS002 (Non-Repeatable Read Demo)",
            Color.FromArgb(180, 83, 9))
        {
            this.Text = "👤 [User 2] Quầy Thủ Thư - Lập Phiếu Cho Mượn Nhanh";
            BuildUI();
        }

        private void BuildUI()
        {
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Non-Repeatable Read:\n1. Bấm nút [Xác Nhận Cho Mượn 1 Cuốn (CS002)] KHI Quản lý kho bên trái đang trong 10s kiểm kê.\n2. Cuốn sách CS002 lập tức chuyển sang 'DangMuon' và Commit thành công.\n3. Khi Quản lý đọc lần 2, số lượng sách CoSan của đầu sách S001 sẽ bị giảm từ 3 xuống 2!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(146, 64, 14)
            });
            pnlBody.Controls.Add(pnlGuide);

            var grpAction = new GroupBox
            {
                Text = "📖 Quầy Thủ Thư - Cho Mượn Sách Nhanh",
                Dock = DockStyle.Top,
                Height = 190,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            lblCuonSach = new Label
            {
                Text = "• Cuốn sách: CS002 (Thuộc đầu sách S001 - Lập trình C#)\n• Độc giả: DG001 - Nguyễn Văn A\n• Thao tác: Chuyển trạng thái từ 'CoSan' -> 'DangMuon'",
                Location = new Point(20, 30),
                Size = new Size(490, 65),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            btnChoMuon = new Button
            {
                Text = "📖 2. XÁC NHẬN CHO MƯỢN 1 CUỐN (CS002)",
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
            pnlGuide.BringToFront();
        }

        private async Task ExecuteChoMuon()
        {
            btnChoMuon.Enabled = false;

            try
            {
                int rows = await DatabaseHelper.ExecuteNonQueryAsync("UPDATE CuonSach SET TrangThai = 'DangMuon' WHERE MaCuonSach = 'CS002'");
                if (rows > 0)
                {
                    SetAlert("Cho Mượn Thành Công", "Đã chuyển cuốn sách CS002 sang 'DangMuon' và lưu vào CSDL thành công.", AlertType.Success);
                    MessageBox.Show("Thủ thư đã xác nhận cho mượn cuốn sách CS002 thành công!\nTrạng thái cuốn sách đã chuyển thành 'DangMuon'.", "Cho Mượn Sách Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    SetAlert("Cảnh Báo", "Không tìm thấy cuốn sách CS002 hoặc sách đã ở trạng thái mượn.", AlertType.Warning);
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
            "👤 [User 1] Phòng Kế Toán Thư Viện",
            "Kịch bản 4: Báo cáo tổng hợp tiền phạt (REPEATABLE READ - Phantom Read Demo)",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "👤 [User 1] Phòng Kế Toán - Tổng Hợp Báo Cáo Phạt";
            BuildUI();
        }

        private void BuildUI()
        {
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(238, 242, 255),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Phantom Read:\n1. Bấm nút [Xuất Báo Cáo Tổng Hợp Phạt] -> Kế toán đếm tổng số phiếu lần 1 ở mức REPEATABLE READ và đợi 10s.\n2. Trong 10s này, Thủ thư (bên phải) bấm [Ghi Nhận Phiếu Phạt Mới (PP999)].\n3. Hết 10s, Kế toán đếm Lần 2 -> Phát hiện thêm dòng bóng ma PP999 mới xuất hiện!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(30, 58, 138)
            });
            pnlBody.Controls.Add(pnlGuide);

            var grpAction = new GroupBox
            {
                Text = "📑 Nghiệp Vụ Kế Toán - Tổng Hợp Phiếu Phạt",
                Dock = DockStyle.Top,
                Height = 110,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            btnBaoCao = new Button
            {
                Text = "📑 1. XUẤT BÁO CÁO TỔNG HỢP PHẠT (REPEATABLE READ 10s)",
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
                Text = "📈 Bảng Kết Quả Đếm 2 Lần & Nhận Diện Bóng Ma",
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
            pnlGuide.BringToFront();
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
                        MessageBox.Show($"Báo cáo tổng hợp tiền phạt hoàn tất:\n\n• Tổng số phiếu Lần 1: {lan1} phiếu\n• Tổng số phiếu Lần 2: {lan2} phiếu\n\n🚨 PHÁT HIỆN DÒNG BÓNG MA (PHANTOM READ):\nMặc dù dùng REPEATABLE READ (khóa các dòng hiện hữu), nhưng do không khóa khoảng trắng (Range Lock), một thủ thư đã chèn thêm 1 phiếu phạt mới làm tăng số lượng ở Lần 2!", "Kết Quả Tổng Hợp Phiếu Phạt", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            "👤 [User 2] Quầy Thủ Thư (Lập Phiếu Phạt Mới)",
            "Kịch bản 4: Lập phiếu phạt vi phạm PP999 (Phantom Read Demo)",
            Color.FromArgb(180, 83, 9))
        {
            this.Text = "👤 [User 2] Quầy Thủ Thư - Lập Phiếu Phạt Vi Phạm Mới";
            BuildUI();
        }

        private void BuildUI()
        {
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Phantom Read:\n1. Bấm nút [Ghi Nhận Phiếu Phạt Mới] KHI Kế toán bên trái đang trong 10s xuất báo cáo.\n2. Phiếu phạt PP999 được INSERT mới vào bảng PhieuPhat và Commit ngay lập tức.\n3. Khi Kế toán đếm lần 2, tổng số phiếu sẽ tăng thêm 1 (xuất hiện dòng Bóng Ma)!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(146, 64, 14)
            });
            pnlBody.Controls.Add(pnlGuide);

            var grpAction = new GroupBox
            {
                Text = "➕ Thông Tin Phiếu Phạt Mới",
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
                Text = "➕ 2. GHI NHẬN PHIẾU PHẠT MỚI (PP999)",
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
            pnlGuide.BringToFront();
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
        private Button btnT1 = null!;

        public FrmDeadlockWin1() : base(
            "👤 [User 1] Giao Tác T1 (Khóa CS001 -> Đòi CS002)",
            "Kịch bản 5: Bế tắc Deadlock giữa 2 giao dịch phụ thuộc chéo",
            Color.FromArgb(30, 58, 138))
        {
            this.Text = "👤 [User 1] Giao Tác T1 - Chiếm CS001 rồi chờ khóa CS002";
            BuildUI();
        }

        private void BuildUI()
        {
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(238, 242, 255),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Deadlock:\n1. Bấm nút [Thực Thi T1] -> T1 khóa độc quyền CS001 và chờ 5s.\n2. Ngay lập tức, bấm nút [Thực Thi T2] ở cửa sổ bên phải -> T2 khóa CS002 và chờ 5s.\n3. Cả 2 giao tác đòi khóa chéo -> SQL Server tự động Rollback 1 bên (Lỗi 1205 Deadlock)!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(30, 58, 138)
            });
            pnlBody.Controls.Add(pnlGuide);

            var grpAction = new GroupBox
            {
                Text = "⚡ Tiến Trình Giao Tác T1",
                Dock = DockStyle.Top,
                Height = 140,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            btnT1 = new Button
            {
                Text = "⚡ 1. THỰC THI GIAO TÁC T1 (Khóa CS001 -> Đòi CS002)",
                Location = new Point(20, 40),
                Size = new Size(490, 55),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnT1.FlatAppearance.BorderSize = 0;
            btnT1.Click += async (s, e) => await ExecuteT1();

            grpAction.Controls.Add(btnT1);
            pnlBody.Controls.Add(grpAction);

            grpAction.BringToFront();
            pnlGuide.BringToFront();
        }

        private async Task ExecuteT1()
        {
            btnT1.Enabled = false;
            StartCountdown(5, "[T1] Đang giữ khóa CS001");
            SetAlert("Giao Tác T1 Đang Chạy", "Đã khóa CS001, đang chờ 5s trước khi yêu cầu khóa tiếp CS002...", AlertType.Warning);

            try
            {
                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_Demo_Deadlock_T1");
                StopCountdown();
                if (dt.Rows.Count > 0)
                {
                    string msg = dt.Rows[0]["ThongBao"]?.ToString() ?? "";
                    int code = Convert.ToInt32(dt.Rows[0]["ErrorCode"]);
                    if (code == 1205)
                    {
                        SetAlert("Xung Đột Deadlock (Mã lỗi 1205)", "Giao tác T1 bị SQL Server chọn làm nạn nhân (Deadlock Victim) và đã tự động Rollback!", AlertType.Danger);
                        MessageBox.Show("🚨 GIAO DỊCH T1 BỊ HỦY DO DEADLOCK (LỖI 1205)!\n\nSQL Server phát hiện chu trình bế tắc tài nguyên giữa T1 và T2. Giao dịch T1 đã được tự động Rollback để giải phóng khóa.\n\nVui lòng thử lại thao tác.", "Xung Đột Bế Tắc (Deadlock Victim)", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                    else
                    {
                        SetAlert("Giao Dịch T1 Thành Công", msg, AlertType.Success);
                        MessageBox.Show("Giao dịch T1 đã hoàn thành thành công!", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Thực Thi", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi Giao Tác", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnT1.Enabled = true;
            }
        }
    }


    public class FrmDeadlockWin2 : FrmConcurrencyBase
    {
        private Button btnT2 = null!;

        public FrmDeadlockWin2() : base(
            "👤 [User 2] Giao Tác T2 (Khóa CS002 -> Đòi CS001)",
            "Kịch bản 5: Bế tắc Deadlock giữa 2 giao dịch phụ thuộc chéo",
            Color.FromArgb(180, 83, 9))
        {
            this.Text = "👤 [User 2] Giao Tác T2 - Chiếm CS002 rồi chờ khóa CS001";
            BuildUI();
        }

        private void BuildUI()
        {
            var pnlGuide = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(254, 243, 199),
                Padding = new Padding(10)
            };
            pnlGuide.Controls.Add(new Label
            {
                Text = "📌 Hướng Dẫn Nghiệp Vụ Deadlock:\n1. Bấm nút [Thực Thi T2] ngay sau khi T1 vừa chạy.\n2. T2 chiếm giữ khóa CS002 và chờ 5s trước khi đòi tiếp CS001.\n3. Quan sát: 1 trong 2 giao tác sẽ thành công, giao tác còn lại bị SQL Server Rollback do Deadlock!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(146, 64, 14)
            });
            pnlBody.Controls.Add(pnlGuide);

            var grpAction = new GroupBox
            {
                Text = "⚡ Tiến Trình Giao Tác T2",
                Dock = DockStyle.Top,
                Height = 140,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Padding = new Padding(15)
            };

            btnT2 = new Button
            {
                Text = "⚡ 2. THỰC THI GIAO TÁC T2 (Khóa CS002 -> Đòi CS001)",
                Location = new Point(20, 40),
                Size = new Size(490, 55),
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnT2.FlatAppearance.BorderSize = 0;
            btnT2.Click += async (s, e) => await ExecuteT2();

            grpAction.Controls.Add(btnT2);
            pnlBody.Controls.Add(grpAction);

            grpAction.BringToFront();
            pnlGuide.BringToFront();
        }

        private async Task ExecuteT2()
        {
            btnT2.Enabled = false;
            StartCountdown(5, "[T2] Đang giữ khóa CS002");
            SetAlert("Giao Tác T2 Đang Chạy", "Đã khóa CS002, đang chờ 5s trước khi yêu cầu khóa tiếp CS001...", AlertType.Warning);

            try
            {
                var dt = await DatabaseHelper.ExecuteProcedureAsync("sp_Demo_Deadlock_T2");
                StopCountdown();
                if (dt.Rows.Count > 0)
                {
                    string msg = dt.Rows[0]["ThongBao"]?.ToString() ?? "";
                    int code = Convert.ToInt32(dt.Rows[0]["ErrorCode"]);
                    if (code == 1205)
                    {
                        SetAlert("Xung Đột Deadlock (Mã lỗi 1205)", "Giao tác T2 bị SQL Server chọn làm nạn nhân (Deadlock Victim) và đã tự động Rollback!", AlertType.Danger);
                        MessageBox.Show("🚨 GIAO DỊCH T2 BỊ HỦY DO DEADLOCK (LỖI 1205)!\n\nSQL Server phát hiện chu trình bế tắc tài nguyên giữa T1 và T2. Giao dịch T2 đã được tự động Rollback để giải phóng khóa.\n\nVui lòng thử lại thao tác.", "Xung Đột Bế Tắc (Deadlock Victim)", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    }
                    else
                    {
                        SetAlert("Giao Dịch T2 Thành Công", msg, AlertType.Success);
                        MessageBox.Show("Giao dịch T2 đã hoàn thành thành công!", "Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                OnDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                StopCountdown();
                SetAlert("Lỗi Thực Thi", ex.Message, AlertType.Danger);
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi Giao Tác", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnT2.Enabled = true;
            }
        }
    }
}
