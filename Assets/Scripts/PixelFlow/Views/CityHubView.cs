using System;
using System.Collections.Generic;
using UnityEngine;
using PixelFlow.Models;
using Nexus.Core;

namespace PixelFlow.Views
{
    [Mediator(typeof(CityHubMediator))]
    public class CityHubView : View
    {
        [SerializeField] private Transform _cityContainer;
        [SerializeField] private Camera _hubCamera;

        private readonly List<GameObject> _spawnedBuildings = new List<GameObject>();
        private int _lastSpawnedDistrictLvl = -1;
        private int _lastSpawnedLevelsCount = -1;

        public event Action OnCollectTaxesClicked;
        public event Action<UpgradeType> OnUpgradeClicked;

        private void Start()
        {
            if (_cityContainer == null)
            {
                _cityContainer = new GameObject("CityContainer").transform;
                _cityContainer.SetParent(transform, false);
            }

            if (_hubCamera == null)
            {
                _hubCamera = Camera.main;
            }
        }

        public void SetupCamera(bool isHubActive)
        {
            if (_hubCamera == null) _hubCamera = Camera.main;
            if (_hubCamera == null) return;

            if (isHubActive)
            {
                // Hub izometrik görünüm (45 derece)
                _hubCamera.transform.position = new Vector3(8f, 12f, -8f);
                _hubCamera.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
                _hubCamera.orthographic = true;
                _hubCamera.orthographicSize = 7f;
            }
        }

        public void RefreshCityLayout(int districtLvl, int completedLevels)
        {
            if (districtLvl == _lastSpawnedDistrictLvl && completedLevels == _lastSpawnedLevelsCount)
                return;

            _lastSpawnedDistrictLvl = districtLvl;
            _lastSpawnedLevelsCount = completedLevels;

            // Clear old buildings
            foreach (var b in _spawnedBuildings)
            {
                if (b != null) UnityEngine.Object.Destroy(b);
            }
            _spawnedBuildings.Clear();

            // Create Ground Plane
            CreateCityGround();

            // Spawn Districts based on unlock levels
            // 0: Merkez, 1: Liman, 2: Kampus, 3: Tekno, 4: Havalimani, 5: Plaza
            SpawnDistrict(0, "Merkez", new Vector3(2f, 0f, 2f), Color.cyan);
            
            if (districtLvl >= 1 || completedLevels >= 10)
                SpawnDistrict(1, "Liman Bölgesi", new Vector3(6f, 0f, 2f), Color.blue);

            if (districtLvl >= 2 || completedLevels >= 20)
                SpawnDistrict(2, "Üniversite Kampüsü", new Vector3(2f, 0f, 6f), Color.yellow);

            if (districtLvl >= 3 || completedLevels >= 30)
                SpawnDistrict(3, "Teknoloji Vadisi", new Vector3(6f, 0f, 6f), Color.magenta);

            if (districtLvl >= 4 || completedLevels >= 42)
                SpawnDistrict(4, "Havalimanı Bölgesi", new Vector3(10f, 0f, 2f), Color.green);

            if (districtLvl >= 5 || completedLevels >= 55)
                SpawnDistrict(5, "Merkez Plaza", new Vector3(10f, 0f, 6f), Color.red);
        }

        private void CreateCityGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(_cityContainer, false);
            ground.transform.localPosition = new Vector3(6f, -0.1f, 4f);
            ground.transform.localScale = new Vector3(2f, 1f, 1.5f);

            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Mat koyu obsidyen rengi (#0B0F19)
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = new Color(0.043f, 0.059f, 0.098f, 1f);
            }
            _spawnedBuildings.Add(ground);
        }

        private void SpawnDistrict(int districtIndex, string name, Vector3 offset, Color themeColor)
        {
            // Spawn 4 buildings in a 2x2 grid inside this district
            for (int dx = 0; dx < 2; dx++)
            {
                for (int dz = 0; dz < 2; dz++)
                {
                    Vector3 pos = offset + new Vector3(dx * 1.5f, 0f, dz * 1.5f);
                    
                    GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    building.name = $"{name}_Bld_{dx}_{dz}";
                    building.transform.SetParent(_cityContainer, false);
                    
                    // Random building height for low-poly skyline feel
                    float height = UnityEngine.Random.Range(1f, 3.5f + (districtIndex * 0.5f));
                    building.transform.localScale = new Vector3(0.8f, height, 0.8f);
                    building.transform.localPosition = pos + new Vector3(0f, height * 0.5f, 0f);

                    var renderer = building.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Glassmorphic / Neon emissive look material
                        renderer.material = new Material(Shader.Find("Sprites/Default"));
                        renderer.material.color = Color.Lerp(new Color(0.1f, 0.1f, 0.15f, 1f), themeColor, 0.3f);
                    }

                    // Add a tiny glowing neon cube on top of building
                    GameObject neonLight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    neonLight.name = "NeonLight";
                    neonLight.transform.SetParent(building.transform, false);
                    neonLight.transform.localPosition = new Vector3(0f, 0.5f + (0.05f / height), 0f);
                    neonLight.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);

                    var lightRenderer = neonLight.GetComponent<Renderer>();
                    if (lightRenderer != null)
                    {
                        lightRenderer.material = new Material(Shader.Find("Sprites/Default"));
                        lightRenderer.material.color = themeColor;
                    }

                    _spawnedBuildings.Add(building);
                }
            }
        }

        public void TriggerCollectTaxes()
        {
            OnCollectTaxesClicked?.Invoke();
        }

        public void TriggerUpgrade(UpgradeType type)
        {
            OnUpgradeClicked?.Invoke(type);
        }
    }
}
