#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PixelFlow.Data;
using PixelFlow.Services;
using System.Collections.Generic;
using System.Linq;

namespace PixelFlow.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private enum EditMode { None, Node, Path, Bridge, Obstacle, OneWay, Eraser }
        private EditMode _currentMode = EditMode.Node;
        private ColorType _currentColor = ColorType.Red;
        private ObstacleType _currentObstacleType = ObstacleType.Construction;
        private Vector2Int _currentOneWayDirection = Vector2Int.right;
        // GDD §3.2, §3.4: GridNode editör default değerleri
        private ShapeType _currentShape = ShapeType.Circle;
        private NodeType _currentNodeType = PixelFlow.Data.NodeType.Home;
        private bool _currentIsSource = true;
        private int _currentPairIndex = 0;
        private bool _showNodeProperties = false;
        
        private LevelData _data;
        private Vector2Int _lastPaintedCell = new Vector2Int(-1, -1);
        private bool _requireFullGridCoverage = true;
        private bool _showValidator = true;
        private bool _mirrorHorizontal = false;
        private bool _mirrorVertical = false;
        private float _cellSizeSlider = 38f;
        private string _solveStatus = "";

        // UI Styles — cached once in InitStyles, never per-frame
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _statusOkStyle;
        private GUIStyle _statusWarnStyle;
        private GUIStyle _tierBadgeStyle;
        private GUIStyle _bridgeCountStyle;
        private GUIStyle _arrowStyle; // cached for one-way cell arrows

        private void OnEnable()
        {
            _data = (LevelData)target;
            _tierBadgeStyle = null; // force re-init on next GUI
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            InitStyles();

            // Auto-align shapes with colors in LevelData
            bool updatedAny = false;
            if (_data != null && _data.initialNodes != null)
            {
                for (int i = 0; i < _data.initialNodes.Count; i++)
                {
                    var node = _data.initialNodes[i];
                    var expectedShape = GetDefaultShapeForColor(node.color);
                    if (node.shape != expectedShape)
                    {
                        node.shape = expectedShape;
                        _data.initialNodes[i] = node;
                        updatedAny = true;
                    }
                }
            }
            if (updatedAny)
            {
                EditorUtility.SetDirty(_data);
            }

            int complexityScore = CalculateComplexityScore(_data);
            string tierName = GetDifficultyTierName(complexityScore);
            Color tierColor = GetDifficultyTierColor(complexityScore);
            _tierBadgeStyle.normal.textColor = tierColor;

            // Header Banner
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Space(5);
            GUILayout.Label("Pixel Flow Level Editor (Master Studio)", _headerStyle);
            GUILayout.Label($"Difficulty: {tierName} ({complexityScore} pts) | Rule: {(_data.requireFullGridCoverage ? "Full Grid" : "Flexible")}", _tierBadgeStyle);
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
            int newViaductLimit = EditorGUILayout.IntSlider("Viaduct Limit", _data.viaductLimit, 0, 10);
            int newFlowThreshold = EditorGUILayout.IntSlider("Flow Score Target", _data.flowScoreThreshold, 1, 50);
            bool newCoverage = EditorGUILayout.Toggle("Require Full Grid Coverage", _data.requireFullGridCoverage);

            // game_plan.md §2.1.A: 3D Toy Teması & Zıplayan Araç (Bouncy Physics) Ayarları
            GUILayout.Space(5);
            GUILayout.Label("3D Toy Theme & Bouncy Physics (game_plan.md §2.1.A)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            ToyThemeType newToyTheme = (ToyThemeType)EditorGUILayout.EnumPopup("3D Toy Theme", _data.toyTheme);

            float newBounceForce = EditorGUILayout.Slider("Bounce Force (g-force)", _data.bouncyPhysics.BounceForce, 1f, 10f);
            float newBounceDamping = EditorGUILayout.Slider("Bounce Damping", _data.bouncyPhysics.BounceDamping, 0.1f, 1.0f);
            float newSquishFactor = EditorGUILayout.Slider("Squish Factor", _data.bouncyPhysics.SquishFactor, 0.05f, 0.8f);
            EditorGUI.indentLevel--;

            // GDD §3.6: PhaseDefinition ScriptableObject Assignment
            GUILayout.Space(5);
            GUILayout.Label("Phase Configuration (GDD §3.6)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            PhaseDefinitionAsset phaseAsset = (PhaseDefinitionAsset)EditorGUILayout.ObjectField(
                "Phase Template", null, typeof(PhaseDefinitionAsset), false);
            if (phaseAsset != null)
            {
                // Auto-fill fields from PhaseDefinition if assigned
                EditorGUILayout.HelpBox($"Phase: {phaseAsset.Phase} | Levels {phaseAsset.StartLevelIndex}-{phaseAsset.EndLevelIndex}", MessageType.Info);
            }
            EditorGUI.indentLevel--;

            // GDD §9: Difficulty Score Display
            if (_data.difficultyScore > 0)
            {
                GUILayout.Label($"Procedural Difficulty Score (GDD §9): {_data.difficultyScore}", _tierBadgeStyle);
            }

            // GDD §3.5: Yıldız Kriterleri ve Tutorial Event
            GUILayout.Space(5);
            GUILayout.Label("Star Criteria & Tutorial (GDD §3.5, §8)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            string new1Star = EditorGUILayout.TextField("1 Star Criteria", _data.stars.OneStar);
            string new2Star = EditorGUILayout.TextField("2 Stars Criteria", _data.stars.TwoStars);
            string new3Star = EditorGUILayout.TextField("3 Stars Criteria", _data.stars.ThreeStars);
            TutorialEvent newTutorial = (TutorialEvent)EditorGUILayout.EnumPopup("Tutorial Event", _data.tutorialEvent);
            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_data, "Change Level Settings");
                _data.levelIndex = newLevelIndex;
                _data.width = newWidth;
                _data.height = newHeight;
                _data.viaductLimit = newViaductLimit;
                _data.flowScoreThreshold = newFlowThreshold;
                _data.requireFullGridCoverage = newCoverage;
                _data.toyTheme = newToyTheme;
                _data.bouncyPhysics = new BouncyPhysicsConfig
                {
                    BounceForce = newBounceForce,
                    BounceDamping = newBounceDamping,
                    SquishFactor = newSquishFactor
                };
                _data.stars = new StarCriteria { OneStar = new1Star, TwoStars = new2Star, ThreeStars = new3Star };
                _data.tutorialEvent = newTutorial;
                _requireFullGridCoverage = newCoverage;
                SanitizeGridBounds();
                EditorUtility.SetDirty(_data);
            }

            int bridgeCount = _data.bridgePositions.Count;
            bool limitOk = bridgeCount <= _data.viaductLimit;
            _bridgeCountStyle.normal.textColor = limitOk ? new Color(0.12f, 0.65f, 0.22f) : new Color(0.85f, 0.2f, 0.18f);
            GUILayout.Label($"Bridges: {bridgeCount} / Viaduct Limit: {_data.viaductLimit}", _bridgeCountStyle);
            if (!limitOk)
            {
                EditorGUILayout.HelpBox("Bridge count exceeds viaduct limit! Players won't have enough viaducts.", MessageType.Warning);
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Editing Tools Card
            GUILayout.BeginVertical(_cardStyle);
            GUILayout.Label("Editor Controls & Symmetry Tools", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // Edit Mode Selector (Horizontal Buttons)
            GUILayout.Label("Select Tool:", EditorStyles.miniLabel);
            GUILayout.BeginHorizontal();
            EditMode[] modes = { EditMode.Node, EditMode.Path, EditMode.Bridge, EditMode.Obstacle, EditMode.OneWay, EditMode.Eraser };
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

            GUILayout.Space(6);

            // Symmetry & Zoom Toolbar
            GUILayout.BeginHorizontal();
            _mirrorHorizontal = GUILayout.Toggle(_mirrorHorizontal, "Mirror Horizontal (X)", GUILayout.Width(150));
            _mirrorVertical = GUILayout.Toggle(_mirrorVertical, "Mirror Vertical (Y)", GUILayout.Width(140));
            GUILayout.EndHorizontal();

            _cellSizeSlider = EditorGUILayout.Slider("Cell Zoom Size", _cellSizeSlider, 24f, 60f);

            GUILayout.Space(8);

            // Obstacle Type Selector
            if (_currentMode == EditMode.Obstacle)
            {
                GUILayout.Label("Select Obstacle Type:", EditorStyles.miniLabel);
                _currentObstacleType = (ObstacleType)EditorGUILayout.EnumPopup("Obstacle Type", _currentObstacleType);
                GUILayout.Space(5);
            }

            // OneWay Direction Selector
            if (_currentMode == EditMode.OneWay)
            {
                GUILayout.Label("Select OneWay Direction:", EditorStyles.miniLabel);
                string[] directions = { "Right (→)", "Left (←)", "Up (↑)", "Down (↓)" };
                int selectedIndex = 0;
                if (_currentOneWayDirection == Vector2Int.left) selectedIndex = 1;
                else if (_currentOneWayDirection == Vector2Int.up) selectedIndex = 2;
                else if (_currentOneWayDirection == Vector2Int.down) selectedIndex = 3;

                int newIndex = EditorGUILayout.Popup("Direction", selectedIndex, directions);
                if (newIndex == 0) _currentOneWayDirection = Vector2Int.right;
                else if (newIndex == 1) _currentOneWayDirection = Vector2Int.left;
                else if (newIndex == 2) _currentOneWayDirection = Vector2Int.up;
                else if (newIndex == 3) _currentOneWayDirection = Vector2Int.down;
                GUILayout.Space(5);
            }

            // GDD §3.2, §3.4: Node Properties panel (sadece Node modunda)
            if (_currentMode == EditMode.Node)
            {
                _showNodeProperties = EditorGUILayout.Foldout(_showNodeProperties, "Node Properties (GDD §3.2-3.4)");
                if (_showNodeProperties)
                {
                    EditorGUI.indentLevel++;
                    _currentShape = (ShapeType)EditorGUILayout.EnumPopup("Shape", _currentShape);
                    _currentNodeType = (PixelFlow.Data.NodeType)EditorGUILayout.EnumPopup("Node Type", _currentNodeType);
                    _currentIsSource = EditorGUILayout.Toggle("Is Source", _currentIsSource);
                    _currentPairIndex = EditorGUILayout.IntField("Pair Index", _currentPairIndex);
                    EditorGUI.indentLevel--;
                }
            }

            // Color Selector (Horizontal Palette)
            if (_currentMode == EditMode.Node || _currentMode == EditMode.Path)
            {
                GUILayout.Label("Select Color:", EditorStyles.miniLabel);
                GUILayout.BeginHorizontal();
                ColorType[] colors = {
                    ColorType.Red, ColorType.Green, ColorType.Blue,
                    ColorType.Yellow, ColorType.Purple
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
                        _currentShape = GetDefaultShapeForColor(c);
                    }
                }
                GUI.backgroundColor = Color.white; // Reset
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                if (_currentMode == EditMode.Path)
                {
                    if (GUILayout.Button($"Auto-Path Selected Color ({_currentColor})", GUILayout.Height(24)))
                    {
                        AutoSolveSelectedColor(_currentColor);
                    }
                }
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

            // Cached tier badge — color is set per-frame via .normal.textColor
            if (_tierBadgeStyle == null)
            {
                _tierBadgeStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            // Cached bridge count style
            if (_bridgeCountStyle == null)
            {
                _bridgeCountStyle = new GUIStyle(EditorStyles.boldLabel);
            }

            // Cached arrow style for OneWay cells
            if (_arrowStyle == null)
            {
                _arrowStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.2f, 0.8f, 1f, 1f) }
                };
            }
        }

        private void SanitizeGridBounds()
        {
            // Remove items outside new width/height bounds
            _data.initialNodes.RemoveAll(n => n.position.x >= _data.width || n.position.y >= _data.height);
            _data.bridgePositions.RemoveAll(p => p.x >= _data.width || p.y >= _data.height);
            _data.obstacles.RemoveAll(o => o.position.x >= _data.width || o.position.y >= _data.height);
            if (_data.oneWayCells != null)
            {
                _data.oneWayCells.RemoveAll(ow => ow.position.x >= _data.width || ow.position.y >= _data.height);
            }
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
            float cellSize = _cellSizeSlider;
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

            // === PASS 3: Obstacle + Bridge + Node (en üstte) ===
            for (int y = _data.height - 1; y >= 0; y--)
            {
                for (int x = 0; x < _data.width; x++)
                {
                    int drawY = _data.height - 1 - y;
                    Rect cellRect = new Rect(
                        gridRect.x + x * (cellSize + spacing),
                        gridRect.y + drawY * (cellSize + spacing),
                        cellSize, cellSize);

                    Vector2Int pos = new Vector2Int(x, y);
                    bool isBridge = _data.bridgePositions.Contains(pos);
                    var node = _data.initialNodes.Find(n => n.position == pos);
                    bool isNode = node.color != ColorType.None;
                    var obstacle = _data.obstacles != null ? _data.obstacles.Find(o => o.position == pos) : default;
                    bool hasObstacle = _data.obstacles != null && _data.obstacles.Any(o => o.position == pos);

                    if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && cellRect.Contains(mousePos))
                    {
                        if (e.type == EventType.MouseDown || _lastPaintedCell != pos)
                        {
                            HandleCellClick(x, y);
                            _lastPaintedCell = pos;
                            e.Use();
                        }
                    }

                    if (hasObstacle)
                    {
                        Color obsColor = GetObstacleColor(obstacle.type);
                        EditorGUI.DrawRect(new Rect(cellRect.x + 2, cellRect.y + 2, cellSize - 4, cellSize - 4), obsColor);
                    }

                    // Draw OneWay Cells (GDD §2.7 — viyadüğe alternatif)
                    var oneWayCell = _data.oneWayCells != null ? _data.oneWayCells.Find(ow => ow.position == pos) : default;
                    bool isOneWay = _data.oneWayCells != null && _data.oneWayCells.Any(ow => ow.position == pos);
                    if (isOneWay)
                    {
                        Vector2 center = cellRect.center;
                        Handles.BeginGUI();
                        Handles.color = new Color(0.2f, 0.8f, 1f, 1f); // Neon Cyan ok
                        string arrowText = "→";
                        if (oneWayCell.allowedDirection == Vector2Int.left) arrowText = "←";
                        else if (oneWayCell.allowedDirection == Vector2Int.up) arrowText = "↑";
                        else if (oneWayCell.allowedDirection == Vector2Int.down) arrowText = "↓";
                        
                        _arrowStyle.fontSize = Mathf.RoundToInt(cellSize * 0.5f);
                        GUI.Label(new Rect(center.x - cellSize * 0.5f, center.y - cellSize * 0.5f, cellSize, cellSize), arrowText, _arrowStyle);
                        Handles.EndGUI();
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
                        DrawNodeShape(center, cellSize * 0.26f, nodeColor, node.shape);
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
            ApplyCellAction(x, y);

            if (_mirrorHorizontal)
            {
                ApplyCellAction(_data.width - 1 - x, y);
            }
            if (_mirrorVertical)
            {
                ApplyCellAction(x, _data.height - 1 - y);
            }
            if (_mirrorHorizontal && _mirrorVertical)
            {
                ApplyCellAction(_data.width - 1 - x, _data.height - 1 - y);
            }

            EditorUtility.SetDirty(_data);
        }

        private void ApplyCellAction(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _data.width || y >= _data.height) return;
            Vector2Int pos = new Vector2Int(x, y);

            if (_currentMode == EditMode.Eraser)
            {
                _data.initialNodes.RemoveAll(n => n.position == pos);
                if (_data.obstacles != null) _data.obstacles.RemoveAll(o => o.position == pos);
                if (_data.oneWayCells != null) _data.oneWayCells.RemoveAll(ow => ow.position == pos);
                foreach (var sol in _data.solutions)
                {
                    if (sol.pathPositions != null)
                        sol.pathPositions.RemoveAll(p => p == pos);
                }
                _data.bridgePositions.Remove(pos);
            }
            else if (_currentMode == EditMode.Node)
            {
                // GDD §3.2-3.4: Varolan node property'lerini silmeden ÖNCE oku
                var existing = _data.initialNodes.Find(n => n.position == pos);
                if (existing.color != ColorType.None)
                {
                    _currentColor = existing.color;
                    _currentShape = existing.shape;
                    _currentNodeType = existing.type;
                    _currentIsSource = existing.isSource;
                    _currentPairIndex = existing.pairIndex;
                }

                _data.initialNodes.RemoveAll(n => n.position == pos);
                if (_data.obstacles != null) _data.obstacles.RemoveAll(o => o.position == pos);
                if (_data.oneWayCells != null) _data.oneWayCells.RemoveAll(ow => ow.position == pos);
                foreach (var sol in _data.solutions)
                {
                    if (sol.pathPositions != null)
                        sol.pathPositions.Remove(pos);
                }

                _data.initialNodes.Add(new GridNode
                {
                    position = pos,
                    color = _currentColor,
                    shape = _currentShape,
                    type = _currentNodeType,
                    isSource = _currentIsSource,
                    pairIndex = _currentPairIndex
                });
            }
            else if (_currentMode == EditMode.Obstacle)
            {
                _data.initialNodes.RemoveAll(n => n.position == pos);
                if (_data.obstacles == null) _data.obstacles = new List<ObstacleData>();
                _data.obstacles.RemoveAll(o => o.position == pos);
                if (_data.oneWayCells != null) _data.oneWayCells.RemoveAll(ow => ow.position == pos);
                _data.obstacles.Add(new ObstacleData { position = pos, type = _currentObstacleType });
            }
            else if (_currentMode == EditMode.OneWay)
            {
                _data.initialNodes.RemoveAll(n => n.position == pos);
                if (_data.obstacles != null) _data.obstacles.RemoveAll(o => o.position == pos);
                if (_data.oneWayCells == null) _data.oneWayCells = new List<OneWayCell>();
                _data.oneWayCells.RemoveAll(ow => ow.position == pos);
                _data.oneWayCells.Add(new OneWayCell { position = pos, allowedDirection = _currentOneWayDirection });
            }
            else if (_currentMode == EditMode.Bridge)
            {
                _data.initialNodes.RemoveAll(n => n.position == pos);
                if (_data.oneWayCells != null) _data.oneWayCells.RemoveAll(ow => ow.position == pos);
                if (!_data.bridgePositions.Contains(pos))
                    _data.bridgePositions.Add(pos);
                else
                    _data.bridgePositions.Remove(pos);
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
                    else
                    {
                        _data.solutions[solIndex].pathPositions.Remove(pos);
                    }
                }
            }
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

            if (_data.bridgePositions.Count > _data.viaductLimit)
            {
                warnings.Add($"Bridge positions ({_data.bridgePositions.Count}) exceed viaduct limit ({_data.viaductLimit}). Players won't have enough viaducts to place at all crossings.");
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
                case ColorType.Purple: return new Color(0.68f, 0.25f, 0.95f); // Rich purple
                default: return Color.gray;
            }
        }

        // ================= AUTO SOLVER IMPLEMENTATION =================
        private bool SolveLevel(LevelData level)
        {
            var colorNodes = new Dictionary<ColorType, List<Vector2Int>>();
            foreach (var node in level.initialNodes)
            {
                if (node.color == ColorType.None) continue;
                if (!colorNodes.ContainsKey(node.color))
                    colorNodes[node.color] = new List<Vector2Int>();
                colorNodes[node.color].Add(node.position);
            }

            foreach (var kvp in colorNodes)
            {
                if (kvp.Value.Count != 2)
                {
                    Debug.LogError($"Color '{kvp.Key}' has {kvp.Value.Count} nodes. Each color must have exactly 2 nodes to solve.");
                    return false;
                }
            }

            var solver = new RuntimePathSolver();
            if (!solver.Solve(level, out var solutions))
                return false;

            if (_requireFullGridCoverage)
            {
                var coverage = new HashSet<Vector2Int>();
                foreach (var path in solutions.Values)
                    foreach (var p in path)
                        coverage.Add(p);

                for (int x = 0; x < level.width; x++)
                    for (int y = 0; y < level.height; y++)
                    {
                        var pos = new Vector2Int(x, y);
                        if (!coverage.Contains(pos) && !level.bridgePositions.Contains(pos))
                            return false;
                    }
            }

            Undo.RecordObject(level, "Auto-Solve Level");
            level.solutions.Clear();
            foreach (var kvp in solutions)
            {
                level.solutions.Add(new PathSolution
                {
                    color = kvp.Key,
                    pathPositions = new List<Vector2Int>(kvp.Value)
                });
            }
            return true;
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

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 0), color = ColorType.Red, shape = ShapeType.Triangle, type = PixelFlow.Data.NodeType.Home, isSource = true, pairIndex = 0 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(4, 0), color = ColorType.Red, shape = ShapeType.Triangle, type = PixelFlow.Data.NodeType.Office, isSource = false, pairIndex = 1 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 4), color = ColorType.Blue, shape = ShapeType.Circle, type = PixelFlow.Data.NodeType.Hospital, isSource = true, pairIndex = 0 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(4, 4), color = ColorType.Blue, shape = ShapeType.Circle, type = PixelFlow.Data.NodeType.School, isSource = false, pairIndex = 1 });

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

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(2, 0), color = ColorType.Green, shape = ShapeType.Diamond, type = PixelFlow.Data.NodeType.Park, isSource = true, pairIndex = 0 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(2, 4), color = ColorType.Green, shape = ShapeType.Diamond, type = PixelFlow.Data.NodeType.Mall, isSource = false, pairIndex = 1 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 2), color = ColorType.Yellow, shape = ShapeType.Square, type = PixelFlow.Data.NodeType.Home, isSource = true, pairIndex = 0 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(4, 2), color = ColorType.Yellow, shape = ShapeType.Square, type = PixelFlow.Data.NodeType.Office, isSource = false, pairIndex = 1 });

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
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 2), color = ColorType.Red, shape = ShapeType.Triangle, type = PixelFlow.Data.NodeType.Home, isSource = true, pairIndex = 0 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(5, 2), color = ColorType.Red, shape = ShapeType.Triangle, type = PixelFlow.Data.NodeType.Office, isSource = false, pairIndex = 1 });

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(2, 0), color = ColorType.Blue, shape = ShapeType.Circle, type = PixelFlow.Data.NodeType.Hospital, isSource = true, pairIndex = 0 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(2, 5), color = ColorType.Blue, shape = ShapeType.Circle, type = PixelFlow.Data.NodeType.School, isSource = false, pairIndex = 1 });

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(0, 3), color = ColorType.Green, shape = ShapeType.Diamond, type = PixelFlow.Data.NodeType.Park, isSource = true, pairIndex = 0 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(5, 3), color = ColorType.Green, shape = ShapeType.Diamond, type = PixelFlow.Data.NodeType.Mall, isSource = false, pairIndex = 1 });

            _data.initialNodes.Add(new GridNode { position = new Vector2Int(3, 0), color = ColorType.Yellow, shape = ShapeType.Square, type = PixelFlow.Data.NodeType.Home, isSource = true, pairIndex = 0 });
            _data.initialNodes.Add(new GridNode { position = new Vector2Int(3, 5), color = ColorType.Yellow, shape = ShapeType.Square, type = PixelFlow.Data.NodeType.Office, isSource = false, pairIndex = 1 });

            EditorUtility.SetDirty(_data);
        }

        private void AutoSolveSelectedColor(ColorType targetColor)
        {
            var solver = new RuntimePathSolver();
            if (solver.Solve(_data, out var solutions))
            {
                if (solutions.TryGetValue(targetColor, out var path))
                {
                    Undo.RecordObject(_data, "Auto-Solve Selected Color");
                    int solIdx = _data.solutions.FindIndex(s => s.color == targetColor);
                    if (solIdx != -1)
                    {
                        var sol = _data.solutions[solIdx];
                        sol.pathPositions = new List<Vector2Int>(path);
                        _data.solutions[solIdx] = sol;
                    }
                    else
                    {
                        _data.solutions.Add(new PathSolution { color = targetColor, pathPositions = new List<Vector2Int>(path) });
                    }
                    EditorUtility.SetDirty(_data);
                    Debug.Log($"[LevelDataEditor] Auto-solved path for color {targetColor}");
                }
            }
        }

        private static Color GetObstacleColor(ObstacleType obstacleType)
        {
            switch (obstacleType)
            {
                case ObstacleType.Construction: return new Color(0.85f, 0.65f, 0.15f, 0.85f); // Construction Yellow
                case ObstacleType.Lake: return new Color(0.15f, 0.55f, 0.85f, 0.85f);        // Water Blue
                case ObstacleType.Park: return new Color(0.2f, 0.65f, 0.3f, 0.85f);          // Park Green
                case ObstacleType.OneWay: return new Color(0.8f, 0.3f, 0.3f, 0.85f);         // Red OneWay
                case ObstacleType.Ferry: return new Color(0.4f, 0.7f, 0.9f, 0.85f);          // Cyan Ferry
                case ObstacleType.NarrowPass: return new Color(0.4f, 0.4f, 0.45f, 0.85f);     // Dark Steel
                default: return new Color(0.5f, 0.5f, 0.5f, 0.85f);
            }
        }

        private static int CalculateComplexityScore(LevelData lvl)
        {
            int area = lvl.width * lvl.height;
            int nodes = lvl.initialNodes != null ? lvl.initialNodes.Count : 0;
            int bridges = lvl.bridgePositions != null ? lvl.bridgePositions.Count : 0;
            int obstacles = lvl.obstacles != null ? lvl.obstacles.Count : 0;
            return (area * 2) + (nodes * 8) + (bridges * 6) + (obstacles * 4) - (lvl.viaductLimit * 3);
        }

        private static string GetDifficultyTierName(int score)
        {
            if (score < 25) return "Easy";
            if (score < 42) return "Medium";
            if (score < 62) return "Hard";
            if (score < 85) return "Expert";
            return "Master";
        }

        private static Color GetDifficultyTierColor(int score)
        {
            if (score < 25) return new Color(0.12f, 0.65f, 0.22f);
            if (score < 42) return new Color(0.2f, 0.6f, 1f);
            if (score < 62) return new Color(0.9f, 0.6f, 0.1f);
            return new Color(0.85f, 0.2f, 0.18f);
        }

        private void DrawNodeShape(Vector3 center, float radius, Color color, ShapeType shape)
        {
            Handles.BeginGUI();
            switch (shape)
            {
                case ShapeType.Circle:
                    Handles.color = Color.black;
                    Handles.DrawSolidDisc(center, Vector3.forward, radius * 1.25f);
                    Handles.color = color;
                    Handles.DrawSolidDisc(center, Vector3.forward, radius);
                    break;
                case ShapeType.Square:
                    float size = radius * 1.6f;
                    EditorGUI.DrawRect(new Rect(center.x - size * 0.625f, center.y - size * 0.625f, size * 1.25f, size * 1.25f), Color.black);
                    EditorGUI.DrawRect(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), color);
                    break;
                case ShapeType.Triangle:
                    Vector3[] outlineTri = new Vector3[] {
                        new Vector3(center.x, center.y - radius * 1.25f, 0),
                        new Vector3(center.x - radius * 1.1f, center.y + radius * 0.75f, 0),
                        new Vector3(center.x + radius * 1.1f, center.y + radius * 0.75f, 0)
                    };
                    Handles.color = Color.black;
                    Handles.DrawAAConvexPolygon(outlineTri);
                    
                    Vector3[] fillTri = new Vector3[] {
                        new Vector3(center.x, center.y - radius, 0),
                        new Vector3(center.x - radius * 0.866f, center.y + radius * 0.5f, 0),
                        new Vector3(center.x + radius * 0.866f, center.y + radius * 0.5f, 0)
                    };
                    Handles.color = color;
                    Handles.DrawAAConvexPolygon(fillTri);
                    break;
                case ShapeType.Diamond:
                    Vector3[] outlineDia = new Vector3[] {
                        new Vector3(center.x, center.y - radius * 1.25f, 0),
                        new Vector3(center.x + radius * 1.25f, center.y, 0),
                        new Vector3(center.x, center.y + radius * 1.25f, 0),
                        new Vector3(center.x - radius * 1.25f, center.y, 0)
                    };
                    Handles.color = Color.black;
                    Handles.DrawAAConvexPolygon(outlineDia);
                    
                    Vector3[] fillDia = new Vector3[] {
                        new Vector3(center.x, center.y - radius, 0),
                        new Vector3(center.x + radius, center.y, 0),
                        new Vector3(center.x, center.y + radius, 0),
                        new Vector3(center.x - radius, center.y, 0)
                    };
                    Handles.color = color;
                    Handles.DrawAAConvexPolygon(fillDia);
                    break;
                case ShapeType.Star:
                    Handles.color = Color.black;
                    DrawStarPolygons(center, radius * 1.25f);
                    Handles.color = color;
                    DrawStarPolygons(center, radius);
                    break;
            }
            Handles.EndGUI();
        }

        private void DrawStarPolygons(Vector3 center, float radius)
        {
            Vector3[] tri1 = new Vector3[] {
                new Vector3(center.x, center.y - radius, 0),
                new Vector3(center.x - radius * 0.866f, center.y + radius * 0.5f, 0),
                new Vector3(center.x + radius * 0.866f, center.y + radius * 0.5f, 0)
            };
            Vector3[] tri2 = new Vector3[] {
                new Vector3(center.x, center.y + radius, 0),
                new Vector3(center.x - radius * 0.866f, center.y - radius * 0.5f, 0),
                new Vector3(center.x + radius * 0.866f, center.y - radius * 0.5f, 0)
            };
            Handles.DrawAAConvexPolygon(tri1);
            Handles.DrawAAConvexPolygon(tri2);
        }

        public static ShapeType GetDefaultShapeForColor(ColorType color)
        {
            switch (color)
            {
                case ColorType.Blue:    return ShapeType.Circle;
                case ColorType.Red:     return ShapeType.Triangle;
                case ColorType.Yellow:  return ShapeType.Square;
                case ColorType.Green:   return ShapeType.Diamond;
                case ColorType.Purple:  return ShapeType.Star;
                default:                return ShapeType.Circle;
            }
        }
    }
}
#endif
