using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using WordsToolkit.Scripts.Services.BannedWords;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.Utilities;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.Levels.Editor
{
    [CustomEditor(typeof(Level))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Updates available words for all languages in the given level using the current model controller.
        /// </summary>
        public static void UpdateAvailableWordsForLevel(Level level)
        {
            if (level == null || level.languages == null) return;
            var modelController = EditorScope.Resolve<IModelController>();
            if (modelController == null)
            {
                Debug.LogError("Model controller not available for updating available words.");
                return;
            }
            modelController.LoadModels();
            foreach (var languageData in level.languages)
            {
                if (languageData != null && !string.IsNullOrEmpty(languageData.language) && !string.IsNullOrEmpty(languageData.letters))
                {
                    var editors = UnityEngine.Resources.FindObjectsOfTypeAll<LevelDataEditor>();
                    foreach (var editor in editors)
                    {
                        if (editor.level == level)
                        {
                            editor.UpdateAvailableWordsForLanguage(languageData.language, languageData.letters);
                        }
                    }
                }
            }
            // Optionally, mark the level as dirty so Unity saves the changes
            EditorUtility.SetDirty(level);
            // Refresh the inspector UI for this level
            EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
        }
        // Event that will be called to notify level updates
        public static event Action<Level> OnLevelNeedsUpdate;

        // Static method to trigger the update
        public static void NotifyLevelNeedsUpdate(Level level)
        {
            if (level != null)
            {
                OnLevelNeedsUpdate?.Invoke(level);
            }
        }

        // Event that will be called to notify words list updates only
        public static event Action<Level, string> OnWordsListNeedsUpdate;

        // Static method to trigger words list update only
        public static void NotifyWordsListNeedsUpdate(Level level, string languageCode)
        {
            if (level != null && !string.IsNullOrEmpty(languageCode))
            {
                OnWordsListNeedsUpdate?.Invoke(level, languageCode);
            }
        }

        // Event that will be called to notify language tab changes
        public static event Action<Level, string> OnLanguageTabChanged;

        // Static method to trigger language tab change notification
        public static void NotifyLanguageTabChanged(Level level, string languageCode)
        {
            if (level != null && !string.IsNullOrEmpty(languageCode))
            {
                OnLanguageTabChanged?.Invoke(level, languageCode);
            }
        }

        private SerializedProperty languagesProp;
        private SerializedProperty numberProp;
        private SerializedProperty lettersAmountProp;
        private SerializedProperty wordsAmountProp;
        private SerializedProperty colorsTileProp;
        private SerializedProperty minLettersInWordProp;
        private SerializedProperty maxLettersInWordProp;
        private SerializedProperty wordLengthProbabilityProp;
        private IModelController modelController;
        
        // Preview-related fields
        private Dictionary<string, bool> showPreviewDict = new Dictionary<string, bool>();
        private Dictionary<string, CrosswordPreviewHandler.PreviewData> previewDataDict = new Dictionary<string, CrosswordPreviewHandler.PreviewData>();
        
        // Add dictionary to track word list expansion state
        private Dictionary<string, bool> showWordsDict = new Dictionary<string, bool>();

        // Dictionary to track available words expansion state
        private Dictionary<string, bool> showAvailableWordsDict = new Dictionary<string, bool>();

        // Add dictionaries for preview grid dimensions per language
        private Dictionary<string, int> previewColumnDict = new Dictionary<string, int>();
        private Dictionary<string, int> previewRowDict = new Dictionary<string, int>();

        // Default grid dimensions
        private const int DefaultColumns = 10;
        private const int DefaultRows = 7;

        // Selected word for placement
        private string selectedWordToPlace = null;

        // Access to the selected language for filtering
        private string SelectedLanguage
        {
            get
            {
                // Read the preference that's set in LevelManagerWindow
                return EditorPrefs.GetString("WordsToolkit_SelectedLanguage", "All Languages");
            }
        }

        private IModelController Controller
        {
            get
            {
                if (modelController == null)
                {
                    modelController = EditorScope.Resolve<IModelController>();
                }
                return modelController;
            }
        }

        // Path to the icon we want to add
        private const string IconPath = "Assets/WordConnectGameToolkit/Sprites/game_ui/in-game-item-1.png";
        private Texture2D iconTexture;
        
        private bool showGeneratorParams = false;  // Add field for foldout state
        private bool showLanguages = true;
        private LanguageConfiguration languageConfig;
        private const string MIN_LETTERS_PREF = "WordsToolkit_MinLettersInWord";
        private const string MAX_LETTERS_PREF = "WordsToolkit_MaxLettersInWord";
        private const string LENGTH_PROB_PREF = "WordsToolkit_WordLengthProbability";
        
        // Editor-only settings
        private int minLettersInWord = 3;
        private int maxLettersInWord = 6;
        private int wordLengthProbability = 50;
        private Level level;
        private bool showRandomizeTool = true;
        private Texture2D warningIcon;
        
        // Dictionary to store references to words list UI elements for each language
        private Dictionary<string, VisualElement> wordsListElements = new Dictionary<string, VisualElement>();
        
        // Flag to control crossword update when words are changed (automatically disabled when crossword is manually edited)
        private bool updateCrosswordOnChangeWords = true;
        private List<string> allWords;

        private void OnEnable()
        {
            level = target as Level;

            // Subscribe to level update events
            // OnLevelNeedsUpdate += HandleLevelUpdate;

            // Subscribe to words list update events
            OnWordsListNeedsUpdate += HandleWordsListUpdate;

            // Get serialized properties
            numberProp = serializedObject.FindProperty("number");
            languagesProp = serializedObject.FindProperty("languages");
            lettersAmountProp = serializedObject.FindProperty("letters");
            wordsAmountProp = serializedObject.FindProperty("words");
            colorsTileProp = serializedObject.FindProperty("colorsTile");
            minLettersInWordProp = serializedObject.FindProperty("min");
            maxLettersInWordProp = serializedObject.FindProperty("max");
            wordLengthProbabilityProp = serializedObject.FindProperty("difficulty");

            // Load editor preferences
            minLettersInWord = EditorPrefs.GetInt(MIN_LETTERS_PREF, 3);
            maxLettersInWord = EditorPrefs.GetInt(MAX_LETTERS_PREF, 6);
            wordLengthProbability = EditorPrefs.GetInt(LENGTH_PROB_PREF, 50);

            // Subscribe to color tile selection events
            ColorsTileDrawer.OnColorTileSelected += OnColorTileSelected;

            // Subscribe to crossword manual change events
            CrosswordPreviewHandler.OnCrosswordManuallyChanged += OnCrosswordManuallyChanged;

            warningIcon = EditorGUIUtility.IconContent("Warning").image as Texture2D;

            // Load the icon texture
            iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (iconTexture == null)
            {
                Debug.LogWarning($"Icon not found at path: {IconPath}");
            }

            // Update languages when editor is enabled
            UpdateLanguages();
            
            // Remove any banned words from the level's word lists
            RemoveBannedWordsFromLevel();
        }

        private void RemoveBannedWordsFromLevel()
        {
            if (level == null || level.languages == null) return;

            var bannedWordsService = EditorScope.Resolve<IBannedWordsService>();
            if (bannedWordsService == null) return;

            bool levelModified = false;

            foreach (var languageData in level.languages)
            {
                if (languageData == null || languageData.words == null) continue;

                var filteredWords = new List<string>();
                
                foreach (var word in languageData.words)
                {
                    if (string.IsNullOrEmpty(word)) continue;
                    
                    // Only keep words that are not banned
                    if (!bannedWordsService.IsWordBanned(word, languageData.language))
                    {
                        filteredWords.Add(word);
                    }
                    else
                    {
                        Debug.Log($"Removed banned word '{word}' from {languageData.language} word list");
                        levelModified = true;
                    }
                }

                // Update the words array if any changes were made
                if (filteredWords.Count != languageData.words.Length)
                {
                    languageData.words = filteredWords.ToArray();
                    levelModified = true;
                }
            }

            if (levelModified)
            {
                EditorUtility.SetDirty(level);
                serializedObject.Update();
                Debug.Log("Removed banned words from level word lists");
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events when the editor is disabled
            // OnLevelNeedsUpdate -= HandleLevelUpdate;
            OnWordsListNeedsUpdate -= HandleWordsListUpdate;
            ColorsTileDrawer.OnColorTileSelected -= OnColorTileSelected;
            CrosswordPreviewHandler.OnCrosswordManuallyChanged -= OnCrosswordManuallyChanged;
            
            // Clear UI element references to prevent memory leaks
            wordsListElements.Clear();
            
            // Clear reorderable lists to prevent memory leaks and disposed property access
            ClearReorderableLists();
        }

        private void ClearReorderableLists()
        {
            // No longer needed in UIToolkit version
        }

        private void OnColorTileSelected(ColorsTile selectedTile)
        {
            // Update the level's colorsTile field with the selected tile
            if (colorsTileProp != null && serializedObject.targetObject != null)
            {
                colorsTileProp.objectReferenceValue = selectedTile;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }

        public void HandleLevelUpdate(Level updatedLevel)
        {
            // Only update if this editor is editing the updated level
            if (level == updatedLevel)
            {
                EditorUtility.SetDirty(target);
                serializedObject.Update();
                
                // Clear cached data
                previewDataDict.Clear();
                showWordsDict.Clear();
                showAvailableWordsDict.Clear();
                
                // Update available words for all languages
                UpdateAvailableWordsForAllLanguages();
                
                AssetDatabase.Refresh();
                
                // Refresh the inspector in LevelManagerWindow if it's displaying this level
                EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
                
                Repaint();
            }
        }

        public void HandleWordsListUpdate(Level updatedLevel, string languageCode)
        {
            // Only update if this editor is editing the updated level
            if (level == updatedLevel)
            {
                // Mark as dirty and update serialized object first
                EditorUtility.SetDirty(target);
                serializedObject.Update();
                
                // Update only the words list for the specific language
                UpdateCurrentLanguageWordsList(languageCode);
                
                
                // Force a full UI refresh to ensure words appear instantly
                EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
                
                // Repaint to update the UI
                Repaint();
            }
        }

        private void UpdateAvailableWordsForAllLanguages()
        {
            if (level == null || level.languages == null) return;
            
            // Update available words for each language
            foreach (var languageData in level.languages)
            {
                if (languageData != null && !string.IsNullOrEmpty(languageData.language) && !string.IsNullOrEmpty(languageData.letters))
                {
                    UpdateAvailableWordsForLanguage(languageData.language, languageData.letters);
                }
            }
        }

        public void UpdateAvailableWordsForLanguage(string langCode, string letters)
        {
            if (string.IsNullOrEmpty(langCode) || string.IsNullOrEmpty(letters)) return;
            
            try
            {
                // Get words that can be created from the letters
                var availableWords = Controller.GetWordsFromSymbols(letters, langCode).Distinct().OrderBy(x => x).ToList();
                
                // Update the allWords list for this language
                allWords = availableWords;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating available words for language {langCode}: {ex.Message}");
            }
        }

        private void OnCrosswordManuallyChanged()
        {
            // Disable automatic crossword updates when user manually edits the crossword
            updateCrosswordOnChangeWords = false;
        }

        private void OnCrosswordManuallyChanged(Level changedLevel, string languageCode)
        {
            // Only respond if this editor is editing the changed level
            if (level == changedLevel)
            {
                // Set flag to false when crossword is manually changed
                updateCrosswordOnChangeWords = false;
                Debug.Log($"Crossword manually changed for {languageCode}. Auto-update on word removal disabled.");
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.name = "level-data-editor-root";
            root.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            root.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            root.style.flexGrow = 1;
            root.style.flexShrink = 1;
            if (languageConfig == null)
                languageConfig = EditorScope.Resolve<LanguageConfiguration>();
            // Update languages to ensure synchronization
            UpdateLanguages();

            // Bind the root to the serialized object
            root.Bind(serializedObject);

            // Schedule a callback to check property binding after the next frame
            root.schedule.Execute(() =>
            {
                var basicContainer = root.Q("basic-container");
                if (basicContainer != null)
                {
                    Debug.Log($"Basic container found with {basicContainer.childCount} children");
                    var colorsTileField = basicContainer.Q<PropertyField>("colorstile-field");
                    if (colorsTileField != null)
                    {
                        Debug.Log($"ColorsTile field found, height: {colorsTileField.resolvedStyle.height}");
                    }
                }
            }).ExecuteLater(100); // Execute after 100ms to allow UI to layout

            // Add some space
            root.Add(new VisualElement { style = { height = 10 } });

            // Language data section
            var languageContainer = new VisualElement();
            languageContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            languageContainer.style.borderTopWidth = 1;
            languageContainer.style.borderBottomWidth = 1;
            languageContainer.style.borderLeftWidth = 1;
            languageContainer.style.borderRightWidth = 1;
            languageContainer.style.borderTopColor = Color.gray;
            languageContainer.style.borderBottomColor = Color.gray;
            languageContainer.style.borderLeftColor = Color.gray;
            languageContainer.style.borderRightColor = Color.gray;
            languageContainer.style.paddingTop = 10;
            languageContainer.style.paddingBottom = 10;
            languageContainer.style.paddingLeft = 10;
            languageContainer.style.paddingRight = 10;
            languageContainer.style.marginTop = 5;
            languageContainer.style.marginBottom = 5;
            languageContainer.style.flexGrow = 1;
            languageContainer.style.flexShrink = 1;

            // Get the selected language filter
            string selectedLang = SelectedLanguage;
            bool isFiltered = selectedLang != "All Languages";

            // Create a container that will hold the language content and the bottom section
            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Column;
            contentContainer.style.flexGrow = 1;
            contentContainer.style.flexShrink = 1;
            contentContainer.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            // Create language section container (no global scroll)
            var languageSectionContainer = new VisualElement();
            languageSectionContainer.style.flexGrow = 1;
            languageSectionContainer.style.flexShrink = 1;
            languageSectionContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            languageSectionContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            // Create language tabs and content
            CreateLanguageSection(languageSectionContainer);

            contentContainer.Add(languageSectionContainer);

            // Generator parameters section - this will stick to the bottom
            var generatorContainer = new VisualElement();
            generatorContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            generatorContainer.style.borderTopWidth = 1;
            generatorContainer.style.borderTopColor = Color.gray;
            generatorContainer.style.paddingTop = 5;
            generatorContainer.style.paddingBottom = 5;
            generatorContainer.style.paddingLeft = 10;
            generatorContainer.style.paddingRight = 10;
            generatorContainer.style.flexShrink = 0; // Don't shrink
            if(GetEnabledLanguageTabsCount() > 1)
                CreateGeneratorSection(generatorContainer);

            contentContainer.Add(generatorContainer);
            languageContainer.Add(contentContainer);
            root.Add(languageContainer);

            return root;
        }

        private void CreateLanguageSection(VisualElement parent)
        {
            if (languagesProp.arraySize == 0)
            {
                var helpBox = new HelpBox("No language data defined. Add at least one language.", HelpBoxMessageType.Info);
                parent.Add(helpBox);

                // Show Add button when there are no languages
                var addButton = new Button(() => AddLanguageMenu((Level)target))
                {
                    text = "Add Language",
                    tooltip = "Add a new language"
                };
                addButton.style.height = 30;
                parent.Add(addButton);
            }
            else
            {
                // Get the selected tab index
                if (!EditorPrefs.HasKey("WordsToolkit_SelectedLanguageTab"))
                {
                    EditorPrefs.SetInt("WordsToolkit_SelectedLanguageTab", 0);
                }
                int selectedTabIndex = EditorPrefs.GetInt("WordsToolkit_SelectedLanguageTab", 0);

                // Ensure selected tab is in range
                if (selectedTabIndex >= languagesProp.arraySize)
                {
                    selectedTabIndex = Mathf.Max(0, languagesProp.arraySize - 1);
                    EditorPrefs.SetInt("WordsToolkit_SelectedLanguageTab", selectedTabIndex);
                }

                // Create tabs container
                var tabContainer = new VisualElement();
                tabContainer.style.flexDirection = FlexDirection.Row;
                tabContainer.style.width = 200; // Made narrower from 300
                parent.Add(tabContainer);

                // Create content container that will be updated when tabs are clicked
                var contentContainer = new VisualElement();
                contentContainer.style.flexGrow = 1;
                parent.Add(contentContainer);

                // Create tab buttons - only for enabled languages
                var tabButtons = new List<Button>();
                var enabledLanguages = languageConfig?.GetEnabledLanguages() ?? new List<LanguageConfiguration.LanguageInfo>();
                var enabledLanguageCodes = enabledLanguages.Select(lang => lang.code).ToHashSet();
                
                for (int i = 0; i < languagesProp.arraySize; i++)
                {
                    SerializedProperty langProp = languagesProp.GetArrayElementAtIndex(i);
                    string langCode = langProp.FindPropertyRelative("language").stringValue;

                    // Skip this language if it's not enabled
                    if (!enabledLanguageCodes.Contains(langCode))
                        continue;

                    var langInfo = languageConfig?.GetLanguageInfo(langCode);
                    string displayName = langCode;
                    if (langInfo != null)
                    {
                        displayName = langCode;
                    }

                    int tabIndex = i; // Capture for closure
                    bool isSelected = selectedTabIndex == i;
                    
                    var tabButton = new Button(() =>
                    {
                        EditorPrefs.SetInt("WordsToolkit_SelectedLanguageTab", tabIndex);
                        
                        // Update all tab button styles
                        for (int j = 0; j < tabButtons.Count; j++)
                        {
                            if (j == tabIndex)
                            {
                                tabButtons[j].style.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
                            }
                            else
                            {
                                tabButtons[j].style.backgroundColor = StyleKeyword.Null;
                            }
                        }
                        
                        // Clear content container and rebuild with selected language
                        contentContainer.Clear();
                        var selectedLangProp = languagesProp.GetArrayElementAtIndex(tabIndex);
                        CreateLanguageItemUI(selectedLangProp, tabIndex, false, contentContainer);
                        
                        // Notify CrosswordGridWindow about the language change
                        string selectedLanguageCode = selectedLangProp.FindPropertyRelative("language").stringValue;
                        NotifyLanguageTabChanged(level, selectedLanguageCode);
                    })
                    {
                        text = displayName
                    };
                    
                    tabButton.style.flexGrow = 1;
                    tabButton.style.minWidth = 40; // Made narrower from 60
                    tabButton.style.maxWidth = 50; // Added max width to keep tabs compact
                    
                    if (isSelected)
                    {
                        tabButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
                    }
                    
                    tabContainer.Add(tabButton);
                    tabButtons.Add(tabButton);
                }

                // Add "+" button for opening language configuration
                var addLanguageButton = new Button(() => 
                {
                    // Open the Language Configuration asset
                    if (languageConfig != null)
                    {
                        Selection.activeObject = languageConfig;
                          var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
                    if (inspectorType != null)
                    {
                        var inspectorWindow = EditorWindow.GetWindow(inspectorType);
                        if (inspectorWindow != null)
                        {
                            inspectorWindow.Focus();
                        }
                    }
                        EditorGUIUtility.PingObject(languageConfig);
                    }
                    else
                    {
                        Debug.LogWarning("Language Configuration not found!");
                    }
                })
                {
                    text = "+",
                    tooltip = "Open Language Configuration"
                };
                addLanguageButton.style.width = 30;
                addLanguageButton.style.height = 20;
                addLanguageButton.style.marginLeft = 5;
                addLanguageButton.style.fontSize = 14;
                addLanguageButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                tabContainer.Add(addLanguageButton);

                // Display the initially selected language tab - only if it's enabled
                if (languagesProp.arraySize > 0 && selectedTabIndex < languagesProp.arraySize)
                {
                    var selectedLangProp = languagesProp.GetArrayElementAtIndex(selectedTabIndex);
                    string selectedLangCode = selectedLangProp.FindPropertyRelative("language").stringValue;
                    
                    // Check if the selected language is enabled
                    bool isSelectedLanguageEnabled = enabledLanguageCodes.Contains(selectedLangCode);
                    
                    if (isSelectedLanguageEnabled)
                    {
                        CreateLanguageItemUI(selectedLangProp, selectedTabIndex, false, contentContainer);
                    }
                    else
                    {
                        // Find the first enabled language and select it
                        for (int i = 0; i < languagesProp.arraySize; i++)
                        {
                            var langProp = languagesProp.GetArrayElementAtIndex(i);
                            string langCode = langProp.FindPropertyRelative("language").stringValue;
                            
                            if (enabledLanguageCodes.Contains(langCode))
                            {
                                EditorPrefs.SetInt("WordsToolkit_SelectedLanguageTab", i);
                                CreateLanguageItemUI(langProp, i, false, contentContainer);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void CreateGeneratorSection(VisualElement parent)
        {
            var foldout = new Foldout
            {
                text = "All languages generator parameters",
                value = showGeneratorParams
            };
            
            foldout.RegisterValueChangedCallback(evt => showGeneratorParams = evt.newValue);

            var container = new VisualElement();
            container.style.paddingLeft = 15;

            // Add button to generate words for all languages
            var generateAllButton = new Button(() =>
            {
                // Generate words for all languages
                for (int i = 0; i < languagesProp.arraySize; i++)
                {
                    var languageData = level.languages[i];
                    if (languageData != null)
                    {
                        GenerateWordsForLanguage(level, languageData, Controller);
                    }
                }

                // Update crossword for all languages
                UpdateCrossword(level);
            })
            {
                text = "Generate words for all languages",
                tooltip = "Generate words for all languages using current level parameters"
            };
            generateAllButton.style.height = 30;
            generateAllButton.style.flexShrink = 1;
            generateAllButton.style.flexGrow = 1;
            generateAllButton.style.marginRight = 30;
            container.Add(generateAllButton);

            // Add some space
            container.Add(new VisualElement { style = { height = 5 } });

            // Add the button for other languages
            var generateOtherButton = new Button(() =>
            {
                // Get the current selected language tab
                int selectedTabIndex = EditorPrefs.GetInt("WordsToolkit_SelectedLanguageTab", 0);

                // Generate words for all languages except the currently selected one
                for (int i = 0; i < languagesProp.arraySize; i++)
                {
                    if (i != selectedTabIndex)
                    {
                        var languageData = level.languages[i];
                        if (languageData != null)
                        {
                            GenerateWordsForLanguage(level, languageData, Controller);
                        }
                    }
                }

                // Update crossword only for non-current languages
                UpdateCrosswordForOtherLanguages(level, selectedTabIndex);
            })
            {
                text = "Generate words for other languages",
                tooltip = "Generate words for all non-primary languages using current level parameters"
            };
            generateOtherButton.style.height = 30;
            generateOtherButton.style.flexShrink = 0;
            generateOtherButton.style.flexGrow = 1;
            generateOtherButton.style.marginRight = 30;
            container.Add(generateOtherButton);

            // Add some space
            container.Add(new VisualElement { style = { height = 5 } });

            // Always add the container to the foldout, the foldout will handle showing/hiding
            foldout.Add(container);

            parent.Add(foldout);
        }

        private void CreateLanguageItemUI(SerializedProperty languageProperty, int index, bool isFiltered, VisualElement parent)
        {
            var languageContainer = new VisualElement();
            languageContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            languageContainer.style.paddingTop = 5;
            languageContainer.style.paddingBottom = 5;
            languageContainer.style.paddingLeft = 10;
            languageContainer.style.paddingRight = 10;
            languageContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            languageContainer.style.minWidth = 340;
            languageContainer.style.flexGrow = 1; // Allow to grow and fill available space
            languageContainer.style.flexShrink = 1; // Allow to shrink
            languageContainer.style.height = new StyleLength(new Length(100, LengthUnit.Percent)); // Take full height

            // Get properties
            SerializedProperty langCodeProp = languageProperty.FindPropertyRelative("language");
            SerializedProperty lettersProp = languageProperty.FindPropertyRelative("letters");
            SerializedProperty wordsAmountProp = languageProperty.FindPropertyRelative("wordsAmount");
            SerializedProperty wordsProp = languageProperty.FindPropertyRelative("words");

            string langCode = langCodeProp.stringValue;
            // Update expansion state in dictionary
            showWordsDict[langCode] = wordsProp.isExpanded;

            // Add some space
            languageContainer.Add(new VisualElement { style = { height = 5 } });

            // Create word list UI
            CreateWordListUI(wordsProp, langCode, languageContainer);

            // Header with language selection dropdown
            var languageHeader = new VisualElement();
            languageHeader.style.flexDirection = FlexDirection.Row;
            languageHeader.style.alignItems = Align.Center;



            // Language dropdown (if language config is available)
            if (languageConfig != null && languageConfig.languages.Count > 0)
            {
                // Get only enabled languages
                var enabledLanguages = languageConfig.GetEnabledLanguages();
                
                // Create arrays for dropdown
                var displayNames = new string[enabledLanguages.Count];
                var codes = new string[enabledLanguages.Count];

                // Find current index
                int selectedIndex = -1;
                for (int i = 0; i < enabledLanguages.Count; i++)
                {
                    var langInfo = enabledLanguages[i];
                    displayNames[i] = $"{langInfo.displayName} ({langInfo.code})";
                    codes[i] = langInfo.code;

                    if (langInfo.code == langCodeProp.stringValue)
                        selectedIndex = i;
                }

                // If not found, default to first
                if (selectedIndex < 0 && codes.Length > 0)
                    selectedIndex = 0;
            }

            languageContainer.Add(languageHeader);

            // separator line
            var separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = Color.gray;
            separator.style.marginTop = 2;
            separator.style.marginBottom = 2;
            languageContainer.Add(separator);
            // Create available words UI
            CreateAvailableWordsUI(langCode, level.GetLanguageData(langCode).letters, languageContainer, index);

            // Words array - track state and show collapsed by default
            if (!showWordsDict.ContainsKey(langCode))
            {
                showWordsDict[langCode] = false; // Default to collapsed
            }

            parent.Add(languageContainer);
        }

        private void CreateWordListUI(SerializedProperty wordsProp, string langCode, VisualElement parent)
        {
            var wordListContainer = new VisualElement();
            wordListContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.3f);
            wordListContainer.style.paddingTop = 5;
            wordListContainer.style.paddingBottom = 5;
            wordListContainer.style.paddingLeft = 5;
            wordListContainer.style.paddingRight = 5;
            wordListContainer.style.marginTop = 5;
            wordListContainer.style.marginBottom = 5;
            wordListContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            wordListContainer.style.minWidth = 320;
            wordListContainer.style.flexShrink = 0;

            var wordsContent = new VisualElement();
            wordsContent.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

            // Create words list using ListView
            var wordsListContainer = new VisualElement();
            wordsListContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            wordsListContainer.style.flexGrow = 1;

            // Store reference to the words list container for this language
            wordsListElements[langCode] = wordsListContainer;

            // Fill words list from the SerializedProperty using ListView
            FillWordsList(wordsListContainer, wordsProp, langCode);

            wordsContent.Add(wordsListContainer);

            // Add Clear Words button
            var clearWordsButton = new Button(() =>
            {
                if (EditorUtility.DisplayDialog("Clear Words",
                    "Are you sure you want to clear all words and crossword? This action cannot be undone.",
                    "Clear", "Cancel"))
                {
                    serializedObject.Update();
                    // Clear words
                    wordsProp.ClearArray();
                    serializedObject.ApplyModifiedProperties();

                    // Clear crossword preview data
                    if (previewDataDict.ContainsKey(langCode))
                    {
                        previewDataDict[langCode] = null;
                    }

                    // Clear the saved crossword data in the level
                    var languageData = level.GetLanguageData(langCode);
                    if (languageData != null)
                    {
                        //save grid
                        var saveCol = languageData.crosswordData.columns;
                        var saveRow = languageData.crosswordData.rows;
                        languageData.crosswordData = new SerializableCrosswordData();
                        languageData.crosswordData.columns = saveCol;
                        languageData.crosswordData.rows = saveRow;
                    }

                    EditorUtility.SetDirty(serializedObject.targetObject);
                    AssetDatabase.SaveAssets();

                    // Notify that the level needs update to refresh the crossword
                    NotifyLevelNeedsUpdate(level);

                    // Refresh the UI using LevelManagerWindow
                    EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
                }
            })
            {
                text = "Clear Words"
            };
            clearWordsButton.style.height = 30;
            clearWordsButton.style.marginTop = 10;
            clearWordsButton.style.flexShrink = 1;
            clearWordsButton.style.flexGrow = 1;
            clearWordsButton.style.alignSelf = Align.Stretch;

            wordsContent.Add(clearWordsButton);
            wordListContainer.Add(wordsContent);
            parent.Add(wordListContainer);
        }

        private void FillWordsList(VisualElement wordsListContainer, SerializedProperty wordsProp, string langCode)
        {
            // Clear existing content first
            wordsListContainer.Clear();

            // Create ListView for words
            var wordsListView = new ListView();
            wordsListView.name = $"words-listview-{langCode}";
            wordsListView.style.flexGrow = 1;
            wordsListView.style.height = StyleKeyword.Auto; // Auto height based on content

            // Schedule setting the title after the ListView is fully constructed
            wordsListView.schedule.Execute(() =>
            {
                // Target the specific size field we found in debugging
                var sizeField = wordsListView.Q<TextField>("unity-list-view__size-field");
                if (sizeField != null)
                {
                    var label = sizeField.Q<Label>();
                    label.text = "Words";

                    // Set the text input value directly on the TextField
                    sizeField.value = wordsProp.arraySize.ToString();
                }
            }).ExecuteLater(50);

            // Fix for unity-content-container height issue
            wordsListView.schedule.Execute(() =>
            {
                var scrollView = wordsListView.Q<ScrollView>();
                if (scrollView != null)
                {
                    var contentContainer = scrollView.Q(className: "unity-content-container");
                    if (contentContainer != null)
                    {
                        contentContainer.style.minHeight = StyleKeyword.Auto;
                        contentContainer.style.height = StyleKeyword.Auto;
                        contentContainer.style.flexGrow = 1;
                    }
                }
            }).ExecuteLater(1); // Execute after UI is built

            // Bind the ListView to the words property
            wordsListView.BindProperty(wordsProp);

            // Setup ListView properties
            wordsListView.showBorder = true;
            wordsListView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            wordsListView.reorderable = true;
            wordsListView.showAddRemoveFooter = false;
            wordsListView.reorderMode = ListViewReorderMode.Animated;
            wordsListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            wordsListView.fixedItemHeight = 25; // Set a fixed height per item to help with layout

            // Set up the make item callback
            wordsListView.makeItem = () => CreateWordListItem(langCode);

            // Set up the bind item callback
            wordsListView.bindItem = (element, index) => BindWordListItem(element, index, wordsProp, langCode);

            // Set up the unbind item callback to clean up event handlers
            wordsListView.unbindItem = (element, index) => UnbindWordListItem(element, index);

            // Add callback for when items are added/removed
            wordsListView.itemsAdded += (items) =>
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                // Refresh all word icons to update duplicate detection
                RefreshAllWordIconsForLanguage(langCode);
                // Refresh available words UI when words are added
                EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
            };

            wordsListView.itemsRemoved += (items) =>
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                // Refresh all word icons to update duplicate detection
                RefreshAllWordIconsForLanguage(langCode);
                // Update crossword when words are removed (if auto-update is enabled)
                if (updateCrosswordOnChangeWords)
                {
                    var languageData = level.GetLanguageData(langCode);
                    if (languageData != null)
                    {
                        NotifyLevelNeedsUpdate(level);
                    }
                }
                // Refresh available words UI when words are removed
                EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
            };

            wordsListContainer.Add(wordsListView);
        }
        
        private VisualElement CreateWordListItem(string langCode)
        {
            var wordContainer = new VisualElement();
            wordContainer.style.flexDirection = FlexDirection.Row;
            wordContainer.style.alignItems = Align.Center;
            wordContainer.style.paddingTop = 2;
            wordContainer.style.paddingBottom = 2;
            wordContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            // Background color will be set in BindWordListItem based on index
            
            // Warning icon container
            var iconContainer = new VisualElement();
            iconContainer.name = "icon-container";
            iconContainer.style.width = 20;
            iconContainer.style.height = 20;
            iconContainer.style.alignItems = Align.Center;
            iconContainer.style.justifyContent = Justify.Center;
            iconContainer.style.flexShrink = 0;
            iconContainer.style.marginRight = 5;
            wordContainer.Add(iconContainer);
            
            // Word number label
            var numberLabel = new Label();
            numberLabel.name = "number-label";
            numberLabel.style.width = 25;
            numberLabel.style.flexShrink = 0;
            numberLabel.style.marginRight = 5;
            numberLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            wordContainer.Add(numberLabel);
            
            // Word text field
            var wordField = new TextField();
            wordField.name = "word-field";
            wordField.isReadOnly = true;
            wordField.style.flexGrow = 1;
            wordField.style.flexShrink = 1;
            wordField.style.minWidth = 100;
            wordField.style.marginRight = 15;
            wordContainer.Add(wordField);
            
            // Remove button
            var removeButton = new Button();
            removeButton.name = "remove-button";
            removeButton.text = "Remove";
            removeButton.style.width = 60;
            removeButton.style.flexShrink = 0;
            removeButton.style.marginRight = 2;
            wordContainer.Add(removeButton);
            
            // Ban button
            var banButton = new Button();
            banButton.name = "ban-button";
            banButton.text = "Ban";
            banButton.style.width = 40;
            banButton.style.flexShrink = 0;
            wordContainer.Add(banButton);
            
            return wordContainer;
        }
        
        private void BindWordListItem(VisualElement element, int index, SerializedProperty wordsProp, string langCode)
        {
            if (index >= wordsProp.arraySize) return;
            
            // Set zebra striping - alternating background colors
            if (index % 2 == 0)
            {
                // Even rows - lighter color
                element.style.backgroundColor = new Color(0.27f, 0.27f, 0.27f, 1.0f);
            }
            else
            {
                // Odd rows - darker color  
                element.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1.0f);
            }
            
            var wordElement = wordsProp.GetArrayElementAtIndex(index);
            var iconContainer = element.Q<VisualElement>("icon-container");
            var numberLabel = element.Q<Label>("number-label");
            var wordField = element.Q<TextField>("word-field");
            var removeButton = element.Q<Button>("remove-button");
            var banButton = element.Q<Button>("ban-button");
            
            // Update number label
            numberLabel.text = $"{index + 1}";
            
            // Update word field value and bind it
            var wordFieldUserData = wordField.userData;
            if(wordFieldUserData != null)
                wordField.UnregisterValueChangedCallback(wordFieldUserData as EventCallback<ChangeEvent<string>>);
            wordField.value = wordElement.stringValue;
            
            // Create and store the callback for cleanup
            EventCallback<ChangeEvent<string>> wordFieldCallback = evt =>
            {
                string newWord = evt.newValue.ToLower().Trim();
                
                // Check if the new word is banned
                var bannedWordsService = EditorScope.Resolve<IBannedWordsService>();
                if (bannedWordsService != null && !string.IsNullOrEmpty(newWord) && bannedWordsService.IsWordBanned(newWord, langCode))
                {
                    // Show warning and revert to previous value
                    EditorUtility.DisplayDialog("Word is Banned", 
                        $"The word '{newWord}' is in the banned words list for language {langCode}. " +
                        "Please use a different word or unban it first.", "OK");
                    
                    // Revert to original value
                    wordField.value = wordElement.stringValue;
                    return;
                }
                
                wordElement.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                
                // Update icon after word change
                UpdateWordIcon(iconContainer, evt.newValue, langCode);
                
                // Update all other word icons in this language to refresh duplicate detection
                RefreshAllWordIconsForLanguage(langCode);
            };
            
            wordField.RegisterValueChangedCallback(wordFieldCallback);
            wordField.userData = wordFieldCallback; // Store for cleanup
            
            // Update warning icon
            UpdateWordIcon(iconContainer, wordElement.stringValue, langCode);
            
            // Setup remove button
            removeButton.clicked -= removeButton.userData as global::System.Action;
            global::System.Action removeAction = () =>
            {
                RemoveWordAtIndex(wordsProp, index, langCode);
                UpdateCurrentLanguageWordsList(langCode);
                EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
            };
            removeButton.clicked += removeAction;
            removeButton.userData = removeAction; // Store for cleanup
            
            // Setup ban button
            banButton.clicked -= banButton.userData as global::System.Action;
            global::System.Action banAction = () =>
            {
                BanWordAtIndex(wordsProp, index, langCode);
                UpdateCurrentLanguageWordsList(langCode);
                EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
            };
            banButton.clicked += banAction;
            banButton.userData = banAction; // Store for cleanup
        }
        
        private void UnbindWordListItem(VisualElement element, int index)
        {
            var wordField = element.Q<TextField>("word-field");
            var removeButton = element.Q<Button>("remove-button");
            var banButton = element.Q<Button>("ban-button");
            
            // Clean up event handlers
            if (wordField?.userData is EventCallback<ChangeEvent<string>> wordCallback)
            {
                wordField.UnregisterValueChangedCallback(wordCallback);
                wordField.userData = null;
            }
            
            if (removeButton?.userData is global::System.Action removeAction)
            {
                removeButton.clicked -= removeAction;
                removeButton.userData = null;
            }
            
            if (banButton?.userData is global::System.Action banAction)
            {
                banButton.clicked -= banAction;
                banButton.userData = null;
            }
        }
        
        private bool IsWordDuplicateInLevel(string wordValue, string langCode)
        {
            if (string.IsNullOrEmpty(wordValue) || level == null) return false;
            
            var languageData = level.GetLanguageData(langCode);
            if (languageData?.words == null) return false;
            
            // Count occurrences of this word (case-insensitive)
            int count = 0;
            string normalizedWord = wordValue.ToLower().Trim();
            
            foreach (string word in languageData.words)
            {
                if (!string.IsNullOrEmpty(word) && word.ToLower().Trim() == normalizedWord)
                {
                    count++;
                    if (count > 1) return true; // Found duplicate
                }
            }
            
            return false;
        }

        private void RefreshAllWordIconsForLanguage(string langCode)
        {
            // Find the ListView for this language and refresh all visible items
            if (wordsListElements.ContainsKey(langCode) && wordsListElements[langCode] != null)
            {
                var listView = wordsListElements[langCode].Q<ListView>();
                if (listView != null)
                {
                    // Refresh the ListView to trigger rebinding of all items
                    listView.RefreshItems();
                }
            }
        }

        private void UpdateWordIcon(VisualElement iconContainer, string wordValue, string langCode)
        {
            iconContainer.Clear();

            // Check if word is used in other levels
            var usedInLevels = LevelEditorServices.GetUsedInLevels(wordValue, langCode, level);
            bool hasWarning = !string.IsNullOrEmpty(wordValue) && usedInLevels.Length > 0;

            // Get banned words service for banned status check
            var bannedWordsService = EditorScope.Resolve<IBannedWordsService>();
            bool isWordBanned = !string.IsNullOrEmpty(wordValue) && bannedWordsService != null && bannedWordsService.IsWordBanned(wordValue, langCode);

            // Check if word is duplicate within current level
            bool isDuplicate = IsWordDuplicateInLevel(wordValue, langCode);

            // Check if word is not known in model controller
            bool isWordUnknown = !string.IsNullOrEmpty(wordValue)
                                && Controller != null &&
                                !Controller.IsWordKnown(wordValue, langCode);
            
            if (hasWarning || isWordBanned || isDuplicate || isWordUnknown)
            {
                Image warningImage = new Image();
                warningImage.image = warningIcon;
                warningImage.style.width = 16;
                warningImage.style.height = 16;

                // Priority: Banned > Duplicate > Used in other levels
                if (isWordBanned && isDuplicate && hasWarning)
                {
                    // All three issues
                    iconContainer.tooltip = $"CRITICAL: This word is BANNED for {langCode}, DUPLICATED in this level, AND used in other levels";
                    warningImage.tintColor = Color.red;
                }
                else if (isWordBanned && isDuplicate)
                {
                    // Banned + duplicate
                    iconContainer.tooltip = $"CRITICAL: This word is BANNED for {langCode} AND duplicated in this level";
                    warningImage.tintColor = Color.red;
                }
                else if (isWordBanned && hasWarning)
                {
                    // Banned + used in other levels
                    iconContainer.tooltip = $"WARNING: Used in other levels AND this word is banned for {langCode}";
                    warningImage.tintColor = Color.red;
                }
                else if (isDuplicate && hasWarning)
                {
                    // Duplicate + used in other levels
                    string tooltipText = usedInLevels.Length == 1
                        ? $"WARNING: This word is DUPLICATED in this level AND already used in level {usedInLevels[0].number}"
                        : $"WARNING: This word is DUPLICATED in this level AND already used in levels: {string.Join(", ", usedInLevels.OrderBy(l => l.number).Select(l => l.number.ToString()))}";
                    iconContainer.tooltip = tooltipText;
                    warningImage.tintColor = new Color(1.0f, 0.5f, 0.0f); // Orange for multiple issues
                }
                else if (isWordBanned)
                {
                    // Only banned
                    iconContainer.tooltip = $"This word is banned for language {langCode}";
                    warningImage.tintColor = Color.red;
                }
                else if (isDuplicate)
                {
                    // Only duplicate
                    iconContainer.tooltip = "This word appears multiple times in this level";
                    warningImage.tintColor = Color.magenta; // Magenta for duplicates
                }
                else if (hasWarning)
                {
                    // Only used in other levels
                    string tooltipText = usedInLevels.Length == 1
                        ? $"This word has already been used in level {usedInLevels[0].number}"
                        : $"This word has already been used in levels: {string.Join(", ", usedInLevels.OrderBy(l => l.number).Select(l => l.number.ToString()))}";
                    iconContainer.tooltip = tooltipText;
                    warningImage.tintColor = Color.yellow;
                }
                else if (isWordUnknown)
                {
                    // Only unknown word
                    iconContainer.tooltip = "This word is not known in the model controller";
                    warningImage.tintColor = Color.green;
                }

                iconContainer.Add(warningImage);
            }
        }

        private void CreateAvailableWordsUI(string langCode, string letters, VisualElement parent, int languageIndex)
        {
            if (string.IsNullOrEmpty(langCode) || string.IsNullOrEmpty(letters))
            {
                return;
            }

            // Get banned words service
            var bannedWordsService = EditorScope.Resolve<IBannedWordsService>();
            var currentLangData = level.GetLanguageData(langCode);

            // Get words that can be created from the letters
            allWords = new List<string>();
            
            // First add words from GetWordsFromSymbols
            var rawWords = Controller.GetWordsFromSymbols(letters, langCode).Distinct().ToList();
            
            // Filter out banned words unless we want to show them
            bool showBannedWords = EditorPrefs.GetBool($"WordsToolkit_ShowBannedWords_{langCode}", false);
            if (showBannedWords)
            {
                allWords.AddRange(rawWords);
            }
            else
            {
                // Only add non-banned words
                foreach (var word in rawWords)
                {
                    if (bannedWordsService == null || !bannedWordsService.IsWordBanned(word, langCode))
                    {
                        allWords.Add(word);
                    }
                }
            }
            
            // Save all available words to the language data
            if (currentLangData != null)
            {
                var availableWordsContainer = new VisualElement();
                availableWordsContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.3f);
                availableWordsContainer.style.paddingTop = 5;
                availableWordsContainer.style.paddingBottom = 5;
                availableWordsContainer.style.paddingLeft = 5;
                availableWordsContainer.style.paddingRight = 5;
                availableWordsContainer.style.marginTop = 5;
                availableWordsContainer.style.marginBottom = 5;
                availableWordsContainer.style.flexGrow = 1; // Allow to grow and fill available space
                availableWordsContainer.style.flexShrink = 1; // Allow to shrink

                // Allow adding words manually
                var manualWordContainer = new VisualElement();
                manualWordContainer.style.flexDirection = FlexDirection.Row;
                manualWordContainer.style.alignItems = Align.Center;

                var manualWordLabel = new Label("Add Word Manually:");
                manualWordLabel.style.width = 120;
                manualWordContainer.Add(manualWordLabel);
                
                // Store the manual input in EditorPrefs to preserve it between editor updates
                string prefsKey = $"WordsToolkit_ManualWord_{langCode}";
                string manualWord = EditorPrefs.GetString(prefsKey, "");
                
                var manualWordField = new TextField { value = manualWord };
                manualWordField.style.width = 80;
                manualWordField.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
                manualWordField.style.borderTopColor = Color.gray;
                manualWordField.style.borderBottomColor = Color.gray;
                manualWordField.style.borderLeftColor = Color.gray;
                manualWordField.style.borderRightColor = Color.gray;
                manualWordField.style.borderTopWidth = 1;
                manualWordField.style.borderBottomWidth = 1;
                manualWordField.style.borderLeftWidth = 1;
                manualWordField.style.borderRightWidth = 1;
                manualWordField.RegisterValueChangedCallback(evt =>
                {
                    EditorPrefs.SetString(prefsKey, evt.newValue.ToLower().Trim());
                });
                manualWordContainer.Add(manualWordField);
                
                var addButton = new Button(() =>
                {
                    string currentWord = manualWordField.value.ToLower().Trim();
                    if (!string.IsNullOrEmpty(currentWord) && !allWords.Contains(currentWord))
                    {
                        // Check if word is banned before adding
                        if (bannedWordsService != null && bannedWordsService.IsWordBanned(currentWord, langCode))
                        {
                            EditorUtility.DisplayDialog("Word is Banned", 
                                $"The word '{currentWord}' is in the banned words list for language {langCode}. " +
                                "Please unban it first if you want to use it.", "OK");
                            return;
                        }
                        
                        Controller.AddWordAndSave(currentWord, langCode);
                        allWords.Add(currentWord);
                        manualWordField.value = "";
                        EditorPrefs.SetString(prefsKey, "");
                        
                        // Refresh the UI using LevelManagerWindow
                        EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
                    }
                })
                {
                    text = "Add"
                };
                addButton.style.width = 60;
                manualWordContainer.Add(addButton);

                availableWordsContainer.Add(manualWordContainer);
                allWords = allWords.OrderBy(x => x).ToList();

                if (allWords != null && allWords.Any())
                {
                    // Get total counts for better labeling
                    var allRawWords = Controller.GetWordsFromSymbols(letters, langCode).Distinct().ToList();
                    int totalWords = allRawWords.Count;
                    int bannedCount = 0;
                    if (bannedWordsService != null)
                    {
                        bannedCount = allRawWords.Count(word => bannedWordsService.IsWordBanned(word, langCode));
                    }
                    int availableCount = totalWords - bannedCount;
                    
                    bool showBannedWordsToggle = EditorPrefs.GetBool($"WordsToolkit_ShowBannedWords_{langCode}", false);
                    
                    // Available words foldout with better labeling
                    var availableWordsFoldout = new Foldout
                    {
                        text = showBannedWordsToggle 
                            ? $"All Words: ({totalWords} total, {availableCount} available)"
                            : $"Available Words: ({availableCount} available)",
                        value = showAvailableWordsDict.ContainsKey(langCode) ? showAvailableWordsDict[langCode] : true
                    };

                    availableWordsFoldout.style.flexGrow = 1; // Allow to grow and fill available space
                    availableWordsFoldout.RegisterValueChangedCallback(evt => showAvailableWordsDict[langCode] = evt.newValue);

                    var availableWordsContent = new VisualElement();
                    availableWordsContent.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                    availableWordsContent.style.flexGrow = 1; // Allow content to grow
                    availableWordsContent.style.flexShrink = 1; // Allow content to shrink
                    availableWordsContent.style.height = new StyleLength(new Length(100, LengthUnit.Percent)); // Take full height
                    
                    // Create scrollview for the words list
                    var wordsScrollView = new ScrollView(ScrollViewMode.Vertical);
                    wordsScrollView.style.flexGrow = 1; // Fill all available space
                    wordsScrollView.style.flexShrink = 1; // Allow it to shrink if needed
                    wordsScrollView.style.height = new StyleLength(new Length(100, LengthUnit.Percent)); // Take full height
                    wordsScrollView.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

                    
                    foreach (var word in allWords)
                    {
                        var wordContainer = new VisualElement();
                        wordContainer.style.flexDirection = FlexDirection.Row;
                        wordContainer.style.alignItems = Align.Center;
                        wordContainer.style.marginBottom = 2;
                        wordContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                        wordContainer.style.flexShrink = 0;
                        wordContainer.style.minWidth = 280;

                        // Check if word is banned
                        bool isWordBanned = bannedWordsService != null && bannedWordsService.IsWordBanned(word, langCode);
                        
                        // Check if word is used in other levels and add warning icon before the word label
                        var usedInLevels = LevelEditorServices.GetUsedInLevels(word, langCode, level);
                        bool hasWarning = usedInLevels.Length > 0;
                        
                        if (hasWarning)
                        {
                            var warningContainer = new VisualElement();
                            warningContainer.style.width = 20;
                            warningContainer.style.height = 20;
                            warningContainer.style.alignItems = Align.Center;
                            warningContainer.style.justifyContent = Justify.Center;
                            warningContainer.style.flexShrink = 0;
                            warningContainer.style.marginRight = 5;

                            // Create tooltip text with all levels where the word is used
                            string tooltipText = usedInLevels.Length == 1 
                                ? $"This word has already been used in level {usedInLevels[0].number}"
                                : $"This word has already been used in levels: {string.Join(", ", usedInLevels.OrderBy(l => l.number).Select(l => l.number.ToString()))}";
                            
                            // Set tooltip on the container
                            warningContainer.tooltip = tooltipText;

                            var warningImage = new Image();
                            warningImage.image = warningIcon;
                            warningImage.style.width = 16;
                            warningImage.style.height = 16;
                            warningImage.tintColor = Color.yellow;

                            warningContainer.Add(warningImage);
                            wordContainer.Add(warningContainer);
                        }
                        else
                        {
                            // Add placeholder space to maintain alignment
                            var placeholderContainer = new VisualElement();
                            placeholderContainer.style.width = 20;
                            placeholderContainer.style.height = 20;
                            placeholderContainer.style.flexShrink = 0;
                            placeholderContainer.style.marginRight = 5;
                            wordContainer.Add(placeholderContainer);
                        }

                        // Check if word is already used in the level's word list
                        var wordsProp = serializedObject.FindProperty("languages")
                            .GetArrayElementAtIndex(languageIndex)
                            .FindPropertyRelative("words");
                        
                        bool isWordUsedInLevel = false;
                        for (int i = 0; i < wordsProp.arraySize; i++)
                        {
                            if (wordsProp.GetArrayElementAtIndex(i).stringValue == word)
                            {
                                isWordUsedInLevel = true;
                                break;
                            }
                        }

                        // Create word label with asterisk if used in level
                        string displayText = isWordUsedInLevel ? $"{word} *" : word;
                        var wordLabel = new Label(displayText);
                        wordLabel.style.flexGrow = 1;
                        
                        // Style the word label based on banned status and usage in level
                        if (isWordBanned)
                        {
                            wordLabel.style.color = Color.red;
                            wordLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                            wordLabel.tooltip = "This word is banned";
                        }
                        else if (isWordUsedInLevel)
                        {
                            wordLabel.style.color = Color.white;
                            wordLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                            wordLabel.tooltip = "This word is already used in the level";
                        }
                        else
                        {
                            wordLabel.style.color = Color.white;
                            wordLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                        }
                        
                        wordContainer.Add(wordLabel);

                        // Add to Level button (only show for non-banned words that aren't already in the level)
                        if (!isWordBanned && !isWordUsedInLevel)
                        {
                            var addToLevelButton = new Button()
                            {
                                text = "Add to Level"
                            };
                            addToLevelButton.style.width = 80;
                            addToLevelButton.style.marginRight = 5;
                            
                            // Set up the click handler
                            addToLevelButton.clicked += () =>
                            {
                                // Update serialized object to get latest changes from other windows (like CrosswordGridWindow)
                                serializedObject.Update();
                                
                                // Add word to level's word list for the correct language
                                var wordsProp = serializedObject.FindProperty("languages")
                                    .GetArrayElementAtIndex(languageIndex)
                                    .FindPropertyRelative("words");
                                
                                // Check if word already exists (double-check for safety)
                                bool wordExists = false;
                                for (int i = 0; i < wordsProp.arraySize; i++)
                                {
                                    if (wordsProp.GetArrayElementAtIndex(i).stringValue == word)
                                    {
                                        wordExists = true;
                                        break;
                                    }
                                }

                                if (!wordExists)
                                {
                                    wordsProp.InsertArrayElementAtIndex(wordsProp.arraySize);
                                    wordsProp.GetArrayElementAtIndex(wordsProp.arraySize - 1).stringValue = word;
                                    serializedObject.ApplyModifiedProperties();
                                    EditorUtility.SetDirty(serializedObject.targetObject);
                                    if (level != null)
                                    {
                                        EditorUtility.SetDirty(level);
                                    }
                                    
                                    // Notify that the level needs update to refresh the crossword
                                    NotifyLevelNeedsUpdate(level);
                                    
                                    // Hide this button and mark the word as used immediately
                                    addToLevelButton.SetEnabled(false);
                                    addToLevelButton.text = "Added";
                                    addToLevelButton.style.display = DisplayStyle.None;
                                    // Update word label to show it's now used in the level
                                    wordLabel.text = $"{word} *";
                                    wordLabel.style.color = Color.white;
                                    wordLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                                    wordLabel.tooltip = "This word is already used in the level";
                                    // Also update the words list to reflect the change
                                    UpdateCurrentLanguageWordsList(langCode);
                                }
                            };
                            
                            wordContainer.Add(addToLevelButton);
                        }
                        else if (isWordUsedInLevel)
                        {
                            // Add placeholder space to maintain alignment when word is already used
                            var placeholderButton = new VisualElement();
                            placeholderButton.style.width = 80;
                            placeholderButton.style.marginRight = 5;
                            wordContainer.Add(placeholderButton);
                        }

                        // Ban/Unban button
                        if (bannedWordsService != null)
                        {
                            var banButton = new Button(() =>
                            {
                                if (isWordBanned)
                                {
                                    // Unban the word
                                    bannedWordsService.RemoveBannedWord(word, langCode);
                                    Debug.Log($"Removed '{word}' from banned words for language {langCode}");
                                }
                                else
                                {
                                    // Ban the word
                                    bannedWordsService.AddBannedWord(word, langCode);
                                    Debug.Log($"Added '{word}' to banned words for language {langCode}");
                                }
                                
                                // Refresh the UI using LevelManagerWindow
                                EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
                            })
                            {
                                text = isWordBanned ? "Unban" : "Ban"
                            };
                            banButton.style.width = 50;
                            
                            // Style the ban button to match the Add to Level button
                            // No custom background color - use default button styling like Add to Level button
                            
                            wordContainer.Add(banButton);
                        }

                        wordsScrollView.Add(wordContainer);
                    }
                    
                    // Add the scrollview to the content
                    availableWordsContent.Add(wordsScrollView);
                    
                    // Always add the content to the foldout, the foldout will handle showing/hiding
                    availableWordsFoldout.Add(availableWordsContent);

                    availableWordsContainer.Add(availableWordsFoldout);
                }

                parent.Add(availableWordsContainer);
            }
        }


        private bool FillLanguage(LanguageData languageData, SerializedProperty lettersProp, int wordCount, SerializedProperty wordsProp, bool anySuccess, int lettersAmount)
        {
            // Ensure letters exist - use existing or generate if empty
            string letters = "";
            if (string.IsNullOrEmpty(letters))
            {
                // Generate random letters based on language if needed
                letters = LevelEditorServices.GenerateRandomLetters(languageData,  languageData.wordsAmount, lettersAmount, false);
                lettersProp.stringValue = letters;
                serializedObject.ApplyModifiedProperties();
            }

            // Use the existing GenerateWords method
            if (!string.IsNullOrEmpty(letters))
            {
                var level = this.level;
                GenerateWordsForLanguage(level, languageData, Controller);
                anySuccess = true;
            }

            return anySuccess;
        }

        private void RemoveWordAtIndex(SerializedProperty wordsProp, int index, string langCode)
        {
            // Update serialized object to get latest changes from other windows (like CrosswordGridWindow)
            serializedObject.Update();
            
            wordsProp.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);
            AssetDatabase.SaveAssets();

            // Update available words to refresh buttons and marks
            var currentLanguageData = level.GetLanguageData(langCode);
            if (currentLanguageData != null && !string.IsNullOrEmpty(currentLanguageData.letters))
            {
                UpdateAvailableWordsForLanguage(langCode, currentLanguageData.letters);
            }

            // Refresh all word icons to update duplicate detection
            RefreshAllWordIconsForLanguage(langCode);

            // Regenerate the crossword preview with the updated word list only if flag is true
            if (updateCrosswordOnChangeWords)
            {
                var languageData = level.GetLanguageData(langCode);
                if (languageData != null)
                {
                     NotifyLevelNeedsUpdate(level);
                }
            }
        }

        private void BanWordAtIndex(SerializedProperty wordsProp, int index, string langCode)
        {
            if (index >= 0 && index < wordsProp.arraySize)
            {
                // Update serialized object to get latest changes from other windows (like CrosswordGridWindow)
                serializedObject.Update();
                
                string word = wordsProp.GetArrayElementAtIndex(index).stringValue.ToLower();
                
                // Use the BannedWordsService instead of directly manipulating the configuration
                var bannedWordsService = EditorScope.Resolve<IBannedWordsService>();
                if (bannedWordsService != null)
                {
                    // Remove from the word list
                    wordsProp.DeleteArrayElementAtIndex(index);

                    // Add to the banned words list using the service (which will handle saving)
                    bannedWordsService.AddBannedWord(word, langCode);

                    // Apply changes to serialized object
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);

                    // Update available words to refresh buttons and marks
                    var currentLanguageData = level.GetLanguageData(langCode);
                    if (currentLanguageData != null && !string.IsNullOrEmpty(currentLanguageData.letters))
                    {
                        UpdateAvailableWordsForLanguage(langCode, currentLanguageData.letters);
                    }

                    // Refresh all word icons to update duplicate detection
                    RefreshAllWordIconsForLanguage(langCode);

                    // Regenerate the crossword preview with the updated word list
                    if (updateCrosswordOnChangeWords)
                    {
                        var languageData = level.GetLanguageData(langCode);
                        if (languageData != null)
                        {
                            UpdateCrossword(level, languageData);
                        }
                    }

                    Debug.Log($"Added '{word}' to banned words for language {langCode} and removed from word list");
                }
                else
                {
                    Debug.LogError("Could not resolve BannedWordsService from EditorScope!");
                }
            }
        }

        private void GenerateWordsForLanguage(Level level, LanguageData languageData, IModelController Controller)
        {
            string letters = LevelEditorServices.GenerateRandomLetters(languageData, languageData.wordsAmount, level.letters, false);
            languageData.letters = letters;
            LevelEditorServices.GenerateWordsForLanguage(level, languageData, Controller, false);
            UpdateCrossword(level, languageData);
        }

        private void UpdateCrossword(Level level)
        {
            // Update crossword for all languages
            foreach (var languageData in level.languages)
            {
                UpdateCrossword(level, languageData);
            }
        }
        
        private void UpdateCrosswordForOtherLanguages(Level level, int currentTabIndex)
        {
            // Update crossword only for languages that are not the current one
            for (int i = 0; i < level.languages.Count; i++)
            {
                if (i != currentTabIndex) // Skip the current language tab
                {
                    UpdateCrossword(level, level.languages[i]);
                }
            }
        }

        private void UpdateCrossword(Level level, LanguageData languageData)
        {
            // Update serialized object to get latest changes from other windows (like CrosswordGridWindow)
            serializedObject.Update();
            
            // Generate and save the crossword preview
            var existingPreview = CrosswordPreviewHandler.LoadPreviewFromLevel(level, languageData.language);
            int columns = previewColumnDict.ContainsKey(languageData.language) ? previewColumnDict[languageData.language] : languageData.crosswordData.columns;
            int rows = previewRowDict.ContainsKey(languageData.language) ? previewRowDict[languageData.language] : languageData.crosswordData.rows;

            var previewData = GeneratePreviewForLanguage(languageData.words, columns, rows);
            if (previewData != null)
            {
                previewDataDict[languageData.language] = previewData;
                CrosswordPreviewHandler.SavePreviewToLevel(this.level, languageData.language, previewData);
            }
        }

        private void UpdateCrosswordPreservingPlacements(Level level, LanguageData languageData)
        {
            // Load existing crossword preview data to preserve placements
            var existingPreview = CrosswordPreviewHandler.LoadPreviewFromLevel(level, languageData.language);
            if (existingPreview != null && existingPreview.isValid)
            {
                // Update the preview data dictionary
                previewDataDict[languageData.language] = existingPreview;
                
                // Just save the existing preview back to preserve all placements and grid state
                CrosswordPreviewHandler.SwitchWordsPlacements(level, existingPreview,   languageData.language);
                
                // Mark as dirty for editor updates
                EditorUtility.SetDirty(level);
            }
        }

        private static CrosswordPreviewHandler.PreviewData GeneratePreviewForLanguage(string[] words, int columns, int rows)
        {
            if (words == null || words.Length == 0)
                return null;
                
            // Generate preview data using the handler with the specified dimensions
            return CrosswordPreviewHandler.GeneratePreview(words, columns, rows);
        }

        private void GeneratePreviewForLanguage(string langCode, SerializedProperty wordsProp)
        {
            // Extract words from the property
            string[] words = new string[wordsProp.arraySize];
            for (int i = 0; i < wordsProp.arraySize; i++)
            {
                words[i] = wordsProp.GetArrayElementAtIndex(i).stringValue;
            }
            
            // Get columns and rows from the dictionary
            int columns = previewColumnDict.ContainsKey(langCode) ? previewColumnDict[langCode] : DefaultColumns;
            int rows = previewRowDict.ContainsKey(langCode) ? previewRowDict[langCode] : DefaultRows;
            
            // Use the static method to generate the preview
            var previewData = GeneratePreviewForLanguage(words, columns, rows);
            if (previewData != null)
            {
                previewDataDict[langCode] = previewData;
            }
        }

        private void AddLanguageMenu(Level level)
        {
            // If we have a language configuration, show a dropdown menu
            if (languageConfig != null && languageConfig.languages.Count > 0)
            {
                GenericMenu menu = new GenericMenu();

                // Get current languages
                HashSet<string> existingLangs = new HashSet<string>();
                for (int i = 0; i < languagesProp.arraySize; i++)
                {
                    var langProp = languagesProp.GetArrayElementAtIndex(i);
                    var langCode = langProp.FindPropertyRelative("language").stringValue;
                    existingLangs.Add(langCode);
                }

                // Get only enabled languages and add menu items for them
                var enabledLanguages = languageConfig.GetEnabledLanguages();
                foreach (var langInfo in enabledLanguages)
                {
                    bool exists = existingLangs.Contains(langInfo.code);

                    if (exists)
                    {
                        menu.AddDisabledItem(new GUIContent(langInfo.displayName), true);
                    }
                    else
                    {
                        menu.AddItem(new GUIContent(langInfo.displayName), false, () => {
                            AddLanguageToLevel(level, langInfo.code);
                        });
                    }
                }

                // Add option for custom language
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Custom Language..."), false, () => {
                    ShowCustomLanguageDialog(level);
                });

                menu.ShowAsContext();
            }
            else
            {
                // No language config, directly show dialog
                ShowCustomLanguageDialog(level);
            }
        }

        private void ShowCustomLanguageDialog(Level level)
        {
            // Show dialog to enter language code
            string result = EditorInputDialog.Show("Add Language", "Enter language code (e.g., en, fr, es):", "");

            if (!string.IsNullOrEmpty(result))
            {
                AddLanguageToLevel(level, result);
            }
        }

        private void AddLanguageToLevel(Level level, string languageCode)
        {
            // Create the new language data
            level.AddLanguage(languageCode);

            // Mark as dirty
            EditorUtility.SetDirty(level);
            serializedObject.Update();

            // Make sure languages are expanded
            showLanguages = true;
        }

        // Helper method to find or create the CrosswordGenerationConfigSO
        private UnityEngine.Object FindOrCreateCrosswordConfig()
        {
            // Look for existing CrosswordGenerationConfigSO
            string[] guids = AssetDatabase.FindAssets("t:CrosswordGenerationConfigSO");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            }
            
            // If not found, ask if we want to create one
            bool createConfig = EditorUtility.DisplayDialog(
                "Crossword Configuration",
                "No CrosswordGenerationConfigSO found. Would you like to create one?",
                "Create", "Cancel");
                
            if (createConfig)
            {
                // Create a configuration asset
                var configAsset = ScriptableObject.CreateInstance<CrosswordGenerationConfigSO>();
                
                // Make sure the Settings folder exists
                if (!AssetDatabase.IsValidFolder("Assets/WordConnectGameToolkit/Resources/Settings"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/WordConnectGameToolkit/Resources"))
                    {
                        AssetDatabase.CreateFolder("Assets/WordConnectGameToolkit", "Resources");
                    }
                    AssetDatabase.CreateFolder("Assets/WordConnectGameToolkit/Resources", "Settings");
                }
                
                // Save the asset
                AssetDatabase.CreateAsset(configAsset, "Assets/WordConnectGameToolkit/Resources/Settings/CrosswordConfig.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log("Created new CrosswordGenerationConfigSO at Assets/WordConnectGameToolkit/Resources/Settings/CrosswordConfig.asset");
                return configAsset;
            }
            
            return null;
        }

        private void UpdateLanguages()
        {
            if (level == null)
                return;

            if (languageConfig == null || languageConfig.languages == null || languageConfig.languages.Count == 0)
            {
                return;
            }

            bool languagesModified = false;

            // Get only enabled languages from the configuration
            var enabledLanguages = languageConfig.GetEnabledLanguages();
            
            // Check each enabled language in the configuration
            foreach (var configLanguage in enabledLanguages)
            {
                // Skip if language code is null or empty
                if (string.IsNullOrEmpty(configLanguage.code))
                    continue;

                // Check if this language exists in the level's languages
                var langData = level.GetLanguageData(configLanguage.code);
                if (langData == null)
                {
                    AddLanguageToLevel(level, configLanguage.code);
                    languagesModified = true;
                }
                var group = level.GetGroup();
                if(group.GetText(configLanguage.code) == null)
                {
                    group.AddLanguage(configLanguage.code);
                }
            }

            if (languagesModified)
            {
                serializedObject.Update();
                EditorUtility.SetDirty(target);
                Debug.Log("Added missing enabled languages to the level.");
            }
        }

        private int GetEnabledLanguageTabsCount()
        {
            if (level == null || level.languages == null || languageConfig == null)
                return 0;

            var enabledLanguages = languageConfig.GetEnabledLanguages();
            var enabledLanguageCodes = enabledLanguages.Select(lang => lang.code).ToHashSet();
            
            int count = 0;
            for (int i = 0; i < level.languages.Count; i++)
            {
                var languageData = level.languages[i];
                if (languageData != null && enabledLanguageCodes.Contains(languageData.language))
                {
                    count++;
                }
            }
            
            return count;
        }

        private void UpdateCurrentLanguageWordsList(string langCode)
        {
            // Check if we have a stored reference to the words list container for this language
            if (wordsListElements.ContainsKey(langCode) && wordsListElements[langCode] != null)
            {
                // Get the words property for the current language
                int selectedTabIndex = EditorPrefs.GetInt("WordsToolkit_SelectedLanguageTab", 0);
                if (selectedTabIndex >= 0 && selectedTabIndex < languagesProp.arraySize)
                {
                    var selectedLangProp = languagesProp.GetArrayElementAtIndex(selectedTabIndex);
                    var selectedLangCode = selectedLangProp.FindPropertyRelative("language").stringValue;
                    
                    // Only update if this is the currently selected language
                    if (selectedLangCode == langCode)
                    {
                        var wordsProp = selectedLangProp.FindPropertyRelative("words");
                        var wordsListContainer = wordsListElements[langCode];
                        
                        // Find the ListView in the container and refresh it
                        var listView = wordsListContainer.Q<ListView>();
                        if (listView != null)
                        {
                            // Refresh the ListView to reflect the updated data
                            listView.RefreshItems();
                            return;
                        }
                        else
                        {
                            // Fallback: recreate the ListView
                            FillWordsList(wordsListContainer, wordsProp, langCode);
                            return;
                        }
                    }
                }
            }
            
            // Fallback: Refresh the entire inspector if we can't update just the words list
            EditorWindows.LevelManagerWindow.RefreshInspectorForLevel(level);
        }
    }

    // Simple dialog class for text input
    public class EditorInputDialog : EditorWindow
    {
        private string dialogTitle = "";
        private string message = "";
        private string inputText = "";
        private string okButton = "OK";
        private string cancelButton = "Cancel";
        private bool isDone = false;
        private global::System.Action<string> onComplete;

        public static string Show(string title, string message, string inputText = "")
        {
            // Use a non-modal utility window with callback
            EditorInputDialog window = GetWindow<EditorInputDialog>(true, title, true);
            window.titleContent = new GUIContent(title);
            window.dialogTitle = title;
            window.message = message;
            window.inputText = inputText;
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
            window.ShowModalUtility();

            // ShowModalUtility() will block until window is closed
            // For modal windows in Unity, we don't need the busy-wait loop
            return window.inputText;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(message);

            UnityEngine.GUI.SetNextControlName("InputField");
            inputText = EditorGUILayout.TextField(inputText);

            // Focus the text field
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl("InputField");
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(cancelButton))
                {
                    // Just close the window with no changes
                    Close();
                }

                if (GUILayout.Button(okButton))
                {
                    // Keep the current inputText value when closing
                    Close();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Handle enter key
            if (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.KeyDown)
            {
                // Accept the current input and close when Enter is pressed
                Close();
            }
        }
    }
}
