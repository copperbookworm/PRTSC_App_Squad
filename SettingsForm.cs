using System.Drawing;
using System.Windows.Forms;

namespace SquadShot;

internal sealed class SettingsForm : Form
{
    private static readonly Color BgColor = Color.FromArgb(24, 26, 30);
    private static readonly Color FieldColor = Color.FromArgb(44, 48, 56);
    private static readonly Color TextColor = Color.FromArgb(232, 234, 238);
    private static readonly Color MutedColor = Color.FromArgb(148, 155, 166);
    private static readonly Color AccentColor = Color.FromArgb(88, 166, 255);
    private static readonly Font BodyFont = new("Microsoft YaHei UI", 9.5f);
    private static readonly Font SmallFont = new("Microsoft YaHei UI", 8.5f);
    private static readonly Font TitleFont = new("Microsoft YaHei UI", 14f, FontStyle.Bold);
    private static readonly Icon WindowIcon = AppIcon.Create();

    private readonly AppConfig _config;
    private HotkeyBinding _hotkey;

    private readonly TextBox _hotkeyBox = new();
    private readonly TextBox _folderBox = new();
    private readonly ComboBox _formatBox = new();
    private static readonly int[] JpegQualities = { 95, 85, 75 };
    private readonly ComboBox _modeBox = new();
    private readonly CheckBox _mapBox = new();
    private readonly CheckBox _clipboardBox = new();
    private readonly CheckBox _soundBox = new();
    private readonly CheckBox _toastBox = new();
    private readonly CheckBox _startupBox = new();

    public SettingsForm(AppConfig config)
    {
        _config = config.Clone();
        _hotkey = HotkeyBinding.TryParse(_config.Hotkey, out HotkeyBinding parsed, out _) ? parsed : HotkeyBinding.Default;
        BuildUi();
        LoadValues();
    }

    public AppConfig? Result { get; private set; }

    private void BuildUi()
    {
        SuspendLayout();
        Text = "SquadShot 设置";
        Icon = WindowIcon;
        ClientSize = new Size(600, 648);
        BackColor = BgColor;
        ForeColor = TextColor;
        Font = BodyFont;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;

        Controls.Add(MakeLabel("Squad 截图设置", 24, 18, TitleFont, TextColor));
        Controls.Add(MakeLabel("全局热键一键截图 · 自动保存 · 托盘常驻", 26, 56, SmallFont, MutedColor));

        Controls.Add(MakeLabel("1. 截图热键", 24, 96, BodyFont, TextColor));
        _hotkeyBox.Location = new Point(24, 124);
        _hotkeyBox.Size = new Size(280, 28);
        StyleTextBox(_hotkeyBox);
        _hotkeyBox.TextAlign = HorizontalAlignment.Center;
        _hotkeyBox.ShortcutsEnabled = false;
        _hotkeyBox.KeyDown += OnHotkeyKeyDown;
        Controls.Add(_hotkeyBox);
        Controls.Add(MakeLabel("点一下输入框，再按想要的组合键", 320, 130, SmallFont, MutedColor));
        Controls.Add(MakeLabel("默认 PrtSc；若被其它程序占用，可改成 Ctrl + Alt + F10 之类。", 24, 160, SmallFont, MutedColor));

        Controls.Add(MakeLabel("2. 保存位置", 24, 198, BodyFont, TextColor));
        _folderBox.Location = new Point(24, 226);
        _folderBox.Size = new Size(440, 28);
        StyleTextBox(_folderBox);
        Controls.Add(_folderBox);
        Button browseButton = MakeButton("浏览…", false);
        browseButton.Location = new Point(476, 225);
        browseButton.Size = new Size(100, 30);
        browseButton.Click += OnBrowseClick;
        Controls.Add(browseButton);

        Controls.Add(MakeLabel("3. 图片格式与截图范围", 24, 274, BodyFont, TextColor));
        _formatBox.Location = new Point(24, 302);
        _formatBox.Size = new Size(196, 28);
        StyleComboBox(_formatBox);
        _formatBox.Items.AddRange(new object[] { "PNG（无损，推荐）", "JPG（高质量）", "JPG（标准）", "JPG（小体积）" });
        Controls.Add(_formatBox);

        Controls.Add(MakeLabel("截图范围", 240, 306, SmallFont, MutedColor));
        _modeBox.Location = new Point(300, 302);
        _modeBox.Size = new Size(276, 28);
        StyleComboBox(_modeBox);
        _modeBox.Items.AddRange(new object[] { "整个显示器", "仅游戏窗口" });
        Controls.Add(_modeBox);

        SetupCheckBox(_mapBox, "文件名附加地图/层名（读取 Squad 日志，如 Gorodok_RAAS_v1）", 24, 352);
        SetupCheckBox(_clipboardBox, "截图后复制到剪贴板", 24, 386);
        SetupCheckBox(_soundBox, "播放提示音", 24, 420);
        SetupCheckBox(_toastBox, "显示右下角缩略图提示", 24, 454);
        SetupCheckBox(_startupBox, "开机自动启动", 24, 488);

        Controls.Add(MakeLabel("配置：%APPDATA%\\SquadShot\\config.json", 24, 524, SmallFont, MutedColor));
        Controls.Add(MakeLabel("日志：%APPDATA%\\SquadShot\\logs", 24, 544, SmallFont, MutedColor));

        Button testButton = MakeButton("立即截图测试", false);
        testButton.Location = new Point(24, 580);
        testButton.Size = new Size(150, 38);
        testButton.Click += OnTestClick;
        Controls.Add(testButton);

        Button cancelButton = MakeButton("取消", false);
        cancelButton.Location = new Point(474, 580);
        cancelButton.Size = new Size(100, 38);
        cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(cancelButton);

        Button saveButton = MakeButton("保存", true);
        saveButton.Location = new Point(360, 580);
        saveButton.Size = new Size(100, 38);
        saveButton.Click += OnSaveClick;
        Controls.Add(saveButton);

        ResumeLayout(false);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.UseDarkTitleBar(Handle);
    }

