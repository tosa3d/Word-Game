using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using WordsToolkit.Scripts.Levels.Editor.EditorWindows;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.Levels.Editor
{
    [CustomEditor(typeof(LevelGroup))]
    public class LevelGroupEditor : UnityEditor.Editor
    {
        private VisualElement rootContainer;
        private VisualElement localizedTextsList;
        private ScrollView localizedTextsScroll;
        private VisualElement backgroundPreview;
        private PropertyField groupNameField;
        private PropertyField backgroundField;
        private PropertyField colorsTileField;
        private PropertyField targetExtraWordsField;
        private Button addLanguageButton;
        private Label noLanguagesMessage;

        SerializedProperty groupNameProperty;
        SerializedProperty parentGroupProperty;
        SerializedProperty levelsProperty;
        SerializedProperty backgroundProperty;
        SerializedProperty localizedTextsProperty;
        SerializedProperty colorsTileProperty;
        SerializedProperty targetExtraWordsProperty;

        private LanguageConfiguration languageConfig;
        private string[] languageCodes;
        private string[] languageNames;
        private bool colorsTileChanged;

        private void OnEnable()
        {
            groupNameProperty = serializedObject.FindProperty("groupName");
            parentGroupProperty = serializedObject.FindProperty("parentGroup");
            levelsProperty = serializedObject.FindProperty("levels");
            backgroundProperty = serializedObject.FindProperty("background");
            localizedTextsProperty = serializedObject.FindProperty("localizedTexts");
            colorsTileProperty = serializedObject.FindProperty("colorsTile");
            targetExtraWordsProperty = serializedObject.FindProperty("targetExtraWords");
            ColorsTileDrawer.OnColorTileSelected += OnColorTileSelected;

            // Load language configuration
            LoadLanguageConfiguration();
            
            // Register for undo/redo events
            // Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            ColorsTileDrawer.OnColorTileSelected -= OnColorTileSelected;
            // Unregister from undo/redo events
            // Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnColorTileSelected(ColorsTile obj)
        {
            // This is called by the ColorsTileDrawer when a color tile is selected
            colorsTileChanged = true;
            // Also trigger the change handler directly
            HandleColorsTileChange();
        }

        private void HandleColorsTileChange()
        {
            colorsTileChanged = false; // Reset the flag after processing
            var lg = (LevelGroup)target;
            lg.ApplyColorsTileToLevels();

            if (lg.levels != null && lg.levels.Count > 0)
            {
                Undo.RecordObjects(lg.levels.ToArray(), "Update Levels ColorsTile");
                foreach (var level in lg.levels)
                {
                    if (level != null)
                    {
                        EditorUtility.SetDirty(level);
                    }
                }

                AssetDatabase.SaveAssets();
                Debug.Log($"Updated colorsTile for {lg.levels.Count} levels in group {lg.groupName}");
            }
        }

        private void OnUndoRedo()
        {
            // Refresh hierarchy when undo/redo is performed
            RefreshLevelManagerWindow();
        }
        
        private void RefreshLevelManagerWindow()
        {
            // Find and refresh the Level Manager Window if it's open
            var levelManagerWindow = EditorWindow.GetWindow<LevelManagerWindow>(false);
            if (levelManagerWindow != null)
            {
                levelManagerWindow.Repaint();
                var hierarchyTree = levelManagerWindow.GetHierarchyTree();
                if (hierarchyTree != null)
                {
                    hierarchyTree.Reload();
                }
            }
        }
        
        private void LoadLanguageConfiguration()
        {
            // Find the language configuration asset
            string[] guids = AssetDatabase.FindAssets("t:LanguageConfiguration");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                languageConfig = AssetDatabase.LoadAssetAtPath<LanguageConfiguration>(path);
                
                if (languageConfig != null && languageConfig.languages != null)
                {
                    // Get enabled languages
                    var enabledLanguages = languageConfig.GetEnabledLanguages();
                    
                    // Initialize arrays
                    languageCodes = enabledLanguages.Select(l => l.code).ToArray();
                    languageNames = enabledLanguages.Select(l => l.displayName).ToArray();
                    
                    // If arrays are empty, provide at least English as fallback
                    if (languageCodes.Length == 0)
                    {
                        languageCodes = new[] { "en" };
                        languageNames = new[] { "English" };
                    }
                }
            }
            
            // Fallback if no configuration is found
            if (languageCodes == null || languageNames == null)
            {
                languageCodes = new[] { "en" };
                languageNames = new[] { "English" };
            }
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            // Load the UXML file
            var directory = "Assets/WordConnectGameToolkit/UIBuilder";
            var uxmlPath = Path.Combine(directory, "LevelGroupEditor.uxml");
            
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTree == null)
            {
                Debug.LogError($"Could not load UXML file at path: {uxmlPath}");
                return new Label("Failed to load UI");
            }
            
            rootContainer = visualTree.CloneTree();

            // Find UI elements
            localizedTextsList = rootContainer.Q<VisualElement>("localized-texts-list");
            localizedTextsScroll = rootContainer.Q<ScrollView>("localized-texts-scroll");
            backgroundPreview = rootContainer.Q<VisualElement>("background-preview");
            groupNameField = rootContainer.Q<PropertyField>("group-name-field");
            backgroundField = rootContainer.Q<PropertyField>("background-field");
            colorsTileField = rootContainer.Q<PropertyField>("colors-tile-field");
            targetExtraWordsField = rootContainer.Q<PropertyField>("target-extra-words-field");
            addLanguageButton = rootContainer.Q<Button>("add-language-button");
            noLanguagesMessage = rootContainer.Q<Label>("no-languages-message");

            // Bind properties explicitly
            rootContainer.Bind(serializedObject);

            // Set tooltip for targetExtraWordsField if found
            if (targetExtraWordsField != null)
            {
                targetExtraWordsField.tooltip = "Target number of extra words for levels in this group";
            }

            // If colorsTileField is null, create it manually as a fallback
            if (colorsTileField == null)
            {
                Debug.Log("Creating colorsTile PropertyField manually");
                var fieldsContainer = rootContainer.Q<VisualElement>("fields-container");
                if (fieldsContainer != null)
                {
                    colorsTileField = new PropertyField(colorsTileProperty);
                    colorsTileField.name = "colors-tile-field-manual";
                    fieldsContainer.Add(colorsTileField);
                    Debug.Log("Manual colorsTile PropertyField created and added");
                }
            }

            // If targetExtraWordsField is null, create it manually as a fallback
            if (targetExtraWordsField == null)
            {
                var fieldsContainer = rootContainer.Q<VisualElement>("fields-container");
                if (fieldsContainer != null)
                {
                    targetExtraWordsField = new PropertyField(targetExtraWordsProperty);
                    targetExtraWordsField.name = "target-extra-words-field-manual";
                    targetExtraWordsField.tooltip = "Target number of extra words to be found to get a reward";
                    fieldsContainer.Add(targetExtraWordsField);
                }
            }

            // Setup callbacks
            SetupCallbacks();

            // Track only array size changes for localized texts, not content changes
            // This prevents recreating text fields when the user is typing
            lastLocalizedTextsCount = localizedTextsProperty.arraySize;

            // Schedule initial updates for next frame to ensure all properties are ready
            rootContainer.schedule.Execute(() =>
            {
                UpdateLocalizedTextsList();
                UpdateBackgroundPreview();
            });

            return rootContainer;
        }

        private void SetupCallbacks()
        {
            // Group name change callback
            groupNameField.RegisterValueChangeCallback(evt =>
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                RefreshLevelManagerWindow();
            });

            // Background change callback
            backgroundField.RegisterValueChangeCallback(evt =>
            {
                UpdateBackgroundPreview();
                HandleBackgroundChange();
            });

            // Colors tile change callback
            colorsTileField.RegisterValueChangeCallback(evt =>
            {
                HandleColorsTileChange();
            });

            // Button callbacks
            addLanguageButton.clicked += AddNewLanguage;
        }

        private void UpdateBackgroundPreview()
        {
            if (backgroundProperty.objectReferenceValue != null)
            {
                Sprite sprite = (Sprite)backgroundProperty.objectReferenceValue;
                backgroundPreview.style.backgroundImage = new StyleBackground(sprite);
                backgroundPreview.style.backgroundColor = StyleKeyword.None;
                backgroundPreview.style.display = DisplayStyle.Flex;
            }
            else
            {
                backgroundPreview.style.backgroundImage = StyleKeyword.None;
                backgroundPreview.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
                backgroundPreview.style.display = DisplayStyle.Flex;
            }
        }

        private void HandleBackgroundChange()
        {
            var lg = (LevelGroup)target;
            if (lg.levels != null && lg.levels.Count > 0)
            {
                Sprite newBackground = (Sprite)backgroundProperty.objectReferenceValue;
                Undo.RecordObjects(lg.levels.ToArray(), "Update Levels Background");

                foreach (var level in lg.levels)
                {
                    if (level != null)
                    {
                        level.background = newBackground;
                        EditorUtility.SetDirty(level);
                    }
                }

                AssetDatabase.SaveAssets();
            }
        }

        private void AddNewLanguage()
        {
            localizedTextsProperty.arraySize++;
            SerializedProperty newElement = localizedTextsProperty.GetArrayElementAtIndex(localizedTextsProperty.arraySize - 1);
            newElement.FindPropertyRelative("language").stringValue = languageCodes[0]; // Default to first language
            newElement.FindPropertyRelative("title").stringValue = "";
            newElement.FindPropertyRelative("text").stringValue = "";
            
            serializedObject.ApplyModifiedProperties();
            
            // Update the count and refresh the list
            lastLocalizedTextsCount = localizedTextsProperty.arraySize;
            UpdateLocalizedTextsList();
        }

        private void UpdateLocalizedTextsList()
        {
            // Clear existing items
            localizedTextsList.Clear();

            if (localizedTextsProperty.arraySize == 0)
            {
                noLanguagesMessage.style.display = DisplayStyle.Flex;
                localizedTextsList.Add(noLanguagesMessage);
                return;
            }

            noLanguagesMessage.style.display = DisplayStyle.None;

            // Add language items
            bool addedAnyItems = false;
            for (int i = 0; i < localizedTextsProperty.arraySize; i++)
            {
                SerializedProperty element = localizedTextsProperty.GetArrayElementAtIndex(i);
                SerializedProperty languageProp = element.FindPropertyRelative("language");
                string languageCode = languageProp.stringValue;

                // Skip disabled languages
                if (!IsLanguageEnabled(languageCode))
                    continue;

                int index = i; // Capture for closure
                var localizedTextItem = CreateLocalizedTextItem(index);

                // Add separator if we've already added items
                if (addedAnyItems)
                {
                    var separator = new VisualElement();
                    separator.AddToClassList("separator");
                    localizedTextsList.Add(separator);
                }

                localizedTextsList.Add(localizedTextItem);
                addedAnyItems = true;
            }
        }

        private bool IsLanguageEnabled(string languageCode)
        {
            if (languageConfig == null || languageConfig.languages == null)
                return true; // Default to enabled if no config

            var languageInfo = languageConfig.GetLanguageInfo(languageCode);
            return languageInfo != null && languageInfo.enabledByDefault;
        }

        private VisualElement CreateLocalizedTextItem(int index)
        {
            SerializedProperty element = localizedTextsProperty.GetArrayElementAtIndex(index);
            SerializedProperty languageProp = element.FindPropertyRelative("language");
            SerializedProperty titleProp = element.FindPropertyRelative("title");
            SerializedProperty textProp = element.FindPropertyRelative("text");

            var container = new VisualElement();
            container.AddToClassList("localized-text-item");

            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("localized-text-content");

            var fieldsContainer = new VisualElement();
            fieldsContainer.AddToClassList("localized-text-fields");

            // Language dropdown
            var languageContainer = new VisualElement();
            languageContainer.AddToClassList("language-dropdown");
            
            var languageLabel = new Label("Language");
            languageContainer.Add(languageLabel);

            var languageDropdown = new DropdownField();
            languageDropdown.AddToClassList("language-dropdown-field");
            languageDropdown.choices = languageNames.ToList();
            
            string currentLanguage = languageProp.stringValue;
            int selectedIndex = 0;
            for (int j = 0; j < languageCodes.Length; j++)
            {
                if (languageCodes[j] == currentLanguage)
                {
                    selectedIndex = j;
                    break;
                }
            }
            
            languageDropdown.index = selectedIndex;
            languageDropdown.RegisterValueChangedCallback(evt =>
            {
                int newIndex = languageDropdown.index;
                if (newIndex >= 0 && newIndex < languageCodes.Length)
                {
                    languageProp.stringValue = languageCodes[newIndex];
                    serializedObject.ApplyModifiedProperties();
                }
            });
            
            languageContainer.Add(languageDropdown);
            fieldsContainer.Add(languageContainer);

            // Title field
            var titleLabel = new Label("Title");
            titleLabel.AddToClassList("text-field-label");
            fieldsContainer.Add(titleLabel);
            
            var titleField = new TextField();
            titleField.AddToClassList("title-field");
            titleField.value = titleProp.stringValue;
            
            // Register for input changes (includes paste operations)
            titleField.RegisterValueChangedCallback(evt =>
            {
                titleProp.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            });
            
            // Also keep focus out event as backup
            titleField.RegisterCallback<FocusOutEvent>(evt =>
            {
                titleProp.stringValue = titleField.value;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            });
            
            fieldsContainer.Add(titleField);

            // Text field
            var textLabel = new Label("Text");
            textLabel.AddToClassList("text-field-label");
            fieldsContainer.Add(textLabel);
            
            var textField = new TextField();
            textField.AddToClassList("text-area");
            textField.multiline = true;
            textField.value = textProp.stringValue;
            
            // Register for input changes (includes paste operations)
            textField.RegisterValueChangedCallback(evt =>
            {
                textProp.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            });
            
            // Also keep focus out event as backup
            textField.RegisterCallback<FocusOutEvent>(evt =>
            {
                textProp.stringValue = textField.value;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            });
            
            fieldsContainer.Add(textField);

            contentContainer.Add(fieldsContainer);

            // Remove button
            var removeButton = new Button(() => RemoveLanguageItem(index));
            removeButton.text = "Remove";
            removeButton.AddToClassList("remove-button");
            contentContainer.Add(removeButton);

            container.Add(contentContainer);
            return container;
        }

        private void RemoveLanguageItem(int index)
        {
            if (index >= 0 && index < localizedTextsProperty.arraySize)
            {
                localizedTextsProperty.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
                
                // Update the count and refresh the list
                lastLocalizedTextsCount = localizedTextsProperty.arraySize;
                UpdateLocalizedTextsList();
            }
        }
        
        // Helper method to get default language
        private string GetDefaultLanguage()
        {
            // Try to find language configuration to get default language
            string[] guids = AssetDatabase.FindAssets("t:LanguageConfiguration");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var config = AssetDatabase.LoadAssetAtPath<LanguageConfiguration>(path);
                if (config != null && !string.IsNullOrEmpty(config.defaultLanguage))
                {
                    return config.defaultLanguage;
                }
            }

            // Default to English if no configuration found
            return "en";
        }
        
        private void UpdateLevelsLanguages(LevelGroup group)
        {
            if (group == null || group.levels == null || group.levels.Count == 0)
            {
                EditorUtility.DisplayDialog("No Levels", "This group has no levels to update.", "OK");
                return;
            }

            if (group.localizedTexts == null || group.localizedTexts.Count == 0)
            {
                EditorUtility.DisplayDialog("No Languages", 
                    "This group has no languages defined. Add languages to the group first.", "OK");
                return;
            }

            int updatedLevelCount = 0;
            Dictionary<string, int> languageUpdateCounts = new Dictionary<string, int>();
            var modelController = EditorScope.Resolve<IModelController>();
            EditorUtility.DisplayProgressBar("Updating Languages", "Processing levels...", 0f);

            try
            {
                for (int i = 0; i < group.levels.Count; i++)
                {
                    var level = group.levels[i];
                    if (level == null) continue;

                    float progress = (float)i / group.levels.Count;
                    EditorUtility.DisplayProgressBar("Updating Languages", 
                        $"Processing level {i + 1} of {group.levels.Count}...", progress);

                    bool levelUpdated = false;
                    HashSet<string> existingLangs = new HashSet<string>(level.languages.Select(l => l.language));

                    foreach (var localizedText in group.localizedTexts)
                    {
                        if (!existingLangs.Contains(localizedText.language))
                        {
                            level.AddLanguage(localizedText.language);
                            
                            // Generate words for the new language
                            var languageData = level.GetLanguageData(localizedText.language);
                            if (languageData != null)
                            {
                                // Generate random word for letters
                                var words = modelController.GetWordsWithLength(level.letters, localizedText.language);
                                string letters = words.Count > 0 ? words[0] : "";
                                
                                if (!string.IsNullOrEmpty(letters))
                                {
                                    languageData.letters = letters;
                                    
                                    // Generate words from the letters
                                    var generatedWords = modelController.GetWordsFromSymbols(letters, localizedText.language);
                                    if (generatedWords != null && generatedWords.Count() > 0)
                                    {
                                        languageData.words = generatedWords.ToArray();
                                    }
                                }

                                if (!languageUpdateCounts.ContainsKey(localizedText.language))
                                    languageUpdateCounts[localizedText.language] = 0;
                                languageUpdateCounts[localizedText.language]++;
                                
                                levelUpdated = true;
                            }
                        }
                    }

                    if (levelUpdated)
                    {
                        EditorUtility.SetDirty(level);
                        updatedLevelCount++;
                    }
                }

                if (updatedLevelCount > 0)
                {
                    AssetDatabase.SaveAssets();
                    string updateDetails = string.Join("\n",
                        languageUpdateCounts.Select(kvp => $"- {kvp.Key}: {kvp.Value} levels"));
                    string message = $"Updated {updatedLevelCount} levels:\n{updateDetails}";
                    EditorUtility.DisplayDialog("Update Complete", message, "OK");
                    Debug.Log($"[{group.groupName}] {message}");
                }
                else
                {
                    EditorUtility.DisplayDialog("No Changes", 
                        "All levels already have all group languages.", "OK");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private int lastLocalizedTextsCount = -1;

        public void RefreshUI()
        {
            if (rootContainer != null)
            {
                // Schedule updates for next frame to ensure properties are current
                rootContainer.schedule.Execute(() =>
                {
                    // Only update the list if the count has changed
                    if (localizedTextsProperty.arraySize != lastLocalizedTextsCount)
                    {
                        UpdateLocalizedTextsList();
                        lastLocalizedTextsCount = localizedTextsProperty.arraySize;
                    }
                    UpdateBackgroundPreview();
                });
            }
        }
    }
}