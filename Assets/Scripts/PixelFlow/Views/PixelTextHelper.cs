using UnityEngine;
using TMPro;

namespace PixelFlow.Views
{
    /// <summary>
    /// GDD §5.3 (Ek B Font Sistemi): TextMeshPro köprüsü. Projede
    /// TMPro aktif ve kullanılıyor; tüm metin işlemleri TMP_Text
    /// üzerinden yürütülür.
    /// </summary>
    public static class PixelTextHelper
    {
        public static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }

        public static string GetText(TMP_Text text)
        {
            return text != null ? text.text : string.Empty;
        }
    }

    /// <summary>
    /// Başlıklar için ortak font stilleri. Inter Bold/Medium ve Noto Sans
    /// (fallback chain) ileride LocalizationManager tarafından yönetilecek.
    /// </summary>
    public enum PixelFontStyle
    {
        Title = 0,
        Body = 1,
        Number = 2,
        Hint = 3,
    }
}
