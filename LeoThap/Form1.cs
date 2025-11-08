using LeoThap.Auto;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LeoThap
{
    public partial class Form1 : Form
    {
        IntPtr _hwnd = IntPtr.Zero;
        CancellationTokenSource? _cts;

        ComboBox cboWindows = new();
        TextBox txtAssetsDir = new();
        Button btnStart = new();
        Button btnStop = new();
        TextBox txtLog = new();

        public Form1()
        {
            InitializeComponent();
            Load += (s, e) =>
            {
                LoadWindows();
                txtAssetsDir.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
                Append("🧱 Tool Leo Tháp (Boss Trấn Thủ) đã sẵn sàng!");
            };
        }

        private void InitializeComponent()
        {
            Text = "⚔️ Leo Tháp - Boss Trấn Thủ Tool";
            Size = new Size(620, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            cboWindows.Location = new Point(20, 30);
            cboWindows.Size = new Size(280, 25);
            Controls.Add(cboWindows);

            txtAssetsDir.Location = new Point(20, 65);
            txtAssetsDir.Size = new Size(400, 23);
            txtAssetsDir.ReadOnly = true;
            Controls.Add(txtAssetsDir);

            btnStart.Text = "Start";
            btnStart.Location = new Point(440, 30);
            btnStart.Click += BtnStart_Click;
            Controls.Add(btnStart);

            btnStop.Text = "Stop";
            btnStop.Location = new Point(510, 30);
            btnStop.Enabled = false;
            btnStop.Click += (s, e) => { _cts?.Cancel(); ToggleUI(true); };
            Controls.Add(btnStop);

            txtLog.Location = new Point(20, 100);
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(560, 260);
            txtLog.ReadOnly = true;
            Controls.Add(txtLog);
        }

        // ======================================================
        // 🧭 Dò các tiến trình có thể là Game / Flash
        // ======================================================
        private void LoadWindows()
        {
            cboWindows.Items.Clear();

            foreach (var p in Process.GetProcesses().OrderBy(p => p.ProcessName))
            {
                try
                {
                    string name = p.ProcessName.ToLower();
                    string title = p.MainWindowTitle?.Trim() ?? "";

                    if (name.Contains("flash") ||
                        name.Contains("dy") ||
                        name.Contains("magic") ||
                        title.Contains("flash", StringComparison.OrdinalIgnoreCase) ||
                        title.Contains("dy", StringComparison.OrdinalIgnoreCase) ||
                        title.Contains("magic", StringComparison.OrdinalIgnoreCase))
                    {
                        string display = !string.IsNullOrWhiteSpace(title)
                            ? $"{title}  ({p.Id})"
                            : $"{p.ProcessName}  ({p.Id})";
                        cboWindows.Items.Add(display);
                    }
                }
                catch { }
            }

            if (cboWindows.Items.Count > 0)
                cboWindows.SelectedIndex = 0;

            Append($"🔍 Đã tìm thấy {cboWindows.Items.Count} tiến trình có thể là Flash/Game.");
        }

        // ======================================================
        // 🧩 Khi nhấn Start
        // ======================================================
        private void BtnStart_Click(object? sender, EventArgs e)
        {
            if (cboWindows.SelectedItem == null)
            {
                MessageBox.Show("⚠️ Chưa chọn cửa sổ game.");
                return;
            }

            var title = cboWindows.SelectedItem.ToString()!;
            int pidIndex = title.IndexOf(" (");
            if (pidIndex > 0)
                title = title[..pidIndex].Trim();

            Append($"🔎 Đang tìm cửa sổ có tiêu đề chứa: {title}");
            _hwnd = FindWindowByPartialTitle(title);


            if (_hwnd == IntPtr.Zero)
            {
                MessageBox.Show($"❌ Không tìm thấy cửa sổ có tiêu đề chứa: \"{title}\"");
                return;
            }

            Append($"✅ Match HWND: 0x{_hwnd.ToInt64():X}");

            // ✅ Tìm cửa sổ con chứa Flash thật
            IntPtr flashHwnd = FindDeepFlashWindow(_hwnd);
            if (flashHwnd != IntPtr.Zero)
            {
                _hwnd = flashHwnd;
                Append($"✅ Flash window found: 0x{_hwnd.ToInt64():X}");
            }
            else
            {
                Append("⚠️ Không tìm thấy Flash child window — dùng handle cha.");
            }

            ToggleUI(false);
            _cts = new CancellationTokenSource();
            Task.Run(() => RunSequence(_cts.Token));
        }

        // ======================================================
        // 🔍 Tìm cửa sổ chứa tiêu đề gần đúng
        // ======================================================
        IntPtr FindWindowByPartialTitle(string partialTitle)
        {
            IntPtr found = IntPtr.Zero;

            EnumWindows((IntPtr hWnd, IntPtr lParam) =>
            {
                var sb = new StringBuilder(512);
                GetWindowText(hWnd, sb, sb.Capacity);
                string windowText = sb.ToString();

                if (windowText.Contains(partialTitle, StringComparison.OrdinalIgnoreCase))
                {
                    Append($"✅ Match: {windowText}");
                    found = hWnd;
                    return false; // stop enumeration
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProcTop lpEnumFunc, IntPtr lParam);
        delegate bool EnumWindowsProcTop(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // ======================================================
        // 🔁 Logic xử lý chính
        // ======================================================
        private void RunSequence(CancellationToken tk)
        {
            string assets = txtAssetsDir.Text;
            string playerImg = PlayerDetector.DetectPlayerAvatar(_hwnd, assets, 0.85, Append);
            if (string.IsNullOrEmpty(playerImg))
            {
                MessageBox.Show("⚠️ Không phát hiện được nhân vật nào trong khung. Hãy để nhân vật hiển thị rõ ràng.");
                BeginInvoke(() => ToggleUI(true));
                return;
            }
            Append("⚙️ Chuẩn bị kích hoạt AUTO lần đầu...");
            Thread.Sleep(1000);
            ImageHelper.ClickClient(_hwnd, 872, 509, Append);

            string suGiaImg = Path.Combine(assets, "SuGia.png");
            string bossImg = Path.Combine(assets, "Boss.png");
            string bangImg = Path.Combine(assets, "BangDichChuyen.png");
            string lenTangImg = Path.Combine(assets, "LenTang.png");
            string KhieuChien = Path.Combine(assets, "KhieuChien.png");
            string bossPopupImg = Path.Combine(assets, "BossPopup.png");


            AutoBossController.RunBossFlow(
                _hwnd, playerImg, suGiaImg, bossImg, bangImg, lenTangImg, Append, tk
            );

            Append("✅ Hoàn tất quy trình Leo Tháp!");
            BeginInvoke(() => ToggleUI(true));
        }

        // ======================================================
        // 🧠 Các hàm phụ trợ
        // ======================================================

        void Append(string msg)
        {
            BeginInvoke(() => txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {msg}\r\n"));
        }

        void ToggleUI(bool enabled)
        {
            BeginInvoke(() =>
            {
                btnStart.Enabled = enabled;
                btnStop.Enabled = !enabled;
                cboWindows.Enabled = enabled;
            });
        }

        // ======================================================
        // 🧱 Tìm khung Flash con
        // ======================================================
        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        IntPtr FindDeepFlashWindow(IntPtr parent)
        {
            IntPtr found = IntPtr.Zero;
            Append("🔍 Đang quét các cửa sổ con...");

            EnumChildWindows(parent, (child, l) =>
            {
                var sb = new StringBuilder(256);
                GetClassName(child, sb, sb.Capacity);
                string cls = sb.ToString();
                Append($"🧩 Child class: {cls}");

                // các class có thể chứa khung Flash
                if (cls.Contains("ShockwaveFlash", StringComparison.OrdinalIgnoreCase) ||
                    cls.Contains("MacromediaFlashPlayerActiveX", StringComparison.OrdinalIgnoreCase) ||
                    cls.Contains("Flash", StringComparison.OrdinalIgnoreCase) ||
                    cls.Contains("IEFrame", StringComparison.OrdinalIgnoreCase) ||
                    cls.Contains("Internet Explorer_Server", StringComparison.OrdinalIgnoreCase))
                {
                    Append($"✅ Found Flash-like class: {cls} (0x{child.ToInt64():X})");
                    found = child;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            if (found == IntPtr.Zero)
                Append("⚠️ Không tìm thấy class Flash phù hợp trong danh sách trên.");

            return found;
        }
    }
}
