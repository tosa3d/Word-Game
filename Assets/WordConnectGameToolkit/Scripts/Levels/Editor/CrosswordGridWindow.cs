using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using WordsToolkit.Scripts.Utilities;
using System.Text;
using WordsToolkit.Scripts.Levels.Editor.EditorWindows;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.Services;
using WordsToolkit.Scripts.Services.BannedWords;
using WordsToolkit.Scripts.Settings;
using System.IO;

namespace WordsToolkit.Scripts.Levels.Editor
{
    public class CrosswordGridWindow : EditorWindow
    {
        // Reference to the current level and language being edited
        private Level _currentLevel;
        private string _currentLanguageCode;
        private CrosswordPreviewHandler.PreviewData _previewData;
        
        // UI Elements
        private VisualElement _rootElement;
        private ToolbarToggle _autoRefreshToggle;
        private ToolbarToggle _showNumbersToggle;
        private ToolbarToggle _enableEditingToggle;
        private Label _levelLabel;
        private TextField _languageField;
        private VisualElement _gridContainer;
        private VisualElement _letterPalette;
        private Label _statusLabel;
        private Label _instructionsLabel;
        private List<VisualElement> _searchTiles;
        private VisualElement _searchResultsContainer;
        private VisualElement _combinedControlPanel;
        private VisualElement _searchContainer;
        private VisualElement _statusBar;
        private VisualElement _searchResultsArea;
        
        // Grid display settings
        private const int gridCellSize = 40; // Larger for better visual element handling
        private const int gridSpacing = 2;

        // Icon settings
        private const string IconPath = "Assets/WordConnectGameToolkit/Sprites/game_ui/in-game-item-1.png";
        
        // Window state
        private bool _autoRefresh = true;
        private bool _showGridNumbers = false;
        private bool _enableEditing = true;
        private float _lastRefreshTime;
        private const float refreshInterval = 0.5f;
        
        // Editing state
        private static char _currentSelectedLetter = 'A';
        private static bool _isSpecialItemSelected = false;
        private Texture2D warningIcon;
        
        public static void ShowWindow()
        {
            CrosswordGridWindow window = GetWindow<CrosswordGridWindow>("Crossword Grid");
            window.Show();
        }
        
        public static void ShowWindow(Level level, string languageCode)
        {
            CrosswordGridWindow window = GetWindow<CrosswordGridWindow>("Crossword Grid");
            window.Show();
            // Delay setting the level until after the window is shown and UI is initialized
            EditorApplication.delayCall += () => {
                if (window != null)
                {
                    window.SetCurrentLevel(level, languageCode);
                }
            };
        }

        public void SetCurrentLevel(Level level, string languageCode)
        {
            _currentLevel = level;
            _currentLanguageCode = languageCode;
            
            RefreshPreviewData();

            // Clear search letters and results when level is switched
            ClearSearchTiles();
            ClearSearchResults();

            // Update window title to show current level
            if (level != null)
            {
                titleContent = new GUIContent($"Crossword Grid - {level.name} ({languageCode})");
                
                // Save latest level selection to EditorPrefs
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(level, out string guid, out long localId))
                {
                    EditorPrefs.SetString("WordsToolkit_LatestLevelGUID", guid);
                }
            }
            else
            {
                titleContent = new GUIContent("Crossword Grid");
            } // Update UI elements

            UpdateLevelInfo();
            UpdateBackground();
            UpdateEditingUI();
            UpdateGridDisplay();
            UpdateStatusBar();
            UpdateInstructions();
        }
        
        private void UpdateLevelInfo()
        {
            if (_levelLabel != null)
            {
                _levelLabel.text = _currentLevel != null ? $"Level: {_currentLevel.number}" : "No level selected";
            }
            
            if (_languageField != null)
            {
                _languageField.value = _currentLanguageCode ?? "";
            }
        }
        
        private void OnEnable()
        {
            // Subscribe to crossword changes
            CrosswordPreviewHandler.OnCrosswordManuallyChanged += OnCrosswordChanged;
            
            // Subscribe to level updates from LevelDataEditor
            LevelDataEditor.OnLevelNeedsUpdate += OnLevelUpdatedFromEditor;
            
            // Subscribe to language tab changes from LevelDataEditor
            LevelDataEditor.OnLanguageTabChanged += OnLanguageTabChanged;
            
            // Subscribe to hierarchy selection changes from LevelManagerWindow
            LevelManagerWindow.OnHierarchySelectionChanged += OnHierarchySelectionChanged;
            
            // Also subscribe to Unity selection changes as a fallback
            Selection.selectionChanged += OnSelectionChanged;
            
            // Subscribe to Unity's undo/redo events
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            
            // Initialize warning icon
            warningIcon = EditorGUIUtility.IconContent("Warning").image as Texture2D;
            
            // Set minimum window size
            minSize = new Vector2(400, 300);
            
            // Always create UI elements, but preserve the current level data
            Level tempLevel = _currentLevel;
            string tempLanguage = _currentLanguageCode;

            CreateUIElements();

            // Restore the level data after UI creation
            if (tempLevel != null)
            {
                SetCurrentLevel(tempLevel, tempLanguage);
            }
            else
            {
                // Try to load currently selected level
                CheckForSelectedLevel();
            }
        }
        
