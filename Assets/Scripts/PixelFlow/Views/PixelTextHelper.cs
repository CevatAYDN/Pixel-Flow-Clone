using UnityEngine;
using UnityEngine.UI;

namespace PixelFlow.Views
{
    /// <summary>
    /// GDD §5.3 (Ek B Font Sistemi): TextMeshPro köprüsü. Projede
    /// UnityEngine.UI.Text legacy metinleri kullanılıyor; TMP_PRESENT
    /// scripting define aktifse TMP_Text overload'ları derlemeye dahil olur.
    /// Şu an TMP paketi ekli değil; legacy Text API'si yeterli.
    /// Tam TMP migrasyonu için: Packages/manifest.json'a com.unity.textmeshpro
    /// ekle, Project Settings → Player → Scripting Define Symbols'a TMP_PRESENT
    /// yaz, aşağıdaki overload'lar otomatik devreye girer.
    /// </summary>
    public static class PixelTextHelper
    {
        public static void SetText(Text text, string value)
        {
            if (text != null) text.text = value;
        }

        public static string GetText(Text text)
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
