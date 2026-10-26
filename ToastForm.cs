using System.Drawing;
using System.Windows.Forms;

namespace SquadShot;

/// <summary>Borderless, never-activated preview shown in the corner of the screen that was captured.</summary>
internal sealed class ToastForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    private readonly Bitmap _preview;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly int _holdMilliseconds;
    private int _elapsed;
    private bool _fading;

    public ToastForm(Bitmap preview, string headline, string fileName, string meta, Rectangle anchor, int seconds)
    {
        _preview = preview;
        _holdMilliseconds = Math.Clamp(seconds, 1, 15) * 1000;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(22, 24, 28);
        DoubleBuffered = true;

        const int padding = 12;
        const int textWidth = 210;
        int contentHeight = Math.Max(preview.Height, 76);

        var picture = new PictureBox
        {
            Image = preview,
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(padding, padding),
            Size = preview.Size,
            BackColor = Color.FromArgb(12, 13, 16)
        };

        int textLeft = padding + preview.Width + padding;

        var headlineLabel = new Label
        {
            Text = headline,
            AutoSize = false,
            ForeColor = Color.FromArgb(136, 196, 255),
            Font = new Font("Microsoft YaHei UI", 10f, FontStyle.Bold),
            Location = new Point(textLeft, padding + 4),
            Size = new Size(textWidth, 24)
        };

        var fileLabel = new Label
        {
            Text = fileName,
            AutoSize = false,
            ForeColor = Color.FromArgb(236, 238, 242),
            Font = new Font("Microsoft YaHei UI", 8.5f),
            Location = new Point(textLeft, padding + 30),
            Size = new Size(textWidth, 20),
            AutoEllipsis = true
        };

        var metaLabel = new Label
        {
            Text = meta,
            AutoSize = false,
            ForeColor = Color.FromArgb(152, 158, 168),
            Font = new Font("Microsoft YaHei UI", 8.5f),
            Location = new Point(textLeft, padding + 56),
            Size = new Size(textWidth, 42)
        };

        Controls.Add(picture);
        Controls.Add(headlineLabel);
        Controls.Add(fileLabel);
        Controls.Add(metaLabel);

        ClientSize = new Size(
            padding + preview.Width + padding + textWidth + padding,
            Math.Max(contentHeight + padding * 2, 100));

        Rectangle screen = anchor.Width > 0 ? anchor : Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(
            Math.Max(screen.Left + 8, screen.Right - Width - 24),
            Math.Max(screen.Top + 8, screen.Bottom - Height - 56));

        Opacity = 1.0;
        _timer = new System.Windows.Forms.Timer { Interval = 40 };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= WsExNoActivate | WsExToolWindow;
            return parameters;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Color.FromArgb(60, 88, 166, 255), 1f);
        Rectangle border = new(0, 0, Width - 1, Height - 1);
        e.Graphics.DrawRectangle(pen, border);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
            foreach (Control control in Controls)
            {
                if (control is PictureBox box) box.Image = null;
            }
            _preview.Dispose();
        }
        base.Dispose(disposing);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _elapsed += 40;
        if (!_fading && _elapsed >= _holdMilliseconds) _fading = true;
        if (!_fading) return;

        Opacity -= 0.12;
        if (Opacity <= 0.02)
        {
            _timer.Stop();
            Close();
        }
    }
}
