using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.Enums;
using System.Globalization;

namespace WordsToolkit.Scripts.Levels.Editor.EditorWindows
{
    public static class LevelManagerWindowUI
    {
        public static void InitializeUI(LevelManagerWindow window)
        {
            // Load and apply stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/WordConnectGameToolkit/Scripts/Levels/Editor/LevelManagerStyles.uss");
            if (styleSheet != null)
                window.rootVisualElement.styleSheets.Add(styleSheet);

            CreateBasicLayout(window);
        }

        private static void CreateBasicLayout(LevelManagerWindow window)
        {
            // Split view layout - making right panel (now first) narrower at 300px, left panel (now second) wider
            var splitView = new TwoPaneSplitView(0, 400, TwoPaneSplitViewOrientation.Horizontal);
            splitView.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            var toolbar = CreateLeftPanelToolbar(window);
            window.rootVisualElement.Add(toolbar);
            window.rootVisualElement.Add(splitView);
            // Create left panel
            var leftPanel = CreateLeftPanel(window);
            
            // Create right panel
            var rightPanel = CreateRightPanel(window);
            
            // Add panels in switched order - right panel first, then left panel
            splitView.Add(rightPanel);
            splitView.Add(leftPanel);
        }

        private static VisualElement CreateLeftPanel(LevelManagerWindow window)
        {
            var leftPanel = new VisualElement();
            leftPanel.style.flexGrow = 1;
            leftPanel.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            leftPanel.AddToClassList("left-panel");
            var treeViewContainer = CreateTreeViewContainer(window);
            leftPanel.Add(treeViewContainer);

            return leftPanel;
        }

