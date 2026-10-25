using System.Windows.Input;

namespace Other
{
    internal class KeybindNameManager
    {
        public static Dictionary<string, string> KeybindNames = new()
        {
            { "D1", "1"},
            { "D2", "2"},
            { "D3", "3"},
            { "D4", "4"},
            { "D5", "5"},
            { "D6", "6"},
            { "D7", "7"},
            { "D8", "8"},
            { "D9", "9"},
            { "D0", "0"},

            { "OemMinus", "-"},
            { "OemPlus", "+"},
            { "Back", "退格"},
            { "Oem5", "反斜杠 (\\)"},
            { "LMenu", "左 Alt"},
            { "RMenu", "右 Alt"},
            { "RShiftKey", "右 Shift" },
            { "LShiftKey", "左 Shift" },

            { "Oem4", "左方括号"},
            { "OemOpenBrackets", "左方括号"},
            { "Oem6", "右方括号"},
            { "Oem1", ";"},
            { "Oem3", "`" },
            { "OemQuotes", "'"},
            { "OemQuestion", "/"},
            { "OemPeriod", "."},
            { "OemComma", ","},
            { "Return", "回车" },
        };

        public static string ConvertToRegularKey(string keyName)
        {
            try
            {
                KeyConverter kc = new();
                var key = (Key)kc.ConvertFromString(keyName)!;
                return KeybindNames.TryGetValue(key.ToString(), out var displayName) ? displayName : key.ToString();
            }
            catch (Exception)
            {
                return keyName;
            }
        }
    }
}
