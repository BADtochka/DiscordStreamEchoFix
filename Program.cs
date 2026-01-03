using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DiscordAudioGuardTray
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const string AppName = "DiscordAudioGuard";

        [STAThread]
        static void Main(string[] args)
        {
            // Hide console window (if exists)
            HideConsoleWindow();

            // Check if we need to add/remove from autostart
            if (args.Length > 0 && args[0] == "--setup-autostart")
            {
                SetAutoStart(true);
                return;
            }
            else if (args.Length > 0 && args[0] == "--remove-autostart")
            {
                SetAutoStart(false);
                return;
            }

            // Launch main form
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }

        private static void HideConsoleWindow()
        {
            var consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_HIDE);
            }
        }

        public static void SetAutoStart(bool enable)
        {
            string appPath = Application.ExecutablePath ?? string.Empty;
            RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (enable)
            {
                registryKey?.SetValue(AppName, $"\"{appPath}\"");
                MessageBox.Show("Autostart enabled!", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                registryKey?.DeleteValue(AppName, false);
                MessageBox.Show("Autostart disabled!", AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            registryKey?.Close();
        }

        public static bool IsAutoStartEnabled()
        {
            RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            var value = registryKey?.GetValue(AppName);
            registryKey?.Close();
            return value != null;
        }
    }
}
