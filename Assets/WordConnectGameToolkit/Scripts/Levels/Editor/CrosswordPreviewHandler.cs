using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WordsToolkit.Scripts.Utilities;
using System.Text;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.Services.BannedWords;
using Random = UnityEngine.Random;

namespace WordsToolkit.Scripts.Levels.Editor
{
    public static class CrosswordPreviewHandler
    {
        // Event that fires when crossword is manually changed via palette
        public static event Action OnCrosswordManuallyChanged;
        
        // Preview settings
        private const int previewCellSize = 28; // Increased from 24 for better visibility
        private const int previewSpacing = 2;
        public static int defaultGridColumns = 10; // Default fallback grid columns
        public static int defaultGridRows = 7; // Default fallback grid rows
        // Removed minimum height requirement to fit grid exactly
        
        // Color settings
        private static readonly Color backgroundColor = new Color(0.2f, 0.2f, 0.25f); // Dark blue-gray background
        private static readonly Color emptyGridColor = new Color(0.25f, 0.25f, 0.3f); // Slightly lighter than background
        private static readonly Color gridLineColor = new Color(0.4f, 0.4f, 0.45f); // Medium gray for grid lines
        private static readonly Color cellBorderColor = new Color(0.6f, 0.6f, 0.6f); // Light gray for cell borders
        private static readonly Color cellBackgroundColor = new Color(0.9f, 0.9f, 0.95f); // Light color for cells with letters
        private static readonly Color selectedLetterBgColor = new Color(0.7f, 0.9f, 1f); // Light blue for selected letter
        
        // Letter palette settings
        private const int letterPaletteCellSize = 19; // Size of each letter in the palette
        private const int letterPaletteSpacing = 2; // Spacing between letters
        private const int lettersPerRow = 13; // Number of letters per row in the palette
        private const int specialItemOffset = 20; // Spacing between special item and letters
        
        // Currently selected letter for manual placement (static so it persists between calls)
        private static char currentSelectedLetter = 'A';
        
        // Flag to track if special item is selected instead of a letter
        private static bool isSpecialItemSelected = false;
        private static IModelController _modelController;
        
        // Variables for editor state (using dictionaries to support multiple instances)
        // These were moved to LevelDataEditor.cs since we're moving the available words section there
        private static string _selectedWordToPlace = null;

        // Crossword variants cache
        private static Dictionary<string, CrosswordVariantsCache> _crosswordCache = new Dictionary<string, CrosswordVariantsCache>();
        
        // Cache for crossword variants
        private class CrosswordVariantsCache
        {
            public List<PreviewData> variants = new List<PreviewData>();
            public int currentIndex = 0;
            public string[] lastWords = null;
            public int lastColumns = 0;
            public int lastRows = 0;
            
            public bool IsValidFor(string[] words, int columns, int rows)
            {
                return lastWords != null && 
                       lastWords.Length == words.Length &&
                       lastWords.SequenceEqual(words) &&
                       lastColumns == columns &&
                       lastRows == rows;
            }
        }

        // Preview data class to store generated preview information
        public class PreviewData
        {
            private char[,] _grid;
            public char[,] grid 
            {
                get => _grid;
                set 
                {
                    _grid = value;
                    if (_grid != null)
                    {
                        columns = _grid.GetLength(0);
                        rows = _grid.GetLength(1);
                    }
                }
            }
            public List<WordPlacement> placements;
            public Vector2Int minBounds;
            public Vector2Int maxBounds;
            public int columns;
            public int rows;
            public bool isValid => grid != null && placements != null;
            
            // New properties for icon support
            public Texture2D iconTexture = null;
            public string iconPath = "";
            
            // Dictionary to track positions where icons are placed
            public Dictionary<Vector2Int, string> iconPositions = new Dictionary<Vector2Int, string>();
        }

        // Change tracking struct
        public struct PreviewChangeFlags
        {
            public bool paletteChanged;
            public bool gridChanged;

            public static PreviewChangeFlags None => new PreviewChangeFlags { paletteChanged = false, gridChanged = false };
            
            public bool NeedsRepaint => paletteChanged || gridChanged;
        }

        // Method to open the crossword grid in a separate window
        public static void OpenGridWindow(Level level, string languageCode)
        {
            if (level == null || string.IsNullOrEmpty(languageCode))
            {
                Debug.LogError("Cannot open grid window: Level or language code is null");
                return;
            }
            
            CrosswordGridWindow.ShowWindow(level, languageCode);
        }
        
        // Method to trigger the manual change event from external classes
        public static void TriggerManualChangeEvent()
        {
            OnCrosswordManuallyChanged?.Invoke();
        }

