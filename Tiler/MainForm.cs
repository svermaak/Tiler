using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Tiler
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private Timer tileTimer;

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxLength);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SW_RESTORE = 9;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public MainForm()
        {
            InitializeComponent();
            Hide();

            // Set up NotifyIcon
            trayIcon = new NotifyIcon
            {
                Icon = new Icon("windows.ico"), // Replace with your icon path
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true
            };
            trayIcon.ContextMenuStrip.Items.Add("Start Tiling", null, StartTiling);
            trayIcon.ContextMenuStrip.Items.Add("Stop Tiling", null, StopTiling);
            trayIcon.ContextMenuStrip.Items.Add("Set Timer Interval", null, SetTimerInterval);
            trayIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application.Exit());

            // Set up Timer
            tileTimer = new Timer { Interval = 10000 }; // Default interval
            tileTimer.Tick += (s, e) => TileWindows();
        }

        protected override void OnShown(EventArgs e)
        {
            // Hide the form on startup
            base.OnShown(e);
            Hide();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private void StartTiling(object sender, EventArgs e) => tileTimer.Start();

        private void StopTiling(object sender, EventArgs e) => tileTimer.Stop();

        private void SetTimerInterval(object sender, EventArgs e)
        {
            using (Form intervalForm = new Form())
            {
                intervalForm.Text = "Set Timer Interval";
                intervalForm.Size = new Size(250, 150);

                Label label = new Label { Text = "Enter interval (ms):", Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter };
                TextBox inputBox = new TextBox { Dock = DockStyle.Top, TextAlign = HorizontalAlignment.Center };
                Button confirmButton = new Button { Text = "Set", Dock = DockStyle.Top };

                confirmButton.Click += (s, ev) =>
                {
                    if (int.TryParse(inputBox.Text, out int interval) && interval > 0)
                    {
                        tileTimer.Interval = interval;
                        MessageBox.Show($"Interval set to {interval} ms.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        intervalForm.Close();
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid positive number.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                intervalForm.Controls.Add(confirmButton);
                intervalForm.Controls.Add(inputBox);
                intervalForm.Controls.Add(label);

                intervalForm.StartPosition = FormStartPosition.CenterParent;
                intervalForm.ShowDialog();
            }
        }

        public void TileWindows()
        {
            // Get list of visible windows
            List<IntPtr> windows = GetVisibleWindows();

            int count = windows.Count;
            int columns = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((double)count / columns);

            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
            int tileWidth = screenBounds.Width / columns;
            int tileHeight = screenBounds.Height / rows;

            for (int i = 0; i < windows.Count; i++)
            {
                IntPtr windowHandle = windows[i];
                ShowWindow(windowHandle, SW_RESTORE);

                int row = i / columns;
                int column = i % columns;
                int x = column * tileWidth;
                int y = row * tileHeight;

                SetWindowPos(windowHandle, IntPtr.Zero, x, y, tileWidth, tileHeight, SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }

        private List<IntPtr> GetVisibleWindows()
        {
            List<IntPtr> windowList = new List<IntPtr>();
            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder windowText = new StringBuilder(256);
                    GetWindowText(hWnd, windowText, 256);
                    if (windowText.Length > 0)
                    {
                        windowList.Add(hWnd);
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windowList;
        }
    }
}