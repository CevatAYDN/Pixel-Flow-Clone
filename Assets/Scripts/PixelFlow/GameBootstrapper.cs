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
            int retries = 5;
            while (nexusRoot == null && retries > 0)
            {
                nexusRoot = FindAnyObjectByType<Root>();
                if (nexusRoot == null)
                {
                    retries--;
                    yield return new WaitForSeconds(0.1f);
                }
            }
                
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
                Debug.LogWarning("[PixelFlow] No Initial Level assigned in GameBootstrapper! Attempting to load default level...");
                var signalBus = nexusRoot.Context.Container.Resolve<ISignalBus>();
                var defaultLevel = Resources.Load<LevelData>("Levels/Level1");
                if (defaultLevel != null)
                {
                    Debug.Log($"[PixelFlow] Loaded default level from Resources: {defaultLevel.name}");
                    signalBus.Fire(new LoadLevelSignal { LevelToLoad = defaultLevel });
                }
                else
                {
                    Debug.LogError("[PixelFlow] No default level found at Resources/Levels/Level1.asset!");
                }
            }
        }
    }
}