        // Generate preview data from a list of words with configurable grid size
        public static PreviewData GeneratePreview(string[] words, int columns, int rows, int? seed = null, bool reverseWordOrder = false, bool preferVertical = false)
        {
            if (words == null || words.Length == 0)
            {
                return null;
            }

            // Filter out empty words
            words = words.Where(w => !string.IsNullOrEmpty(w)).ToArray();
            if (words.Length == 0)
            {
                return null;
            }
            
            // Generate crossword with the provided or random seed
            char[,] grid;
            List<WordPlacement> placements;
            
            // Use provided seed or generate random one
            int actualSeed = seed ?? Random.Range(1, 10000);

            // If reverse word order is requested, reverse the array
            if (reverseWordOrder)
            {
                words = words.Reverse().ToArray();
            }

            // Try loading config first
            var config = Resources.Load<CrosswordGenerationConfigSO>("Settings/CrosswordConfig");
            bool success;
            
            try
            {
                if (config != null)
                {
                    var configStruct = config.ToConfig();
                    configStruct.seed = actualSeed;
                    
                    // Override columns and rows with the provided values
                    configStruct.columns = columns;
                    configStruct.rows = rows;

                    success = CrosswordGenerator.RegenerateCrossword(words, configStruct, out grid, out placements);
                }
                else
                {
                    // Create a default config with the specified columns and rows
                    var defaultConfig = CrosswordGenerationConfig.Default;
                    defaultConfig.seed = actualSeed;
                    defaultConfig.columns = columns;
                    defaultConfig.rows = rows;
                    
                    success = CrosswordGenerator.RegenerateCrossword(words, defaultConfig, out grid, out placements);
                }
                
                if (success && grid != null)
                {
                    // Calculate grid bounds
                    CrosswordGenerator.CalculateGridBounds(grid, out Vector2Int minBounds, out Vector2Int maxBounds);
                    

                    return new PreviewData
                    {
                        grid = grid,
                        placements = placements,
                        minBounds = minBounds,
                        maxBounds = maxBounds,
                        columns = columns,
                        rows = rows
                    };
                }
                else
                {
                    Debug.LogError("Failed to generate crossword preview");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception during preview generation: {ex.Message}\n{ex.StackTrace}");
            }
            
            return null;
        }

        // Draw preview in the inspector
        public static PreviewChangeFlags DrawPreview(PreviewData previewData, string letters, string langCode, Level level)
        {
            if (previewData == null || !previewData.isValid)
            {
                EditorGUILayout.HelpBox("No valid preview data available.", MessageType.Info);
                return PreviewChangeFlags.None;
            }

            var changes = PreviewChangeFlags.None;
            
            // First, draw the letter palette with language-specific letters, passing level and language code
            bool paletteChanged = DrawLetterPalette(letters, previewData,  previewData.iconTexture, level, langCode);
            if (paletteChanged)
                changes.paletteChanged = true;

            GUILayout.BeginHorizontal(GUILayout.Width(298));
            {
                // Display usage instructions based on selected mode
                if (isSpecialItemSelected)
                {
                    EditorGUILayout.HelpBox("Click on a letter to place the special item above it", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Left-click: Place letter | Right-click: Remove", MessageType.Info);
                }

            }
            GUILayout.EndHorizontal();

            // Always use the dimensions from the preview data
            int gridWidth = previewData.columns;
            int gridHeight = previewData.rows;
            
            // Validate grid width and height (fallback to defaults if needed)
            if (gridWidth <= 0) gridWidth = defaultGridColumns;
            if (gridHeight <= 0) gridHeight = defaultGridRows;

            // Initialize min/max to show full grid
            Vector2Int min = Vector2Int.zero;
            Vector2Int max = new Vector2Int(gridWidth - 1, gridHeight - 1);

            // Calculate preview size
            float totalWidth = gridWidth * (previewCellSize + previewSpacing);
            float totalHeight = gridHeight * (previewCellSize + previewSpacing);

            // Create horizontal layout to place grid and buttons side by side
            EditorGUILayout.BeginHorizontal();

            // Begin the preview area - fitting the grid exactly with no extra space
            EditorGUILayout.BeginVertical();
            Rect previewRect = GUILayoutUtility.GetRect(totalWidth, totalHeight, GUILayout.Width(totalWidth), GUILayout.Height(totalHeight));

            GUILayout.Space(5);
            // Add horizontal layout for buttons below the grid
            EditorGUILayout.BeginHorizontal();

            // Add "Open Grid Window" button
            if (GUILayout.Button(new GUIContent("Grid Window", EditorGUIUtility.IconContent("SceneView Icon").image), GUILayout.Width(100), GUILayout.Height(30)))
            {
                CrosswordGridWindow.ShowWindow(level, langCode);
            }

            // Add cache info and clear cache button
            if (level != null && !string.IsNullOrEmpty(langCode))
            {
                string cacheInfo = GetCacheInfo(level, langCode);
                EditorGUILayout.LabelField(cacheInfo, EditorStyles.miniLabel, GUILayout.Width(150));
                
                if (GUILayout.Button("Clear Cache", GUILayout.Width(80), GUILayout.Height(30)))
                {
                    ClearCrosswordCache(level, langCode);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Define styles
            GUIStyle cellStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16, // Increased font size
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black },
                padding = new RectOffset(0, 0, 0, 0), // Remove any padding
                margin = new RectOffset(0, 0, 0, 0),  // Remove any margin
                contentOffset = Vector2.zero         // Reset any content offset
            };

            GUIStyle numberStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.0f, 0.0f, 0.6f) } // Stronger blue
            };

            // Draw dark background
            EditorGUI.DrawRect(previewRect, backgroundColor);

            // Draw entire grid (including empty cells)
            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    // Calculate position for this cell
                    float posX = previewRect.x + (x - min.x) * (previewCellSize + previewSpacing);
                    float posY = previewRect.y + (y - min.y) * (previewCellSize + previewSpacing);

                    Rect cellRect = new Rect(posX, posY, previewCellSize, previewCellSize);

                    // Safe grid access with bounds checking
                    char c = (x >= 0 && x < previewData.grid.GetLength(0) &&
                            y >= 0 && y < previewData.grid.GetLength(1)) ? previewData.grid[x, y] : (char)0;
                    bool hasLetter = (c != 0);

                    // Check if this cell has an icon
                    Vector2Int pos = new Vector2Int(x, y);
                    bool hasIcon = previewData.iconPositions.ContainsKey(pos);

                    // Draw cell background (different color for empty cells vs. letter/icon cells)
                    Color cellColor = hasLetter || hasIcon ? cellBackgroundColor : emptyGridColor;
                    EditorGUI.DrawRect(cellRect, cellColor);

