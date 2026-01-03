using DiscordAudioGuardTray.Config;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DiscordAudioGuardTray.Forms
{
    public class SettingsForm : Form
    {
        private NumericUpDown numCheckInterval = null!;
        private CheckBox chkShowNotifications = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private Button btnRefresh = null!;
        private FlowLayoutPanel devicesPanel = null!;
        private Label lblCheckInterval = null!;
        private Label lblDevices = null!;
        private Panel headerPanel = null!;

        private MainForm _mainForm;
        private List<DeviceInfo> _availableDevices = new List<DeviceInfo>();

        public SettingsForm(MainForm mainForm)
        {
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            InitializeComponent();
            LoadAvailableDevices();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            // Create controls
            lblCheckInterval = new Label();
            numCheckInterval = new NumericUpDown();
            chkShowNotifications = new CheckBox();
            lblDevices = new Label();
            devicesPanel = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            btnRefresh = new Button();
            headerPanel = new Panel();

            ((System.ComponentModel.ISupportInitialize)numCheckInterval).BeginInit();
            SuspendLayout();

            // Header
            headerPanel.BackColor = Color.FromArgb(88, 101, 242); // Discord blurple color
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 50;

            var titleLabel = new Label();
            titleLabel.Text = "Discord Audio Guard Settings";
            titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            headerPanel.Controls.Add(titleLabel);

            // lblCheckInterval
            lblCheckInterval.AutoSize = true;
            lblCheckInterval.Location = new Point(20, 70);
            lblCheckInterval.Name = "lblCheckInterval";
            lblCheckInterval.Size = new Size(140, 15);
            lblCheckInterval.TabIndex = 0;
            lblCheckInterval.Text = "Check interval (ms):";

            // numCheckInterval
            numCheckInterval.Location = new Point(170, 68);
            numCheckInterval.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numCheckInterval.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numCheckInterval.Name = "numCheckInterval";
            numCheckInterval.Size = new Size(120, 23);
            numCheckInterval.TabIndex = 1;
            numCheckInterval.Value = new decimal(new int[] { 1000, 0, 0, 0 });

            // chkShowNotifications
            chkShowNotifications.AutoSize = true;
            chkShowNotifications.Location = new Point(20, 100);
            chkShowNotifications.Name = "chkShowNotifications";
            chkShowNotifications.Size = new Size(195, 19);
            chkShowNotifications.TabIndex = 2;
            chkShowNotifications.Text = "Show system notifications";
            chkShowNotifications.UseVisualStyleBackColor = true;

            // lblDevices
            lblDevices.AutoSize = true;
            lblDevices.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDevices.Location = new Point(20, 130);
            lblDevices.Name = "lblDevices";
            lblDevices.Size = new Size(276, 19);
            lblDevices.TabIndex = 3;
            lblDevices.Text = "Select devices to ignore (unmute Discord):";

            // devicesPanel
            devicesPanel.AutoScroll = true;
            devicesPanel.BackColor = SystemColors.ControlLight;
            devicesPanel.BorderStyle = BorderStyle.FixedSingle;
            devicesPanel.Location = new Point(20, 155);
            devicesPanel.Name = "devicesPanel";
            devicesPanel.Size = new Size(440, 200);
            devicesPanel.TabIndex = 4;
            devicesPanel.FlowDirection = FlowDirection.TopDown;
            devicesPanel.WrapContents = false;

            // btnRefresh
            btnRefresh.Location = new Point(310, 365);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(70, 30);
            btnRefresh.TabIndex = 5;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += BtnRefresh_Click;

            // btnSave
            btnSave.BackColor = Color.FromArgb(88, 101, 242);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(385, 365);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 30);
            btnSave.TabIndex = 6;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = false;
            btnSave.Click += BtnSave_Click;

            // btnCancel
            btnCancel.Location = new Point(20, 365);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 30);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;

            // SettingsForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(480, 410);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(btnRefresh);
            Controls.Add(devicesPanel);
            Controls.Add(lblDevices);
            Controls.Add(chkShowNotifications);
            Controls.Add(numCheckInterval);
            Controls.Add(lblCheckInterval);
            Controls.Add(headerPanel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Settings";

            ((System.ComponentModel.ISupportInitialize)numCheckInterval).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void LoadAvailableDevices()
        {
            _availableDevices.Clear();

            // NULL HANDLING: check devicesPanel
            if (devicesPanel == null)
            {
                devicesPanel = new FlowLayoutPanel();
            }

            devicesPanel.Controls.Clear();

            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    // NULL HANDLING: check result
                    var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                    if (devices == null)
                    {
                        MessageBox.Show("Failed to get audio device list", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    foreach (var device in devices)
                    {
                        var deviceInfo = new DeviceInfo
                        {
                            Id = device?.ID ?? "unknown",
                            FriendlyName = device?.FriendlyName ?? "Unknown Device",
                            State = device?.State.ToString() ?? "Unknown"
                        };
                        _availableDevices.Add(deviceInfo);
                    }
                }

                // Sort alphabetically
                _availableDevices = _availableDevices.OrderBy(d => d.FriendlyName).ToList();

                // Create checkboxes for each device
                foreach (var device in _availableDevices)
                {
                    var checkBox = new CheckBox
                    {
                        Text = $"{device.FriendlyName} ({device.State})",
                        Tag = device.FriendlyName,
                        AutoSize = true,
                        Margin = new Padding(5, 3, 0, 3)
                    };

                    // Set width only if panel is initialized
                    if (devicesPanel != null)
                    {
                        checkBox.Width = devicesPanel.Width - 25;
                        devicesPanel.Controls.Add(checkBox);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading devices: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSettings()
        {
            var config = ConfigManager.Config;

            // Check interval
            numCheckInterval.Value = config.CheckIntervalMs;

            // Notifications
            chkShowNotifications.Checked = config.ShowNotifications;

            // Mark selected devices
            var ignoredDevices = config.IgnoredDeviceNames;

            if (devicesPanel != null)
            {
                foreach (Control control in devicesPanel.Controls)
                {
                    if (control is CheckBox checkBox)
                    {
                        checkBox.Checked = ignoredDevices.Contains(checkBox.Tag?.ToString() ?? "");
                    }
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void SaveSettings()
        {
            var oldIgnoredDevices = ConfigManager.Config.IgnoredDeviceNames.ToList();

            ConfigManager.UpdateConfig(config =>
            {
                config.CheckIntervalMs = (int)numCheckInterval.Value;
                config.ShowNotifications = chkShowNotifications.Checked;

                // Update device list
                config.Devices.Clear();

                foreach (var deviceInfo in _availableDevices)
                {
                    var deviceConfig = new DeviceConfig
                    {
                        FriendlyName = deviceInfo.FriendlyName,
                        Id = deviceInfo.Id,
                        Ignored = IsDeviceChecked(deviceInfo.FriendlyName)
                    };
                    config.Devices.Add(deviceConfig);
                }
            });

            // Get new ignored devices
            var newIgnoredDevices = ConfigManager.Config.IgnoredDeviceNames.ToList();

            // Determine which devices changed status
            var becameIgnored = newIgnoredDevices.Except(oldIgnoredDevices).ToList();
            var becameUnignored = oldIgnoredDevices.Except(newIgnoredDevices).ToList();

            // Show information about changes
            string message = "";
            if (becameIgnored.Count > 0)
            {
                message += $"Added to ignore list: {string.Join(", ", becameIgnored)}\n";
            }
            if (becameUnignored.Count > 0)
            {
                message += $"Removed from ignore list: {string.Join(", ", becameUnignored)}";
            }

            // Show tooltip with information about changes
            if (!string.IsNullOrEmpty(message.Trim()))
            {
                _mainForm?.ShowNotification(message.Trim());
            }

            // Notify main form about settings changes
            _mainForm?.OnConfigUpdated();

            MessageBox.Show("Settings saved successfully!", "Discord Audio Guard",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool IsDeviceChecked(string deviceName)
        {
            if (devicesPanel == null) return false;

            foreach (Control control in devicesPanel.Controls)
            {
                if (control is CheckBox checkBox &&
                    checkBox.Tag?.ToString() == deviceName)
                {
                    return checkBox.Checked;
                }
            }
            return false;
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadAvailableDevices();
            LoadSettings(); // Reload saved settings
        }
    }

    // Helper class for device information
    internal class DeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }
}
