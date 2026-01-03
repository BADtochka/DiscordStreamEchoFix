using DiscordAudioGuardTray.Config;
using DiscordAudioGuardTray.Forms;
using NAudio.CoreAudioApi;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DiscordAudioGuardTray
{
    public class MainForm : Form
    {
        private NotifyIcon _trayIcon = null!;
        private System.Windows.Forms.Timer _timer = null!;
        private bool _running = true;
        private Thread? _workerThread;
        private ContextMenuStrip _contextMenu = null!;
        private AppConfig _config = null!;
        private Icon _appIcon = null!;

        public MainForm()
        {
            // Load configuration
            _config = ConfigManager.Config;

            // Load application icon
            LoadAppIcon();

            // Initialize form components
            InitializeComponent();

            // Set up tray icon
            InitializeTrayIcon();

            // Set up timer
            InitializeTimer();

            // Hide main window
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Icon = _appIcon; // Set form icon

            // Start worker thread
            _workerThread = new Thread(WorkerThreadProc);
            _workerThread.Start();
        }

        private void LoadAppIcon()
        {
            try
            {
                // Try to load icon from file
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    _appIcon = new Icon(iconPath);
                }
                else
                {
                    // If file not found, use default system icon
                    _appIcon = SystemIcons.Application;
                    Console.WriteLine("app.ico file not found, using default icon.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading icon: {ex.Message}");
                _appIcon = SystemIcons.Application;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Main form settings
            this.ClientSize = new System.Drawing.Size(300, 200);
            this.Name = "MainForm";
            this.Text = "Discord Audio Guard";

            this.ResumeLayout(false);
        }

        private void InitializeTrayIcon()
        {
            _contextMenu = new ContextMenuStrip();

            // Menu item: Settings
            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (sender, e) => OpenSettings();
            _contextMenu.Items.Add(settingsItem);

            // Menu item: Auto-start
            var autoStartItem = new ToolStripMenuItem("Start with Windows");
            autoStartItem.Checked = Program.IsAutoStartEnabled();
            autoStartItem.Click += (sender, e) =>
            {
                bool newState = !autoStartItem.Checked;
                Program.SetAutoStart(newState);
                autoStartItem.Checked = newState;
            };
            _contextMenu.Items.Add(autoStartItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Menu item: Status
            var statusItem = new ToolStripMenuItem($"Interval: {_config.CheckIntervalMs} ms");
            statusItem.Enabled = false;
            _contextMenu.Items.Add(statusItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Menu item: Exit
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (sender, e) => ExitApplication();
            _contextMenu.Items.Add(exitItem);

            // Create tray icon
            _trayIcon = new NotifyIcon
            {
                Icon = _appIcon, // Use loaded icon
                Text = "Discord Audio Guard\nMutes Discord on all devices",
                Visible = true,
                ContextMenuStrip = _contextMenu
            };

            // Double-click icon to open settings
            _trayIcon.DoubleClick += (sender, e) => OpenSettings();
        }

        private void InitializeTimer()
        {
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = _config.CheckIntervalMs;
            _timer.Tick += (sender, e) => UpdateStatus();
            _timer.Start();
        }

        public void OnConfigUpdated()
        {
            // Update configuration
            _config = ConfigManager.Config;

            // Update timer with new interval
            if (_timer != null)
            {
                _timer.Interval = _config.CheckIntervalMs;
            }

            // Immediately apply new settings
            ThreadPool.QueueUserWorkItem(state =>
            {
                MuteDiscordSessions();
            });

            // Update status in menu
            UpdateStatus();
        }

        private void OpenSettings()
        {
            // Open settings form modally
            using (var settingsForm = new SettingsForm(this))
            {
                settingsForm.ShowDialog();
            }
        }

        private void WorkerThreadProc()
        {
            while (_running)
            {
                try
                {
                    MuteDiscordSessions();
                    Thread.Sleep(_config.CheckIntervalMs);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss}: {ex.Message}");
                    Thread.Sleep(5000);
                }
            }
        }

        private void MuteDiscordSessions()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                foreach (var device in devices)
                {
                    // Check if device is ignored
                    bool isIgnored = _config.IgnoredDeviceNames.Contains(device.FriendlyName);

                    var sessionManager = device.AudioSessionManager;
                    if (sessionManager == null) continue;

                    for (int i = 0; i < sessionManager.Sessions.Count; i++)
                    {
                        var session = sessionManager.Sessions[i];
                        uint pid = session.GetProcessID;

                        if (pid == 0) continue;

                        string processName = "";
                        try
                        {
                            processName = Process.GetProcessById((int)pid).ProcessName;
                        }
                        catch (ArgumentException)
                        {
                            continue;
                        }

                        if (processName.Equals("Discord", StringComparison.OrdinalIgnoreCase))
                        {
                            var volume = session.SimpleAudioVolume;
                            if (volume == null) continue;

                            if (isIgnored)
                            {
                                // If device is ignored - unmute
                                if (volume.Mute)
                                {
                                    volume.Mute = false;
                                    volume.Volume = 1.0f; // Restore volume

                                    if (_config.ShowNotifications)
                                    {
                                        ShowNotification($"Restored Discord audio on device: {device.FriendlyName}");
                                    }
                                }
                            }
                            else
                            {
                                // If device is not ignored - mute
                                if (!volume.Mute)
                                {
                                    volume.Mute = true;

                                    if (_config.ShowNotifications)
                                    {
                                        ShowNotification($"Muted Discord on device: {device.FriendlyName}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Find this method in MainForm.cs and ensure it's PUBLIC:
        public void ShowNotification(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowNotification(message)));
                return;
            }

            var notification = new NotificationForm(message);
            notification.Show();
        }
        private void UpdateStatus()
        {
            if (_contextMenu.InvokeRequired)
            {
                _contextMenu.Invoke(new Action(UpdateStatus));
                return;
            }

            // Update status menu item
            if (_contextMenu.Items.Count > 3)
            {
                var statusItem = _contextMenu.Items[3] as ToolStripMenuItem;
                if (statusItem != null)
                {
                    // Use the correct property
                    statusItem.Text = $"Interval: {_config.CheckIntervalMs} ms | Ignored devices: {_config.IgnoredDeviceNames.Count}";
                }
            }
        }
        private void ExitApplication()
        {
            _running = false;

            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(2000);
            }

            _timer?.Stop();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            // Release icon
            _appIcon?.Dispose();

            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }
            base.OnFormClosing(e);
        }
    }
}
