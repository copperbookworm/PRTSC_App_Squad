using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SquadShot;

internal sealed class HotkeyRegisterException : Exception
{
    public HotkeyRegisterException(HotkeyBinding binding, int win32Error)
        : base("注册热键 " + binding.Display + " 失败（Win32 错误 " + win32Error + "），可能已被其它程序占用。")
    {
        Binding = binding;
        Win32Error = win32Error;
    }

    public HotkeyBinding Binding { get; }

    public int Win32Error { get; }
}

internal sealed class HotkeyManager : IDisposable
{
    private const int HotkeyId = 0x5753;

    private readonly MessageWindow _window;
    private bool _registered;
    private bool _disposed;

    public event EventHandler? Triggered;

    public HotkeyManager()
    {
        _window = new MessageWindow();
        _window.HotkeyPressed += (_, _) => Triggered?.Invoke(this, EventArgs.Empty);

        // Force the handle to exist so RegisterHotKey has a target. The form is never shown.
        _ = _window.Handle;
    }

    public bool IsRegistered => _registered;

    public void Register(HotkeyBinding binding)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Unregister();

        uint modifiers = binding.Modifiers | Native.MOD_NOREPEAT;
        if (!Native.RegisterHotKey(_window.Handle, HotkeyId, modifiers, (uint)binding.Key))
        {
            throw new HotkeyRegisterException(binding, Marshal.GetLastWin32Error());
        }
        _registered = true;
        Logger.Info("已注册热键 " + binding.Display);
    }

    public void Unregister()
    {
        if (!_registered) return;
        Native.UnregisterHotKey(_window.Handle, HotkeyId);
        _registered = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Unregister();
        _window.Dispose();
    }

    private sealed class MessageWindow : Form
    {
        public MessageWindow()
        {
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-4000, -4000);
            Size = new Size(1, 1);
        }

        public event EventHandler? HotkeyPressed;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                return;
            }
            base.WndProc(ref m);
        }
    }
}