        private void CreateUIElements()
        {
            // Clear any existing content
            rootVisualElement.Clear();
            
            // Load and apply styles
            var styleSheet = Resources.Load<StyleSheet>("CrosswordGridWindow");
            if (styleSheet == null)
            {
                // Try to load from the Editor folder
                string[] guids = AssetDatabase.FindAssets("CrosswordGridWindow t:StyleSheet");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                }
            }
            
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }
            
            // Create root container
            _rootElement = new VisualElement();
            _rootElement.pickingMode = PickingMode.Ignore;
            _rootElement.style.flexGrow = 1;
            rootVisualElement.Add(_rootElement);
            
            // Create toolbar
            // CreateToolbar();
            
            // Create main content area
            var mainContent = new VisualElement();
            mainContent.style.flexGrow = 1;
            mainContent.style.flexDirection = FlexDirection.Column;
            mainContent.pickingMode = PickingMode.Ignore;
            _rootElement.Add(mainContent);
            
            // Create letter palette
            CreateLetterPalette(mainContent);

            // Create letter buttons row (if editing is enabled, will be populated in UpdateGridDisplay)
            var letterButtonsContainer = new VisualElement();
            letterButtonsContainer.style.flexShrink = 0;
            letterButtonsContainer.style.flexGrow = 0;
            letterButtonsContainer.name = "letterButtonsContainer";
            letterButtonsContainer.AddToClassList("letterButtonsContainer");
            mainContent.Add(letterButtonsContainer);

            // Create scrollable grid container
            var gridScrollView = new ScrollView();
            gridScrollView.name = "gridScrollView";
            gridScrollView.AddToClassList("grid-scroll-view");
            gridScrollView.mode = ScrollViewMode.VerticalAndHorizontal;
            gridScrollView.pickingMode = PickingMode.Ignore;
            mainContent.Add(gridScrollView);

            _gridContainer = new VisualElement();
            _gridContainer.name = "gridContainer";
            _gridContainer.AddToClassList("grid-container");
            _gridContainer.style.alignSelf = Align.Center;
            _gridContainer.style.flexShrink = 0; // Don't shrink the container
            _gridContainer.style.width = StyleKeyword.Auto; // Size to content
            _gridContainer.style.marginTop = 10;
            _gridContainer.style.flexDirection = FlexDirection.Column; // Stack grid and controls vertically
            _gridContainer.pickingMode = PickingMode.Ignore;
            gridScrollView.Add(_gridContainer);

            var buttonsRow = new VisualElement();
            buttonsRow.pickingMode = PickingMode.Ignore;
            buttonsRow.style.paddingBottom = 20;
            buttonsRow.style.paddingLeft = 20;
            buttonsRow.style.alignSelf = Align.Center;
            CreateCombinedControlPanel(buttonsRow);
            _combinedControlPanel = buttonsRow; // Store reference
            buttonsRow.name = "combinedControlPanel";
            _combinedControlPanel.AddToClassList("combined-control-panel-x");
            mainContent.Add(buttonsRow);

            CreateSearchContainer(buttonsRow);
            
            // Create search text area
            CreateSearchResultsArea(buttonsRow);
            
            CreateStatusBar();

            // Create instructions
            _instructionsLabel = new Label();
            _instructionsLabel.AddToClassList("instructions-label");
            _rootElement.Add(_instructionsLabel);
            
            UpdateInstructions();
        }

        private void CreateSearchContainer(VisualElement mainContent)
        {
            var searchContainer = new VisualElement();
            searchContainer.AddToClassList("search-container");

            var label = new Label("Search by Letters:");
            label.style.marginRight = 10;
            searchContainer.Add(label);

            var searchTiles = new VisualElement();
            searchTiles.style.flexDirection = FlexDirection.Row;
            searchTiles.style.flexGrow = 1; // Grow to fit available space
            searchTiles.style.flexShrink = 1; // Allow shrinking if needed
            searchTiles.style.justifyContent = Justify.SpaceBetween; // Distribute evenly
            searchTiles.style.alignSelf = Align.Stretch;
            
            // Initialize the search tiles list
            _searchTiles = new List<VisualElement>();
            
            for (int i = 0; i < 11; i++)
            {
                var cell = new VisualElement();
                // use class grid-cell for styling
                cell.AddToClassList("grid-cell");
                cell.style.position = Position.Relative;
                
                // Add position number in the top-left corner
                var numberLabel = new Label((i + 1).ToString());
                numberLabel.style.position = Position.Absolute;
                numberLabel.style.left = 2;
                numberLabel.style.top = 2;
                numberLabel.style.fontSize = 8;
                numberLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                numberLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                numberLabel.AddToClassList("search-tile-number");
                cell.Add(numberLabel);
                
                // Add click handler to get letter from currently selected letter
                cell.RegisterCallback<MouseDownEvent>(evt => HandleSearchTileClick(evt, cell, i));
                
                // Store reference to the cell
                _searchTiles.Add(cell);
                searchTiles.Add(cell);
            }
            searchContainer.Add(searchTiles);
            
            // Add clear button for search functionality
            var clearButton = new Button(() => {
                ClearSearchTiles();
                ClearSearchResults();
            })
            {
                tooltip = "Clear all search tiles and results"
            };
            clearButton.style.height = 40;
            clearButton.style.width = 60;
            clearButton.style.marginLeft = 10;
            clearButton.style.flexShrink = 0; // Don't shrink the clear button
            
            // Create a container for icon and text
            var clearContent = new VisualElement();
            clearContent.style.flexDirection = FlexDirection.Row;
            clearContent.style.alignItems = Align.Center;
            clearContent.style.justifyContent = Justify.Center;
            clearContent.style.height = Length.Percent(100);
            clearContent.style.width = Length.Percent(100);
            
            // Add eraser icon
            try
            {
                var eraserIcon = EditorGUIUtility.IconContent("Grid.EraserTool").image;
                if (eraserIcon != null)
                {
                    var iconElement = new VisualElement();
                    iconElement.style.backgroundImage = new StyleBackground(eraserIcon as Texture2D);
                    iconElement.style.width = 12;
                    iconElement.style.height = 12;
                    iconElement.style.marginRight = 3;
                    iconElement.style.alignSelf = Align.Center;
                    clearContent.Add(iconElement);
                }
            }
            catch { /* Ignore icon loading errors */ }
            
            var clearLabel = new Label("Clear");
            clearLabel.style.fontSize = 11;
            clearLabel.style.alignSelf = Align.Center;
            clearContent.Add(clearLabel);
            clearButton.Add(clearContent);
            
            searchTiles.Add(clearButton);
            
            _searchContainer = searchContainer; // Store reference
            mainContent.Add(searchContainer);
        }

        private void CreateSearchResultsArea(VisualElement mainContent)
        {
            var resultsContainer = new VisualElement();
            resultsContainer.AddToClassList("search-results-container");
            resultsContainer.style.flexDirection = FlexDirection.Column;
            resultsContainer.style.marginTop = 10;
            resultsContainer.style.marginBottom = 10;
            resultsContainer.style.paddingLeft = 10;
            resultsContainer.style.paddingRight = 10;

            var label = new Label("Search Results:");
            label.style.marginBottom = 5;
            resultsContainer.Add(label);

            _searchResultsContainer = new VisualElement();
            _searchResultsContainer.style.flexDirection = FlexDirection.Column;
            _searchResultsContainer.style.minHeight = 120;
            _searchResultsContainer.style.maxHeight = 200;
            _searchResultsContainer.style.overflow = Overflow.Hidden;
            _searchResultsContainer.style.marginLeft = -35;
            _searchResultsContainer.style.marginRight = -20;
            _searchResultsContainer.AddToClassList("search-results");
            
            resultsContainer.Add(_searchResultsContainer);
            _searchResultsArea = resultsContainer; // Store reference
            mainContent.Add(resultsContainer);
        }

        private void CreateArrows(VisualElement parent)
        {
            // Arrow buttons section - separate container below the main buttons
            var arrowSection = new VisualElement();
            arrowSection.style.flexDirection = FlexDirection.Column;
            arrowSection.style.alignItems = Align.Center;
            arrowSection.style.alignSelf = Align.Center;
            arrowSection.style.paddingBottom = 5;
            arrowSection.style.paddingRight = 10;

            // Up arrow
            var upButton = new Button(() => MoveGrid(-Vector2Int.up));
            upButton.text = "↑";
            upButton.tooltip = "Move grid content up";
            upButton.AddToClassList("arrow-button");
            arrowSection.Add(upButton);

            // Left/Right row
            var horizontalRow = new VisualElement();
            horizontalRow.style.flexDirection = FlexDirection.Row;
            horizontalRow.style.justifyContent = Justify.Center;
            horizontalRow.style.marginBottom = 2;
            arrowSection.Add(horizontalRow);

            var leftButton = new Button(() => MoveGrid(Vector2Int.left));
            leftButton.text = "←";
            leftButton.tooltip = "Move grid content left";
            leftButton.AddToClassList("arrow-button");
            horizontalRow.Add(leftButton);

            var rightButton = new Button(() => MoveGrid(Vector2Int.right));
            rightButton.text = "→";
            rightButton.tooltip = "Move grid content right";
            rightButton.AddToClassList("arrow-button");
            rightButton.style.marginLeft = 15;
            horizontalRow.Add(rightButton);

            // Down arrow
            var downButton = new Button(() => MoveGrid(-Vector2Int.down));
            downButton.text = "↓";
            downButton.tooltip = "Move grid content down";
            downButton.AddToClassList("arrow-button");
            arrowSection.Add(downButton);

            parent.Add(arrowSection);
        }

        private void CreateToolbar()
        {
            var toolbar = new Toolbar();
            _rootElement.Add(toolbar);
            
            // Level info
            _levelLabel = new Label("No level selected");
            _levelLabel.style.minWidth = 150;
            toolbar.Add(_levelLabel);
            
            // Language field
            _languageField = new TextField();
            _languageField.style.width = 60;
            _languageField.RegisterValueChangedCallback(evt => {
                if (_currentLevel != null && evt.newValue != _currentLanguageCode)
                {
                    SetCurrentLevel(_currentLevel, evt.newValue);
                }
            });
            toolbar.Add(_languageField);
            
            // Flexible space
            var flexibleSpace = new VisualElement();
            flexibleSpace.style.flexGrow = 1;
            toolbar.Add(flexibleSpace);
            
        }

        private void CreateLetterPalette(VisualElement parent)
        {
            // Create the letter palette container
            _letterPalette = new VisualElement();
            _letterPalette.name = "letter-palette-container";
            _letterPalette.style.display = DisplayStyle.None; // Initially hidden
            parent.Add(_letterPalette);
            
            // The UpdateLetterPalette method will handle loading and applying the UXML
        }
        
        private void CreateCombinedControlPanel(VisualElement parent)
        {
            var controlPanel = new VisualElement();
            controlPanel.pickingMode = PickingMode.Ignore;
            controlPanel.style.flexDirection = FlexDirection.Row;
            controlPanel.style.alignItems = Align.Center;
            controlPanel.style.justifyContent = Justify.SpaceBetween;
            controlPanel.style.paddingBottom = 5;
            controlPanel.style.paddingTop = 10;
            controlPanel.style.flexShrink = 0;
            controlPanel.style.borderTopLeftRadius = 5;
            controlPanel.style.borderTopRightRadius = 5;
            controlPanel.style.borderBottomLeftRadius = 5;
            controlPanel.style.borderBottomRightRadius = 5;
            controlPanel.style.alignSelf = Align.Stretch; // Stretch to parent width
            controlPanel.style.width = Length.Percent(100); // Take full parent width
            parent.Add(controlPanel);

            // Add arrows to the left side of the buttons row
            CreateArrows(controlPanel);

            // Clear button with icon
            var clearButton = new Button(() => ClearGridWithConfirmation());
            clearButton.AddToClassList("control-panel-button");
            clearButton.style.width = 70;
            clearButton.tooltip = "Clear the entire crossword grid";
            
            // Create a container for icon and text
            var clearContent = new VisualElement();
            clearContent.style.flexDirection = FlexDirection.Row;
            clearContent.style.alignItems = Align.Center;
            clearContent.style.justifyContent = Justify.Center;
            clearContent.style.height = Length.Percent(100); // Fill button height
            clearContent.style.width = Length.Percent(100); // Fill button width
            
            // Add eraser icon
            try
            {
                var eraserIcon = EditorGUIUtility.IconContent("Grid.EraserTool").image;
                if (eraserIcon != null)
                {
                    var iconElement = new VisualElement();
                    iconElement.style.backgroundImage = new StyleBackground(eraserIcon as Texture2D);
                    iconElement.style.width = 16;
                    iconElement.style.height = 16;
                    iconElement.style.marginRight = 4;
                    iconElement.style.alignSelf = Align.Center; // Center the icon vertically
                    clearContent.Add(iconElement);
                }
            }
            catch { /* Ignore icon loading errors */ }
            
            var clearLabel = new Label("Clear");
            clearLabel.style.fontSize = 12;
            clearLabel.style.alignSelf = Align.Center; // Center the label vertically
            clearContent.Add(clearLabel);
            clearButton.Add(clearContent);
            controlPanel.Add(clearButton);
            
            // Refresh button with icon
            var refreshButton = new Button();
            refreshButton.name = "refreshButton";
            refreshButton.AddToClassList("control-panel-button");
            refreshButton.style.width = 70;
            refreshButton.tooltip = "Generate a new crossword layout";
            refreshButton.clicked += RefreshCrosswordGrid;

            // Create a container for icon and text
            var refreshContent = new VisualElement();
            refreshContent.style.flexDirection = FlexDirection.Row;
            refreshContent.style.alignItems = Align.Center;
            refreshContent.style.justifyContent = Justify.Center;
            refreshContent.style.height = Length.Percent(100); // Fill button height
            refreshContent.style.width = Length.Percent(100); // Fill button width
            
            // Add refresh icon
            try
            {
                var refreshIcon = EditorGUIUtility.IconContent("Refresh").image;
                if (refreshIcon != null)
                {
                    var iconElement = new VisualElement();
                    iconElement.style.backgroundImage = new StyleBackground(refreshIcon as Texture2D);
                    iconElement.style.width = 16;
                    iconElement.style.height = 16;
                    iconElement.style.marginRight = 4;
                    iconElement.style.alignSelf = Align.Center; // Center the icon vertically
                    refreshContent.Add(iconElement);
                }
            }
            catch { /* Ignore icon loading errors */ }
            
            var refreshLabel = new Label("Refresh");
            refreshLabel.style.fontSize = 12;
            refreshLabel.style.alignSelf = Align.Center; // Center the label vertically
            refreshContent.Add(refreshLabel);
            refreshButton.Add(refreshContent);
            controlPanel.Add(refreshButton);
            
            // Apply button with check icon
            var applyButton = new Button(() => ApplyCrosswordChanges());
            applyButton.AddToClassList("control-panel-button");
            applyButton.style.width = 120;
            applyButton.tooltip = "Apply crossword changes to the level data";
            
            // Create a container for icon and text
            var applyContent = new VisualElement();
            applyContent.style.flexDirection = FlexDirection.Row;
            applyContent.style.alignItems = Align.Center;
            applyContent.style.justifyContent = Justify.Center;
            applyContent.style.height = Length.Percent(100); // Fill button height
            applyContent.style.width = Length.Percent(100); // Fill button width
            
            // Add check icon
            try
            {
                var checkIcon = EditorGUIUtility.IconContent("TestPassed").image; // or "d_Valid" for another check style
                if (checkIcon != null)
                {
                    var iconElement = new VisualElement();
                    iconElement.style.backgroundImage = new StyleBackground(checkIcon as Texture2D);
                    iconElement.style.width = 16;
                    iconElement.style.height = 16;
                    iconElement.style.marginRight = 4;
                    iconElement.style.alignSelf = Align.Center; // Center the icon vertically
                    applyContent.Add(iconElement);
                }
            }
            catch { /* Ignore icon loading errors */ }
            
            var applyLabel = new Label("Apply");
            applyLabel.style.fontSize = 12;
            applyLabel.style.alignSelf = Align.Center; // Center the label vertically
            applyContent.Add(applyLabel);
            applyButton.Add(applyContent);
            controlPanel.Add(applyButton);
            
            parent.Add(controlPanel);


        }
        
        private void CreateStatusBar()
        {
            var statusBar = new VisualElement();
            statusBar.AddToClassList("status-bar");
            
            _statusLabel = new Label();
            _statusLabel.style.fontSize = 12;
            // statusBar.Add(_statusLabel);
            
            var flexSpace = new VisualElement();
            flexSpace.style.flexGrow = 1;
            // statusBar.Add(flexSpace);
            
            var refreshLabel = new Label();
            refreshLabel.style.fontSize = 12;
            refreshLabel.text = "";
            // statusBar.Add(refreshLabel);
            
            _statusBar = statusBar; // Store reference
            _rootElement.Add(statusBar);
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            CrosswordPreviewHandler.OnCrosswordManuallyChanged -= OnCrosswordChanged;
            LevelDataEditor.OnLevelNeedsUpdate -= OnLevelUpdatedFromEditor;
            LevelDataEditor.OnLanguageTabChanged -= OnLanguageTabChanged;
            LevelManagerWindow.OnHierarchySelectionChanged -= OnHierarchySelectionChanged;
            Selection.selectionChanged -= OnSelectionChanged;
            
            // Unsubscribe from Unity's undo/redo events
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }
        
        private void OnUndoRedoPerformed()
        {
            // Refresh the UI when undo/redo is performed
            if (_currentLevel != null)
            {
                RefreshPreviewData();
                UpdateLevelInfo();
                UpdateEditingUI();
                UpdateGridDisplay();
                UpdateStatusBar();
                UpdateLetterPalette();
                
                // Repaint the window to ensure UI updates
                Repaint();
            }
        }
        
        private void OnCrosswordChanged()
        {
            // Refresh the preview data when crossword changes
            RefreshPreviewData();
            UpdateGridDisplay();
            UpdateStatusBar();
        }
        
        private void OnLevelUpdatedFromEditor(Level updatedLevel)
        {
            // Check if this window is displaying the updated level
            if (_currentLevel == updatedLevel)
            {
                // Regenerate the crossword with the updated words
                var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
                if (languageData != null && languageData.words != null && languageData.words.Length > 0)
                {
                    UpdateCrossword(_currentLevel, languageData);
                }

                // Refresh the preview data and UI to reflect the updates from LevelDataEditor
                RefreshPreviewData();
                UpdateGridDisplay();
                UpdateStatusBar();
                UpdateLetterPalette(); // Update letter palette in case letters changed
                
                // Update search results if there are letters in search tiles
                // This ensures that used words are marked as used (bolded) after level updates
                string letters = GetLettersFromSearchTiles();
                if (!string.IsNullOrEmpty(letters))
                {
                    SearchWordsFromTiles();
                }
            }
        }
        
        private void OnLanguageTabChanged(Level level, string languageCode)
        {
            // Only update if this window is showing the same level
            if (_currentLevel == level && _currentLanguageCode != languageCode)
            {
                // Update to the new language
                SetCurrentLevel(level, languageCode);
            }
        }
        
        private void OnHierarchySelectionChanged(LevelHierarchyItem selectedItem)
        {
            // Handle selection from LevelHierarchyTreeView
            if (selectedItem != null && selectedItem.type == LevelHierarchyItem.ItemType.Level && selectedItem.levelAsset != null)
            {
                // Use the same language selection logic as the "Open Grid" button
                // This preserves the user's currently selected language tab
                string languageCode = LevelEditorUtility.GetLanguageCodeForLevel(selectedItem.levelAsset);
                
                if (!string.IsNullOrEmpty(languageCode))
                {
                    SetCurrentLevel(selectedItem.levelAsset, languageCode);
                }
                else
                {
                    // Fallback: try to find any available language data in the level
                    string foundLanguage = FindAvailableLanguage(selectedItem.levelAsset);
                    if (!string.IsNullOrEmpty(foundLanguage))
                    {
                        SetCurrentLevel(selectedItem.levelAsset, foundLanguage);
                    }
                    else
                    {
                        // Set the level anyway but with a default language
                        SetCurrentLevel(selectedItem.levelAsset, "en");
                    }
                }
            }
            else if (selectedItem == null || selectedItem.type != LevelHierarchyItem.ItemType.Level)
            {
                // Clear the current level if a non-level item is selected
                _currentLevel = null;
                _currentLanguageCode = null;
                _previewData = null;
                UpdateLevelInfo();
                UpdateEditingUI();
                UpdateGridDisplay();
                UpdateStatusBar();
            }
        }
        
        private void OnSelectionChanged()
        {
            CheckForSelectedLevel();
        }
        
        private void CheckForSelectedLevel()
        {
            // Only auto-load if we don't already have a level set
            if (_currentLevel != null) return;
            
            // First, try to load the latest used level from EditorPrefs
            string latestLevelGuid = EditorPrefs.GetString("WordsToolkit_LatestLevelGUID", string.Empty);
            if (!string.IsNullOrEmpty(latestLevelGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(latestLevelGuid);
                if (!string.IsNullOrEmpty(path))
                {
                    Level latestLevel = AssetDatabase.LoadAssetAtPath<Level>(path);
                    if (latestLevel != null)
                    {
                        // Use the same language selection logic as the rest of the system
                        string languageCode = LevelEditorUtility.GetLanguageCodeForLevel(latestLevel);
                        if (!string.IsNullOrEmpty(languageCode))
                        {
                            SetCurrentLevel(latestLevel, languageCode);
                            Debug.Log($"CrosswordGridWindow: Loaded latest level '{latestLevel.name}' with language '{languageCode}' from EditorPrefs");
                            return;
                        }
                        else
                        {
                            // Fallback
                            string foundLanguage = FindAvailableLanguage(latestLevel);
                            if (!string.IsNullOrEmpty(foundLanguage))
                            {
                                SetCurrentLevel(latestLevel, foundLanguage);
                                Debug.Log($"CrosswordGridWindow: Loaded latest level '{latestLevel.name}' with fallback language '{foundLanguage}' from EditorPrefs");
                                return;
                            }
                        }
                    }
                }
            }
            
            // If no latest level found, check if a Level asset is selected in the project
            if (Selection.activeObject != null)
            {
                Level selectedLevel = Selection.activeObject as Level;
                if (selectedLevel != null)
                {
                    // Use the same language selection logic as the rest of the system
                    string languageCode = LevelEditorUtility.GetLanguageCodeForLevel(selectedLevel);
                    if (!string.IsNullOrEmpty(languageCode))
                    {
                        SetCurrentLevel(selectedLevel, languageCode);
                        Debug.Log($"CrosswordGridWindow: Loaded level '{selectedLevel.name}' with language '{languageCode}' from project selection");
                    }
                    else
                    {
                        // Fallback
                        string foundLanguage = FindAvailableLanguage(selectedLevel);
                        if (!string.IsNullOrEmpty(foundLanguage))
                        {
                            SetCurrentLevel(selectedLevel, foundLanguage);
                            Debug.Log($"CrosswordGridWindow: Loaded level '{selectedLevel.name}' with fallback language '{foundLanguage}' from project selection");
                        }
                        else
                        {
                            Debug.LogWarning($"Level '{selectedLevel.name}' has no language data available.");
                        }
                    }
                }
            }
            
            // If still no level found, try to load the first available level
            if (_currentLevel == null)
            {
                var allLevels = AssetDatabase.FindAssets("t:Level")
                    .Select(guid => AssetDatabase.LoadAssetAtPath<Level>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(level => level != null)
                    .OrderBy(level => level.number)
                    .ToArray();
                
                if (allLevels.Length > 0)
                {
                    Level firstLevel = allLevels[0];
                    string foundLanguage = FindAvailableLanguage(firstLevel);
                    if (!string.IsNullOrEmpty(foundLanguage))
                    {
                        SetCurrentLevel(firstLevel, foundLanguage);
                        Debug.Log($"CrosswordGridWindow: Loaded first available level '{firstLevel.name}' with language '{foundLanguage}'");
                    }
                }
            }
        }
        
        private string FindAvailableLanguage(Level level)
        {
            if (level == null || level.languages == null || level.languages.Count == 0) 
                return null;

            // First, try to use the currently selected language tab (same as LevelEditorUtility.GetLanguageCodeForLevel)
            int selectedTabIndex = EditorPrefs.GetInt("WordsToolkit_SelectedLanguageTab", 0);
            if (selectedTabIndex >= 0 && selectedTabIndex < level.languages.Count)
            {
                var selectedLanguage = level.languages[selectedTabIndex];
                if (selectedLanguage != null && !string.IsNullOrEmpty(selectedLanguage.language))
                {
                    return selectedLanguage.language;
                }
            }

            // Fallback: return the first available language
            for (int i = 0; i < level.languages.Count; i++)
            {
                var languageData = level.languages[i];
                if (languageData != null && !string.IsNullOrEmpty(languageData.language))
                {
                    return languageData.language;
                }
            }
            
            return null;
        }
        
        private void LoadSelectedLevel()
        {
            // Force load the selected level even if we already have one
            if (Selection.activeObject != null)
            {
                Level selectedLevel = Selection.activeObject as Level;
                if (selectedLevel != null)
                {
                    // Use the same language selection logic as the rest of the system
                    string languageCode = LevelEditorUtility.GetLanguageCodeForLevel(selectedLevel);
                    if (!string.IsNullOrEmpty(languageCode))
                    {
                        SetCurrentLevel(selectedLevel, languageCode);
                        Debug.Log($"Loaded level: {selectedLevel.name} with language: {languageCode}");
                    }
                    else
                    {
                        // Fallback: try to find any available language data in the level
                        string foundLanguage = FindAvailableLanguage(selectedLevel);
                        if (!string.IsNullOrEmpty(foundLanguage))
                        {
                            SetCurrentLevel(selectedLevel, foundLanguage);
                            Debug.Log($"Loaded level: {selectedLevel.name} with fallback language: {foundLanguage}");
                        }
                        else
                        {
                            // Set the level anyway but with a default language
                            SetCurrentLevel(selectedLevel, "en");
                            Debug.LogWarning($"Level '{selectedLevel.name}' has no language data. Using default language 'en'.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Selected object is not a Level asset.");
                }
            }
            else
            {
                Debug.LogWarning("No object selected in Project window.");
            }
        }
        
        private void RefreshPreviewData()
        {
            if (_currentLevel != null && !string.IsNullOrEmpty(_currentLanguageCode))
            {
                _previewData = CrosswordPreviewHandler.LoadPreviewFromLevel(_currentLevel, _currentLanguageCode);
                
                // Ensure icon data is properly initialized
                if (_previewData != null && _previewData.isValid)
                {
                    if (string.IsNullOrEmpty(_previewData.iconPath))
                    {
                        _previewData.iconPath = IconPath;
                    }
                    
                    // Try to load icon texture if path is specified
                    if (_previewData.iconTexture == null && !string.IsNullOrEmpty(_previewData.iconPath))
                    {
                        // Try to load the texture from Resources or assign a default
                        // For now, we'll leave it null and use text fallback
                    }
                }
            }
        }

        private void UpdateEditingUI()
        {
            if (_letterPalette == null) return;
            
            bool hasValidLevel = _enableEditing && _currentLevel != null && _currentLevel;
            
            if (hasValidLevel)
            {
                _letterPalette.style.display = DisplayStyle.Flex;
                UpdateLetterPalette();
            }
            else
            {
                _letterPalette.style.display = DisplayStyle.None;
            }
            
            // Hide/show additional UI elements based on level selection
            if (_combinedControlPanel != null)
            {
                _combinedControlPanel.style.display = (_currentLevel != null && _currentLevel) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            if (_searchContainer != null)
            {
                _searchContainer.style.display = (_currentLevel != null && _currentLevel) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            if (_statusBar != null)
            {
                _statusBar.style.display = (_currentLevel != null && _currentLevel) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            if (_searchResultsArea != null)
            {
                _searchResultsArea.style.display = (_currentLevel != null && _currentLevel) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            if (_instructionsLabel != null)
            {
                _instructionsLabel.style.display = (_currentLevel != null && _currentLevel) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        private void UpdateLetterPalette()
        {
            // Load UXML template with multiple path attempts
            string[] possiblePaths = {
                "Assets/WordConnectGameToolkit/UIBuilder/CrosswordLetterPalette.uxml",
                "Assets/WordConnectGameToolkit/UIBuilder/CrosswordLetterPalette",
                "CrosswordLetterPalette.uxml",
                "CrosswordLetterPalette"
            };
            
            VisualTreeAsset visualTree = null;
            string usedPath = "";
            
            foreach (var path in possiblePaths)
            {
                visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (visualTree != null)
                {
                    usedPath = path;
                    break;
                }
            }
            
            // Also try finding by name
            if (visualTree == null)
            {
                string[] guids = AssetDatabase.FindAssets("CrosswordLetterPalette t:VisualTreeAsset");
                if (guids.Length > 0)
                {
                    usedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(usedPath);
                }
            }
            
            UpdateLetterPalette(visualTree);
        }

        private void UpdateLetterPalette(VisualTreeAsset visualTree)
        {
            if (_letterPalette == null || _currentLevel == null) return;
            
            _letterPalette.Clear();
            
            var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
            if (languageData == null) return;

            if (visualTree != null)
            {
                // Clone the tree and get the root letter-palette container from UXML
                var clonedTree = visualTree.CloneTree();

                    // Fallback: if no letter-palette container found, add the entire cloned tree
                    _letterPalette.Add(clonedTree);
                    
                    // Load USS stylesheet
                    var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/WordConnectGameToolkit/Scripts/Levels/Editor/CrosswordLetterPalette.uss");
                    if (styleSheet != null)
                    {
                        _letterPalette.styleSheets.Add(styleSheet);
                    }
                    
                    BindPropertiesToUXML(clonedTree, languageData);
                    return;

            }
            else
            {
                Debug.LogError("CrosswordGridWindow: UXML file not found at any of the attempted paths");
            }
            
            // Fallback to manual creation if UXML not found
            // CreateLetterPaletteManually(languageData);
        }

        private void BindPropertiesToUXML(VisualElement paletteRoot, LanguageData languageData)
        {
            if (_currentLevel == null) return;
            
            var serializedObject = new SerializedObject(_currentLevel);

            // Bind letters field (TextField with label)
            var lettersField = paletteRoot.Q<TextField>("letters-field");
            var initialValue = languageData.letters ?? "";
            lettersField.RegisterValueChangedCallback(evt =>
            {
                // Don't trigger on initial setup (when previous value is empty and new value matches initial data)
                if (evt.newValue != evt.previousValue &&
                    !(string.IsNullOrEmpty(evt.previousValue) && evt.newValue == initialValue))
                {
                    // Record undo before changing letters
                    if (_currentLevel != null)
                    {
                        // Ensure grid data is serialized before recording undo
                        var langData = _currentLevel.GetLanguageData(_currentLanguageCode);
                        if (langData?.crosswordData != null)
                        {
                            langData.crosswordData.SerializeGrid();
                        }

                        Undo.RecordObject(_currentLevel, "Change Letters");
                    }

                    languageData.letters = evt.newValue;
                    var lettersLength = paletteRoot.Q<IntegerField>("letters-length");
                    if (lettersLength != null)
                    {
                        lettersLength.value = evt.newValue?.Length ?? 0;
                    }

                    _currentLevel.letters = evt.newValue?.Length ?? 0;
                    EditorUtility.SetDirty(_currentLevel);

                    // Generate new words for the updated letters
                    if (!string.IsNullOrEmpty(evt.newValue))
                    {
                        LevelDataEditor.UpdateAvailableWordsForLevel(_currentLevel);
                        RefreshPreviewData();
                    }

                    UpdateGridDisplay();
                }
            });
            lettersField.value = initialValue;

            // Bind letters length field
            var lettersLength = paletteRoot.Q<TextField>("letters-length");
            lettersLength.value = _currentLevel.letters.ToString();
            lettersLength.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int value))
                {
                    _currentLevel.letters = value;
                    EditorUtility.SetDirty(_currentLevel);
                    EditorPrefs.SetInt("WordsToolkit_LettersAmount", value);
                }
            });

            var generateLettersCheck = paletteRoot.Q<Toggle>("generate-letters-check");
            generateLettersCheck.value = EditorPrefs.GetBool("WordsToolkit_GenerateLetters", true);
            generateLettersCheck.RegisterValueChangedCallback(evt => {
                if (_currentLevel != null)
                {
                    EditorUtility.SetDirty(_currentLevel);
                    EditorPrefs.SetBool("WordsToolkit_GenerateLetters", evt.newValue);
                }
            });
            // Bind generate button
            var generateButton = paletteRoot.Q<Button>("generate-button");
            generateButton.clicked += () => {
                if (_currentLevel != null && languageData != null)
                {
                    // Record undo before generating new letters
                    if (_currentLevel != null)
                    {
                        // Ensure grid data is serialized before recording undo
                        var langData = _currentLevel.GetLanguageData(_currentLanguageCode);
                        if (langData?.crosswordData != null)
                        {
                            langData.crosswordData.SerializeGrid();
                        }

                        Undo.RecordObject(_currentLevel, "Generate Random Letters");
                    }

                    var modelController = EditorScope.Resolve<IModelController>();
                    string letters = LevelEditorServices.GenerateRandomLetters(languageData, languageData.wordsAmount, _currentLevel.letters, generateLettersCheck.value );
                    languageData.letters = letters;

                    // Update the letters field in the UI
                    if (lettersField != null) lettersField.value = languageData.letters;
                    if (lettersLength != null) lettersLength.value = (languageData.letters?.Length ?? 0).ToString();

                    languageData.words = new string[0];
                    EditorUtility.SetDirty(_currentLevel);
                    LevelEditorServices.GenerateAvailableWords(_currentLevel, modelController, languageData);
                    UpdateCrossword(_currentLevel, languageData);
                    LevelDataEditor.NotifyWordsListNeedsUpdate(_currentLevel, languageData.language);

                    RefreshPreviewData();
                    UpdateLetterPalette();
                    UpdateGridDisplay();
                    UpdateStatusBar();

                }
            };

            // Bind grid size controls - columns field
            var columnsField = paletteRoot.Q<TextField>("columns-field");
            columnsField.value = (_previewData?.columns ?? 10).ToString();
            columnsField.RegisterValueChangedCallback(evt => {
                if (_previewData != null && int.TryParse(evt.newValue, out int value) && value >= 5 && value <= 50)
                {
                    _previewData.columns = value;
                    CrosswordPreviewHandler.SavePreviewToLevel(_currentLevel, _currentLanguageCode, _previewData);
                    EditorUtility.SetDirty(_currentLevel);
                    EditorPrefs.SetInt("WordsToolkit_grid_x", value);
                    UpdateGridDisplay();
                    UpdateStatusBar();
                }
            });
            
            // Bind grid size controls - rows field
            var rowsField = paletteRoot.Q<TextField>("rows-field");
            rowsField.value = (_previewData?.rows ?? 7).ToString();
            rowsField.RegisterValueChangedCallback(evt => {
                if (_previewData != null && int.TryParse(evt.newValue, out int value) && value >= 5 && value <= 50)
                {
                    _previewData.rows = value;
                    CrosswordPreviewHandler.SavePreviewToLevel(_currentLevel, _currentLanguageCode, _previewData);
                    EditorUtility.SetDirty(_currentLevel);
                    EditorPrefs.SetInt("WordsToolkit_grid_y", value);
                    UpdateGridDisplay();
                    UpdateStatusBar();
                }
            });
            
            // Bind Test Level button
            var testLevelButton = paletteRoot.Q<Button>("test-level-button");
            if (testLevelButton != null)
            {
                testLevelButton.clicked += TestCurrentLevel;
            }
            
            // Bind hard level toggle with automatic binding
            var hardLevelField = paletteRoot.Q<Toggle>("hard-level-field");
            if (hardLevelField != null)
            {
                hardLevelField.Bind(serializedObject);
            }
            
            var enableTimerField = paletteRoot.Q<Toggle>("enable-timer-field");
            var duration = paletteRoot.Q<FloatField>("Duration");
            
            if (enableTimerField != null)
            {
                if (duration != null)
                {
                    duration.Bind(serializedObject);
                    duration.SetEnabled(_currentLevel.enableTimer);
                    enableTimerField.RegisterValueChangedCallback(evt =>
                    {
                        duration.SetEnabled(enableTimerField.value);
                    });
                }
                
                enableTimerField.Bind(serializedObject);
            }
            else if (duration != null)
            {
                duration.Bind(serializedObject);
            }

            // Bind background field with automatic binding
            var backgroundField = paletteRoot.Q<ObjectField>("background-field");
            if (backgroundField != null)
            {
                backgroundField.Bind(serializedObject);
            }
            
            // Bind colors tile field with automatic binding (PropertyField)
            var colorsTileField = paletteRoot.Q<PropertyField>("colors-tile-field");
            if (colorsTileField != null)
            {
                colorsTileField.Bind(serializedObject);
            }
        }

        private VisualElement CreateLetterButtonsRow()
        {
            if (_currentLevel == null) return null;

            var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
            if (languageData == null || string.IsNullOrEmpty(languageData.letters)) return null;

            var buttonsRow = new VisualElement();
            buttonsRow.style.flexDirection = FlexDirection.Row;
            buttonsRow.style.flexWrap = Wrap.Wrap;
            buttonsRow.style.flexShrink = 0; // Don't shrink
            buttonsRow.style.paddingTop = 5;
            buttonsRow.style.paddingBottom = 5;
            buttonsRow.style.alignSelf = Align.FlexStart;
            buttonsRow.style.justifyContent = Justify.FlexStart;

            // Special item button
            var itemButton = new Button(() => {
                _isSpecialItemSelected = true;
                UpdateGridDisplay(); // Refresh the grid display to update button states
            });

            // Load and set the icon image
            var iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (iconTexture != null)
            {
                var iconElement = new VisualElement();
                iconElement.style.backgroundImage = new StyleBackground(iconTexture);
                iconElement.style.width = 20;
                iconElement.style.height = 20;
                iconElement.style.alignSelf = Align.Center;
                iconElement.style.position = Position.Absolute;
                iconElement.style.left = Length.Percent(50);
                iconElement.style.top = Length.Percent(50);
                iconElement.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
                itemButton.Add(iconElement);
            }
            else
            {
                // Fallback to text if image cannot be loaded
                itemButton.text = "Item";
            }

            itemButton.style.width = 40;
            itemButton.style.height = 35;
            itemButton.style.marginRight = 5;
            itemButton.style.marginBottom = 5;
            itemButton.AddToClassList("item-button");
            if (_isSpecialItemSelected)
            {
                itemButton.AddToClassList("letter-button-selected");
            }
            else
            {
                itemButton.RemoveFromClassList("letter-button-selected");
            }
            buttonsRow.Add(itemButton);

            // Letter buttons
            foreach (char c in languageData.letters.ToUpper())
            {
                if (char.IsLetter(c))
                {
                    var letterButton = new Button(() => {
                        _currentSelectedLetter = c;
                        _isSpecialItemSelected = false;
                        UpdateGridDisplay(); // Refresh the grid display to update button states
                    });
                    letterButton.text = c.ToString();
                    letterButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                    letterButton.AddToClassList("letter-button");
                    if (!_isSpecialItemSelected && c == _currentSelectedLetter)
                    {
                        letterButton.AddToClassList("letter-button-selected");
                    }
                    else
                    {
                        letterButton.RemoveFromClassList("letter-button-selected");
                    }

                    buttonsRow.Add(letterButton);
                }
            }

            return buttonsRow;
        }

        private void UpdateGridDisplay()
        {
            if (_gridContainer == null) return;

            _gridContainer.Clear();

            // Clear and update letter buttons container
            var letterButtonsContainer = _rootElement.Q<VisualElement>("letterButtonsContainer");
            if (letterButtonsContainer != null)
            {
                letterButtonsContainer.Clear();
                
                if (_enableEditing && _currentLevel != null)
                {
                    var buttonsRow = CreateLetterButtonsRow();
                    if (buttonsRow != null)
                    {
                        buttonsRow.style.alignSelf = Align.FlexEnd;
                        letterButtonsContainer.Add(buttonsRow);
                    }
                }
            }

            if (_currentLevel == null || string.IsNullOrEmpty(_currentLanguageCode))
            {
                var noLevelLabel = new Label("No level selected.\n\nTo use this window:\n1. Select a level in the Level Editor hierarchy\n2. Or select a Level asset in the Project window and click 'Load Selected'\n\nThe window will automatically load selected levels from the hierarchy.");
                noLevelLabel.style.fontSize = 14;
                noLevelLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                noLevelLabel.style.whiteSpace = WhiteSpace.Normal;
                noLevelLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noLevelLabel.style.alignSelf = Align.Center;
                _gridContainer.Add(noLevelLabel);
                return;
            }

            if (_previewData == null || !_previewData.isValid)
            {
                var noDataLabel = new Label("No valid crossword data available for this level and language.\n\nGenerate a crossword in the Level Inspector first.");
                noDataLabel.style.fontSize = 14;
                noDataLabel.style.color = new Color(1f, 0.7f, 0.7f);
                noDataLabel.style.whiteSpace = WhiteSpace.Normal;
                noDataLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noDataLabel.style.alignSelf = Align.Center;
                _gridContainer.Add(noDataLabel);
                return;
            }

            CreateGridVisualElements();
        }

        private void UpdateCrossword(Level level, LanguageData languageData)
        {
            // Generate and save the crossword preview (same as LevelDataEditor)
            const int DefaultColumns = 10;
            const int DefaultRows = 7;
            
            // Use existing grid size from level data if available and valid, otherwise use defaults
            int columns = (languageData.crosswordData != null && languageData.crosswordData.columns > 0) 
                ? languageData.crosswordData.columns 
                : DefaultColumns;
            int rows = (languageData.crosswordData != null && languageData.crosswordData.rows > 0) 
                ? languageData.crosswordData.rows 
                : DefaultRows;

            var previewData = GeneratePreviewForLanguage(languageData.words, columns, rows);
            if (previewData != null)
            {
                CrosswordPreviewHandler.SavePreviewToLevel(level, languageData.language, previewData);
            }
        }

        private CrosswordPreviewHandler.PreviewData GeneratePreviewForLanguage(string[] words, int columns, int rows)
        {
            if (words.Length == 0)
            {
                ClearGrid();
            }
            if (words == null || words.Length == 0)
                return null;

            return CrosswordPreviewHandler.GeneratePreview(words, columns, rows);
        }

        private void CreateGridVisualElements()
        {
            if (_previewData == null || !_previewData.isValid) return;

            int gridWidth = _previewData.columns;
            int gridHeight = _previewData.rows;

            var gridElement = new VisualElement();
            gridElement.pickingMode = PickingMode.Ignore;
            gridElement.style.flexDirection = FlexDirection.Column;
            gridElement.style.alignSelf = Align.Center;
            gridElement.style.flexShrink = 0; // Don't shrink
            gridElement.style.width = StyleKeyword.Auto; // Size to content
            gridElement.AddToClassList("grid-background");

            _gridContainer.Add(gridElement);

            // Create grid rows
            for (int y = 0; y < gridHeight; y++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.flexShrink = 0; // Don't shrink rows
                row.style.width = StyleKeyword.Auto; // Size rows to content
                gridElement.Add(row);

                // Create cells in this row
                for (int x = 0; x < gridWidth; x++)
                {
                    CreateGridCell(row, x, y);
                }
            }

            // Update combined control panel width to match grid width
            if (_combinedControlPanel != null)
            {
                // Use the actual rendered width of the grid container with delay
                _combinedControlPanel.schedule.Execute(() =>
                {
                    float actualGridWidth = _gridContainer.resolvedStyle.width;
                    _combinedControlPanel.style.width = actualGridWidth;
                    _combinedControlPanel.style.minWidth = 400;
                    _combinedControlPanel.style.maxWidth = 566;
                }).StartingIn(100);
            }

            // Create combined control panel below the grid
        }

        private void CreateGridCell(VisualElement parent, int x, int y)
        {
            var cell = new VisualElement();
            cell.style.width = gridCellSize;
            cell.style.height = gridCellSize;
            cell.style.marginRight = gridSpacing;
            cell.style.marginBottom = gridSpacing;

            // Safe grid access with bounds checking
            char c = (x >= 0 && x < _previewData.grid.GetLength(0) &&
                    y >= 0 && y < _previewData.grid.GetLength(1)) ? _previewData.grid[x, y] : (char)0;
            bool hasLetter = (c != 0);

            // Check if this cell has an icon
            Vector2Int pos = new Vector2Int(x, y);
            bool hasIcon = _previewData.iconPositions.ContainsKey(pos);

            // Apply CSS classes based on cell state
            if (hasLetter || hasIcon)
            {
                cell.AddToClassList("grid-cell");
            }
            else
            {
                cell.AddToClassList("grid-cell-empty");
            }

            // Add letter label if it exists
            if (hasLetter)
            {
                var letterLabel = new Label(c.ToString().ToUpper());
                letterLabel.AddToClassList("grid-cell-letter");
                cell.Add(letterLabel);

                // Add word number if enabled
                if (_showGridNumbers && _previewData.placements != null)
                {
                    foreach (var placement in _previewData.placements)
                    {
                        if (placement.startPosition.x == x && placement.startPosition.y == y)
                        {
                            var numberLabel = new Label(placement.wordNumber.ToString());
                            numberLabel.AddToClassList("grid-cell-number");
                            cell.Add(numberLabel);
                            break;
                        }
                    }
                }
            }

            // Add icon indicator if it exists
            if (hasIcon)
            {
                var iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
                if (iconTexture != null)
                {
                    var iconElement = new VisualElement();
                    iconElement.style.backgroundImage = new StyleBackground(iconTexture);
                    iconElement.style.width = gridCellSize/2 - 4; // Slightly smaller than cell
                    iconElement.style.height = gridCellSize/2- 4;
                    iconElement.style.position = Position.Absolute;
                    iconElement.style.left = 2;
                    iconElement.style.top = 2;
                    iconElement.AddToClassList("grid-cell-icon");
                    cell.Add(iconElement);
                }
                else
                {
                    // Fallback to text if image cannot be loaded
                    var iconLabel = new Label("Item");
                    iconLabel.AddToClassList("grid-cell-icon");
                    cell.Add(iconLabel);
                }
            }

            // Add mouse interaction
            if (_enableEditing)
            {
                cell.RegisterCallback<MouseDownEvent>(evt => HandleCellClick(evt, x, y, hasLetter, hasIcon));
            }

            parent.Add(cell);
        }

        private void HandleCellClick(MouseDownEvent evt, int x, int y, bool hasLetter, bool hasIcon)
        {
            if (!_enableEditing) return;

            bool changed = false;
            Vector2Int pos = new Vector2Int(x, y);

            // Record undo before making changes
            if (_currentLevel != null)
            {
                // Ensure grid data is serialized before recording undo
                var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
                if (languageData?.crosswordData != null)
                {
                    languageData.crosswordData.SerializeGrid();
                }
                
                Undo.RecordObject(_currentLevel, "Crossword Grid Edit");
            }

            if (evt.button == 1) // Right click - handle removal
            {
                if (hasIcon)
                {
                    // Remove special item first
                    _previewData.iconPositions.Remove(pos);
                    changed = true;
                }
                else if (hasLetter)
                {
                    // Remove letter
                    _previewData.grid[x, y] = (char)0;
                    changed = true;
                }
            }
            else if (evt.button == 0) // Left click - handle placement
            {
                if (_isSpecialItemSelected && hasLetter)
                {
                    // Handle special item placement/toggle
                    if (_previewData.iconPositions.ContainsKey(pos))
                    {
                        _previewData.iconPositions.Remove(pos);
                    }
                    else
                    {
                        _previewData.iconPositions[pos] = _previewData.iconPath ?? IconPath;
                    }
                    changed = true;
                }
                else if (!_isSpecialItemSelected)
                {
                    // Place letter regardless of icon presence
                    _previewData.grid[x, y] = _currentSelectedLetter;
                    changed = true;
                }
            }

            if (changed)
            {
                // Save changes and trigger events
                CrosswordPreviewHandler.SavePreviewToLevel(_currentLevel, _currentLanguageCode, _previewData);
                CrosswordPreviewHandler.TriggerManualChangeEvent();
                
                // Mark object as dirty for Unity
                if (_currentLevel != null)
                {
                    EditorUtility.SetDirty(_currentLevel);
                }

                EditorUtility.SetDirty(_currentLevel);
                evt.StopPropagation();
                UpdateGridDisplay();
                UpdateStatusBar();
            }
        }

        private void HandleSearchTileClick(MouseDownEvent evt, VisualElement cell, int index)
        {
            if (!_enableEditing) return;

            if (evt.button == 0) // Left click - place current selected letter
            {
                if (!_isSpecialItemSelected)
                {
                    // Clear any existing letter content but preserve the number label
                    ClearSearchTileContent(cell, index);

                    // Add the current selected letter to the search tile
                    var letterLabel = new Label(_currentSelectedLetter.ToString().ToUpper());
                    letterLabel.AddToClassList("grid-cell-letter");
                    cell.Add(letterLabel);
                    SearchWordsFromTiles();
                }
            }
            else if (evt.button == 1) // Right click - clear the cell
            {
                ClearSearchTileContent(cell, index);
                SearchWordsFromTiles();
            }

            evt.StopPropagation();
        }

        private void ClearSearchTileContent(VisualElement cell, int index)
        {
            // Find and preserve the number label
            var numberLabel = cell.Q<Label>(className: "search-tile-number");

            // Clear all content
            cell.Clear();

            // Re-add the preserved number label, or create a new one if not found
            if (numberLabel == null)
            {
                numberLabel = new Label((index + 1).ToString());
                numberLabel.style.position = Position.Absolute;
                numberLabel.style.left = 2;
                numberLabel.style.top = 2;
                numberLabel.style.fontSize = 8;
                numberLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                numberLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                numberLabel.AddToClassList("search-tile-number");
            }

            cell.Add(numberLabel);
        }

        private void ClearSearchTiles()
        {
            if (_searchTiles == null) return;

            for (int i = 0; i < _searchTiles.Count; i++)
            {
                ClearSearchTileContent(_searchTiles[i], i);
            }

            // Clear search results when all tiles are cleared
            ClearSearchResults();
        }

        private void ClearSearchResults()
        {
            if (_searchResultsContainer != null)
            {
                _searchResultsContainer.Clear();
            }
        }

        private void SearchWordsFromTiles()
        {
            if (_searchTiles == null || _currentLanguageCode == null)
            {
                Debug.LogWarning("Cannot search: search tiles or language not available");
                return;
            }

            // Check if there are any letters placed on the tiles
            string letters = GetLettersFromSearchTiles();
            if (string.IsNullOrEmpty(letters))
            {
                // No letters placed on tiles, clear search results
                UpdateSearchResults("Click on tiles to add letters first, then search for words.");
                return;
            }

            // Get pattern from search tiles
            string searchPattern = GetSearchPatternFromTiles();

            if (string.IsNullOrEmpty(searchPattern))
            {
                Debug.LogWarning("No search pattern found in tiles");
                UpdateSearchResults("Click on tiles to add letters first, then search for words.");
                return;
            }

            try
            {
                // Get model controller
                var modelController = EditorScope.Resolve<IModelController>();
                if (modelController == null)
                {
                    Debug.LogError("Model controller not available");
                    UpdateSearchResults("Error: Model controller not available.");
                    return;
                }

                // Ensure model is loaded
                modelController.LoadModels();

                // Search for words using the pattern
                var foundWords = SearchWordsByPattern(searchPattern, _currentLanguageCode);

                if (foundWords.Count == 0)
                {
                    UpdateSearchResultsWithWords(new List<string>());
                }
                else
                {
                    // Display words as individual fields with warning icons
                    UpdateSearchResultsWithWords(foundWords);
                }

                Debug.Log($"Pattern search completed. Found {foundWords.Count} words for pattern: {searchPattern}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during pattern search: {e.Message}");
                UpdateSearchResults($"Error during search: {e.Message}");
            }
        }

        private string GetLettersFromSearchTiles()
        {
            if (_searchTiles == null) return "";

            string letters = "";
            foreach (var tile in _searchTiles)
            {
                // Check if tile has a letter label (specifically with grid-cell-letter class)
                var labels = tile.Query<Label>().ToList();
                var letterLabel = labels.FirstOrDefault(l => l.ClassListContains("grid-cell-letter"));
                if (letterLabel != null && !string.IsNullOrEmpty(letterLabel.text))
                {
                    letters += letterLabel.text;
                }
            }

            return letters.ToUpper();
        }

        private string GetSearchPatternFromTiles()
        {
            if (_searchTiles == null) return "";

            string pattern = "";
            foreach (var tile in _searchTiles)
            {
                // Check if tile has a letter label (specifically with grid-cell-letter class)
                var labels = tile.Query<Label>().ToList();
                var letterLabel = labels.FirstOrDefault(l => l.ClassListContains("grid-cell-letter"));
                if (letterLabel != null && !string.IsNullOrEmpty(letterLabel.text))
                {
                    pattern += letterLabel.text.ToLower();
                }
                else
                {
                    pattern += "*"; // Wildcard for empty positions
                }
            }

            // Pattern is always 11 characters long
            return pattern;
        }

        private List<string> SearchWordsByPattern(string pattern, string language)
        {
            var modelController = EditorScope.Resolve<IModelController>();
            if (modelController == null || !modelController.IsModelLoaded(language))
            {
                return new List<string>();
            }

            var allWords =  modelController.GetWordsFromSymbols(_currentLevel.GetLetters(language), language);
            var matchingWords = new List<string>();

            foreach (var word in allWords)
            {
                if (DoesWordMatchPattern(word, pattern))
                {
                    matchingWords.Add(word);
                }
            }

            return matchingWords.OrderBy(w => w).ToList();
        }

        private bool DoesWordMatchPattern(string word, string pattern)
        {
            if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(pattern))
                return false;

            // Pattern should always be 11 characters, word can be shorter
            if (word.Length > pattern.Length)
                return false;

            word = word.ToLower();

            // Check each position in the word against the pattern
            for (int i = 0; i < word.Length; i++)
            {
                char patternChar = pattern[i];
                char wordChar = word[i];

                // If pattern has a specific letter, word must match at that position
                // '*' means wildcard (any letter or nothing)
                if (patternChar != '*' && patternChar != wordChar)
                {
                    return false;
                }
            }

            // Check remaining pattern positions after word ends
            // They should all be wildcards (*) since the word is shorter
            for (int i = word.Length; i < pattern.Length; i++)
            {
                if (pattern[i] != '*')
                {
                    return false; // Pattern requires a letter but word has ended
                }
            }

            return true;
        }

        private int GetWordLengthFromTiles()
        {
            if (_searchTiles == null) return 0;

            int count = 0;
            foreach (var tile in _searchTiles)
            {
                // Check if tile has a letter label (specifically with grid-cell-letter class)
                var labels = tile.Query<Label>().ToList();
                var letterLabel = labels.FirstOrDefault(l => l.ClassListContains("grid-cell-letter"));
                if (letterLabel != null && !string.IsNullOrEmpty(letterLabel.text))
                {
                    count++;
                }
            }

            return count;
        }

        private void UpdateSearchResults(string content)
        {
            if (_searchResultsContainer != null)
            {
                _searchResultsContainer.Clear();

                var label = new Label(content);
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.marginBottom = 5;
                _searchResultsContainer.Add(label);
            }
        }

        private void UpdateSearchResultsWithWords(List<string> words)
        {
            if (_searchResultsContainer == null) return;

            _searchResultsContainer.Clear();

            if (words == null || words.Count == 0)
            {
                var noResultsLabel = new Label("No words found");
                noResultsLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                noResultsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                _searchResultsContainer.Add(noResultsLabel);
                return;
            }

            // Get services for checking word status
            var bannedWordsService = EditorScope.Resolve<IBannedWordsService>();

            const int wordsPerColumn = 8; // Number of words per column
            int totalColumns = (int)Math.Ceiling((double)words.Count / wordsPerColumn);

            // Create table container with horizontal layout for columns
            var tableContainer = new VisualElement();
            tableContainer.pickingMode = PickingMode.Ignore;
            tableContainer.style.flexDirection = FlexDirection.Row;
            tableContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

            // Create columns
            for (int col = 0; col < totalColumns; col++)
            {
                var columnContainer = new VisualElement();
                columnContainer.pickingMode = PickingMode.Ignore;
                columnContainer.style.flexDirection = FlexDirection.Column;
                columnContainer.style.flexGrow = 1;
                columnContainer.style.marginRight = 10; // Space between columns

                // Add words to this column (8 words per column)
                for (int row = 0; row < wordsPerColumn; row++)
                {
                    int wordIndex = col * wordsPerColumn + row;

                    if (wordIndex < words.Count)
                    {
                        string word = words[wordIndex];

                        // Check if word is banned
                        bool isWordBanned = !string.IsNullOrEmpty(word) &&
                                          bannedWordsService != null &&
                                          bannedWordsService.IsWordBanned(word, _currentLanguageCode);

                        // Check if word is used in other levels
                        var usedInLevels = LevelEditorServices.GetUsedInLevels(word, _currentLanguageCode, _currentLevel);
                        bool isWordUsedInOtherLevels = !string.IsNullOrEmpty(word) && usedInLevels.Length > 0;

                        // Check if word is already used in the current level's word list (crossword)
                        bool isWordUsedInLevel = false;
                        var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
                        if (languageData != null && languageData.words != null)
                        {
                            for (int j = 0; j < languageData.words.Length; j++)
                            {
                                if (languageData.words[j] == word)
                                {
                                    isWordUsedInLevel = true;
                                    break;
                                }
                            }
                        }

                        // Create word container
                        var wordContainer = new VisualElement();
                        wordContainer.pickingMode = PickingMode.Ignore;
                        wordContainer.style.flexDirection = FlexDirection.Row;
                        wordContainer.style.alignItems = Align.Center;
                        wordContainer.style.marginBottom = 1; // Reduced margin
                        wordContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

                        // Always add icon container for consistent alignment
                        var iconContainer = new VisualElement();
                        iconContainer.pickingMode = PickingMode.Ignore;
                        iconContainer.style.width = 20;
                        iconContainer.style.height = 20;
                        iconContainer.style.alignItems = Align.Center;
                        iconContainer.style.justifyContent = Justify.Center;
                        iconContainer.style.flexShrink = 0;
                        iconContainer.style.marginRight = 5;

                        // Add warning icon if needed
                        if (isWordBanned || isWordUsedInOtherLevels)
                        {
                            if (isWordBanned && isWordUsedInOtherLevels)
                            {
                                // Show both warning and banned status
                                iconContainer.tooltip = $"WARNING: Used in other levels AND this word is banned for {_currentLanguageCode}";

                                var combinedImage = new Image();
                                combinedImage.image = warningIcon;
                                combinedImage.style.width = 14; // Smaller icon
                                combinedImage.style.height = 14;
                                combinedImage.tintColor = Color.red; // Red for banned + used
                                iconContainer.Add(combinedImage);
                            }
                            else if (isWordBanned)
                            {
                                // Show only banned status
                                iconContainer.tooltip = $"This word is banned for language {_currentLanguageCode}";

                                var bannedImage = new Image();
                                bannedImage.image = warningIcon;
                                bannedImage.style.width = 14; // Smaller icon
                                bannedImage.style.height = 14;
                                bannedImage.tintColor = Color.red;
                                iconContainer.Add(bannedImage);
                            }
                            else if (isWordUsedInOtherLevels)
                            {
                                // Show only warning status
                                string tooltipText = usedInLevels.Length == 1
                                    ? $"This word has already been used in level {usedInLevels[0].number}"
                                    : $"This word has already been used in levels: {string.Join(", ", usedInLevels.OrderBy(l => l.number).Select(l => l.number.ToString()))}";
                                iconContainer.tooltip = tooltipText;

                                var warningImage = new Image();
                                warningImage.image = warningIcon;
                                warningImage.style.width = 14; // Smaller icon
                                warningImage.style.height = 14;
                                warningImage.tintColor = Color.yellow;
                                iconContainer.Add(warningImage);
                            }
                        }
                        // Icon container is always added, even if empty, for consistent alignment

                        wordContainer.Add(iconContainer);

                        // Create text field for the word - smaller size
                        var wordField = new TextField();
                        // Add asterisk if word is used in current level's crossword
                        string displayText = isWordUsedInLevel ? $"{word} *" : word;
                        wordField.value = displayText;
                        wordField.isReadOnly = true;
                        wordField.style.flexGrow = 1;
                        wordField.style.marginRight = 5;
                        wordField.style.fontSize = 12; // Smaller font
                        wordField.style.height = 20; // Smaller height

                        // Apply bold styling if word is used in current level's crossword
                        if (isWordUsedInLevel)
                        {
                            wordField.style.unityFontStyleAndWeight = FontStyle.Bold;
                            wordField.tooltip = "This word is already used in the level";
                        }
                        else
                        {
                            wordField.style.unityFontStyleAndWeight = FontStyle.Normal;
                        }

                        wordContainer.Add(wordField);
                        columnContainer.Add(wordContainer);
                    }
                }

                tableContainer.Add(columnContainer);
            }

            _searchResultsContainer.Add(tableContainer);
        }

        private string FormatWordsInColumns(List<string> words)
        {
            if (words == null || words.Count == 0)
                return "";

            const int wordsPerColumn = 8; // Number of words per column
            var result = new StringBuilder();

            // Calculate the maximum word length for proper spacing
            int maxWordLength = words.Max(w => w.Length);
            int columnWidth = Math.Max(maxWordLength + 6, 14); // Increased spacing: at least 14 characters wide, +6 for more space

            // Calculate number of columns needed
            int totalColumns = (int)Math.Ceiling((double)words.Count / wordsPerColumn);

            // Get banned words service for checking banned status
            var bannedWordsService = EditorScope.Resolve<IBannedWordsService>();

            // Display words in columns (8 words per column)
            for (int row = 0; row < wordsPerColumn; row++)
            {
                var rowText = new StringBuilder();

                for (int col = 0; col < totalColumns; col++)
                {
                    int wordIndex = col * wordsPerColumn + row;

                    if (wordIndex < words.Count)
                    {
                        string word = words[wordIndex];

                        // Check if word is banned
                        bool isWordBanned = !string.IsNullOrEmpty(word) &&
                                          bannedWordsService != null &&
                                          bannedWordsService.IsWordBanned(word, _currentLanguageCode);

                        // Check if word is used in other levels
                        var usedInLevels = LevelEditorServices.GetUsedInLevels(word, _currentLanguageCode, _currentLevel);
                        bool isWordUsedInOtherLevels = !string.IsNullOrEmpty(word) && usedInLevels.Length > 0;

                        // Add warning markers
                        string displayWord = word;
                        if (isWordBanned && isWordUsedInOtherLevels)
                        {
                            displayWord = "⚠⚠ " + word; // Double warning for both banned and used
                        }
                        else if (isWordBanned)
                        {
                            displayWord = "⚠ " + word; // Warning for banned
                        }
                        else if (isWordUsedInOtherLevels)
                        {
                            displayWord = "⚠ " + word; // Warning for used in other levels
                        }

                        rowText.Append(displayWord.PadRight(columnWidth));
                    }
                    else
                    {
                        // Pad with spaces if no word at this position
                        rowText.Append("".PadRight(columnWidth));
                    }
                }

                // Only add the row if it has at least one word
                string rowString = rowText.ToString().TrimEnd();
                if (!string.IsNullOrEmpty(rowString))
                {
                    result.AppendLine(rowString);
                }
            }

            return result.ToString();
        }

        private void FillSearchTilesFromCurrentLetters()
        {
            if (_searchTiles == null || _currentLevel == null) return;

            var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
            if (languageData == null || string.IsNullOrEmpty(languageData.letters))
            {
                UpdateSearchResults("No letters available in current level. Please set letters in the Level Editor first.");
                return;
            }

            // Clear existing tiles first
            ClearSearchTiles();

            // Fill tiles with available letters (up to the number of tiles we have)
            string letters = languageData.letters.ToUpper();
            int maxTiles = Math.Min(letters.Length, _searchTiles.Count);

            for (int i = 0; i < maxTiles; i++)
            {
                var tile = _searchTiles[i];
                var letterLabel = new Label(letters[i].ToString());
                letterLabel.AddToClassList("grid-cell-letter");
                tile.Add(letterLabel);
            }

            // Show info in text area
            UpdateSearchResults($"Filled search tiles with letters from current level: {letters}\n\nClick 'Search Words' to find possible words, or modify the tiles by clicking on them.");
        }

        private void UpdateStatusBar()
        {
            if (_statusLabel == null) return;

            if (_previewData != null && _previewData.isValid)
            {
                _statusLabel.text = $"Grid: {_previewData.columns}x{_previewData.rows} | Words: {(_previewData.placements?.Count ?? 0)} | Icons: {(_previewData.iconPositions?.Count ?? 0)}";
            }
            else
            {
                _statusLabel.text = "No valid data";
            }
        }

        private void UpdateInstructions()
        {
            if (_instructionsLabel == null) return;

            if (_enableEditing && _previewData != null && _previewData.isValid)
            {
                string gridInstruction = _isSpecialItemSelected
                    ? "Click on a letter cell to place/remove special item"
                    : "Left-click: Place letter | Right-click: Remove letter";
                _instructionsLabel.text = $"{gridInstruction}";
            }
            else
            {
                _instructionsLabel.text = "";
            }
        }

        private void MoveGrid(Vector2Int direction)
        {
            if (_previewData == null || _previewData.grid == null) return;

            // Record undo before moving
            if (_currentLevel != null)
            {
                // Ensure grid data is serialized before recording undo
                var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
                if (languageData?.crosswordData != null)
                {
                    languageData.crosswordData.SerializeGrid();
                }
                
                Undo.RecordObject(_currentLevel, "Move Grid");
            }

            // Check if move would push any non-empty cell out of bounds
            for (int y = 0; y < _previewData.rows; y++)
            {
                for (int x = 0; x < _previewData.columns; x++)
                {
                    if (_previewData.grid[x, y] != 0)  // If cell has content
                    {
                        int newX = x + direction.x;
                        int newY = y + direction.y;

                        // If move would push this non-empty cell out of bounds, cancel the move
                        if (newX < 0 || newX >= _previewData.columns || newY < 0 || newY >= _previewData.rows)
                        {
                            Debug.LogWarning("Cannot move grid in that direction - would push content out of bounds");
                            return; // Cancel the move
                        }
                    }
                }
            }

            char[,] newGrid = new char[_previewData.columns, _previewData.rows];
            Dictionary<Vector2Int, string> newIconPositions = new Dictionary<Vector2Int, string>();

            // Move each cell
            for (int y = 0; y < _previewData.rows; y++)
            {
                for (int x = 0; x < _previewData.columns; x++)
                {
                    // Calculate new position
                    int newX = x + direction.x;
                    int newY = y + direction.y;

                    // Check if new position is within bounds
                    if (newX >= 0 && newX < _previewData.columns && newY >= 0 && newY < _previewData.rows)
                    {
                        newGrid[newX, newY] = _previewData.grid[x, y];

                        // Move icons if present at this position
                        Vector2Int oldPos = new Vector2Int(x, y);
                        if (_previewData.iconPositions.ContainsKey(oldPos))
                        {
                            newIconPositions[new Vector2Int(newX, newY)] = _previewData.iconPositions[oldPos];
                        }
                    }
                }
            }

            // Update the grid and icon positions
            _previewData.grid = newGrid;
            _previewData.iconPositions = newIconPositions;

            // Update word placements positions
            foreach (var placement in _previewData.placements)
            {
                placement.startPosition += direction;
            }

            // Save the changes
            CrosswordPreviewHandler.SavePreviewToLevel(_currentLevel, _currentLanguageCode, _previewData);
            CrosswordPreviewHandler.TriggerManualChangeEvent();

            EditorUtility.SetDirty(_currentLevel);
            UpdateGridDisplay();
            UpdateStatusBar();
        }

        private void HandleCellInteraction(Rect cellRect, int x, int y, bool hasLetter, bool hasIcon)
        {
            if (!_enableEditing) return; // Only handle interactions if editing is enabled

            if (UnityEngine.Event.current.type == EventType.MouseDown && cellRect.Contains(UnityEngine.Event.current.mousePosition))
            {
                bool changed = false;
                Vector2Int pos = new Vector2Int(x, y);

                if (UnityEngine.Event.current.button == 1) // Right click - handle removal
                {
                    if (hasIcon)
                    {
                        // Remove special item first
                        _previewData.iconPositions.Remove(pos);
                        changed = true;
                    }
                    else if (hasLetter)
                    {
                        // Remove letter
                        _previewData.grid[x, y] = (char)0;
                        changed = true;
                    }
                }
                else if (UnityEngine.Event.current.button == 0) // Left click - handle placement
                {
                    if (_isSpecialItemSelected && hasLetter)
                    {
                        // Handle special item placement/toggle
                        if (_previewData.iconPositions.ContainsKey(pos))
                        {
                            _previewData.iconPositions.Remove(pos);
                        }
                        else
                        {
                            _previewData.iconPositions[pos] = _previewData.iconPath ?? IconPath;
                        }
                        changed = true;
                    }
                    else if (!_isSpecialItemSelected)
                    {
                        // Place letter regardless of icon presence
                        _previewData.grid[x, y] = _currentSelectedLetter;
                        changed = true;
                    }
                }

                if (changed)
                {
                    // Save changes and trigger events
                    CrosswordPreviewHandler.SavePreviewToLevel(_currentLevel, _currentLanguageCode, _previewData);
                    CrosswordPreviewHandler.TriggerManualChangeEvent();

                    EditorUtility.SetDirty(_currentLevel);
                    UnityEngine.Event.current.Use(); // Consume the event
                    Repaint();
                }
            }
        }

        private void ClearGrid()
        {
            if (_currentLevel == null || _previewData == null) return;

            // Clear the grid
            for (int y = 0; y < _previewData.rows; y++)
            {
                for (int x = 0; x < _previewData.columns; x++)
                {
                    _previewData.grid[x, y] = '\0';
                }
            }

            // Clear placements and icon positions
            _previewData.placements.Clear();
            _previewData.iconPositions.Clear();

            // Save the cleared state
            CrosswordPreviewHandler.SavePreviewToLevel(_currentLevel, _currentLanguageCode, _previewData);

            // Trigger manual change event (this was missing!)
            CrosswordPreviewHandler.TriggerManualChangeEvent();

            EditorUtility.SetDirty(_currentLevel);
            UpdateGridDisplay();
            UpdateStatusBar();
        }

        private void ClearGridWithConfirmation()
        {
            if (_currentLevel == null || _previewData == null) return;

            if (EditorUtility.DisplayDialog(
                "Clear Crossword",
                "Are you sure you want to clear the entire crossword?",
                "Clear",
                "Cancel"))
            {
                // Record undo before clearing
                if (_currentLevel != null)
                {
                    // Ensure grid data is serialized before recording undo
                    var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
                    if (languageData?.crosswordData != null)
                    {
                        languageData.crosswordData.SerializeGrid();
                    }
                    
                    Undo.RecordObject(_currentLevel, "Clear Crossword Grid");
                }
                
                ClearGrid();
            }
        }

        private void RefreshCrosswordGrid()
        {
            if (_currentLevel == null || _previewData == null) return;

            // Record undo before refreshing
            if (_currentLevel != null)
            {
                // Ensure grid data is serialized before recording undo
                var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
                if (languageData?.crosswordData != null)
                {
                    languageData.crosswordData.SerializeGrid();
                }
                
                Undo.RecordObject(_currentLevel, "Refresh Crossword Grid");
            }

            CrosswordPreviewHandler.RefreshCrossword(_previewData, _currentLanguageCode, _currentLevel);

            UpdateGridDisplay();
            UpdateStatusBar();
            EditorUtility.SetDirty(_currentLevel);
        }

        private void ApplyCrosswordChanges()
        {
            if (_currentLevel == null || _previewData == null || !_previewData.isValid) return;

            var languageData = _currentLevel.GetLanguageData(_currentLanguageCode);
            if (string.IsNullOrEmpty(languageData?.letters))
            {
                // Show dialog if no letters are defined
                EditorUtility.DisplayDialog(
                    "No Letters Defined",
                    "Please define letters in the Letters field above before applying changes.",
                    "OK");
                return;
            }

            // Record undo before applying changes
            if (_currentLevel != null)
            {
                // Ensure grid data is serialized before recording undo
                if (languageData?.crosswordData != null)
                {
                    languageData.crosswordData.SerializeGrid();
                }
                
                Undo.RecordObject(_currentLevel, "Apply Crossword Changes");
            }

            // Update word placements from the grid (with regeneration enabled)
            bool wasRegenerated = CrosswordPreviewHandler.UpdateWordPlacementsFromGrid(_previewData, true, _currentLevel, _currentLanguageCode);

            // Update the language data's word list from the crossword placements
            if (_previewData.placements != null && _previewData.placements.Count > 0)
            {
                LevelEditorServices.UpdateWordsFromCrossword(_currentLevel, _currentLanguageCode, _previewData.placements);
            }

            // Save changes to the level
            CrosswordPreviewHandler.SavePreviewToLevel(_currentLevel, _currentLanguageCode, _previewData);

            if (wasRegenerated)
            {
                Debug.Log("[Apply] Crossword was automatically regenerated due to missing words.");
                // Show a dialog to inform the user
                EditorUtility.DisplayDialog(
                    "Crossword Regenerated",
                    "The crossword was automatically regenerated because some words were missing from the grid. The new layout should resolve any overlapping word issues.",
                    "OK");
            }

            EditorUtility.SetDirty(_currentLevel);

            // Notify LevelDataEditor that the level has been updated from external source
            LevelDataEditor.NotifyWordsListNeedsUpdate(_currentLevel, _currentLanguageCode);

            UpdateGridDisplay();
            UpdateStatusBar();
            
            // Update search results if there are letters in search tiles
            // This ensures that used words are marked as used (bolded) after applying changes
            string letters = GetLettersFromSearchTiles();
            if (!string.IsNullOrEmpty(letters))
            {
                SearchWordsFromTiles();
            }
        }

        private void ShowSettingsMenu()
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Auto Refresh"), _autoRefresh, () => {
                _autoRefresh = !_autoRefresh;
                if (_autoRefresh) _lastRefreshTime = Time.realtimeSinceStartup;
                if (_autoRefreshToggle != null) _autoRefreshToggle.value = _autoRefresh;
            });

            menu.AddItem(new GUIContent("Show Grid Numbers"), _showGridNumbers, () => {
                _showGridNumbers = !_showGridNumbers;
                if (_showNumbersToggle != null) _showNumbersToggle.value = _showGridNumbers;
                UpdateGridDisplay();
            });

            menu.AddItem(new GUIContent("Enable Editing"), _enableEditing, () => {
                _enableEditing = !_enableEditing;
                if (_enableEditingToggle != null) _enableEditingToggle.value = _enableEditing;
                UpdateEditingUI();
                UpdateInstructions();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Undo (Ctrl+Z)"), false, () => {
                Undo.PerformUndo();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Clear Search Tiles"), false, () => {
                ClearSearchTiles();
            });

            menu.AddItem(new GUIContent("Clear Text Area"), false, () => {
                ClearSearchResults();
            });

            menu.AddItem(new GUIContent("Search Words from Tiles"), false, () => {
                SearchWordsFromTiles();
            });

            menu.AddItem(new GUIContent("Fill Tiles from Current Letters"), false, () => {
                FillSearchTilesFromCurrentLetters();
            });

            menu.AddItem(new GUIContent("Reset Window"), false, () => {
                _autoRefresh = true;
                _showGridNumbers = false;
                _enableEditing = true;

                // Update toggles
                if (_autoRefreshToggle != null) _autoRefreshToggle.value = _autoRefresh;
                if (_showNumbersToggle != null) _showNumbersToggle.value = _showGridNumbers;
                if (_enableEditingToggle != null) _enableEditingToggle.value = _enableEditing;

                UpdateEditingUI();
                UpdateGridDisplay();
                UpdateInstructions();
            });
            
            menu.ShowAsContext();
        }

        private void UpdateBackground()
        {
            if (_currentLevel != null && rootVisualElement != null)
            {
                var serializedObject = new SerializedObject(_currentLevel);
                var backgroundProp = serializedObject.FindProperty("background");
                
                // Create background field in the toolbar if it doesn't exist
                var toolbar = rootVisualElement.Q<Toolbar>();
                if (toolbar == null) return; // Exit early if toolbar not ready
                
                var imguiContainer = rootVisualElement.parent?.Q<IMGUIContainer>();
                if (imguiContainer != null)
                {
                    // imguiContainer.pickingMode = PickingMode.Ignore;
                }
                if (toolbar != null)
                {
                    var existingField = toolbar.Q<PropertyField>("backgroundField");
                    if (existingField == null)
                    {
                        var backgroundField = new PropertyField(backgroundProp) { name = "backgroundField" };
                        backgroundField.style.width = 200;
                        backgroundField.style.marginRight = 10;
                        backgroundField.Bind(serializedObject);
                        toolbar.Insert(0, backgroundField);
                    }
                }
            }
        }
        
        private void TestCurrentLevel()
        {
            LevelEditorServices.TestLevel(_currentLevel, _currentLanguageCode);
        }
        
    }
}
