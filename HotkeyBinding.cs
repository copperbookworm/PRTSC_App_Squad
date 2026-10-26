using System.Windows.Forms;

namespace SquadShot;

internal sealed record HotkeyBinding(uint Modifiers, Keys Key)
{
    public static HotkeyBinding Default { get; } = new(0, Keys.PrintScreen);

    public string Display => Format(Modifiers, Key);

    public static string Format(uint modifiers, Keys key)
    {
        var parts = new List<string>();
        if ((modifiers & Native.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & Native.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & Native.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & Native.MOD_WIN) != 0) parts.Add("Win");
        parts.Add(KeyLabel(key));
        return string.Join(" + ", parts);
    }

    public static string KeyLabel(Keys key)
    {
        int code = (int)key;
        if (code >= (int)Keys.D0 && code <= (int)Keys.D9)
        {
            return ((char)('0' + (code - (int)Keys.D0))).ToString();
        }
        return key.ToString();
    }

    public static bool TryParse(string? text, out HotkeyBinding binding, out string? error)
    {
        binding = Default;
        error = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "热键不能为空";
            return false;
        }

        uint modifiers = 0;
        Keys key = Keys.None;
        string[] tokens = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string raw in tokens)
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= Native.MOD_CONTROL;
                    continue;
                case "alt":
                    modifiers |= Native.MOD_ALT;
                    continue;
                case "shift":
                    modifiers |= Native.MOD_SHIFT;
                    continue;
                case "win":
                case "windows":
                    modifiers |= Native.MOD_WIN;
                    continue;
            }

            if (!TryParseKey(raw, out key))
            {
                error = "无法识别的按键：" + raw;
                return false;
            }
        }

        if (key == Keys.None)
        {
            error = "热键里缺少主键（例如 PrintScreen、F10）";
            return false;
        }

        binding = new HotkeyBinding(modifiers, key);
        return true;
    }

    private static bool TryParseKey(string token, out Keys key)
    {
        string lower = token.ToLowerInvariant();
        switch (lower)
        {
            case "prtsc":
            case "prtscr":
                key = Keys.PrintScreen;
                return true;
            case "esc":
                key = Keys.Escape;
                return true;
            case "space":
                key = Keys.Space;
                return true;
            case "enter":
            case "return":
                key = Keys.Enter;
                return true;
            case "del":
            case "delete":
                key = Keys.Delete;
                return true;
            case "ins":
            case "insert":
                key = Keys.Insert;
                return true;
            case "pause":
            case "break":
                key = Keys.Pause;
                return true;
        }

        if (token.Length == 1)
        {
            char c = char.ToUpperInvariant(token[0]);
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
            {
                key = (Keys)c;
                return true;
            }
        }

        if (Enum.TryParse(token, true, out key) && key != Keys.None)
        {
            return true;
        }

        key = Keys.None;
        return false;
    }
}
