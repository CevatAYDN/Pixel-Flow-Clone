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

            // Simple RTL character check
            bool hasRtlChar = false;
            foreach (char c in text)
            {
                if ((c >= 0x0600 && c <= 0x06FF) || (c >= 0x0590 && c <= 0x05FF))
                {
                    hasRtlChar = true;
                    break;
                }
            }

            if (!hasRtlChar) return text;

            // Reverse string for display in standard TMP components without RTL native shaper
            char[] chars = text.ToCharArray();
            System.Array.Reverse(chars);
            return new string(chars);
        }
    }
}
