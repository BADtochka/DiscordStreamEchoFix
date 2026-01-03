using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordAudioGuardTray
{
  public class NotificationForm : Form
  {
    private Label messageLabel = null!;
    private CancellationTokenSource? _cancellationTokenSource;

    public NotificationForm(string message)
    {
      InitializeComponent();
      messageLabel.Text = message;
      SetPosition();
    }

    private void InitializeComponent()
    {
      messageLabel = new Label();
      this.SuspendLayout();

      // Form settings
      this.FormBorderStyle = FormBorderStyle.None;
      this.StartPosition = FormStartPosition.Manual;
      this.ShowInTaskbar = false;
      this.TopMost = true;
      this.BackColor = Color.FromArgb(88, 101, 242); // Discord blurple color
      this.Opacity = 0.9;
      this.DoubleBuffered = true;

      // messageLabel
      messageLabel.AutoSize = true;
      messageLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
      messageLabel.ForeColor = Color.White;
      messageLabel.BackColor = Color.Transparent;
      messageLabel.Padding = new Padding(15, 8, 15, 8);
      messageLabel.Dock = DockStyle.Fill;
      messageLabel.TextAlign = ContentAlignment.MiddleCenter;

      // Add controls
      this.Controls.Add(messageLabel);

      // Form dimensions
      this.Size = new Size(400, 50);
      this.ResumeLayout(false);
    }

    private void SetPosition()
    {
      // Get screen working area (excluding taskbar)
      Rectangle screen = Screen.PrimaryScreen!.WorkingArea;

      // Position: horizontally centered, 50px from bottom
      this.Location = new Point(
          (screen.Width - this.Width) / 2,
          screen.Height - this.Height - 50
      );
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      // Smooth fade-in animation
      this.Opacity = 0;
      for (double opacity = 0; opacity <= 0.9; opacity += 0.1)
      {
        this.Opacity = opacity;
        Application.DoEvents();
        Thread.Sleep(30);
      }

      // Start timer for auto-close
      _cancellationTokenSource = new CancellationTokenSource();
      var cancellationToken = _cancellationTokenSource.Token;

      Task.Run(async () =>
      {
        await Task.Delay(3000, cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
        {
          this.Invoke(new Action(() =>
                {
                  // Smooth fade-out animation
                  for (double opacity = 0.9; opacity > 0; opacity -= 0.1)
                  {
                    this.Opacity = opacity;
                    Application.DoEvents();
                    Thread.Sleep(30);
                  }
                  this.Close();
                }));
        }
      }, cancellationToken);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
      _cancellationTokenSource?.Cancel();
      base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        _cancellationTokenSource?.Dispose();
      }
      base.Dispose(disposing);
    }
  }
}
