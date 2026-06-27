#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Models;
using System.Collections.Generic;

namespace PixelFlow.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private enum EditMode { None, Node, Path, Eraser }
        private EditMode _currentMode = EditMode.Node;
        private ColorType _currentColor = ColorType.Red;
        
        private LevelData _data;

        private void OnEnable()
        {
            _data = (LevelData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            GUILayout.Label("Level Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            int newLevelIndex = EditorGUILayout.IntField("Level Index", _data.levelIndex);
            int newWidth = EditorGUILayout.IntSlider("Width", _data.width, 3, 10);
            int newHeight = EditorGUILayout.IntSlider("Height", _data.height, 3, 10);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_data, "Change Level Settings");
                _data.levelIndex = newLevelIndex;
                _data.width = newWidth;
                _data.height = newHeight;
                EditorUtility.SetDirty(_data);
            }

            GUILayout.Space(10);
            GUILayout.Label("Edit Tools", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            _currentMode = (EditMode)EditorGUILayout.EnumPopup("Mode", _currentMode);
            _currentColor = (ColorType)EditorGUILayout.EnumPopup("Color", _currentColor);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Grid Editor (Click/Drag to paint)", EditorStyles.boldLabel);

            DrawVisualGrid();
            
            GUILayout.Space(10);
            if (GUILayout.Button("Clear All Data"))
            {
                Undo.RecordObject(_data, "Clear Data");
                _data.initialNodes.Clear();
                _data.solutions.Clear();
                EditorUtility.SetDirty(_data);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVisualGrid()
        {
            float cellSize = 40f;
            float spacing = 2f;
            
            Rect gridRect = GUILayoutUtility.GetRect(_data.width * (cellSize + spacing), _data.height * (cellSize + spacing));
            
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;
            
            for (int y = _data.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < _data.width; x++)
                {
                    int drawY = _data.height - 1 - y;
                    
                    Rect cellRect = new Rect(gridRect.x + x * (cellSize + spacing), gridRect.y + drawY * (cellSize + spacing), cellSize, cellSize);
                    
                    Color cellColor = Color.gray;
                    string cellText = "";
                    
                    var node = _data.initialNodes.Find(n => n.position.x == x && n.position.y == y);
                    if (node.color != ColorType.None)
                    {
                        cellColor = GetUnityColor(node.color);
                        cellText = "N";
                    }
                    else
                    {
                        foreach (var sol in _data.solutions)
                        {
                            if (sol.pathPositions != null && sol.pathPositions.Contains(new Vector2Int(x, y)))
                            {
                                cellColor = GetUnityColor(sol.color);
                                cellColor.a = 0.5f; 
                                cellText = "P";
                                break;
                            }
                        }
                    }

                    EditorGUI.DrawRect(cellRect, cellColor);
                    
                    if (!string.IsNullOrEmpty(cellText))
                    {
                        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                        style.alignment = TextAnchor.MiddleCenter;
                        style.normal.textColor = Color.white;
                        GUI.Label(cellRect, cellText, style);
                    }

                    if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && cellRect.Contains(mousePos))
                    {
                        HandleCellClick(x, y);
                        e.Use();
                    }
                }
            }
        }

        private void HandleCellClick(int x, int y)
        {
            Undo.RecordObject(_data, "Edit Grid");
            
            Vector2Int pos = new Vector2Int(x, y);
            
            if (_currentMode == EditMode.Eraser)
            {
                _data.initialNodes.RemoveAll(n => n.position == pos);
                foreach (var sol in _data.solutions)
                {
                    if (sol.pathPositions != null)
                        sol.pathPositions.RemoveAll(p => p == pos);
                }
            }
            else if (_currentMode == EditMode.Node)
            {
                _data.initialNodes.RemoveAll(n => n.position == pos);
                _data.initialNodes.Add(new GridNode { position = pos, color = _currentColor });
            }
            else if (_currentMode == EditMode.Path)
            {
                int solIndex = _data.solutions.FindIndex(s => s.color == _currentColor);
                if (solIndex == -1)
                {
                    var newSol = new PathSolution { color = _currentColor, pathPositions = new List<Vector2Int>() };
                    newSol.pathPositions.Add(pos);
                    _data.solutions.Add(newSol);
                }
                else
                {
                    if (_data.solutions[solIndex].pathPositions == null)
                    {
                        var updatedSol = _data.solutions[solIndex];
                        updatedSol.pathPositions = new List<Vector2Int>();
                        _data.solutions[solIndex] = updatedSol;
                    }

                    if (!_data.solutions[solIndex].pathPositions.Contains(pos))
                    {
                        _data.solutions[solIndex].pathPositions.Add(pos);
                    }
                }
            }
            
            EditorUtility.SetDirty(_data);
        }

        private Color GetUnityColor(ColorType color)
        {
            switch (color)
            {
                case ColorType.Red: return Color.red;
                case ColorType.Green: return Color.green;
                case ColorType.Blue: return Color.blue;
                case ColorType.Yellow: return Color.yellow;
                case ColorType.Orange: return new Color(1f, 0.5f, 0f);
                case ColorType.Purple: return new Color(0.5f, 0f, 0.5f);
                default: return Color.gray;
            }
        }
    }
}
#endif