    private void LoadValues()
    {
        _hotkeyBox.Text = _hotkey.Display;
        _folderBox.Text = _config.EffectiveSaveDirectory;
        _formatBox.SelectedIndex = _config.IsJpeg ? QualityIndexFor(_config.JpegQuality) : 0;
        _modeBox.SelectedIndex = string.Equals(_config.CaptureMode, "window", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _mapBox.Checked = _config.IncludeMapName;
        _clipboardBox.Checked = _config.CopyToClipboard;
        _soundBox.Checked = _config.PlaySound;
        _toastBox.Checked = _config.ShowToast;
        _startupBox.Checked = AutoStart.IsEnabled() || _config.AutoStart;
    }

    private static int QualityIndexFor(int quality)
    {
        int best = 0;
        int bestDelta = int.MaxValue;
        for (int i = 0; i < JpegQualities.Length; i++)
        {
            int delta = Math.Abs(JpegQualities[i] - quality);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = i;
            }
        }
        return best + 1;
    }

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        Keys key = e.KeyCode;
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin or Keys.None) return;

        uint modifiers = 0;
        if ((ModifierKeys & Keys.Control) != 0) modifiers |= Native.MOD_CONTROL;
        if ((ModifierKeys & Keys.Alt) != 0) modifiers |= Native.MOD_ALT;
        if ((ModifierKeys & Keys.Shift) != 0) modifiers |= Native.MOD_SHIFT;

        _hotkey = new HotkeyBinding(modifiers, key);
        _hotkeyBox.Text = _hotkey.Display;
    }

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择截图保存目录",
            SelectedPath = Directory.Exists(_folderBox.Text) ? _folderBox.Text : _config.EffectiveSaveDirectory,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folderBox.Text = dialog.SelectedPath;
        }
    }

    private AppConfig BuildCurrentConfig()
    {
        AppConfig config = _config.Clone();
        config.SaveDirectory = _folderBox.Text.Trim();
        int formatIndex = Math.Max(0, _formatBox.SelectedIndex);
        config.ImageFormat = formatIndex > 0 ? "jpg" : "png";
        config.JpegQuality = formatIndex > 0
            ? JpegQualities[Math.Min(formatIndex, JpegQualities.Length) - 1]
            : config.JpegQuality;
        config.CaptureMode = _modeBox.SelectedIndex == 1 ? "window" : "monitor";
        config.IncludeMapName = _mapBox.Checked;
        config.CopyToClipboard = _clipboardBox.Checked;
        config.PlaySound = _soundBox.Checked;
        config.ShowToast = _toastBox.Checked;
        config.AutoStart = _startupBox.Checked;
        config.Hotkey = _hotkey.Display;
        return config;
    }

    private void OnTestClick(object? sender, EventArgs e)
    {
        try
        {
            using CaptureResult result = CaptureService.Capture(BuildCurrentConfig());
            if (!result.Success)
            {
                MessageBox.Show(this, "截图失败：" + (result.Error ?? "未知错误"), "SquadShot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show(
                this,
                "已保存：" + result.FilePath + Environment.NewLine +
                "尺寸：" + result.Width + " × " + result.Height + Environment.NewLine +
                "来源：" + result.Source + Environment.NewLine +
                "地图：" + (result.MapName ?? "未识别") + Environment.NewLine +
                "耗时：" + result.ElapsedMs + " ms",
                "SquadShot 截图测试",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("设置窗测试截图失败", ex);
            MessageBox.Show(this, "截图失败：" + ex.Message, "SquadShot", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        string folder = _folderBox.Text.Trim();
        if (folder.Length == 0)
        {
            MessageBox.Show(this, "请填写截图保存目录。", "SquadShot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!HotkeyBinding.TryParse(_hotkey.Display, out HotkeyBinding binding, out string? error))
        {
            MessageBox.Show(this, error ?? "热键无效。", "SquadShot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(folder));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "无法创建保存目录：" + ex.Message, "SquadShot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        AppConfig config = BuildCurrentConfig();
        config.Hotkey = binding.Display;
        Result = config;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SetupCheckBox(CheckBox box, string text, int x, int y)
    {
        box.Text = text;
        box.AutoSize = true;
        box.ForeColor = TextColor;
        box.BackColor = BgColor;
        box.FlatStyle = FlatStyle.Flat;
        box.Font = BodyFont;
        box.Location = new Point(x, y);
        Controls.Add(box);
    }

    private Label MakeLabel(string text, int x, int y, Font font, Color color) => new()
    {
        Text = text,
        AutoSize = true,
        Font = font,
        ForeColor = color,
        BackColor = Color.Transparent,
        Location = new Point(x, y)
    };

    private Button MakeButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Font = BodyFont,
            BackColor = primary ? AccentColor : FieldColor,
            ForeColor = primary ? Color.FromArgb(12, 18, 28) : TextColor,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = primary ? AccentColor : Color.FromArgb(70, 76, 88);
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(120, 186, 255) : Color.FromArgb(58, 63, 74);
        return button;
    }

    private static void StyleTextBox(TextBox box)
    {
        box.BackColor = FieldColor;
        box.ForeColor = TextColor;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = BodyFont;
    }

    private static void StyleComboBox(ComboBox box)
    {
        box.FlatStyle = FlatStyle.Flat;
        box.BackColor = FieldColor;
        box.ForeColor = TextColor;
        box.Font = BodyFont;
        box.DropDownStyle = ComboBoxStyle.DropDownList;
    }
}
