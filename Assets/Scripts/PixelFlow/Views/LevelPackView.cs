using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Nexus.Core;
using PixelFlow.Data;

namespace PixelFlow.Views
{
    [Mediator(typeof(LevelPackMediator))]
    public class LevelPackView : View
    {
        [SerializeField] private Transform _buttonContainer;
        [SerializeField] private GameObject _levelButtonPrefab;
        [SerializeField] private LevelPack _levelPack;

        public LevelPack LevelPackData => _levelPack;

        private readonly List<Button> _buttons = new List<Button>();

        public void PopulateButtons(int levelCount, Action<int> onLevelSelected)
        {
            ClearButtons();

            for (int i = 0; i < levelCount; i++)
            {
                int index = i;
                GameObject btnObj;
                if (_levelButtonPrefab != null)
                {
                    btnObj = Instantiate(_levelButtonPrefab, _buttonContainer);
                }
                else
                {
                    btnObj = new GameObject($"LevelButton_{i}", typeof(Button));
                    btnObj.transform.SetParent(_buttonContainer, false);
                    var text = btnObj.AddComponent<Text>();
                    text.text = $"Level {i + 1}";
                }

                var button = btnObj.GetComponent<Button>();
                button.onClick.AddListener(() => onLevelSelected?.Invoke(index));
                _buttons.Add(button);
            }
        }

        private void ClearButtons()
        {
            foreach (var btn in _buttons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            _buttons.Clear();
        }
    }
}