                    // Draw grid border
                    Handles.color = hasLetter || hasIcon ? cellBorderColor : gridLineColor;
                    Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.x + cellRect.width, cellRect.y));
                    Handles.DrawLine(new Vector3(cellRect.x, cellRect.y + cellRect.height), new Vector3(cellRect.x + cellRect.width, cellRect.y + cellRect.height));
                    Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.x, cellRect.y + cellRect.height));
                    Handles.DrawLine(new Vector3(cellRect.x + cellRect.width, cellRect.y), new Vector3(cellRect.x + cellRect.width, cellRect.y + cellRect.height));

                    // First draw the letter if it exists
                    if (hasLetter)
                    {
                        string letterStr = c.ToString().ToUpper();

                        // Use an EditorStyles approach instead of GUI.skin
                        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                        labelStyle.alignment = TextAnchor.MiddleLeft;
                        labelStyle.fontSize = 14;
                        labelStyle.fontStyle = FontStyle.Bold;
                        labelStyle.normal.textColor = Color.black;
                        labelStyle.padding = new RectOffset(10, 0, 0, 0);
                        labelStyle.margin = new RectOffset(0, 0, 0, 0);

                        // Use EditorGUI.LabelField instead of GUI.Label
                        EditorGUI.LabelField(cellRect, letterStr, labelStyle);

                        // Numbers are hidden - commented out number display code
                        // foreach (var placement in previewData.placements)
                        // {
                        //     if (placement.startPosition.x == x && placement.startPosition.y == y)
                        //     {
                        //         Rect numberRect = new Rect(cellRect.x + 2, cellRect.y + 1, cellRect.width, cellRect.height);
                        //         EditorGUI.LabelField(numberRect, placement.wordNumber.ToString(), numberStyle);
                        //         break;
                        //     }
                        // }
                    }

                    // Then draw the icon if it exists (in top-left corner)
                    if (hasIcon)
                    {
                        if (previewData.iconTexture != null)
                        {
                            // Draw the icon smaller and in the top-left corner
                            Rect smallIconRect = new Rect(
                                cellRect.x + cellRect.width * 0.05f, // Position on left side of cell
                                cellRect.y + cellRect.height * 0.05f, // At top
                                cellRect.width * 0.4f,
                                cellRect.height * 0.4f
                            );

                            EditorGUI.DrawTextureTransparent(smallIconRect, previewData.iconTexture);
                        }
                        else
                        {
                            // Fallback if texture is not available - smaller text in top left
                            GUIStyle smallItemStyle = new GUIStyle(EditorStyles.miniLabel);
                            smallItemStyle.alignment = TextAnchor.UpperLeft;
                            smallItemStyle.normal.textColor = Color.black;

                            Rect smallTextRect = new Rect(
                                cellRect.x + 2,
                                cellRect.y + 2,
                                cellRect.width * 0.4f,
                                cellRect.height * 0.4f
                            );

                            EditorGUI.LabelField(smallTextRect, "Item", smallItemStyle);
                        }
                    }

                    // Handle mouse click on grid cell for letter or icon placement/removal
                    if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.button == 1) // Right click - handle removal
                        {
                            // Trigger event to notify that crossword was manually changed
                            OnCrosswordManuallyChanged?.Invoke();
                            
                            changes.gridChanged = true;
                            if (hasIcon)
                            {
                                // Remove special item first
                                previewData.iconPositions.Remove(pos);
                            }
                            else
                            {
                                // If no icon, remove letter
                                previewData.grid[x, y] = (char)0;
                            }
                        }
                        else if (Event.current.button == 0) // Left click - handle placement
                        {
                            if (isSpecialItemSelected && hasLetter)
                            {
                                changes.gridChanged = true;
                                // Handle special item placement/toggle
                                if (previewData.iconPositions.ContainsKey(pos))
                                {
                                    previewData.iconPositions.Remove(pos);
                                }
                                else
                                {
                                    previewData.iconPositions[pos] = previewData.iconPath;
                                }
                            }
                            else if (!isSpecialItemSelected)
                            {
                                // Place letter regardless of icon presence
                                changes.gridChanged = true;
                                previewData.grid[x, y] = currentSelectedLetter;
                            }
                        }

                        if (changes.gridChanged)
                        {
                            SavePreviewToLevel(level, langCode, previewData);
                            // Force all Unity windows to repaint to show changes immediately
                            EditorUtility.SetDirty(level);
                            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                            Event.current.Use(); // Consume the event to prevent other handlers
                        }
                    }
                }
            }


            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            return changes;
        }

        public static void RefreshCrossword(PreviewData previewData, string langCode, Level level)
        {
            if (level == null || previewData == null)
            {
                Debug.LogError("Cannot refresh crossword: Level or preview data is null");
                return;
            }

            // Generate cache key based on level and language
            string cacheKey = $"{level.GetInstanceID()}_{langCode}";

            // Get current level data
            var words = level.GetLanguageData(langCode)?.words;
            if (words == null || words.Length == 0)
            {
                Debug.LogError("No words found for crossword generation");
                return;
            }

            // Check if we need to regenerate the cache
            if (!_crosswordCache.ContainsKey(cacheKey) ||
                !_crosswordCache[cacheKey].IsValidFor(words, previewData.columns, previewData.rows))
            {
                Debug.Log("Generating new crossword variants cache...");
                GenerateCrosswordVariantsCache(cacheKey, words, previewData.columns, previewData.rows);
            }

            var cache = _crosswordCache[cacheKey];

            // If no variants were generated successfully, fall back to original behavior
            if (cache.variants.Count == 0)
            {
                Debug.LogWarning("No crossword variants could be generated. Falling back to single generation.");
                RefreshCrosswordLegacy(previewData, langCode, level);
                return;
            }

            // Move to next variant (cycle through available variants)
            cache.currentIndex = (cache.currentIndex + 1) % cache.variants.Count;
            var selectedVariant = cache.variants[cache.currentIndex];

            // Debug.Log($"Switching to crossword variant {cache.currentIndex + 1}/{cache.variants.Count}");

            // Store the old icon positions
            var oldIconPositions = new Dictionary<Vector2Int, string>(previewData.iconPositions);

            // Copy the selected variant to current preview data
            previewData.grid = (char[,])selectedVariant.grid.Clone();
            previewData.placements = selectedVariant.placements.Select(p => new WordPlacement
            {
                word = p.word,
                wordNumber = p.wordNumber,
                startPosition = p.startPosition,
                isHorizontal = p.isHorizontal
            }).ToList();
            previewData.minBounds = selectedVariant.minBounds;
            previewData.maxBounds = selectedVariant.maxBounds;
            previewData.columns = selectedVariant.columns;
            previewData.rows = selectedVariant.rows;

            // Restore icon data
            previewData.iconPositions = new Dictionary<Vector2Int, string>();

            // Find all valid positions in the new grid (positions with letters)
            List<Vector2Int> validPositions = new List<Vector2Int>();
            for (int y = 0; y < previewData.grid.GetLength(1); y++)
            {
                for (int x = 0; x < previewData.grid.GetLength(0); x++)
                {
                    if (previewData.grid[x, y] != '\0')
                    {
                        validPositions.Add(new Vector2Int(x, y));
                    }
                }
            }

            // Randomly assign icons to valid positions
            foreach (var iconEntry in oldIconPositions)
            {
                if (validPositions.Count > 0)
                {
                    int randomIndex = Random.Range(0, validPositions.Count);
                    Vector2Int newPos = validPositions[randomIndex];
                    previewData.iconPositions[newPos] = iconEntry.Value;
                    validPositions.RemoveAt(randomIndex);
                }
            }

            // Save to level
            SavePreviewToLevel(level, langCode, previewData);

            // Force inspector repaint
            EditorUtility.SetDirty(level);
        }

        // Generate cache of crossword variants
        private static void GenerateCrosswordVariantsCache(string cacheKey, string[] words, int columns, int rows)
        {
            const int maxVariants = 20;
            const int maxAttemptsPerVariant = 50; // Increased attempts per variant
            
            var cache = new CrosswordVariantsCache
            {
                variants = new List<PreviewData>(),
                currentIndex = -1, // Will be incremented to 0 on first use
                lastWords = (string[])words.Clone(),
                lastColumns = columns,
                lastRows = rows
            };

            var generatedGrids = new HashSet<string>(); // To avoid duplicate grids
            
            for (int variantIndex = 0; variantIndex < maxVariants; variantIndex++)
            {
                PreviewData bestVariant = null;
                int bestWordCount = 0;
                
                for (int attempt = 0; attempt < maxAttemptsPerVariant; attempt++)
                {
                    // Use different strategies for generating variants
                    bool reverseWords = (attempt % 3) == 1;
                    bool shuffleWords = (attempt % 3) == 2;
                    int seed = Random.Range(1, 100000) + variantIndex * 10000 + attempt * 100;
                    
                    string[] wordsToUse = (string[])words.Clone();
                    if (shuffleWords)
                    {
                        // Shuffle the words array
                        for (int i = 0; i < wordsToUse.Length; i++)
                        {
                            int randomIndex = Random.Range(0, wordsToUse.Length);
                            (wordsToUse[i], wordsToUse[randomIndex]) = (wordsToUse[randomIndex], wordsToUse[i]);
                        }
                    }
                    else if (reverseWords)
                    {
                        wordsToUse = wordsToUse.Reverse().ToArray();
                    }

                    var candidate = GeneratePreview(wordsToUse, columns, rows, seed, false);
                    
                    if (candidate != null && candidate.isValid)
                    {
                        // Check for problematic overlaps (both position overlaps and missing words)
                        bool hasProblematicOverlaps = LevelEditorServices.CheckForOverlappingWords(candidate.placements, wordsToUse);
                        
                        // Create a string representation of the grid for uniqueness checking
                        string gridSignature = GridToString(candidate.grid);
                        
                        // Check if this grid layout is unique, has a good word count, and no problematic overlaps
                        if (!generatedGrids.Contains(gridSignature) && 
                            candidate.placements.Count > bestWordCount &&
                            !hasProblematicOverlaps)
                        {
                            bestVariant = candidate;
                            bestWordCount = candidate.placements.Count;
                        }
                    }
                }
                
                // Add the best variant found for this index
                if (bestVariant != null)
                {
                    string gridSignature = GridToString(bestVariant.grid);
                    generatedGrids.Add(gridSignature);
                    cache.variants.Add(bestVariant);
                }
            }
            
            // Store the cache
            _crosswordCache[cacheKey] = cache;
        }

        // Convert grid to string for uniqueness comparison
        private static string GridToString(char[,] grid)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    sb.Append(grid[x, y] == '\0' ? '.' : grid[x, y]);
                }
                sb.Append('|');
            }
            return sb.ToString();
        }

        // Legacy refresh method as fallback
        private static void RefreshCrosswordLegacy(PreviewData previewData, string langCode, Level level)
        {
            if (level != null)
            {
                // Generate preview with current level data
                var words = level.GetLanguageData(langCode)?.words;
                if (words != null && words.Length > 0)
                {
                    const int maxAttempts = 10; // Increased max attempts
                    PreviewData newPreview = null;
                    bool isDifferent = false;

                    // Try generating a different crossword using different strategies
                    for (int attempt = 0; attempt < maxAttempts && !isDifferent; attempt++)
                    {
                        // Try different strategies for each attempt
                        bool reverseWords = (attempt % 2) == 1; // Alternate between normal and reversed word order
                        int seed = Random.Range(1, 10000) + attempt * 1000; // Use different seed ranges for each attempt
                        
                        newPreview = GeneratePreview(
                            words, 
                            previewData.columns, 
                            previewData.rows,
                            seed,
                            reverseWords);

                        if (newPreview != null && newPreview.isValid)
                        {
                            // Check if the new design is different enough from the current one
                            isDifferent = !AreCrosswordsEqual(previewData.grid, newPreview.grid);
                            
                            // If still not different enough, try with adjusted dimensions
                            if (!isDifferent && attempt > maxAttempts / 2)
                            {
                                // Try with slightly adjusted dimensions
                                int adjustedColumns = previewData.columns + ((attempt % 2) == 0 ? 1 : -1);
                                int adjustedRows = previewData.rows + ((attempt % 2) == 0 ? -1 : 1);
                                
                                // Keep dimensions within reasonable bounds
                                adjustedColumns = Mathf.Clamp(adjustedColumns, 8, 12);
                                adjustedRows = Mathf.Clamp(adjustedRows, 6, 8);
                                
                                newPreview = GeneratePreview(
                                    words,
                                    adjustedColumns,
                                    adjustedRows,
                                    seed,
                                    reverseWords);

                                if (newPreview != null && newPreview.isValid)
                                {
                                    isDifferent = !AreCrosswordsEqual(previewData.grid, newPreview.grid);
                                }
                            }
                        }
                    }

                    if (newPreview != null && newPreview.isValid)
                    {
                        if (!isDifferent)
                        {
                            Debug.LogWarning("Could not generate a significantly different crossword design after " + maxAttempts + " attempts. Try adding or removing some words to get more variations.");
                        }

                        // Store the old icon positions
                        var oldIconPositions = new Dictionary<Vector2Int, string>(previewData.iconPositions);

                        // Copy icon data from old preview
                        newPreview.iconTexture = previewData.iconTexture;
                        newPreview.iconPath = previewData.iconPath;
                        newPreview.iconPositions = new Dictionary<Vector2Int, string>();

                        // Find all valid positions in the new grid (positions with letters)
                        List<Vector2Int> validPositions = new List<Vector2Int>();
                        for (int y = 0; y < newPreview.grid.GetLength(1); y++)
                        {
                            for (int x = 0; x < newPreview.grid.GetLength(0); x++)
                            {
                                if (newPreview.grid[x, y] != '\0')
                                {
                                    validPositions.Add(new Vector2Int(x, y));
                                }
                            }
                        }

                        // Randomly assign icons to valid positions
                        foreach (var iconEntry in oldIconPositions)
                        {
                            if (validPositions.Count > 0)
                            {
                                int randomIndex = Random.Range(0, validPositions.Count);
                                Vector2Int newPos = validPositions[randomIndex];
                                newPreview.iconPositions[newPos] = iconEntry.Value;
                                validPositions.RemoveAt(randomIndex);
                            }
                        }

                        // Update the current preview data with the new one
                        previewData.grid = newPreview.grid;
                        previewData.placements = newPreview.placements;
                        previewData.minBounds = newPreview.minBounds;
                        previewData.maxBounds = newPreview.maxBounds;
                        previewData.iconPositions = newPreview.iconPositions;

                        // Save to level
                        SavePreviewToLevel(level, langCode, previewData);

                        // Force inspector repaint
                        EditorUtility.SetDirty(level);
                    }
                }
            }
        }

        // Draw the letter palette with specific letters and special item
        private static bool DrawLetterPalette(string letters , PreviewData previewData, Texture2D iconTexture = null, Level level = null, string langCode = null)
        {
            bool paletteChanged = false;
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Letter & Item Palette", EditorStyles.boldLabel);

            // Display letters text field
            if (level != null && !string.IsNullOrEmpty(langCode))
            {
                var languageData = level.GetLanguageData(langCode);
                if (languageData != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    // Make text field editable and save changes
                    EditorGUI.BeginChangeCheck();
                    string currentLetters = languageData.letters ?? "";

                    string newLetters = EditorGUILayout.TextField(currentLetters, GUILayout.Width(150));
                    GUILayout.Space(50);

                    // Arrow buttons layout
                    EditorGUILayout.BeginVertical(GUILayout.Width(60));
                    {
                        GUILayout.Space(-40);

                        // Space for alignment

                        // Up arrow
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        var height = 25;
                        if (GUILayout.Button("↑", GUILayout.Width(height), GUILayout.Height(height)))
                        {
                            MoveGrid(previewData, -Vector2Int.up);
                            SavePreviewToLevel(level, langCode, previewData);
                            paletteChanged = true;
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space(-4);

                        // Left, Right arrows row
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(-2);
                        if (GUILayout.Button("←", GUILayout.Width(height), GUILayout.Height(height)))
                        {
                            MoveGrid(previewData, Vector2Int.left);
                            SavePreviewToLevel(level, langCode, previewData);
                            paletteChanged = true;
                        }
                        GUILayout.Space(20);
                            EditorGUILayout.Space(2);
                        if (GUILayout.Button("→", GUILayout.Width(height), GUILayout.Height(height)))
                        {
                            MoveGrid(previewData, Vector2Int.right);
                            SavePreviewToLevel(level, langCode, previewData);
                            paletteChanged = true;
                        }
                        EditorGUILayout.EndHorizontal();

                        // Down arrow
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("↓", GUILayout.Width(height), GUILayout.Height(height)))
                        {
                            MoveGrid(previewData, -Vector2Int.down);
                            SavePreviewToLevel(level, langCode, previewData);
                            paletteChanged = true;
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Save changes back to language data
                        languageData.letters = newLetters;
                        EditorUtility.SetDirty(level);
                        paletteChanged = true;
                    }
                    EditorGUILayout.Space(5);
                }
            }
            else if (!string.IsNullOrEmpty(letters))
            {
                // Fallback to read-only display if level/langCode not provided
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Letters", letters);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space(5);
            }

            GUILayout.Space(-20);

            // List to maintain order of letters
            List<char> lettersList = new List<char>();
            
            // Only add letters if they are specified
            if (!string.IsNullOrEmpty(letters))
            {
                // Add all letters including duplicates
                foreach (char c in letters.ToUpper())
                {
                    if (char.IsLetter(c))
                    {
                        lettersList.Add(c);
                    }
                }
            }
            
            // Add one extra space for special item
            int letterCount = lettersList.Count + 1; 
            int rows = Mathf.CeilToInt((float)letterCount / lettersPerRow);
            
            // Define the palette area
            float rowHeight = (letterPaletteCellSize + letterPaletteSpacing);
            float requiredWidth = (lettersPerRow * (letterPaletteCellSize + letterPaletteSpacing)) + specialItemOffset; // Add offset to total width
            
            Rect paletteRect = GUILayoutUtility.GetRect(requiredWidth, rows * rowHeight);
            
            // Style for the letters - create new style from scratch for better control
            GUIStyle letterStyle = new GUIStyle();
            letterStyle.normal.textColor = Color.black;
            letterStyle.fontStyle = FontStyle.Bold;
            letterStyle.fontSize = 14;
            letterStyle.alignment = TextAnchor.MiddleCenter;
            letterStyle.clipping = TextClipping.Clip;
            
            // Draw background
            // EditorGUI.DrawRect(paletteRect, backgroundColor);

            // Draw special item first (on the left)
            float posX = paletteRect.x+5;
            float posY = paletteRect.y;
            
            Rect specialItemRect = new Rect(posX, posY, letterPaletteCellSize, letterPaletteCellSize);
            
            // Draw background with selection highlight if needed
            Color itemBgColor = isSpecialItemSelected ? selectedLetterBgColor : cellBackgroundColor;
            EditorGUI.DrawRect(specialItemRect, itemBgColor);
            
            // Draw border
            Handles.color = cellBorderColor;
            Handles.DrawLine(new Vector3(specialItemRect.x, specialItemRect.y), new Vector3(specialItemRect.x + specialItemRect.width, specialItemRect.y));
            Handles.DrawLine(new Vector3(specialItemRect.x, specialItemRect.y + specialItemRect.height), new Vector3(specialItemRect.x + specialItemRect.width, specialItemRect.y + specialItemRect.height));
            Handles.DrawLine(new Vector3(specialItemRect.x, specialItemRect.y), new Vector3(specialItemRect.x, specialItemRect.y + specialItemRect.height));
            Handles.DrawLine(new Vector3(specialItemRect.x + specialItemRect.width, specialItemRect.y), new Vector3(specialItemRect.x + specialItemRect.width, specialItemRect.y + specialItemRect.height));

            // Draw icon or placeholder text
            if (iconTexture != null)
            {
                // Draw the icon with padding
                Rect paddedRect = new Rect(
                    specialItemRect.x + specialItemRect.width * 0.1f,
                    specialItemRect.y + specialItemRect.height * 0.1f,
                    specialItemRect.width * 0.8f,
                    specialItemRect.height * 0.8f
                );

                EditorGUI.DrawTextureTransparent(paddedRect, iconTexture);
            }
            else
            {
                // Fallback to text
                EditorGUI.LabelField(specialItemRect, "Item", letterStyle);
            }
            
            // Handle click on special item to select it
            if (Event.current.type == EventType.MouseDown && 
                specialItemRect.Contains(Event.current.mousePosition) &&
                Event.current.button == 0)
            {
                isSpecialItemSelected = true;
                paletteChanged = true;
                Event.current.Use(); // Consume the event
            }

            // Draw letters after special item
            for (int i = 0; i < lettersList.Count; i++)
            {
                int row = i / (lettersPerRow - 1); // One less per row to account for special item
                int col = i % (lettersPerRow - 1);

                // Current letter
                char letter = lettersList[i];

                // Calculate position (shifted right by one cell plus offset)
                float posX1 = paletteRect.x + (col + 1) * (letterPaletteCellSize + letterPaletteSpacing) + specialItemOffset;
                float posY1 = paletteRect.y + row * (letterPaletteCellSize + letterPaletteSpacing);

                Rect letterRect = new Rect(posX1, posY1, letterPaletteCellSize, letterPaletteCellSize);
                
                // Draw background with special color if this is the selected letter
                Color bgColor = (!isSpecialItemSelected && letter == currentSelectedLetter) ? 
                    selectedLetterBgColor : cellBackgroundColor;
                EditorGUI.DrawRect(letterRect, bgColor);
                
                // Draw border
                Handles.color = cellBorderColor;
                Handles.DrawLine(new Vector3(letterRect.x, letterRect.y), new Vector3(letterRect.x + letterRect.width, letterRect.y));
                Handles.DrawLine(new Vector3(letterRect.x, letterRect.y + letterRect.height), new Vector3(letterRect.x + letterRect.width, letterRect.y + letterRect.height));
                Handles.DrawLine(new Vector3(letterRect.x, letterRect.y), new Vector3(letterRect.x, letterRect.y + letterRect.height));
                Handles.DrawLine(new Vector3(letterRect.x + letterRect.width, letterRect.y), new Vector3(letterRect.x + letterRect.width, letterRect.y + letterRect.height));
                
                // Draw letter using EditorGUI.LabelField
                string letterStr = letter.ToString();
                EditorGUI.LabelField(letterRect, letterStr, letterStyle);
                
                // Handle click on letter to select it
                if (Event.current.type == EventType.MouseDown && 
                    letterRect.Contains(Event.current.mousePosition) &&
                    Event.current.button == 0)
                {
                    currentSelectedLetter = letter;
                    isSpecialItemSelected = false;
                    // paletteChanged = true;
                    Event.current.Use(); // Consume the event
                }
            }
            
            return paletteChanged;
        }

        public static void SwitchWordsPlacements(Level level, PreviewData previewData, string currentLanguage)
        {
            var languageData = level.GetLanguageData(currentLanguage);
            var words = languageData?.words;
            // rearrange placements
            var placements = previewData.placements;
            for (int i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                // Find the word in the words array
                int wordIndex = Array.FindIndex(words, w => w.Equals(placement.word, StringComparison.OrdinalIgnoreCase));
                if (wordIndex >= 0)
                {
                    placement.wordNumber = wordIndex + 1; // Word numbers are 1-based
                }
            }
            placements = placements.OrderBy(p => p.wordNumber).ToList();
            previewData.placements = placements;
            SavePreviewToLevel(level, currentLanguage, previewData);
        }
        // Save preview data to the specified language in the level
        public static void SavePreviewToLevel(Level level, string languageCode, PreviewData previewData)
        {
            if (level == null || string.IsNullOrEmpty(languageCode) || previewData == null || !previewData.isValid)
            {
                Debug.LogError("Cannot save preview: invalid parameters");
                return;
            }
            
            // Make sure the grid dimensions are valid
            if (previewData.columns <= 0) previewData.columns = defaultGridColumns;
            if (previewData.rows <= 0) previewData.rows = defaultGridRows;
            
            // Check if the grid needs to be resized to match the specified dimensions
            int actualColumns = previewData.grid.GetLength(0);
            int actualRows = previewData.grid.GetLength(1);
            
            // Resize the grid if necessary
            if (previewData.columns != actualColumns || previewData.rows != actualRows)
            {
                Debug.Log($"Resizing grid from {actualColumns}x{actualRows} to {previewData.columns}x{previewData.rows}");
                
                // Create a new grid with the specified dimensions
                char[,] newGrid = new char[previewData.columns, previewData.rows];
                
                // Copy existing content to the new grid (only the overlapping part)
                int copyColumns = Mathf.Min(actualColumns, previewData.columns);
                int copyRows = Mathf.Min(actualRows, previewData.rows);
                
                for (int y = 0; y < copyRows; y++)
                {
                    for (int x = 0; x < copyColumns; x++)
                    {
                        newGrid[x, y] = previewData.grid[x, y];
                    }
                }
                
                // Replace the old grid with the new one
                previewData.grid = newGrid;
            }
            
            // Update word placements before saving to ensure they're current
            UpdateWordPlacementsFromGrid(previewData); // Don't allow regeneration here to avoid recursive calls
            
            var languageData = level.GetLanguageData(languageCode);
            if (languageData == null)
            {
                Debug.LogError($"Language {languageCode} not found in level {level.name}");
                return;
            }
            
            // Create or update crossword data
            if (languageData.crosswordData == null)
                languageData.crosswordData = new SerializableCrosswordData();
                
            var crosswordData = languageData.crosswordData;
            
            // Update the dimensions and grid
            crosswordData.columns = previewData.columns;
            crosswordData.rows = previewData.rows;
            
            // Make sure the grid matches the specified dimensions
            if (previewData.grid.GetLength(0) != previewData.columns || 
                previewData.grid.GetLength(1) != previewData.rows)
            {
                // It's safer to make a new copy to ensure the dimensions match exactly
                char[,] newGrid = new char[previewData.columns, previewData.rows];
                int copyColumns = Mathf.Min(previewData.grid.GetLength(0), previewData.columns);
                int copyRows = Mathf.Min(previewData.grid.GetLength(1), previewData.rows);
                
                // Copy the existing data
                for (int y = 0; y < copyRows; y++)
                {
                    for (int x = 0; x < copyColumns; x++)
                    {
                        newGrid[x, y] = previewData.grid[x, y];
                    }
                }
                
                crosswordData.grid = newGrid;
            }
            else
            {
                crosswordData.grid = previewData.grid;
            }
            
            crosswordData.minBounds = previewData.minBounds;
            crosswordData.maxBounds = previewData.maxBounds;
            
            // Convert word placements
            crosswordData.placements.Clear();
            
            // Add only regular word placements (no empty words!)
            foreach (var placement in previewData.placements)
            {
                crosswordData.placements.Add(new SerializableWordPlacement
                {
                    word = placement.word,
                    wordNumber = placement.wordNumber,
                    startPosition = placement.startPosition,
                    isHorizontal = placement.isHorizontal,
                    isSpecialItem = false, // Always false for real words
                    specialItemPath = ""
                });
            }
            
            // Store special items separately (cleaner approach)
            crosswordData.specialItems.Clear();
            foreach (var iconEntry in previewData.iconPositions)
            {
                crosswordData.specialItems.Add(new SerializableSpecialItem
                {
                    position = iconEntry.Key,
                    itemPath = iconEntry.Value
                });
            }
            
            // Serialize grid for storage
            crosswordData.SerializeGrid();
        }
        
        // New method to update the word list in the language data
        private static void UpdateLevelWordList(LanguageData languageData, List<WordPlacement> placements)
        {
            if (languageData == null || placements == null)
                return;
                
            // Extract words from placements and convert to lowercase
            string[] newWords = placements.Select(p => p.word.ToLower()).ToArray();
            
            // Update the language data's word list
            languageData.words = newWords;
        }
        
        // Method to scan the grid and update word placements
        public static bool UpdateWordPlacementsFromGrid(PreviewData previewData, bool allowRegeneration = false, Level level = null, string langCode = null)
        {
            if (previewData == null || previewData.grid == null)
                return false;

            // Create new list for updated placements
            List<WordPlacement> newPlacements = new List<WordPlacement>();

            // Get the actual dimensions of the grid array
            int actualColumns = previewData.grid.GetLength(0);
            int actualRows = previewData.grid.GetLength(1);

            // Ensure we don't exceed the actual grid dimensions
            int safeColumns = Mathf.Min(previewData.columns, actualColumns);
            int safeRows = Mathf.Min(previewData.rows, actualRows);

            // First pass - find all horizontal words
            for (int y = 0; y < safeRows; y++)
            {
                StringBuilder currentWord = new StringBuilder();
                Vector2Int startPos = Vector2Int.zero;

                for (int x = 0; x < safeColumns; x++)
                {
                    char c = previewData.grid[x, y];

                    if (c != 0) // If there's a letter
                    {
                        if (currentWord.Length == 0) // Start of a new word
                            startPos = new Vector2Int(x, y);

                        currentWord.Append(c);
                    }

                    // If we hit an empty cell or end of row, check if we have a word
                    if ((c == 0 || x == safeColumns - 1) && currentWord.Length > 0)
                    {
                        if (currentWord.Length > 1) // Words must be at least 2 letters
                        {
                            string word = currentWord.ToString();

                            // Persian/Arabic: Extract horizontal words in reverse order if it's an RTL language
                            if (PersianLanguageUtility.IsRTL(langCode))
                            {
                                word = PersianLanguageUtility.Reverse(word);
                            }

                            newPlacements.Add(new WordPlacement
                            {
                                word = word,
                                startPosition = startPos,
                                isHorizontal = true,
                                wordNumber = 0 // Will be assigned later
                            });
                        }

                        // Reset for next word
                        currentWord.Clear();
                    }
                }
            }

            // Second pass - find all vertical words
            for (int x = 0; x < safeColumns; x++)
            {
                StringBuilder currentWord = new StringBuilder();
                Vector2Int startPos = Vector2Int.zero;

                for (int y = 0; y < safeRows; y++)
                {
                    char c = previewData.grid[x, y];

                    if (c != 0) // If there's a letter
                    {
                        if (currentWord.Length == 0) // Start of a new word
                            startPos = new Vector2Int(x, y);

                        currentWord.Append(c);
                    }

                    // If we hit an empty cell or end of column, check if we have a word
                    if ((c == 0 || y == safeRows - 1) && currentWord.Length > 0)
                    {
                        if (currentWord.Length > 1) // Words must be at least 2 letters
                        {
                            string word = currentWord.ToString();

                            newPlacements.Add(new WordPlacement
                            {
                                word = word,
                                startPosition = startPos,
                                isHorizontal = false,
                                wordNumber = 0 // Will be assigned later
                            });
                        }

                        // Reset for next word
                        currentWord.Clear();
                    }
                }
            }
            
            // Assign word numbers
            AssignWordNumbers(newPlacements, previewData.placements);
            
            // Update placements in preview data
            previewData.placements = newPlacements;
            
            // Update preview data dimensions to match actual grid size
            // This prevents future discrepancies between grid size and stored dimensions
            previewData.columns = actualColumns;
            previewData.rows = actualRows;
            
            // Recalculate grid bounds
            CrosswordGenerator.CalculateGridBounds(previewData.grid, out previewData.minBounds, out previewData.maxBounds);
            
            return false; // No regeneration was needed or performed
        }
        
        // Helper method to assign word numbers
        private static void AssignWordNumbers(List<WordPlacement> placements, List<WordPlacement> oldPlacements)
        {
            // if all words from old and new are the same, take numbers from old placements
            if (oldPlacements != null && placements.Count == oldPlacements.Count && placements.All(p=>p.word == oldPlacements.FirstOrDefault(op => op.word == p.word)?.word))
            {
                for (int i = 0; i < placements.Count; i++)
                {
                    var index = oldPlacements.FindIndex(p => p.word == placements[i].word);
                    placements[i].wordNumber = oldPlacements[index].wordNumber;
                }
                return;
            }
            // Assign numbers sequentially
            for (int i = 0; i < placements.Count; i++)
            {
                placements[i].wordNumber = i + 1;
            }
        }

        
        // Load preview data from the level
        public static PreviewData LoadPreviewFromLevel(Level level, string languageCode)
        {
            if (level == null || string.IsNullOrEmpty(languageCode))
            {
                Debug.LogError("Cannot load preview: Level or language code is null");
                return null;
            }
                
            var languageData = level.GetLanguageData(languageCode);
            if (languageData == null)
            {
                Debug.LogError($"Language data not found for {languageCode}");
                return null;
            }

            // Initialize crossword data if it's null
            if (languageData.crosswordData == null)
            {
                defaultGridColumns = EditorPrefs.GetInt("WordsToolkit_grid_x", 10);
                defaultGridRows = EditorPrefs.GetInt("WordsToolkit_grid_y", 7);
                languageData.crosswordData = new SerializableCrosswordData {
                    columns = defaultGridColumns,
                    rows = defaultGridRows,
                    grid = new char[defaultGridColumns, defaultGridRows],
                    minBounds = Vector2Int.zero,
                    maxBounds = new Vector2Int(defaultGridColumns - 1, defaultGridRows - 1),
                    placements = new List<SerializableWordPlacement>(),
                    specialItems = new List<SerializableSpecialItem>() // Initialize special items list
                };
                EditorUtility.SetDirty(level);
            }
            
            var crosswordData = languageData.crosswordData;
            
            // Deserialize the grid
            crosswordData.DeserializeGrid();
            
            // Create default grid if deserialization failed or grid is null after deserializing
            if (crosswordData.grid == null)
            {
                // Always use default dimensions for consistency
                if(crosswordData.columns <= 0) crosswordData.columns = defaultGridColumns;
                if(crosswordData.rows <= 0) crosswordData.rows = defaultGridRows;

                // Create empty grid
                crosswordData.grid = new char[crosswordData.columns, crosswordData.rows];
                
                // Initialize bounds
                crosswordData.minBounds = Vector2Int.zero;
                crosswordData.maxBounds = new Vector2Int(crosswordData.columns - 1, crosswordData.rows - 1);
                
                // Make sure we have a placements list
                if (crosswordData.placements == null)
                {
                    crosswordData.placements = new List<SerializableWordPlacement>();
                }
                
                // Make sure we have a special items list
                if (crosswordData.specialItems == null)
                {
                    crosswordData.specialItems = new List<SerializableSpecialItem>();
                }
                
                EditorUtility.SetDirty(level);
                AssetDatabase.SaveAssets();
            }
                
            // Create preview data
            PreviewData previewData = new PreviewData
            {
                grid = crosswordData.grid,
                placements = new List<WordPlacement>(),
                columns = crosswordData.columns,
                rows = crosswordData.rows,
                minBounds = crosswordData.minBounds,
                maxBounds = crosswordData.maxBounds,
                iconPositions = new Dictionary<Vector2Int, string>(),
                iconPath = "special_item" // Ensure we have a default iconPath
            };
            
            // Load word placements (only real words, no empty words!)
            foreach (var serPlacement in crosswordData.placements)
            {
                // All placements should be real words now
                previewData.placements.Add(new WordPlacement
                {
                    word = serPlacement.word,
                    wordNumber = serPlacement.wordNumber,
                    startPosition = serPlacement.startPosition,
                    isHorizontal = serPlacement.isHorizontal
                });
            }
            
            // Load special items from separate storage
            if (crosswordData.specialItems != null)
            {
                foreach (var specialItem in crosswordData.specialItems)
                {
                    previewData.iconPositions[specialItem.position] = specialItem.itemPath;
                }
            }
            
            return previewData;
        }

        // Debug method to load and process crossword for a specific language
        public static void LoadCrossword(string language, Level level)
        {
            if (level == null)
            {
                Debug.LogError("Cannot load crossword: Level is null");
                return;
            }

            LanguageData langData = level.GetLanguageData(language);
            if (langData == null)
            {
                Debug.LogError($"Language data not found for language: {language}");
                return;
            }
            
            // No crossword data found
            if (langData.crosswordData == null)
            {
                Debug.LogWarning("No crossword data found");
            }
        }

        // Method to move the grid content in a direction
        private static void MoveGrid(PreviewData previewData, Vector2Int direction)
        {
            if (previewData == null || previewData.grid == null) return;

            // Check if move would push any non-empty cell out of bounds
            for (int y = 0; y < previewData.rows; y++)
            {
                for (int x = 0; x < previewData.columns; x++)
                {
                    if (previewData.grid[x, y] != 0)  // If cell has content
                    {
                        int newX = x + direction.x;
                        int newY = y + direction.y;
                        
                        // If move would push this non-empty cell out of bounds, cancel the move
                        if (newX < 0 || newX >= previewData.columns || newY < 0 || newY >= previewData.rows)
                        {
                            return; // Cancel the move
                        }
                    }
                }
            }

            char[,] newGrid = new char[previewData.columns, previewData.rows];
            Dictionary<Vector2Int, string> newIconPositions = new Dictionary<Vector2Int, string>();

            // Move each cell
            for (int y = 0; y < previewData.rows; y++)
            {
                for (int x = 0; x < previewData.columns; x++)
                {
                    // Calculate new position
                    int newX = x + direction.x;
                    int newY = y + direction.y;

                    // Check if new position is within bounds
                    if (newX >= 0 && newX < previewData.columns && newY >= 0 && newY < previewData.rows)
                    {
                        newGrid[newX, newY] = previewData.grid[x, y];
                        
                        // Move icons if present at this position
                        Vector2Int oldPos = new Vector2Int(x, y);
                        if (previewData.iconPositions.ContainsKey(oldPos))
                        {
                            newIconPositions[new Vector2Int(newX, newY)] = previewData.iconPositions[oldPos];
                        }
                    }
                }
            }

            // Update the grid and icon positions
            previewData.grid = newGrid;
            previewData.iconPositions = newIconPositions;

            // Update word placements positions
            foreach (var placement in previewData.placements)
            {
                placement.startPosition += direction;
            }
        }

        private static bool AreCrosswordsEqual(char[,] grid1, char[,] grid2)
        {
            if (grid1 == null || grid2 == null) return false;
            if (grid1.GetLength(0) != grid2.GetLength(0) || grid1.GetLength(1) != grid2.GetLength(1))
                return false;

            for (int y = 0; y < grid1.GetLength(1); y++)
            {
                for (int x = 0; x < grid1.GetLength(0); x++)
                {
                    if (grid1[x, y] != grid2[x, y])
                        return false;
                }
            }
            return true;
        }

        // Method to clear crossword variants cache (useful when words change)
        public static void ClearCrosswordCache(Level level = null, string langCode = null)
        {
            if (level != null && !string.IsNullOrEmpty(langCode))
            {
                string cacheKey = $"{level.GetInstanceID()}_{langCode}";
                if (_crosswordCache.ContainsKey(cacheKey))
                {
                    _crosswordCache.Remove(cacheKey);
                    Debug.Log($"Cleared crossword cache for {level.name} - {langCode}");
                }
            }
            else
            {
                _crosswordCache.Clear();
                Debug.Log("Cleared all crossword caches");
            }
        }

        // Method to get cache info for debugging
        public static string GetCacheInfo(Level level, string langCode)
        {
            if (level == null || string.IsNullOrEmpty(langCode))
                return "Invalid parameters";
                
            string cacheKey = $"{level.GetInstanceID()}_{langCode}";
            if (!_crosswordCache.ContainsKey(cacheKey))
                return "No cache available";
                
            var cache = _crosswordCache[cacheKey];
            return $"Cache: {cache.variants.Count} variants, current: {cache.currentIndex + 1}";
        }

        // Method to invalidate cache when words change (should be called from LevelDataEditor)
        public static void InvalidateCacheForWordsChange(Level level, string langCode)
        {
            if (level != null && !string.IsNullOrEmpty(langCode))
            {
                string cacheKey = $"{level.GetInstanceID()}_{langCode}";
                if (_crosswordCache.ContainsKey(cacheKey))
                {
                    _crosswordCache.Remove(cacheKey);
                    Debug.Log($"Invalidated crossword cache due to words change for {level.name} - {langCode}");
                }
            }
        }
    }
}
