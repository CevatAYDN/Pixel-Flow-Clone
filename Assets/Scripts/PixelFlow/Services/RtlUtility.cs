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

            // Preserve format placeholders like {0}, {1}, {2}, etc. so string.Format never breaks
            var placeholders = new System.Collections.Generic.List<string>();
            string tokenized = System.Text.RegularExpressions.Regex.Replace(text, @"\{[0-9]+\}", m =>
            {
                placeholders.Add(m.Value);
                return $"__P{placeholders.Count - 1}__";
            });

            char[] chars = tokenized.ToCharArray();
            System.Array.Reverse(chars);
            string reversed = new string(chars);

            for (int i = 0; i < placeholders.Count; i++)
            {
                string reversedToken = $"__{i}P__";
                reversed = reversed.Replace(reversedToken, placeholders[i]);
            }

            return reversed;
        }

        public static string SafeFormat(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
            {
                return (args != null && args.Length > 0 && args[0] != null) ? args[0].ToString() : string.Empty;
            }

            try
            {
                return string.Format(format, args);
            }
            catch (System.FormatException)
            {
                if (args != null && args.Length > 0)
                {
                    string cleanFormat = format.Replace("{", "").Replace("}", "").Trim();
                    return $"{cleanFormat} {args[0]}";
                }
                return format;
            }
        }
    }
}
