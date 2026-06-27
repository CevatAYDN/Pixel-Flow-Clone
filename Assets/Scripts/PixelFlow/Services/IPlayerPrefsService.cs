namespace PixelFlow.Services
{
    /// <summary>
    /// Oyunda kalıcı küçük veri saklamak için ince bir soyutlama.
    /// Modellerin doğrudan UnityEngine.PlayerPrefs'e bağımlı olmasını engeller;
    /// EditMode testlerde in-memory fake implementasyonla değiştirilebilir.
    /// </summary>
    public interface IPlayerPrefsService
    {
        int GetInt(string key, int defaultValue = 0);
        void SetInt(string key, int value);
        bool GetBool(string key, bool defaultValue = false);
        void SetBool(string key, bool value);
        void Save();
    }
}