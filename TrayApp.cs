using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SquadShot;

internal sealed class TrayApp : ApplicationContext
{
    private readonly HotkeyManager _hotkeys = new();
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _captureItem = new();
    private readonly ToolStripMenuItem _folderItem = new();
    private readonly ToolStripMenuItem _settingsItem = new();
    private readonly ToolStripMenuItem _autoStartItem = new();
    private readonly ToolStripMenuItem _hotkeyItem = new();
    private readonly System.Windows.Forms.Timer _startupTimer = new() { Interval = 200 };

    private AppConfig _config;
    private HotkeyBinding? _registeredHotkey;
    private bool _capturing;

    public TrayApp(AppConfig config, bool openSettings)
    {
        _config = config;

        _tray = new NotifyIcon
        {
            Icon = AppIcon.Create(),
            Visible = true,
            Text = "SquadShot 截图"
        };

        _hotkeyItem.Enabled = false;
        _captureItem.Click += (_, _) => CaptureNow();
        _folderItem.Click += (_, _) => OpenFolder();
        _settingsItem.Click += (_, _) => OpenSettings();
        _autoStartItem.Click += (_, _) => ToggleAutoStart();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_hotkeyItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_captureItem);
        menu.Items.Add(_folderItem);
        menu.Items.Add(_settingsItem);
        menu.Items.Add(_autoStartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApp());

        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => CaptureNow();
        _tray.BalloonTipClicked += (_, _) => OpenFolder();

        _hotkeys.Triggered += (_, _) => CaptureNow();
        ApplyHotkeyOrWarn();
        SynchronizeAutoStart();
        UpdateMenuText();

        if (openSettings)
        {
            _startupTimer.Tick += (_, _) =>
            {
                _startupTimer.Stop();
                _startupTimer.Dispose();
                OpenSettings();
            };
            _startupTimer.Start();
        }

        Logger.Info("SquadShot 已启动");
    }

    private void CaptureNow()
    {
        if (_capturing) return;
        _capturing = true;
        try
        {
            using CaptureResult result = CaptureService.Capture(_config);
            CliReport.Write(result);
            if (!result.Success)
            {
                _tray.ShowBalloonTip(4000, "截图失败", result.Error ?? "未知错误", ToolTipIcon.Error);
                return;
            }

            if (_config.CopyToClipboard && result.FilePath != null)
            {
                CopyImageToClipboard(result.FilePath);
            }

            if (_config.PlaySound)
            {
                SystemSounds.Asterisk.Play();
            }

            if (_config.ShowToast && result.Preview != null && result.FilePath != null)
            {
                string fileName = Path.GetFileName(result.FilePath);
                if (fileName.StartsWith("Squad_", StringComparison.Ordinal)) fileName = fileName["Squad_".Length..];
                string meta = (result.MapName ?? "地图未识别") + Environment.NewLine +
                              result.Width + " × " + result.Height + " · " + result.ElapsedMs + " ms";
                var toast = new ToastForm((Bitmap)result.Preview.Clone(), "截图已保存", fileName, meta, result.Bounds, _config.ToastSeconds);
                toast.Show();
            }
        }
        catch (Exception ex)
        {
            Logger.Error("截图流程异常", ex);
            _tray.ShowBalloonTip(4000, "截图失败", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            _capturing = false;
        }
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_config);
        if (dialog.ShowDialog() != DialogResult.OK || dialog.Result is not AppConfig updated) return;

        if (!TryRegisterHotkey(updated.Hotkey, out string? error))
        {
            MessageBox.Show(error ?? "热键注册失败。", "SquadShot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _config = updated;
        _config.Save();
        AutoStart.Set(_config.AutoStart);
        SynchronizeAutoStart();
        UpdateMenuText();
        Logger.Info("设置已保存");
    }

    private void ToggleAutoStart()
    {
        bool enabled = !AutoStart.IsEnabled();
        AutoStart.Set(enabled);
        _config.AutoStart = enabled;
        _config.Save();
        SynchronizeAutoStart();
    }

    private void SynchronizeAutoStart()
    {
        _autoStartItem.Checked = AutoStart.IsEnabled();
    }

    private void UpdateMenuText()
    {
        string hotkey = _registeredHotkey?.Display ?? "无（请在设置里指定）";
        _hotkeyItem.Text = "当前热键：" + hotkey;
        _captureItem.Text = "立即截图    " + hotkey;
        _folderItem.Text = "打开截图文件夹";
        _settingsItem.Text = "设置…";
        _autoStartItem.Text = "开机自动启动";
        _tray.Text = ("SquadShot — " + hotkey + " 截图").Length > 62
            ? "SquadShot 截图工具"
            : "SquadShot — " + hotkey + " 截图";
    }

    private void ApplyHotkeyOrWarn()
    {
        if (TryRegisterHotkey(_config.Hotkey, out string? error)) return;

        _registeredHotkey = null;
        Logger.Warn("热键注册失败：" + error);
        _tray.ShowBalloonTip(
            6000,
            "热键未注册",
            (error ?? "热键注册失败") + Environment.NewLine + "请从托盘菜单打开设置，换一个组合键。",
            ToolTipIcon.Warning);
    }

    private bool TryRegisterHotkey(string hotkey, out string? error)
    {
        error = null;
        if (!HotkeyBinding.TryParse(hotkey, out HotkeyBinding binding, out string? parseError))
        {
            error = parseError ?? "热键无效。";
            return false;
        }

        if (_registeredHotkey != null && _registeredHotkey.Key == binding.Key && _registeredHotkey.Modifiers == binding.Modifiers)
        {
            return true;
        }

        try
        {
            _hotkeys.Register(binding);
            _registeredHotkey = binding;
            return true;
        }
        catch (HotkeyRegisterException ex)
        {
            error = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            error = "热键注册失败：" + ex.Message;
            return false;
        }
    }

    private void OpenFolder()
    {
        try
        {
            string directory = _config.EffectiveSaveDirectory;
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + directory + "\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Logger.Error("打开截图文件夹失败", ex);
            _tray.ShowBalloonTip(4000, "无法打开文件夹", ex.Message, ToolTipIcon.Error);
        }
    }

    private static void CopyImageToClipboard(string path)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using var image = new Bitmap(path);
                Clipboard.SetImage(image);
                return;
            }
            catch (ExternalException)
            {
                Thread.Sleep(60);
            }
        }
        Logger.Warn("复制到剪贴板失败");
    }

    private void ExitApp()
    {
        Logger.Info("SquadShot 退出");
        _hotkeys.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _startupTimer.Dispose();
            _hotkeys.Dispose();
            _tray.Dispose();
        }
        base.Dispose(disposing);
    }
}
