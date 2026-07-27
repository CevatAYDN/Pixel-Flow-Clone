using System.Text;

namespace PixelFlow.Services
{
    /// <summary>
    /// game_plan.md §6 / §13: RTL Text Helper (Arabic / Hebrew text reversal and shaping utility).
    /// </summary>
    public static class RtlUtility
    {
        public static bool IsRtlLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return false;
            string code = langCode.ToLowerInvariant();
            return code == "ar" || code == "he" || code == "fa" || code == "ur";
        }

        public static string ProcessRtlText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            char[] chars = text.ToCharArray();
            System.Array.Reverse(chars);
            return new string(chars);
        }
    }
}
