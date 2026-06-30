#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using System.Collections.Generic;
using System.Linq;

namespace PixelFlow.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private enum EditMode { None, Node, Path, Bridge, Eraser }
        private EditMode _currentMode = EditMode.Node;
        private ColorType _currentColor = ColorType.Red;
        
        private LevelData _data;
        private Vector2Int _lastPaintedCell = new Vector2Int(-1, -1);
        private bool _requireFullGridCoverage = true;
        private bool _showValidator = true;
        private string _solveStatus = "";

        // UI Styles
        private GUIStyle _headerStyle;
        private GUIStyle _toolbarStyle;
        private GUIStyle _colorSwatchStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _statusOkStyle;
        private GUIStyle _statusWarnStyle;

        private void OnEnable()
        {
            _data = (LevelData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            InitStyles();

            // Header Banner
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Space(5);
            GUILayout.Label("Pixel Flow Level Editor", _headerStyle);
            GUILayout.Label("Create and validate grid levels with ease.", EditorStyles.miniLabel);
            GUILayout.Space(5);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Level Configuration Card
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Grid Configuration", EditorStyles.boldLabel);
            GUILayout.Space(5);
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
                SanitizeGridBounds();
                EditorUtility.SetDirty(_data);
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Editing Tools Card
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Editor Controls", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // Edit Mode Selector (Horizontal Buttons)
            GUILayout.Label("Select Tool:", EditorStyles.miniLabel);
            GUILayout.BeginHorizontal();
            EditMode[] modes = { EditMode.Node, EditMode.Path, EditMode.Bridge, EditMode.Eraser };
            foreach (var m in modes)
            {
                bool isSelected = _currentMode == m;
                GUI.backgroundColor = isSelected ? new Color(0.2f, 0.6f, 1f) : Color.white;
                if (GUILayout.Button(m.ToString(), GUILayout.Height(28)))
                {
                    _currentMode = m;
                    _lastPaintedCell = new Vector2Int(-1, -1);
                }
            }
            GUI.backgroundColor = Color.white; // Reset
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Color Selector (Horizontal Palette)
            if (_currentMode == EditMode.Node || _currentMode == EditMode.Path)
            {
                GUILayout.Label("Select Color:", EditorStyles.miniLabel);
                GUILayout.BeginHorizontal();
                ColorType[] colors = {
                    ColorType.Red, ColorType.Green, ColorType.Blue,
                    ColorType.Yellow, ColorType.Orange, ColorType.Purple,
                    ColorType.Cyan, ColorType.Magenta
                };

                foreach (var c in colors)
                {
                    bool isSelected = _currentColor == c;
                    Color colorValue = GetVisualColor(c);

                    // Draw colored swatch
                    GUI.backgroundColor = colorValue;
                    string label = isSelected ? "✔" : "";
                    
                    // Create style overlay for selected color
                    var buttonStyle = new GUIStyle(GUI.skin.button);
                    buttonStyle.normal.textColor = Color.white;
                    buttonStyle.fontStyle = FontStyle.Bold;

                    if (GUILayout.Button(label, buttonStyle, GUILayout.Width(35), GUILayout.Height(30)))
                    {
                        _currentColor = c;
                    }
                }
                GUI.backgroundColor = Color.white; // Reset
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Grid Editor Section
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Interactive Grid (Click & Drag to Draw)", EditorStyles.boldLabel);
            GUILayout.Space(10);

            DrawVisualGrid();

            GUILayout.Space(10);
            
            // Clean control actions
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Paths", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Paths", "Are you sure you want to clear all path solutions?", "Yes", "No"))
                {
                    Undo.RecordObject(_data, "Clear Paths");
                    _data.solutions.Clear();
                    EditorUtility.SetDirty(_data);
                }
            }
            if (GUILayout.Button("Reset Entire Grid", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Level Data", "This will wipe out all nodes, paths, and bridges. Reset?", "Yes", "No"))
                {
                    Undo.RecordObject(_data, "Clear Grid");
                    _data.initialNodes.Clear();
                    _data.solutions.Clear();
                    _data.bridgePositions.Clear();
                    EditorUtility.SetDirty(_data);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Solver & Validator Section
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Auto-Solver & Level Diagnostics", EditorStyles.boldLabel);
            GUILayout.Space(5);

            _requireFullGridCoverage = EditorGUILayout.Toggle("Require Perfect Coverage", _requireFullGridCoverage);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Auto-Solver", GUILayout.Height(30)))
            {
                bool success = SolveLevel(_data);
                if (success)
                {
                    _solveStatus = "✔ Level Solved Successfully! Path solution generated.";
                    EditorUtility.SetDirty(_data);
                }
                else
                {
                    _solveStatus = "✘ Unsolvable! Check node pairs, bridges, or coverage rules.";
                }
            }
            GUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_solveStatus))
            {
                EditorGUILayout.HelpBox(_solveStatus, _solveStatus.StartsWith("✔") ? MessageType.Info : MessageType.Warning);
            }

            GUILayout.Space(10);

            // Validator foldout
            _showValidator = EditorGUILayout.Foldout(_showValidator, "Validation Report");
            if (_showValidator)
            {
                RunLevelValidation();
            }

            GUILayout.EndVertical();

            GUILayout.Space(15);

            // Level templates card
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Predefined Layout Templates", EditorStyles.boldLabel);
            GUILayout.Label("Applying templates will overwrite current level configuration.", EditorStyles.miniLabel);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("5x5 Basic Grid", GUILayout.Height(25))) ApplyTemplate5x5();
            if (GUILayout.Button("5x5 Cross Grid", GUILayout.Height(25))) ApplyTemplateCross();
            if (GUILayout.Button("6x6 Bridge Grid", GUILayout.Height(25))) ApplyTemplateBridge6x6();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);
            serializedObject.ApplyModifiedProperties();
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter
                };
                if (EditorGUIUtility.isProSkin)
                    _headerStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
                else
                    _headerStyle.normal.textColor = new Color(0f, 0.3f, 0.6f);
            }

            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle(GUI.skin.box);
                _cardStyle.padding = new RectOffset(10, 10, 10, 10);
                _cardStyle.margin = new RectOffset(4, 4, 4, 4);
            }

            if (_statusOkStyle == null)
            {
                _statusOkStyle = new GUIStyle(EditorStyles.label);
                _statusOkStyle.normal.textColor = new Color(0.1f, 0.6f, 0.2f);
                _statusOkStyle.fontStyle = FontStyle.Bold;
            }

            if (_statusWarnStyle == null)
            {
                _statusWarnStyle = new GUIStyle(EditorStyles.label);
                _statusWarnStyle.normal.textColor = new Color(0.8f, 0.4f, 0f);
                _statusWarnStyle.fontStyle = FontStyle.Bold;
            }
        }

        private void SanitizeGridBounds()
        {
            // Remove items outside new width/height bounds
            _data.initialNodes.RemoveAll(n => n.position.x >= _data.width || n.position.y >= _data.height);
            _data.bridgePositions.RemoveAll(p => p.x >= _data.width || p.y >= _data.height);
            foreach (var sol in _data.solutions)
            {
                if (sol.pathPositions != null)
                {
                    sol.pathPositions.RemoveAll(p => p.x >= _data.width || p.y >= _data.height);
                }
            }
        }

        private void DrawVisualGrid()
        {
            float cellSize = 38f;
            float spacing = 3f;

            // Centers the grid container in the inspector width
            float totalWidth = _data.width * (cellSize + spacing);
            float totalHeight = _data.height * (cellSize + spacing);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Background container frame
            Rect bgFrame = new Rect(gridRect.x - 5, gridRect.y - 5, totalWidth + 10, totalHeight + 10);
            EditorGUI.DrawRect(bgFrame, new Color(0.08f, 0.08f, 0.1f, 1f));

            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            // === PASS 1: Hücre arkaplanları (en altta) ===
            for (int y = _data.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < _data.width; x++)
                {
                    int drawY = _data.height - 1 - y;
                    Rect cellRect = new Rect(
                        gridRect.x + x * (cellSize + spacing),
                        gridRect.y + drawY * (cellSize + spacing),
                        cellSize, cellSize);
                    EditorGUI.DrawRect(cellRect, new Color(0.18f, 0.18f, 0.22f, 1f));
                }
            }

            // === PASS 2: Path çizgileri (ortada) ===
            foreach (var sol in _data.solutions)
            {
                if (sol.pathPositions != null && sol.pathPositions.Count > 1 && sol.color != ColorType.None)
                {
                    Handles.BeginGUI();
                    Handles.color = GetVisualColor(sol.color);

                    List<Vector3> points = new List<Vector3>();
                    foreach (var p in sol.pathPositions)
                    {
                        int drawY = _data.height - 1 - p.y;
                        points.Add(new Vector2(
                            gridRect.x + p.x * (cellSize + spacing) + cellSize / 2f,
                            gridRect.y + drawY * (cellSize + spacing) + cellSize / 2f));
                    }

                    Handles.DrawAAPolyLine(6f, points.ToArray());
                    Handles.EndGUI();
                }
            }

            // === PASS 3: Bridge + Node (en üstte) ===
            for (int y = _data.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < _data.width; x++)
                {
                    int drawY = _data.height - 1 - y;
                    Rect cellRect = new Rect(
                        gridRect.x + x * (cellSize + spacing),
                        gridRect.y + drawY * (cellSize + spacing),
                        cellSize, cellSize);

                    bool isBridge = _data.bridgePositions.Contains(new Vector2Int(x, y));
                    var node = _data.initialNodes.Find(n => n.position.x == x && n.position.y == y);
                    bool isNode = node.color != ColorType.None;

                    if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && cellRect.Contains(mousePos))
                    {
                        Vector2Int cellPos = new Vector2Int(x, y);
                        if (e.type == EventType.MouseDown || _lastPaintedCell != cellPos)
                        {
                            HandleCellClick(x, y);
                            _lastPaintedCell = cellPos;
                            e.Use();
                        }
                    }

                    if (isBridge)
                    {
                        Vector2 center = cellRect.center;
                        Handles.BeginGUI();
                        Handles.color = new Color(0.35f, 0.35f, 0.4f, 1f);
                        Handles.DrawAAPolyLine(4f, new Vector3(center.x - cellSize * 0.4f, center.y), new Vector3(center.x + cellSize * 0.4f, center.y));
                        Handles.DrawAAPolyLine(4f, new Vector3(center.x, center.y - cellSize * 0.4f), new Vector3(center.x, center.y + cellSize * 0.4f));
                        Handles.EndGUI();
                    }

                    if (isNode)
                    {
                        Vector2 center = cellRect.center;
                        Color nodeColor = GetVisualColor(node.color);
                        Handles.BeginGUI();
                        Handles.color = Color.black;
                        Handles.DrawSolidDisc(new Vector3(center.x, center.y, 0), Vector3.forward, cellSize * 0.32f);
                        Handles.color = nodeColor;
                        Handles.DrawSolidDisc(new Vector3(center.x, center.y, 0), Vector3.forward, cellSize * 0.26f);
                        Handles.EndGUI();
                    }
                }
            }

            // Reset painted cell status when mouse is released
            if (e.type == EventType.MouseUp)
            {
                _lastPaintedCell = new Vector2Int(-1, -1);
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
                _data.bridgePositions.Remove(pos);
            }
            else if (_currentMode == EditMode.Node)
            {
                _data.initialNodes.RemoveAll(n => n.position == pos);
                // Erase any solution path node under this new node
                foreach (var sol in _data.solutions)
                {
                    if (sol.pathPositions != null)
                        sol.pathPositions.Remove(pos);
                }
                _data.initialNodes.Add(new GridNode { position = pos, color = _currentColor });
            }
            else if (_currentMode == EditMode.Bridge)
            {
                _data.initialNodes.RemoveAll(n => n.position == pos);
                if (!_data.bridgePositions.Contains(pos))
                    _data.bridgePositions.Add(pos);
                else
                    _data.bridgePositions.Remove(pos);
            }
            else if (_currentMode == EditMode.Path)
            {
                // Path Mode allows editing a solution manually cell-by-cell
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
                    else
                    {
                        // Remove if clicked again to allow backtracking
                        _data.solutions[solIndex].pathPositions.Remove(pos);
                    }
                }
            }

            EditorUtility.SetDirty(_data);
        }

        private void RunLevelValidation()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            // 1. Validate Node counts per color
            var nodeColors = _data.initialNodes.GroupBy(n => n.color).ToDictionary(g => g.Key, g => g.Count());
            foreach (var kvp in nodeColors)
            {
                if (kvp.Value != 2)
                {
                    errors.Add($"Color '{kvp.Key}' has {kvp.Value} nodes. It must have exactly 2.");
                }
            }

            // 2. Validate Solution Path Connections
            foreach (var sol in _data.solutions)
            {
                if (sol.color == ColorType.None) continue;
                if (sol.pathPositions == null || sol.pathPositions.Count == 0)
                {
                    warnings.Add($"Color '{sol.color}' has a solution entry but empty path positions.");
                    continue;
                }

                // Check endpoints
                var path = sol.pathPositions;
                var colorNodes = _data.initialNodes.Where(n => n.color == sol.color).Select(n => n.position).ToList();

                if (colorNodes.Count == 2)
                {
                    Vector2Int startNode = colorNodes[0];
                    Vector2Int endNode = colorNodes[1];
                    Vector2Int pathStart = path[0];
                    Vector2Int pathEnd = path[path.Count - 1];

                    bool connected = (pathStart == startNode && pathEnd == endNode) || (pathStart == endNode && pathEnd == startNode);
                    if (!connected)
                    {
                        errors.Add($"Path for '{sol.color}' does not connect its two nodes properly.");
                    }
                }
                else
                {
                    errors.Add($"Cannot check path for '{sol.color}' because it doesn't have exactly 2 nodes.");
                }

                // Check path continuity
                for (int i = 0; i < path.Count - 1; i++)
                {
                    int stepDist = Mathf.Abs(path[i].x - path[i + 1].x) + Mathf.Abs(path[i].y - path[i + 1].y);
                    if (stepDist != 1)
                    {
                        errors.Add($"Path for '{sol.color}' is broken between cell {path[i]} and {path[i+1]}.");
                    }
                }
            }

            // 3. Verify Bridge Crossing rules
            foreach (var bridge in _data.bridgePositions)
            {
                int crossingPathsCount = 0;
                List<ColorType> crossingColors = new List<ColorType>();
                foreach (var sol in _data.solutions)
                {
                    if (sol.pathPositions != null && sol.pathPositions.Contains(bridge))
                    {
                        crossingPathsCount++;
                        crossingColors.Add(sol.color);
                    }
                }

                if (crossingColors.Count > 2)
                {
                    errors.Add($"Bridge at {bridge} is crossed by {crossingColors.Count} paths ({string.Join(", ", crossingColors)}). Maximum is 2.");
                }
                else if (crossingColors.Count == 2)
                {
                    // Check that the two paths cross perpendicularly
                    var color1 = crossingColors[0];
                    var color2 = crossingColors[1];
                    var path1 = _data.solutions.First(s => s.color == color1).pathPositions;
                    var path2 = _data.solutions.First(s => s.color == color2).pathPositions;

                    Vector2Int dir1 = GetPathDirectionThroughCell(path1, bridge);
                    Vector2Int dir2 = GetPathDirectionThroughCell(path2, bridge);

                    if (dir1 != Vector2Int.zero && dir2 != Vector2Int.zero)
                    {
                        if (Vector2.Dot(dir1, dir2) != 0)
                        {
                            errors.Add($"Bridge at {bridge} has two overlapping paths. They must cross perpendicularly.");
                        }
                    }
                }
                else if (crossingColors.Count == 0)
                {
                    warnings.Add($"Bridge at {bridge} is not crossed by any path in this solution.");
                }
            }

            // 4. Verify Perfect Grid Coverage (if toggled)
            if (_requireFullGridCoverage)
            {
                bool hasUncovered = false;
                for (int x = 0; x < _data.width; x++)
                {
                    for (int y = 0; y < _data.height; y++)
                    {
                        Vector2Int p = new Vector2Int(x, y);
                        // Check if any solution path covers this cell
                        bool covered = _data.solutions.Any(s => s.pathPositions != null && s.pathPositions.Contains(p));
                        if (!covered)
                        {
                            hasUncovered = true;
                        }
                    }
                }
                if (hasUncovered)
                {
                    warnings.Add("Perfect grid coverage is toggled ON but some empty cells remain uncovered.");
                }
            }

            // Draw Report
            if (errors.Count == 0 && warnings.Count == 0)
            {
                GUILayout.Label("✔ Level validation passes. Ready for gameplay!", _statusOkStyle);
            }
            else
            {
                foreach (var err in errors)
                {
                    EditorGUILayout.HelpBox("Error: " + err, MessageType.Error);
                }
                foreach (var warn in warnings)
                {
                    EditorGUILayout.HelpBox("Warning: " + warn, MessageType.Warning);
                }
            }
        }

        private Vector2Int GetPathDirectionThroughCell(List<Vector2Int> path, Vector2Int cell)
        {
            int idx = path.IndexOf(cell);
            if (idx <= 0 || idx >= path.Count - 1) return Vector2Int.zero;
            // Return vector from previous to next
            return path[idx + 1] - path[idx - 1];
        }

        private Color GetVisualColor(ColorType color)
        {
            switch (color)
            {
                case ColorType.Red: return new Color(1f, 0.22f, 0.22f); // Vibrant light red
                case ColorType.Green: return new Color(0.2f, 0.85f, 0.3f); // Emerald green
                case ColorType.Blue: return new Color(0.2f, 0.55f, 1f); // Royal blue
                case ColorType.Yellow: return new Color(1f, 0.85f, 0.1f); // Warm yellow
                case ColorType.Orange: return new Color(1f, 0.55f, 0.05f); // Neon orange
                case ColorType.Purple: return new Color(0.68f, 0.25f, 0.95f); // Rich purple
                case ColorType.Cyan: return new Color(0.08f, 0.88f, 0.92f); // Electric cyan
                case ColorType.Magenta: return new Color(0.95f, 0.2f, 0.72f); // Bright magenta
                default: return Color.gray;
            }
        }

        // ================= AUTO SOLVER IMPLEMENTATION =================
        private bool SolveLevel(LevelData level)
        {
            // Collect node groups
            var colorNodes = new Dictionary<ColorType, List<Vector2Int>>();
            foreach (var node in level.initialNodes)
            {
                if (node.color == ColorType.None) continue;
                if (!colorNodes.ContainsKey(node.color))
                    colorNodes[node.color] = new List<Vector2Int>();
                colorNodes[node.color].Add(node.position);
            }

            List<ColorType> colorsToSolve = colorNodes.Keys.ToList();

            // Sanity checks
            foreach (var kvp in colorNodes)
            {
                if (kvp.Value.Count != 2)
                {
                    Debug.LogError($"Color '{kvp.Key}' has {kvp.Value.Count} nodes. Each color must have exactly 2 nodes to solve.");
                    return false;
                }
            }

            int w = level.width;
            int h = level.height;

            ColorType[,] gridOccupancy = new ColorType[w, h];
            HashSet<Vector2Int> bridges = new HashSet<Vector2Int>(level.bridgePositions);

            var pathSolutions = new Dictionary<ColorType, List<Vector2Int>>();
            foreach (var color in colorsToSolve)
            {
                pathSolutions[color] = new List<Vector2Int>();
            }

            // Mark nodes on occupancy grid
            foreach (var node in level.initialNodes)
            {
                gridOccupancy[node.position.x, node.position.y] = node.color;
            }

            if (SolveRecursive(0, colorsToSolve, colorNodes, pathSolutions, gridOccupancy, bridges, w, h))
            {
                // Apply solutions
                Undo.RecordObject(level, "Auto-Solve Level");
                level.solutions.Clear();
                foreach (var kvp in pathSolutions)
                {
                    level.solutions.Add(new PathSolution
                    {
                        color = kvp.Key,
                        pathPositions = new List<Vector2Int>(kvp.Value)
                    });
                }
                return true;
            }
            return false;
        }

        private bool SolveRecursive(int colorIndex, List<ColorType> colors, Dictionary<ColorType, List<Vector2Int>> colorNodes, Dictionary<ColorType, List<Vector2Int>> solutions, ColorType[,] grid, HashSet<Vector2Int> bridges, int w, int h)
        {
            if (colorIndex >= colors.Count)
            {
                if (_requireFullGridCoverage)
                {
                    // Check if all cells are filled, except maybe bridge cells that can be double crossed or crossed once
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            if (grid[x, y] == ColorType.None && !bridges.Contains(new Vector2Int(x, y)))
                                return false; // Found an empty, uncovered cell
                        }
                    }
                }
                return true;
            }

            ColorType color = colors[colorIndex];
            Vector2Int start = colorNodes[color][0];
            Vector2Int end = colorNodes[color][1];

            List<Vector2Int> currentPath = new List<Vector2Int> { start };
            return FindPathRecursive(start, end, color, currentPath, colorIndex, colors, colorNodes, solutions, grid, bridges, w, h);
        }

        private bool FindPathRecursive(Vector2Int current, Vector2Int end, ColorType color, List<Vector2Int> path, int colorIndex, List<ColorType> colors, Dictionary<ColorType, List<Vector2Int>> colorNodes, Dictionary<ColorType, List<Vector2Int>> solutions, ColorType[,] grid, HashSet<Vector2Int> bridges, int w, int h)
        {
            if (current == end)
            {
                solutions[color] = new List<Vector2Int>(path);
                if (SolveRecursive(colorIndex + 1, colors, colorNodes, solutions, grid, bridges, w, h))
                    return true;
                
                solutions[color].Clear();
                return false;
            }

            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            // Sort directions by heuristic distance to end node
            System.Array.Sort(dirs, (d1, d2) => {
                int dist1 = Mathf.Abs((current + d1).x - end.x) + Mathf.Abs((current + d1).y - end.y);
                int dist2 = Mathf.Abs((current + d2).x - end.x) + Mathf.Abs((current + d2).y - end.y);
                return dist1.CompareTo(dist2);
            });

            foreach (var dir in dirs)
            {
                Vector2Int next = current + dir;

                if (next.x < 0 || next.x >= w || next.y < 0 || next.y >= h)
                    continue;

                // Path cannot intersect itself
                if (path.Contains(next))
                    continue;

                bool isBridge = bridges.Contains(next);
                bool isValid = false;

                if (next == end)
                {
                    isValid = true;
                }
                else if (isBridge)
                {
                    // Bridge rule: must cross straight (next + dir)
                    Vector2Int bridgeExit = next + dir;
                    if (bridgeExit.x >= 0 && bridgeExit.x < w && bridgeExit.y >= 0 && bridgeExit.y < h && !path.Contains(bridgeExit))
                    {
                        // Check occupancy on next + dir
                        if (grid[bridgeExit.x, bridgeExit.y] == ColorType.None || bridgeExit == end)
                        {
                            // Check bridge usage: can cross if it's empty, or crossed perpendicularly by exactly 1 other path
                            int otherUseCount = 0;
                            ColorType otherColor = ColorType.None;
                            for (int i = 0; i < colorIndex; i++)
                            {
                                var otherPath = solutions[colors[i]];
                                if (otherPath.Contains(next))
                                {
                                    otherUseCount++;
                                    otherColor = colors[i];
                                }
                            }

                            if (otherUseCount == 0)
                            {
                                isValid = true;
                            }
                            else if (otherUseCount == 1)
                            {
                                // Verify perpendicular crossing direction
                                var otherPath = solutions[otherColor];
                                int otherIdx = otherPath.IndexOf(next);
                                if (otherIdx > 0 && otherIdx < otherPath.Count - 1)
                                {
                                    Vector2Int otherIn = next - otherPath[otherIdx - 1];
                                    Vector2Int otherOut = otherPath[otherIdx + 1] - next;
                                    if (otherIn == otherOut && Vector2.Dot(dir, otherIn) == 0)
                                    {
                                        isValid = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Regular cell: must be empty
                    if (grid[next.x, next.y] == ColorType.None)
                    {
                        isValid = true;
                    }
                }

                if (isValid)
                {
                    if (isBridge)
                    {
                        Vector2Int bridgeExit = next + dir;
                        ColorType oldNext = grid[next.x, next.y];
                        ColorType oldExit = grid[bridgeExit.x, bridgeExit.y];

                        grid[next.x, next.y] = color;
                        grid[bridgeExit.x, bridgeExit.y] = color;
                        path.Add(next);
                        path.Add(bridgeExit);

                        if (FindPathRecursive(bridgeExit, end, color, path, colorIndex, colors, colorNodes, solutions, grid, bridges, w, h))
                            return true;

                        path.RemoveAt(path.Count - 1);
                        path.RemoveAt(path.Count - 1);
                        grid[next.x, next.y] = oldNext;
                        grid[bridgeExit.x, bridgeExit.y] = oldExit;
                    }
                    else
                    {
                        grid[next.x, next.y] = color;
                        path.Add(next);

                        if (FindPathRecursive(next, end, color, path, colorIndex, colors, colorNodes, solutions, grid, bridges, w, h))
                            return true;

                        path.RemoveAt(path.Count - 1);
                        grid[next.x, next.y] = ColorType.None;
                    }
                }
            }

            return false;
        }

        // ================= LEVEL TEMPLATES =================
        private void ApplyTemplate5x5()
        {
            Undo.RecordObject(_data, "Apply Template 5x5");
            _data.width = 5;
            _data.height = 5;
            _data.initialNodes.Clear();
            _data.solutions.Clear();
            _data.bridgePositions.Clear();

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 4), color = ColorType.Blue });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(4, 4), color = ColorType.Blue });

            EditorUtility.SetDirty(_data);
        }

        private void ApplyTemplateCross()
        {
            Undo.RecordObject(_data, "Apply Template Cross");
            _data.width = 5;
            _data.height = 5;
            _data.initialNodes.Clear();
            _data.solutions.Clear();
            _data.bridgePositions.Clear();

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(2, 0), color = ColorType.Green });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(2, 4), color = ColorType.Green });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 2), color = ColorType.Yellow });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(4, 2), color = ColorType.Yellow });

            EditorUtility.SetDirty(_data);
        }

        private void ApplyTemplateBridge6x6()
        {
            Undo.RecordObject(_data, "Apply Template Bridge 6x6");
            _data.width = 6;
            _data.height = 6;
            _data.initialNodes.Clear();
            _data.solutions.Clear();
            _data.bridgePositions.Clear();

            // Set bridge at (2, 2) and (3, 3)
            _data.bridgePositions.Add(new Vector2Int(2, 2));
            _data.bridgePositions.Add(new Vector2Int(3, 3));

            // Set nodes
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 2), color = ColorType.Red });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(5, 2), color = ColorType.Red });

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(2, 0), color = ColorType.Blue });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(2, 5), color = ColorType.Blue });

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 3), color = ColorType.Green });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(5, 3), color = ColorType.Green });

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(3, 0), color = ColorType.Yellow });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(3, 5), color = ColorType.Yellow });

            EditorUtility.SetDirty(_data);
        }
    }
}
#endif
