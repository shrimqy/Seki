using Sefirah.App.Data.Enums;
using Windows.System;

namespace Sefirah.App.Utils;

public static class KeyMapper
{
    private static readonly Dictionary<int, string> SpecialCharacterMap = new()
    {
        { 186, ";" },
        { 187, "=" },
        { 188, "," },
        { 189, "-" },
        { 190, "." },
        { 191, "/" },
        { 192, "`" },
        { 219, "[" },
        { 220, "\\" },
        { 221, "]" },
        { 222, "'" }
    };

    private static readonly Dictionary<int, string> ShiftedSpecialCharacterMap = new()
    {
        { 186, ":" },
        { 187, "+" },
        { 188, "<" },
        { 189, "_" },
        { 190, ">" },
        { 191, "?" },
        { 192, "~" },
        { 219, "{" },
        { 220, "|" },
        { 221, "}" },
        { 222, "\"" }
    };

    public static (string keyText, bool isAction) MapWindowsKeyToTextOrAction(VirtualKey windowsKey, int originalKey, bool shiftPressed = false, bool ctrlPressed = false, bool altPressed = false)
    {
        // Handle special characters using OriginalKey
        if (IsSpecialCharacter(originalKey))
        {
            return (MapSpecialCharacter(originalKey, shiftPressed), false);
        }

        // Handle Ctrl key combinations for common actions
        if (ctrlPressed)
        {
            return windowsKey switch
            {
                VirtualKey.C => (nameof(KeyboardActionType.CtrlC), true),
                VirtualKey.V => (nameof(KeyboardActionType.CtrlV), true),
                VirtualKey.X => (nameof(KeyboardActionType.CtrlX), true),
                VirtualKey.A => (nameof(KeyboardActionType.CtrlA), true),
                _ => ("", false)
            };
        }

        if (windowsKey >= VirtualKey.A && windowsKey <= VirtualKey.Z)
        {
            return (shiftPressed
                ? MapAlphabetKeyWithShift(windowsKey)
                : MapAlphabetKeyWithoutShift(windowsKey), false);
        }

        if (windowsKey >= VirtualKey.Number0 && windowsKey <= VirtualKey.Number9)
        {
            return (shiftPressed
                ? MapShiftedNumberKey(windowsKey)
                : MapNumberKeyWithoutShift(windowsKey), false);
        }

        return windowsKey switch
        {
            VirtualKey.Enter => (nameof(KeyboardActionType.Enter), true),
            VirtualKey.Space => (" ", false),
            VirtualKey.Back => (nameof(KeyboardActionType.Backspace), true),
            VirtualKey.Tab => (nameof(KeyboardActionType.Tab), true),
            VirtualKey.Escape => (nameof(KeyboardActionType.Escape), true),
            _ => ("", false)
        };
    }

    private static bool IsSpecialCharacter(int originalKey)
    {
        return SpecialCharacterMap.ContainsKey(originalKey);
    }

    private static string MapSpecialCharacter(int originalKey, bool shiftPressed)
    {
        if (shiftPressed && ShiftedSpecialCharacterMap.TryGetValue(originalKey, out string shiftedChar))
        {
            return shiftedChar;
        }

        return SpecialCharacterMap.TryGetValue(originalKey, out string normalChar) ? normalChar : "";
    }

    private static string MapAlphabetKeyWithShift(VirtualKey windowsKey) => ((char)windowsKey).ToString();
    private static string MapAlphabetKeyWithoutShift(VirtualKey windowsKey) => ((char)(windowsKey + 32)).ToString();
    private static string MapShiftedNumberKey(VirtualKey windowsKey) => windowsKey switch
    {
        VirtualKey.Number1 => "!",
        VirtualKey.Number2 => "@",
        VirtualKey.Number3 => "#",
        VirtualKey.Number4 => "$",
        VirtualKey.Number5 => "%",
        VirtualKey.Number6 => "^",
        VirtualKey.Number7 => "&",
        VirtualKey.Number8 => "*",
        VirtualKey.Number9 => "(",
        VirtualKey.Number0 => ")",
        _ => ""
    };
    private static string MapNumberKeyWithoutShift(VirtualKey windowsKey) => (windowsKey - VirtualKey.Number0).ToString();
}