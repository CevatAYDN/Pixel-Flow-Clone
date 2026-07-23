using System;

namespace PixelFlow.Data
{
    /// <summary>
    /// game_plan.md §2.2: Strict Zero-Hardcode & Zero-Mock Data Policy.
    /// Eksik veri veya konfigürasyon tespit edildiğinde Play Mode ve Build anında
    /// sessiz fallback'leri engellemek için sert hata olarak fırlatılır.
    /// </summary>
    public class DataValidationException : Exception
    {
        public DataValidationException(string message) : base($"[Zero-Hardcode Policy Violation] {message}")
        {
        }

        public DataValidationException(string message, Exception innerException) 
            : base($"[Zero-Hardcode Policy Violation] {message}", innerException)
        {
        }
    }
}