        private static Toolbar CreateLeftPanelToolbar(LevelManagerWindow window)
        {
            var toolbar = new Toolbar();
            toolbar.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.flexWrap = Wrap.NoWrap;
            toolbar.AddToClassList("toolbar");

            var refreshButton = new ToolbarButton(() =>
            {
                LevelEditorUtility.RefreshHierarchy(window.HierarchyTree);

            }) { text = "Refresh" };
            refreshButton.style.width = 60;
            refreshButton.AddToClassList("toolbar-button");

            var createButton = new ToolbarMenu { text = " Create" };
            createButton.AddToClassList("toolbar-menu");

            // Populate Create menu
            createButton.menu.AppendAction("New Root Group", _ =>
            {
                window.HierarchyTree?.CreateRootGroup();
                window.HierarchyTree?.Reload();
            });
            
            // New Subgroup - only enabled when a group is selected
            createButton.menu.AppendAction("New Subgroup", _ =>
            {
                var sel = window.SelectedItem;
                if (sel != null && sel.type == LevelHierarchyItem.ItemType.Group)
                {
                    window.HierarchyTree?.OnCreateSubgroup?.Invoke(sel);
                    window.HierarchyTree?.Reload();
                }
            }, _ => 
                window.SelectedItem != null && window.SelectedItem.type == LevelHierarchyItem.ItemType.Group 
                ? DropdownMenuAction.Status.Normal 
                : DropdownMenuAction.Status.Disabled);
            
            // New Level - only enabled when a group is selected
            createButton.menu.AppendAction("New Level", _ =>
            {
                var sel = window.SelectedItem;
                if (sel != null && sel.type == LevelHierarchyItem.ItemType.Group)
                {
                    window.HierarchyTree?.OnCreateLevel?.Invoke(sel);
                    window.HierarchyTree?.Reload();
                }
            }, _ =>
                window.SelectedItem != null && window.SelectedItem.type == LevelHierarchyItem.ItemType.Group 
                ? DropdownMenuAction.Status.Normal 
                : DropdownMenuAction.Status.Disabled);
            var deleteButton = new ToolbarButton(() => {
                if (window.SelectedItem != null)
                {
                    switch (window.SelectedItem.type)
                    {
                        case LevelHierarchyItem.ItemType.Group:
                            if (EditorUtility.DisplayDialog("Delete Group",
                                "Are you sure you want to delete this group and all its levels?", "Yes", "No"))
                            {
                                LevelEditorUtility.DeleteGroup(window.SelectedItem.groupAsset);
                                window.HierarchyTree?.Reload();
                            }
                            break;

                        case LevelHierarchyItem.ItemType.Level:
                            if (EditorUtility.DisplayDialog("Delete Level",
                                "Are you sure you want to delete this level?", "Yes", "No"))
                            {
                                LevelEditorUtility.DeleteLevel(window.SelectedItem.levelAsset);
                                window.HierarchyTree?.Reload();
                            }
                            break;
                    }
                }
            }) { text = "Delete" };
            deleteButton.AddToClassList("toolbar-button");

            var languageConfigButton = new ToolbarButton(() => {
                var languageConfig = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/WordConnectGameToolkit/Resources/Settings/LanguageConfiguration.asset");
                if (languageConfig != null)
                {
                    Selection.activeObject = languageConfig;
                    
                    // Focus the Inspector window to make it active
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
            }) { text = "Language Config" };
            languageConfigButton.AddToClassList("toolbar-button");
            
            var bannedWordsButton = CreateBannedWordsButton();
            bannedWordsButton.AddToClassList("toolbar-button");

            var deleteAllButton = new ToolbarButton(() => {
                if (EditorUtility.DisplayDialog("Delete All Levels and Groups",
                    "Are you sure you want to delete ALL levels and groups? This action cannot be undone!",
                    "Yes, Delete Everything", "Cancel"))
                {
                    // Find and delete all level groups
                    string[] groupGuids = AssetDatabase.FindAssets("t:LevelGroup");
                    foreach (string guid in groupGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        AssetDatabase.DeleteAsset(path);
                    }

                    // Find and delete all levels
                    string[] levelGuids = AssetDatabase.FindAssets("t:Level");
                    foreach (string guid in levelGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        AssetDatabase.DeleteAsset(path);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    window.HierarchyTree?.Reload();
                    EditorUtility.DisplayDialog("Deletion Complete", 
                        "All levels and groups have been deleted.", "OK");
                }
            }) { text = "Delete All Levels" };
            deleteAllButton.AddToClassList("toolbar-button");

            toolbar.Add(refreshButton);
            // toolbar.Add(createButton);
            // toolbar.Add(deleteButton);
            toolbar.Add(languageConfigButton);
            toolbar.Add(bannedWordsButton);
            toolbar.Add(deleteAllButton);

            return toolbar;
        }

        private static ToolbarButton CreateBannedWordsButton()
        {
            return new ToolbarButton(() => {
                var bannedWords = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/WordConnectGameToolkit/Resources/BannedWords/BannedWords.asset");
                if (bannedWords != null)
                {
                    Selection.activeObject = bannedWords;
                    
                    // Focus the Inspector window to make it active
                    var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
                    if (inspectorType != null)
                    {
                        var inspectorWindow = EditorWindow.GetWindow(inspectorType);
                        if (inspectorWindow != null)
                        {
                            inspectorWindow.Focus();
                        }
                    }
                    
                    EditorGUIUtility.PingObject(bannedWords);
                }
            })
            {
                text = "Banned Words"
            };
        }

        private static IMGUIContainer CreateTreeViewContainer(LevelManagerWindow window)
        {
            return new IMGUIContainer(() =>
            {
                if (window.HierarchyTree != null)
                {
                    Rect rect = EditorGUILayout.GetControlRect(
                        false,
                        GUILayout.ExpandHeight(true),
                        GUILayout.ExpandWidth(true)
                    );
                    window.HierarchyTree.OnGUI(rect);
                }
            })
            {
                style = { flexGrow = 1 }
            };
        }

        private static VisualElement CreateRightPanel(LevelManagerWindow window)
        {
            window.m_RightPanel = new VisualElement();
            window.m_RightPanel.AddToClassList("right-panel");

            var levelControlPanel = CreateLevelControlPanel(window);
            window.m_RightPanel.Add(levelControlPanel);

            window.m_ActionButtonsContainer = new VisualElement();
            window.m_ActionButtonsContainer.AddToClassList("action-buttons-container");
            window.m_RightPanel.Add(window.m_ActionButtonsContainer);

            // Create inspector container for UIToolkit content
            window.m_InspectorContainer = new VisualElement();
            window.m_InspectorContainer.AddToClassList("inspector-container");
            window.m_InspectorContainer.style.flexGrow = 1;
            window.m_InspectorContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            window.m_RightPanel.Add(window.m_InspectorContainer);

            return window.m_RightPanel;
        }

        private static VisualElement CreateLevelControlPanel(LevelManagerWindow window)
        {
            var levelControlPanel = new VisualElement();
            levelControlPanel.style.flexDirection = FlexDirection.Row;
            levelControlPanel.style.alignItems = Align.Center;
            levelControlPanel.style.justifyContent = Justify.FlexStart;
            levelControlPanel.AddToClassList("header-panel");

            // Create navigation controls
            var prevButton = new Button(() => NavigateLevel(window, -1)) { text = "<" };
            prevButton.AddToClassList("nav-button");

            // Create level title container
            window.LevelTitleContainer = new VisualElement();
            window.LevelTitleContainer.style.flexDirection = FlexDirection.Row;
            window.LevelTitleContainer.style.alignItems = Align.Center;

            var levelLabel = new Label("Level");
            levelLabel.style.marginRight = 5;
            window.LevelTitleContainer.Add(levelLabel);

            window.LevelNumberField = new TextField();
            window.LevelNumberField.style.width = 40;
            window.LevelNumberField.style.marginLeft = 3;
            window.LevelNumberField.style.marginRight = 3;
            window.LevelNumberField.style.paddingLeft = 5;
            window.LevelNumberField.style.paddingRight = 5;
            window.LevelNumberField.style.paddingTop = 0;
            window.LevelNumberField.style.paddingBottom = 0;
            window.LevelNumberField.style.unityTextAlign = TextAnchor.MiddleCenter;
            window.LevelNumberField.isDelayed = true;
            window.LevelNumberField.RegisterValueChangedCallback(evt => {
                if (int.TryParse(evt.newValue, out int levelNum))
                {
                    JumpToLevel(window, levelNum);
                }
            });
            window.LevelTitleContainer.Add(window.LevelNumberField);

            window.HeaderLabel = new Label("No Selection");
            window.HeaderLabel.AddToClassList("header-label");
            window.HeaderLabel.style.fontSize = 14;
            window.HeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            window.HeaderLabel.style.flexGrow = 1;

            var nextButton = new Button(() => NavigateLevel(window, 1)) { text = ">" };
            nextButton.AddToClassList("nav-button");

            var addButton = new Button(() => AddLevel(window)) { text = "+" };
            addButton.AddToClassList("nav-button");

            var removeButton = new Button(() => RemoveLevel(window)) { text = "-" };
            removeButton.AddToClassList("nav-button");

            // Open Grid button
            var openGridButton = new Button(() => {
                var selectedItem = window.SelectedItem;
                if (selectedItem?.levelAsset != null)
                {
                    // Get language code using the static utility method
                    string languageCode = LevelEditorUtility.GetLanguageCodeForLevel(selectedItem.levelAsset);
                    if (!string.IsNullOrEmpty(languageCode))
                    {
                        // Open the CrosswordGridWindow
                        WordsToolkit.Scripts.Levels.Editor.CrosswordGridWindow.ShowWindow(selectedItem.levelAsset, languageCode);
                    }
                }
            }) { text = "Open Grid" };
            openGridButton.style.flexGrow = 1;
            openGridButton.style.backgroundColor = new Color(112 / 255f, 63 / 255f, 33 / 255f, 1f);
            openGridButton.style.height = 30;
            openGridButton.style.marginRight = 5;
            // // Test level button
            // var testButton = new Button(() => {
            //     var selectedItem = window.SelectedItem;
            //     if (selectedItem?.levelAsset != null)
            //     {
            //         // Get currently selected language tab from LevelDataEditor
            //         int selectedTabIndex = EditorPrefs.GetInt("WordsToolkit_SelectedLanguageTab", 0);
            //         string languageCode = selectedItem.levelAsset.languages[selectedTabIndex].language;
            //
            //         // Use the encapsulated TestLevel method
            //         LevelEditorServices.TestLevel(selectedItem.levelAsset, languageCode);
            //     }
            // }) { text = "Test level" };

            // Add all controls to panel
            levelControlPanel.Add(window.HeaderLabel); // Move header label to first position
            levelControlPanel.Add(prevButton);
            levelControlPanel.Add(window.LevelTitleContainer);
            levelControlPanel.Add(nextButton);
            levelControlPanel.Add(addButton);
            levelControlPanel.Add(removeButton);
            levelControlPanel.Add(openGridButton);
            // levelControlPanel.Add(testButton);

            return levelControlPanel;
        }

        private static void NavigateLevel(LevelManagerWindow window, int direction)
        {
            if (window.SelectedItem == null || window.SelectedItem.type == LevelHierarchyItem.ItemType.Group)
                return;

            var level = window.SelectedItem.levelAsset;
            if (level == null) return;

            var targetIndex = level.number + direction;
            var allLevels = Resources.LoadAll<Level>("Levels").ToList();
            var targetLevel = allLevels.FirstOrDefault(l => l.number == targetIndex);
            if (targetLevel != null)
            {
                window.HierarchyTree.SelectAsset(targetLevel);
                window.LevelNumberField.value = targetLevel.number.ToString();
                
                // Get the selected item and trigger the selection changed event
                var selectedItem = window.HierarchyTree.GetSelectedItem();
                if (selectedItem != null)
                {
                    // Update the window's internal state and trigger events
                    window.TriggerSelectionChanged(selectedItem);
                }
            }
            else
            {
                // Show error and reset field
                if (window.SelectedItem.type == LevelHierarchyItem.ItemType.Level)
                {
                    window.LevelNumberField.value = window.SelectedItem.levelAsset.number.ToString();
                }
                else
                {
                    window.LevelNumberField.value = "1";
                }

                EditorUtility.DisplayDialog("Invalid Level Number",
                    $"Level {targetIndex} not found in this group.", "OK");
            }
        }

        private static void JumpToLevel(LevelManagerWindow window, int levelNum)
        {
            if (window.SelectedItem == null) return;

            var allLevels = Resources.LoadAll<Level>("Levels").ToList();

            // Find the level with the given number
            var targetLevel = allLevels.FirstOrDefault(l => l.number == levelNum);

            // If level not found, show error and reset field
            if (targetLevel == null)
            {
                // Show error and reset field to current level
                if (window.SelectedItem.type == LevelHierarchyItem.ItemType.Level)
                {
                    window.LevelNumberField.value = window.SelectedItem.levelAsset.number.ToString();
                }
                else
                {
                    window.LevelNumberField.value = "1";
                }

                EditorUtility.DisplayDialog("Invalid Level Number",
                    $"Level {levelNum} not found in this group.", "OK");
                return;
            }

            // Level found, select it
            if (targetLevel != null)
            {
                window.HierarchyTree.SelectAsset(targetLevel);
                
                // Get the selected item and trigger the selection changed event
                var selectedItem = window.HierarchyTree.GetSelectedItem();
                if (selectedItem != null)
                {
                    // Update the window's internal state and trigger events
                    window.TriggerSelectionChanged(selectedItem);
                }
            }
        }

        private static void AddLevel(LevelManagerWindow window)
        {
            if (window.SelectedItem == null) return;

            // If a level is selected, get its parent group
            LevelGroup parentGroup = null;
            if (window.SelectedItem.type == LevelHierarchyItem.ItemType.Level && window.SelectedItem.levelAsset != null)
            {
                parentGroup = FindParentGroup(window.SelectedItem.levelAsset);
            }
            // If a group is selected, use it directly
            else if (window.SelectedItem.type == LevelHierarchyItem.ItemType.Group && window.SelectedItem.groupAsset != null)
            {
                parentGroup = window.SelectedItem.groupAsset;
            }

            if (parentGroup != null)
            {
                var levelItem = new LevelHierarchyItem
                {
                    type = LevelHierarchyItem.ItemType.Group,
                    groupAsset = parentGroup
                };
                window.HierarchyTree.OnCreateLevel?.Invoke(levelItem);
            }
        }

        private static void RemoveLevel(LevelManagerWindow window)
        {
            if (window.SelectedItem == null || 
                window.SelectedItem.type != LevelHierarchyItem.ItemType.Level || 
                window.SelectedItem.levelAsset == null) 
                return;

            var level = window.SelectedItem.levelAsset;
            var parentGroup = FindParentGroup(level);

            if (parentGroup != null)
            {
                // Get the level index before deletion
                int levelIndex = -1;
                if (parentGroup.levels != null)
                {
                    levelIndex = parentGroup.levels.IndexOf(level);
                }

                // Show confirmation dialog
                if (EditorUtility.DisplayDialog("Delete Level",
                    $"Are you sure you want to delete Level {level.number }?",
                    "Delete", "Cancel"))
                {
                    // Remove from parent group
                    if (parentGroup.levels != null)
                    {
                        parentGroup.levels.Remove(level);
                        EditorUtility.SetDirty(parentGroup);
                    }

                    // Delete the asset
                    string assetPath = AssetDatabase.GetAssetPath(level);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }

                    // Save changes and notify about hierarchy change
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    // Notify about the change in hierarchy
                    window.HierarchyTree?.OnHierarchyChanged?.Invoke();
                    window.HierarchyTree?.Reload();

                    // Try to select another level or the parent group
                    if (parentGroup.levels != null && parentGroup.levels.Count > 0)
                    {
                        // Try to select the same index or the previous one
                        int newIndex = Mathf.Min(levelIndex, parentGroup.levels.Count - 1);
                        if (newIndex >= 0 && newIndex < parentGroup.levels.Count)
                        {
                            window.HierarchyTree.SelectAsset(parentGroup.levels[newIndex]);
                        }
                    }
                    else
                    {
                        // If no levels left, select the group
                        window.HierarchyTree.SelectAsset(parentGroup);
                    }
                }
            }
        }

        private static void AddCollectionButtons(VisualElement buttonContainer, LevelHierarchyItem item)
        {
            var addGroupButton = new Button(() =>
            {
                var newGroup = ScriptableObject.CreateInstance<LevelGroup>();
                newGroup.name = "New Group";
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{item.folderPath}/NewGroup.asset");
                AssetDatabase.CreateAsset(newGroup, assetPath);
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(newGroup);
            })
            {
                text = "Add Group"
            };
            addGroupButton.AddToClassList("action-button");
            buttonContainer.Add(addGroupButton);
        }

        private static void AddLevelButtons(VisualElement buttonContainer, LevelHierarchyItem item)
        {
            // Test button has been moved to the level control panel, so this method is now empty
            // but kept for potential future level-specific buttons
        }

        public static void UpdateInspector(LevelManagerWindow window, LevelHierarchyItem selectedItem)
        {
            // Clear previous editor state
            window.ActionButtonsContainer.Clear();
            window.InspectorContainer.Clear();
            if (window.cachedEditor != null)
            {
                Object.DestroyImmediate(window.cachedEditor);
                window.cachedEditor = null;
            }

            UpdateHeaderDisplay(window, selectedItem);
            CreateActionButtonsForSelection(window, selectedItem);
            PopulateInspectorContainer(window, selectedItem);

            window.Repaint();
        }

        private static void PopulateInspectorContainer(LevelManagerWindow window, LevelHierarchyItem selectedItem)
        {
            if (selectedItem == null)
            {
                var helpBox = new HelpBox("Select an item from the hierarchy to edit its properties", HelpBoxMessageType.Info);
                window.InspectorContainer.Add(helpBox);
                
                var spacer = new VisualElement { style = { height = 10 } };
                window.InspectorContainer.Add(spacer);
                
                var createButton = new Button(() =>
                {
                    window.HierarchyTree?.CreateRootGroup();
                    window.HierarchyTree?.Reload();
                })
                {
                    text = "Create New Group"
                };
                createButton.style.height = 30;
                window.InspectorContainer.Add(createButton);
                return;
            }

            switch (selectedItem.type)
            {
                case LevelHierarchyItem.ItemType.Collection:
                    CreateCollectionInspectorUI(window, selectedItem);
                    break;

                case LevelHierarchyItem.ItemType.Group:
                    CreateGroupInspectorUI(window, selectedItem);
                    break;

                case LevelHierarchyItem.ItemType.Level:
                    CreateLevelInspectorUI(window, selectedItem);
                    break;

                default:
                    var unknownLabel = new Label("Unknown item type");
                    window.InspectorContainer.Add(unknownLabel);
                    break;
            }
        }

        private static void CreateCollectionInspectorUI(LevelManagerWindow window, LevelHierarchyItem item)
        {
            var titleLabel = new Label($"Collection Folder: {item.folderPath}");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            window.InspectorContainer.Add(titleLabel);

            var spacer = new VisualElement { style = { height = 10 } };
            window.InspectorContainer.Add(spacer);

            var stats = CollectionStats.GetCollectionStats(item.folderPath);

            var statsContainer = new VisualElement();
            statsContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            statsContainer.style.paddingTop = 10;
            statsContainer.style.paddingBottom = 10;
            statsContainer.style.paddingLeft = 10;
            statsContainer.style.paddingRight = 10;
            statsContainer.style.marginTop = 5;
            statsContainer.style.marginBottom = 5;

            var groupCountLabel = new Label($"Total Groups: {stats.groupCount}");
            groupCountLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statsContainer.Add(groupCountLabel);

            var levelCountLabel = new Label($"Total Levels: {stats.levelCount}");
            levelCountLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            statsContainer.Add(levelCountLabel);

            window.InspectorContainer.Add(statsContainer);
        }

        private static void CreateGroupInspectorUI(LevelManagerWindow window, LevelHierarchyItem item)
        {
            if (item.groupAsset == null) return;

            UnityEditor.Editor.CreateCachedEditor(item.groupAsset, null, ref window.cachedEditor);
            if (window.cachedEditor != null)
            {
                // Check if the editor supports UI Toolkit (like our new LevelGroupEditor)
                if (window.cachedEditor is LevelGroupEditor levelGroupEditor)
                {
                    // Use the UI Toolkit CreateInspectorGUI method
                    var inspectorUI = levelGroupEditor.CreateInspectorGUI();
                    if (inspectorUI != null)
                    {
                        // Configure the inspector UI to fit the window
                        inspectorUI.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                        inspectorUI.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
                        inspectorUI.style.flexGrow = 1;
                        inspectorUI.style.flexShrink = 1;

                        // Add directly to container without additional scroll view wrapper
                        window.InspectorContainer.Add(inspectorUI);
                        
                        // Refresh the UI to ensure it's up to date
                        levelGroupEditor.RefreshUI();
                    }
                }
            }
        }

        private static void CreateLevelInspectorUI(LevelManagerWindow window, LevelHierarchyItem item)
        {
            if (item.levelAsset == null) return;

            UnityEditor.Editor.CreateCachedEditor(item.levelAsset, null, ref window.cachedEditor);
            if (window.cachedEditor != null)
            {
                // Check if the editor is a LevelDataEditor and supports UIToolkit
                if (window.cachedEditor is LevelDataEditor levelDataEditor)
                {
                    // Use the UIToolkit CreateInspectorGUI method
                    var inspectorUI = levelDataEditor.CreateInspectorGUI();
                    if (inspectorUI != null)
                    {
                        // Configure the inspector UI to fit the window
                        inspectorUI.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                        inspectorUI.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
                        inspectorUI.style.flexGrow = 1;
                        inspectorUI.style.flexShrink = 1;

                        // Ensure container is clear before adding (safety check)
                        if (window.InspectorContainer.childCount > 0)
                        {
                            window.InspectorContainer.Clear();
                        }

                        // Add directly to container without additional scroll view wrapper
                        window.InspectorContainer.Add(inspectorUI);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Failed to create cached editor for level asset");
            }
        }

        private static void CreateActionButtonsForSelection(LevelManagerWindow window, LevelHierarchyItem selectedItem)
        {
            if (selectedItem == null) return;

            var buttonContainer = new VisualElement();
            buttonContainer.AddToClassList("button-row");

            switch (selectedItem.type)
            {
                case LevelHierarchyItem.ItemType.Collection:
                    AddCollectionButtons(buttonContainer, selectedItem);
                    break;

                case LevelHierarchyItem.ItemType.Level:
                    AddLevelButtons(buttonContainer, selectedItem);
                    break;
            }

            window.ActionButtonsContainer.Add(buttonContainer);
        }

        private static void UpdateHeaderDisplay(LevelManagerWindow window, LevelHierarchyItem selectedItem)
        {
            var levelControlPanel = window.m_RightPanel.Q<VisualElement>(className: "header-panel");

            if (selectedItem?.type == LevelHierarchyItem.ItemType.Group)
            {
                // Show group name in header, hide navigation
                window.LevelTitleContainer.style.display = DisplayStyle.None;
                window.HeaderLabel.style.display = DisplayStyle.Flex;
                window.HeaderLabel.text = selectedItem.groupAsset != null ? selectedItem.groupAsset.groupName : "No Group Name";

                // Hide navigation buttons
                foreach (var button in levelControlPanel.Children().Where(c => c.ClassListContains("nav-button")))
                {
                    button.style.display = DisplayStyle.None;
                }
                
                // Hide OpenGrid and test level buttons
                foreach (var button in levelControlPanel.Children().OfType<Button>())
                {
                    if (button.text == "Open Grid" || button.text == "Test level")
                    {
                        button.style.display = DisplayStyle.None;
                    }
                }
                return;
            }

            // Show navigation for levels and other types
            window.HeaderLabel.style.display = DisplayStyle.None;
            foreach (var button in levelControlPanel.Children().Where(c => c.ClassListContains("nav-button")))
            {
                button.style.display = DisplayStyle.Flex;
            }
            
            // Show OpenGrid and test level buttons for levels
            foreach (var button in levelControlPanel.Children().OfType<Button>())
            {
                if (button.text == "Open Grid" || button.text == "Test level")
                {
                    button.style.display = DisplayStyle.Flex;
                }
            }

            if (selectedItem?.type == LevelHierarchyItem.ItemType.Level && selectedItem.levelAsset != null)
            {
                // Use the level's internal number property
                var level = selectedItem.levelAsset;
                int levelNumber = level.number;

                // Show level title container
                window.LevelTitleContainer.style.display = DisplayStyle.Flex;
                window.LevelNumberField.value = levelNumber.ToString();
            }
            else
            {
                window.LevelTitleContainer.style.display = DisplayStyle.None;
                window.HeaderLabel.style.display = DisplayStyle.Flex;
                window.HeaderLabel.text = selectedItem?.displayName ?? "No Selection";
            }
        }

        private static LevelGroup FindParentGroup(Level level)
        {
            if (level == null) return null;
            
            // Search for the level in all groups
            string[] groupGuids = AssetDatabase.FindAssets("t:LevelGroup");
            foreach (string guid in groupGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelGroup group = AssetDatabase.LoadAssetAtPath<LevelGroup>(path);
                
                if (group != null && group.levels != null && group.levels.Contains(level))
                {
                    return group;
                }
            }
            
            return null;
        }
    }
}
