using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace PixelFlow.Services
{
    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        event Action<string> OnLanguageChanged;
        void SetLanguage(string langCode);
        string GetString(string key, string fallback = "");
        string FormatRTLIfNeeded(string text);
        bool IsRTL { get; }
    }
}
