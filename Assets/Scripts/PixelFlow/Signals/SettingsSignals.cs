using PixelFlow.Models;

namespace PixelFlow.Signals
{
    /// <summary>
    /// Ayar mutasyonları için signal'lar. MVCS §15.3 KURAL 3 & §15.9 KURAL 5:
    /// View/Mediator doğrudan Model'e yazmaz; Signal → Command → Model akışı kullanılır.
    /// (Tema değişimi zaten ChangeThemeSignal/ChangeThemeCommand ile bu akışı kullanır.)
    /// </summary>

    /// <summary>Ses kanalı seçici (master/sfx/music).</summary>
    public enum AudioChannel
    {
        Master,
        Sfx,
        Music
    }

    /// <summary>Bir ses kanalının seviyesini değiştirir.</summary>
    public struct ChangeAudioVolumeSignal
    {
        public AudioChannel Channel;
        public float Value;
    }

    /// <summary>Renk körlüğü modunu değiştirir.</summary>
    public struct ChangeColorBlindModeSignal
    {
        public ColorBlindMode Mode;
    }

    /// <summary>Haptik geri bildirimi açar/kapatır.</summary>
    public struct ToggleHapticsSignal
    {
        public bool Disabled;
    }
}
