using System;
using UnityEngine;

namespace PixelFlow.Signals
{
    /// <summary>
    /// GDD §4.2: 3. renk çizimi reddedildiğinde (hücrede zaten 2 renk varken)
    /// öne çıkan görsel geri bildirim için ateşlenir.
    /// GridMediator dinler ve ilgili CellView'e pulse animasyonu çalar.
    /// </summary>
    public struct ThirdColorRejectionSignal
    {
        public Vector2Int Position;
    }
}