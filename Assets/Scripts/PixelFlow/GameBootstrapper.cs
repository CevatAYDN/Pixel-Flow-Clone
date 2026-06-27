using UnityEngine;
using Nexus.Core;
using PixelFlow.Data;
using PixelFlow.Signals;
using System.Collections;

namespace PixelFlow
{
    public class GameBootstrapper : MonoBehaviour
    {
        public LevelData initialLevel;
        public Root nexusRoot;

        private IEnumerator Start()
        {
            if (nexusRoot == null)
                nexusRoot = FindAnyObjectByType<Root>();
                
            if (nexusRoot == null)
            {
                Debug.LogError("[PixelFlow] Nexus Root not found! Cannot start game.");
                yield break;
            }

            // Wait until Nexus Root is fully initialized
            while (!nexusRoot.IsInitialized)
            {
                yield return null;
            }

            if (initialLevel != null)
            {
                Debug.Log($"[PixelFlow] Bootstrapper firing LoadLevelSignal for: {initialLevel.name} ({initialLevel.width}x{initialLevel.height})");
                var signalBus = nexusRoot.Context.Container.Resolve<ISignalBus>();
                signalBus.Fire(new LoadLevelSignal { LevelToLoad = initialLevel });
            }
            else
            {
                Debug.LogWarning("[PixelFlow] No Initial Level assigned in GameBootstrapper!");
            }
        }
    }
}
