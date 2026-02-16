using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using WordsToolkit.Scripts.NLP;

using System;
using Object = UnityEngine.Object;
using WordsToolkit.Scripts.Levels.Editor.EditorWindows;

namespace WordsToolkit.Scripts.Levels.Editor
{
    
    public class LevelHierarchyTreeView : TreeView
    {
        private IModelController ModelController => EditorScope.Resolve<IModelController>();
        
        // Event for selection changes
        public global::System.Action<LevelHierarchyItem> OnSelectionChanged;

        // Event for delete request
        public global::System.Action<LevelHierarchyItem> OnDeleteItem;

        // Event for creating subgroups
        public global::System.Action<LevelHierarchyItem> OnCreateSubgroup;

        // Event for creating levels
        public global::System.Action<LevelHierarchyItem> OnCreateLevel;
        // Event fired when hierarchy changes (e.g., drag & drop)
        public global::System.Action OnHierarchyChanged;

        // Dictionary to map tree IDs to hierarchy items
        private Dictionary<int, LevelHierarchyItem> m_IdToItem = new Dictionary<int, LevelHierarchyItem>();

        // Root items for groups directly (removed the collection level)
        private List<LevelHierarchyItem> m_RootItems = new List<LevelHierarchyItem>();

        // Column headers
        private enum ColumnId
        {
            Name
        }

        public LevelHierarchyTreeView(TreeViewState state)
            : base(state)
        {
            // Single-click selection
            showBorder = true;
            showAlternatingRowBackgrounds = true;

            // Configure columns
            Reload();
        }

        public LevelHierarchyTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader)
            : base(state, multiColumnHeader)
        {
            // Single-click selection
            showBorder = true;
            showAlternatingRowBackgrounds = true;

            // Configure columns
            // multiColumnHeader.sortingChanged += OnSortingChanged;

            // Show Name column only
            multiColumnHeader.state.visibleColumns = new int[] { 0 }; 

            Reload();
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 100,
                    minWidth = 50,
                    autoResize = true,
                    allowToggleVisibility = false
                }
            };

            return new MultiColumnHeaderState(columns);
        }

        protected override TreeViewItem BuildRoot()
        {
            // Clear existing data
            m_IdToItem.Clear();
            m_RootItems.Clear();

            // Create root item (not visible in the tree)
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

            // Find all level groups and build the hierarchy
            DiscoverLevelGroups();

            // Only add top-level items to the root
            var allItems = new List<TreeViewItem>();

            // Add root items directly to the root as children
            if (m_RootItems.Count > 0)
            {
                root.children = new List<TreeViewItem>(m_RootItems);
            }
            else
            {
                root.children = new List<TreeViewItem>(); // Empty list to avoid null ref
                Debug.Log("BuildRoot: No root items found");
            }

            return root;
        }

        private void DiscoverLevelGroups()
        {
            // Clear existing data to prevent duplicates
            m_IdToItem.Clear();
            m_RootItems.Clear();

            // Track processed levels to avoid duplicates
            var processedLevelIds = new HashSet<int>();
            int itemId = 1; // Start IDs from 1 (0 is used for invisible root)

            // First: Find all LevelGroup assets and create their hierarchy items
            string[] groupGuids = AssetDatabase.FindAssets("t:LevelGroup");

            Dictionary<LevelGroup, LevelHierarchyItem> groupToItem = new Dictionary<LevelGroup, LevelHierarchyItem>();

            // Create all group items
            foreach (string groupGuid in groupGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(groupGuid);
                LevelGroup group = AssetDatabase.LoadAssetAtPath<LevelGroup>(path);

                if (group != null)
                {
                    var groupItem = new LevelHierarchyItem
                    {
                        id = itemId++,
                        displayName = string.IsNullOrEmpty(group.groupName) ?
                            Path.GetFileNameWithoutExtension(path) : group.groupName,
                        depth = 0,
                        type = LevelHierarchyItem.ItemType.Group,
                        groupAsset = group,
                        assetPath = path,
                        icon = EditorGUIUtility.FindTexture("ScriptableObject Icon"),
                        children = new List<TreeViewItem>()
                    };

                    m_IdToItem[groupItem.id] = groupItem;
                    groupToItem[group] = groupItem;
                }
            }

            // Setup group hierarchy
            foreach (var pair in groupToItem)
            {
                LevelGroup group = pair.Key;
                LevelHierarchyItem item = pair.Value;

                if (group.parentGroup != null && groupToItem.TryGetValue(group.parentGroup, out LevelHierarchyItem parentItem))
                {
                    if (parentItem.children == null)
                        parentItem.children = new List<TreeViewItem>();
                    parentItem.children.Add(item);
                }
                else
                {
                    m_RootItems.Add(item);
                }
            }

            // Set proper depth for groups
            foreach (var rootItem in m_RootItems)
            {
                rootItem.depth = 0;
                SetChildrenDepth(rootItem, 1);
            }

            // Add levels to their groups
            foreach (var pair in groupToItem)
            {
                LevelGroup group = pair.Key;
                LevelHierarchyItem groupItem = pair.Value;

                if (group.levels != null && group.levels.Count > 0)
                {
                    var validLevels = group.levels
                        .Where(l => l != null)
                        .OrderBy(l => l.number)
                        .ToList();

                    foreach (var level in validLevels)
                    {
                        int levelId = level.GetInstanceID();
                        if (processedLevelIds.Contains(levelId))
                            continue;

                        processedLevelIds.Add(levelId);
                        string levelPath = AssetDatabase.GetAssetPath(level);

                        var levelItem = new LevelHierarchyItem
                        {
                            id = itemId++,
                            displayName = $"Level {level.number}",
                            depth = groupItem.depth + 1,
                            type = LevelHierarchyItem.ItemType.Level,
                            levelAsset = level,
                            assetPath = levelPath,
                            icon = EditorGUIUtility.FindTexture("ScriptableObject Icon")
                        };

                        m_IdToItem[levelItem.id] = levelItem;
                        if (groupItem.children == null)
                            groupItem.children = new List<TreeViewItem>();

                        groupItem.children.Add(levelItem);
                    }
                }
                else
                {
                    Debug.Log($"No levels found in group: {group.groupName}");
                }
            }

            // Update group display names to include level count
            foreach (var pair in groupToItem)
            {
                LevelGroup group = pair.Key;
                LevelHierarchyItem groupItem = pair.Value;
                
                int levelCount = groupItem.children?.Count ?? 0;
                string baseName = string.IsNullOrEmpty(group.groupName) ?
                    Path.GetFileNameWithoutExtension(groupItem.assetPath) : group.groupName;
                
                groupItem.displayName = $"{baseName} ({levelCount})";
            }

            // Find and add standalone levels (levels not in any group)
            string[] levelGuids = AssetDatabase.FindAssets("t:Level");
            foreach (string levelGuid in levelGuids)
            {
                string levelPath = AssetDatabase.GUIDToAssetPath(levelGuid);
                Level level = AssetDatabase.LoadAssetAtPath<Level>(levelPath);

                if (level != null)
                {
                    int levelId = level.GetInstanceID();
                    if (!processedLevelIds.Contains(levelId))
                    {
                        processedLevelIds.Add(levelId);

                        var levelItem = new LevelHierarchyItem
                        {
                            id = itemId++,
                            displayName = $"Level {level.number}",
                            depth = 0,
                            type = LevelHierarchyItem.ItemType.Level,
                            levelAsset = level,
                            assetPath = levelPath,
                            icon = EditorGUIUtility.FindTexture("ScriptableObject Icon")
                        };

                        m_IdToItem[levelItem.id] = levelItem;
                        m_RootItems.Add(levelItem);
                    }
                }
            }

            // Sort root items - groups first, then levels by number
            m_RootItems = m_RootItems
                .OrderBy(item => item is LevelHierarchyItem levelItem ? 0 : 1)
                .ThenBy(item => item is LevelHierarchyItem levelItem && levelItem.levelAsset != null ? 
                    levelItem.levelAsset.number : 0)
                .ToList();

        }

        // Helper method to recursively set depth of child items
        private void SetChildrenDepth(TreeViewItem item, int depth)
        {
            if (item.children == null)
                return;

            foreach (var child in item.children)
            {
                child.depth = depth;
                SetChildrenDepth(child, depth + 1);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            // Check if we're in a drag operation to disable highlighting
            bool isDragging = DragAndDrop.GetGenericData("LevelHierarchyItems") != null;
            
            if (isDragging)
            {
                // Temporarily disable selection highlighting during drag
                args.selected = false;
                args.focused = false;
            }
            
            if (args.item is LevelHierarchyItem item)
            {
                // Calculate button rects
                float buttonWidth = 20;
                float buttonSpacing = 2;
                float rightMargin = 4;
                
                var adjustedRect = args.rowRect;
                
                // Only show buttons for groups
                if (item.type == LevelHierarchyItem.ItemType.Group)
                {
                    // Place buttons at the right side of the row
                    var rowRect = args.rowRect;
                    var minusRect = new Rect(rowRect.xMax - buttonWidth - rightMargin, rowRect.y + 1, buttonWidth, rowRect.height - 2);
                    var plusRect = new Rect(minusRect.x - buttonWidth - buttonSpacing, rowRect.y + 1, buttonWidth, rowRect.height - 2);
                    
                    // Adjust the content rect to not overlap with buttons
                    adjustedRect.xMax = plusRect.x - buttonSpacing;

                    // Draw + button for creating subgroups
                    var plusStyle = new GUIStyle(EditorStyles.miniButton) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                    if (UnityEngine.GUI.Button(minusRect, "+", plusStyle))
                    {
                        OnCreateLevel?.Invoke(item);
                        Event.current.Use();
                    }

                    // // Draw - button for deleting groups
                    // var minusStyle = new GUIStyle(EditorStyles.miniButton) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                    // if (UnityEngine.GUI.Button(minusRect, "−", minusStyle))  // Using minus sign (U+2212) for better appearance
                    // {
                    //     bool confirm = EditorUtility.DisplayDialog(
                    //         "Delete Group",
                    //         "Are you sure you want to delete this group?",
                    //         "Delete",
                    //         "Cancel"
                    //     );
                    //
                    //     if (confirm)
                    //     {
                    //         OnDeleteItem?.Invoke(item);
                    //         Event.current.Use();
                    //     }
                    // }
                }

                args.rowRect = adjustedRect;

                // Draw columns with adjusted rect
                for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), item, (ColumnId)args.GetColumn(i), ref args);
                }
            }
            else
            {
                base.RowGUI(args);
            }
        }

        private void CellGUI(Rect cellRect, LevelHierarchyItem item, ColumnId column, ref RowGUIArgs args)
        {
            // Only have Name column which shows the tree structure
            base.RowGUI(args);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            // Don't change selection during drag operations to prevent highlighting
            if (DragAndDrop.GetGenericData("LevelHierarchyItems") != null)
            {
                return;
            }
            
            if (selectedIds != null && selectedIds.Count > 0 && m_IdToItem.TryGetValue(selectedIds[0], out LevelHierarchyItem item))
            {
                OnSelectionChanged?.Invoke(item);
                
                // Automatically open CrosswordGridWindow when a level is selected
                if (item.type == LevelHierarchyItem.ItemType.Level && item.levelAsset != null)
                {
                    string languageCode = LevelEditorUtility.GetLanguageCodeForLevel(item.levelAsset);
                    if (!string.IsNullOrEmpty(languageCode))
                    {
                        WordsToolkit.Scripts.Levels.Editor.CrosswordGridWindow.ShowWindow(item.levelAsset, languageCode);
                    }
                }
            }
            else
            {
                OnSelectionChanged?.Invoke(null);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            if (m_IdToItem.TryGetValue(id, out LevelHierarchyItem item))
            {
                // When double-clicking an item, ping it in the Project window
                if (item.type == LevelHierarchyItem.ItemType.Group)
                {
                    EditorGUIUtility.PingObject(item.groupAsset);
                }
                else if (item.type == LevelHierarchyItem.ItemType.Level)
                {
                    EditorGUIUtility.PingObject(item.levelAsset);
                }
            }
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return false; // Disable renaming in the tree view
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            // Let base class handle UI aspects of renaming
            base.RenameEnded(args);

            // If renaming was cancelled or name didn't change, do nothing
            if (args.acceptedRename == false || string.IsNullOrEmpty(args.newName))
                return;

            // Find the item being renamed
            var item = FindItem(args.itemID, rootItem) as LevelHierarchyItem;
            if (item == null)
                return;

            // Handle renaming based on item type
            switch (item.type)
            {
                case LevelHierarchyItem.ItemType.Group:
                    if (item.groupAsset != null)
                    {
                        // Update group name
                        Undo.RecordObject(item.groupAsset, "Rename Group");
                        item.groupAsset.groupName = args.newName;
                        item.displayName = args.newName;
                        EditorUtility.SetDirty(item.groupAsset);
                        AssetDatabase.SaveAssets();
                    }
                    break;

                case LevelHierarchyItem.ItemType.Level:
                    if (item.levelAsset != null)
                    {
                        // Update level display name if there's a custom name property
                        Undo.RecordObject(item.levelAsset, "Rename Level");
                        // If your Level class has a name property:
                        // item.levelAsset.levelName = args.newName;
                        item.displayName = args.newName;
                        EditorUtility.SetDirty(item.levelAsset);
                        AssetDatabase.SaveAssets();
                    }
                    break;

                case LevelHierarchyItem.ItemType.Collection:
                    // Renaming collections would involve renaming folders on disk,
                    // which is more complex. For now, we'll just update the display name.
                    item.displayName = args.newName;
                    break;
            }

            // Refresh the tree to show updated names
            Reload();
            
            // If you have events for rename operations:
            // if (OnItemRenamed != null)
            //     OnItemRenamed(item, args.newName);
        }

        protected override void KeyEvent()
        {
            base.KeyEvent();

            // Handle escape key press to cancel drag operations
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                // Check if there's an active drag operation
                if (DragAndDrop.GetGenericData("LevelHierarchyItems") != null)
                {
                    // Cancel the drag operation
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData("LevelHierarchyItems", null);
                    Event.current.Use();
                    Repaint();
                }
            }

            // Handle delete key press
            if (Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace))
            {
                var selection = GetSelection();
                if (selection.Count > 0)
                {
                    var selectedItems = selection
                        .Select(id => m_IdToItem.TryGetValue(id, out LevelHierarchyItem item) ? item : null)
                        .Where(item => item != null)
                        .ToList();

                    if (selectedItems.Count > 0)
                    {
                        string itemTypes = selectedItems.Count == 1 
                            ? selectedItems[0].type.ToString() 
                            : "items";
                        
                        bool confirm = EditorUtility.DisplayDialog(
                            $"Delete {itemTypes}",
                            $"Are you sure you want to delete {selectedItems.Count} selected {itemTypes}?",
                            "Delete",
                            "Cancel"
                        );

                        if (confirm)
                        {
                            foreach (var item in selectedItems)
                            {
                                OnDeleteItem?.Invoke(item);
                            }
                        }
                    }
                    
                    Event.current.Use();
                }
            }
        }

        public void SelectAsset(Object asset)
        {
            if (asset == null) return;

            // Find the item representing this asset
            foreach (var entry in m_IdToItem)
            {
                var item = entry.Value;
                if ((item.type == LevelHierarchyItem.ItemType.Group && item.groupAsset == asset) ||
                    (item.type == LevelHierarchyItem.ItemType.Level && item.levelAsset == asset))
                {
                    // Select this item
                    SetSelection(new List<int> { item.id }, TreeViewSelectionOptions.RevealAndFrame);
                    break;
                }
            }
        }

        public new void ExpandAll()
        {
            // Expand all items
            SetExpanded(GetRows().Select(r => r.id).ToList());
        }

        public new void CollapseAll()
        {
            // With no collections level, we can just collapse everything
            SetExpanded(new List<int>());
        }

        public List<LevelHierarchyItem> GetAllItems()
        {
            return m_IdToItem.Values.ToList();
        }

        public LevelHierarchyItem CreateSubGroup(LevelHierarchyItem parentItem)
        {
            // Create a new level group asset
            LevelGroup newGroup = ScriptableObject.CreateInstance<LevelGroup>();

            // Set default name
            newGroup.groupName = "New Group";

            // Set parent to be the same as the current group's parent
            if (parentItem != null && parentItem.type == LevelHierarchyItem.ItemType.Group)
            {
                newGroup.parentGroup = parentItem.groupAsset.parentGroup;
            }

            // Initialize localized texts with all languages from configuration
            // with 'en' as the default language
            string[] configGuids = AssetDatabase.FindAssets("t:LanguageConfiguration");
            if (configGuids.Length > 0)
            {
                string configPath = AssetDatabase.GUIDToAssetPath(configGuids[0]);
                var config = AssetDatabase.LoadAssetAtPath<LanguageConfiguration>(configPath);
                if (config != null && config.languages != null && config.languages.Count > 0)
                {
                    foreach (var langInfo in config.languages)
                    {
                        // Add empty localized text entries for each language
                        newGroup.localizedTexts.Add(new LocalizedTextGroup
                        {
                            language = langInfo.code,
                            text = ""
                        });
                    }
                }
            }

            // Determine save path - always save in Resources/Groups folder
            string defaultFolder = "Assets/WordConnectGameToolkit/Resources/Groups";
            if (!Directory.Exists(defaultFolder))
            {
                Directory.CreateDirectory(defaultFolder);
                AssetDatabase.Refresh();
            }

            // Add a timestamp to ensure the new group appears at the bottom when sorted alphabetically
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{defaultFolder}/ZZZ_NewGroup_{timestamp}.asset");

            // Create the asset file
            AssetDatabase.CreateAsset(newGroup, path);
            AssetDatabase.SaveAssets();

            // Reload to see the new group
            Reload();

            // Find the newly created item
            LevelHierarchyItem newItem = null;
            foreach (var entry in m_IdToItem)
            {
                if (entry.Value.groupAsset == newGroup)
                {
                    newItem = entry.Value;
                    break;
                }
            }

            // Select the new item after a frame to ensure the tree is updated
            if (newItem != null)
            {
                EditorApplication.delayCall += () => {
                    // Set selection in tree
                    SetSelection(new List<int> { newItem.id }, TreeViewSelectionOptions.RevealAndFrame);
                    SetExpanded(newItem.id, true);
                    
                    // Update selection state
                    OnSelectionChanged?.Invoke(newItem);
                    Selection.activeObject = newGroup;
                    EditorGUIUtility.PingObject(newGroup);

                    // Create a level in the new group without selecting it
                    var createdLevel = CreateLevel(newItem);

                    // Reset selection back to the group
                    EditorApplication.delayCall += () => {
                        SetSelection(new List<int> { newItem.id }, TreeViewSelectionOptions.RevealAndFrame);
                        OnSelectionChanged?.Invoke(newItem);
                        Selection.activeObject = newGroup;

                        // Wait one more frame and rename the asset to remove the ZZZ_ prefix
                        EditorApplication.delayCall += () => {
                            string newPath = path.Replace("ZZZ_", "");
                            AssetDatabase.MoveAsset(path, newPath);
                            AssetDatabase.SaveAssets();
                        };
                    };
                };
            }

            return newItem;
        }

        public LevelHierarchyItem CreateLevel(LevelHierarchyItem parentItem)
        {
            // We need a parent group to create a level
            if (parentItem == null || parentItem.type != LevelHierarchyItem.ItemType.Group || parentItem.groupAsset == null)
            {
                Debug.LogError("Cannot create level: Invalid parent group");
                return null;
            }

            // Create a new level data asset
            Level newLevel = ScriptableObject.CreateInstance<Level>();
            newLevel.letters = EditorPrefs.GetInt("WordsToolkit_LettersAmount", 5);

            // Set default properties - find highest level number across all levels and add 1
            int highestNumber = LevelHierarchyUtility.GetHighestLevelNumber();
            newLevel.number = highestNumber + 1;

            // Ensure Resources/Levels folder exists
            string levelsFolder = "Assets/WordConnectGameToolkit/Resources/Levels";
            if (!Directory.Exists(levelsFolder))
            {
                // Create Resources folder if it doesn't exist
                string resourcesFolder = "Assets/WordConnectGameToolkit/Resources";
                if (!Directory.Exists(resourcesFolder))
                {
                    Directory.CreateDirectory(resourcesFolder);
                }
                Directory.CreateDirectory(levelsFolder);
                AssetDatabase.Refresh();
            }

            string path = $"{levelsFolder}/level_{newLevel.GetHashCode()}.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            // Add enabled languages from configuration
            string[] configGuids = AssetDatabase.FindAssets("t:LanguageConfiguration");
            
            if (configGuids.Length > 0)
            {
                string configPath = AssetDatabase.GUIDToAssetPath(configGuids[0]);
                var config = AssetDatabase.LoadAssetAtPath<LanguageConfiguration>(configPath);
                
                if (config != null)
                {
                    var enabledLanguages = config.GetEnabledLanguages();
                    
                    foreach (var langInfo in enabledLanguages)
                    {
                        var langData = newLevel.AddLanguage(langInfo.code);
                        langData.wordsAmount = newLevel.words;

                        // Initialize empty arrays and crossword data
                        langData.words = new string[0];
                        var columns = EditorPrefs.GetInt("WordsToolkit_grid_x", CrosswordPreviewHandler.defaultGridColumns);
                        var rows = EditorPrefs.GetInt("WordsToolkit_grid_y", CrosswordPreviewHandler.defaultGridRows);
                        langData.crosswordData = new SerializableCrosswordData {
                            columns = columns,
                            rows = rows,
                            grid = new char[columns, rows],
                            minBounds = Vector2Int.zero,
                            maxBounds = new Vector2Int(CrosswordPreviewHandler.defaultGridColumns - 1, CrosswordPreviewHandler.defaultGridRows - 1),
                            placements = new List<SerializableWordPlacement>()
                        };
                    }
                }
                else
                {
                    Debug.LogWarning("Failed to load language configuration asset");
                }
            }
            else
            {
                // No configuration found, add English as default
                var langData = newLevel.AddLanguage("en");
                langData.wordsAmount = newLevel.words;
                // Initialize empty arrays and crossword data
                langData.words = new string[0];
                langData.crosswordData = new SerializableCrosswordData {
                    columns = CrosswordPreviewHandler.defaultGridColumns,
                    rows = CrosswordPreviewHandler.defaultGridRows,
                    grid = new char[CrosswordPreviewHandler.defaultGridColumns, CrosswordPreviewHandler.defaultGridRows],
                    minBounds = Vector2Int.zero,
                    maxBounds = new Vector2Int(CrosswordPreviewHandler.defaultGridColumns - 1, CrosswordPreviewHandler.defaultGridRows - 1),
                    placements = new List<SerializableWordPlacement>()
                };
            }
            
            // Inherit background and colorsTile from parent group
            if (parentItem.groupAsset.background != null)
            {
                newLevel.background = parentItem.groupAsset.background;
            }
            
            if (parentItem.groupAsset.colorsTile != null)
            {
                newLevel.colorsTile = parentItem.groupAsset.colorsTile;
            }
            
            // Create the asset file
            AssetDatabase.CreateAsset(newLevel, path);
            
            // Add the level to the parent group
            if (parentItem.groupAsset.levels == null)
            {
                parentItem.groupAsset.levels = new List<Level>();
            }

            parentItem.groupAsset.levels.Add(newLevel);
            
            EditorUtility.SetDirty(parentItem.groupAsset);
            AssetDatabase.SaveAssets();
            
            // Reload to show the new level
            Reload();

            // Find the newly created item
            LevelHierarchyItem newItem = null;
            foreach (var entry in m_IdToItem)
            {
                if (entry.Value.levelAsset == newLevel)
                {
                    newItem = entry.Value;
                    break;
                }
            }

            if (newItem == null)
            {
                Debug.LogError("Failed to find newly created level item after reload");
            }
            else
            {
                EditorUtility.SetDirty(newLevel);
                AssetDatabase.SaveAssets();
            }
            LevelEditorUtility.RefreshHierarchy(this);
            return newItem;
        }

        // Handle right-click context menu
        protected override void ContextClickedItem(int id)
        {
            if (m_IdToItem.TryGetValue(id, out LevelHierarchyItem item))
            {
                GenericMenu menu = new GenericMenu();
                
                // Context menu based on item type
                if (item.type == LevelHierarchyItem.ItemType.Group)
                {
                    menu.AddItem(new GUIContent("Create Group"), false, () => {
                        OnCreateSubgroup?.Invoke(item);
                    });
                    
                    menu.AddItem(new GUIContent("Create Level"), false, () => {
                        OnCreateLevel?.Invoke(item);
                    });
                    
                    menu.AddSeparator("");
                    
                    menu.AddItem(new GUIContent("Delete Group"), false, () => {
                        OnDeleteItem?.Invoke(item);
                    });
                }
                else if (item.type == LevelHierarchyItem.ItemType.Level)
                {
                    // Find parent group to allow creating sibling levels
                    LevelHierarchyItem parentItem = FindParentGroupForLevel(item);
                    if (parentItem != null)
                    {
                        menu.AddItem(new GUIContent("Create Level"), false, () => {
                            OnCreateLevel?.Invoke(parentItem);
                        });
                    }
                    
                    menu.AddSeparator("");
                    
                    menu.AddItem(new GUIContent("Delete Level"), false, () => {
                        OnDeleteItem?.Invoke(item);
                    });
                }
                
                menu.AddSeparator("");
                
                menu.AddItem(new GUIContent("Locate in Project Window"), false, () => {
                    if (item.type == LevelHierarchyItem.ItemType.Group)
                    {
                        EditorGUIUtility.PingObject(item.groupAsset);
                    }
                    else if (item.type == LevelHierarchyItem.ItemType.Level)
                    {
                        EditorGUIUtility.PingObject(item.levelAsset);
                    }
                });
                
                menu.ShowAsContext();
                Event.current.Use(); // This ensures the event is consumed and doesn't propagate
            }
            else
            {
                // Right-click on empty space or invalid item
                ContextClicked();
            }
        }

        // Handle right-click in empty area
        protected override void ContextClicked()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Create Group"), false, () => {
                OnCreateSubgroup?.Invoke(null);
            });
            menu.ShowAsContext();
        }
        
        // Find parent group for a level item
        private LevelHierarchyItem FindParentGroupForLevel(LevelHierarchyItem levelItem)
        {
            if (levelItem == null || levelItem.type != LevelHierarchyItem.ItemType.Level || levelItem.levelAsset == null)
                return null;
                
            foreach (var entry in m_IdToItem)
            {
                if (entry.Value.type == LevelHierarchyItem.ItemType.Group && 
                    entry.Value.groupAsset != null && 
                    entry.Value.groupAsset.levels != null &&
                    entry.Value.groupAsset.levels.Contains(levelItem.levelAsset))
                {
                    return entry.Value;
                }
            }
            
            return null;
        }

        // Enable dragging operations
        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            // Make sure at least one item is selected
            if (args.draggedItemIDs == null || args.draggedItemIDs.Count == 0)
                return false;
            
            // Select the dragged item
            SetSelection(new[] { args.draggedItemIDs[0] }, TreeViewSelectionOptions.FireSelectionChanged);
            
            // Allow dragging both group and level items
            return true;
        }

        // Set up the drag operation
        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            // Get selected items for dragging
            List<LevelHierarchyItem> draggedItems = new List<LevelHierarchyItem>();
            foreach (int id in args.draggedItemIDs)
            {
                if (m_IdToItem.TryGetValue(id, out LevelHierarchyItem item))
                {
                    draggedItems.Add(item);
                    // Ensure the item is selected during drag
                    SetSelection(new[] { id }, TreeViewSelectionOptions.FireSelectionChanged);
                }
            }
            
            if (draggedItems.Count == 0)
                return;
            
            // Set drag data
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("LevelHierarchyItems", draggedItems);
            DragAndDrop.StartDrag("Dragging Level Items");
        }

        // Override to provide visual feedback during drag operations
        protected override void BeforeRowsGUI()
        {
            base.BeforeRowsGUI();

            // Check if drag operation has ended
            bool isDragging = DragAndDrop.GetGenericData("LevelHierarchyItems") != null;
            if (!isDragging && Event.current.type == EventType.Repaint)
            {
                // Drag operation has ended, force a repaint to restore normal highlighting
                // Repaint();
            }

            // Draw insertion line during drag operations
            if (isDragging)
            {
                var draggedItems = DragAndDrop.GetGenericData("LevelHierarchyItems") as List<LevelHierarchyItem>;
                if (draggedItems != null && draggedItems.Count == 1 && draggedItems[0].type == LevelHierarchyItem.ItemType.Level)
                {
                    DrawInsertionLine();
                }
            }
        }

        // Override to disable default drag highlighting and target highlighting
        protected override bool CanChangeExpandedState(TreeViewItem item)
        {
            // During drag operations, prevent default TreeView highlighting and targeting
            if (DragAndDrop.GetGenericData("LevelHierarchyItems") != null)
            {
                return false;
            }
            return base.CanChangeExpandedState(item);
        }

        private void DrawInsertionLine()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var mousePos = Event.current.mousePosition;
            var rows = GetRows();
            
            bool lineDrawn = false;
            
            // First, try to find a row that the mouse is directly over
            for (int i = 0; i < rows.Count; i++)
            {
                var rowRect = GetRowRect(i);
                
                // Check if mouse is over this row
                if (rowRect.Contains(mousePos))
                {
                    var item = rows[i] as LevelHierarchyItem;
                    if (item?.type == LevelHierarchyItem.ItemType.Level)
                    {
                        // Determine if we should draw line above or below based on mouse position
                        float lineY;
                        bool drawAbove = (mousePos.y - rowRect.y) < (rowRect.height * 0.5f);
                        
                        if (drawAbove)
                        {
                            lineY = rowRect.y;
                        }
                        else
                        {
                            lineY = rowRect.yMax;
                        }
                        
                        // Draw the insertion line
                        var lineRect = new Rect(rowRect.x + GetInsertionLineIndent(item), lineY - 1, rowRect.width - GetInsertionLineIndent(item), 2);
                        EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.7f, 1f, 0.8f)); // Blue insertion line
                        lineDrawn = true;
                        break;
                    }
                    else if (item?.type == LevelHierarchyItem.ItemType.Group)
                    {
                        // Draw line at the end of the group (where levels would be added)
                        var lineRect = new Rect(rowRect.x + GetInsertionLineIndent(item) + 20, rowRect.yMax - 1, rowRect.width - GetInsertionLineIndent(item) - 20, 2);
                        EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.7f, 1f, 0.8f)); // Blue insertion line
                        lineDrawn = true;
                        break;
                    }
                }
            }
            
            // If no line was drawn and mouse is below all rows, draw at the end
            if (!lineDrawn && rows.Count > 0)
            {
                var lastRowRect = GetRowRect(rows.Count - 1);
                if (mousePos.y > lastRowRect.yMax)
                {
                    // Find the last group to determine proper indentation
                    for (int i = rows.Count - 1; i >= 0; i--)
                    {
                        var item = rows[i] as LevelHierarchyItem;
                        if (item?.type == LevelHierarchyItem.ItemType.Group)
                        {
                            var lineRect = new Rect(lastRowRect.x + GetInsertionLineIndent(item) + 20, lastRowRect.yMax + 2, lastRowRect.width - GetInsertionLineIndent(item) - 20, 2);
                            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.7f, 1f, 0.8f)); // Blue insertion line
                            break;
                        }
                        else if (item?.type == LevelHierarchyItem.ItemType.Level)
                        {
                            var lineRect = new Rect(lastRowRect.x + GetInsertionLineIndent(item), lastRowRect.yMax + 2, lastRowRect.width - GetInsertionLineIndent(item), 2);
                            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.7f, 1f, 0.8f)); // Blue insertion line
                            break;
                        }
                    }
                }
            }
        }

        // Override drag handling methods
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            // Check if we have valid drag data
            List<LevelHierarchyItem> draggedItems = DragAndDrop.GetGenericData("LevelHierarchyItems") as List<LevelHierarchyItem>;
            if (draggedItems == null || draggedItems.Count == 0)
                return DragAndDropVisualMode.None;


            // Only handle single level drags for reordering
            if (draggedItems.Count != 1 || draggedItems[0].type != LevelHierarchyItem.ItemType.Level)
                return DragAndDropVisualMode.None;

            var draggedLevel = draggedItems[0];
            
            // Force no target highlighting by completely disabling TreeView's built-in target system
            args.parentItem = null;
            args.insertAtIndex = -1;
            
            // Use mouse position to determine drop target and position
            var mousePos = Event.current.mousePosition;
            var rows = GetRows();
            
            LevelGroup targetGroup = null;
            int insertIndex = -1;
            
            // Find the row under the mouse
            for (int i = 0; i < rows.Count; i++)
            {
                var rowRect = GetRowRect(i);
                
                if (rowRect.Contains(mousePos))
                {
                    var targetItem = rows[i] as LevelHierarchyItem;
                    if (targetItem?.type == LevelHierarchyItem.ItemType.Level)
                    {
                        targetGroup = GetLevelGroup(targetItem);
                        if (targetGroup != null)
                        {
                            int targetIndex = targetGroup.levels.IndexOf(targetItem.levelAsset);
                            if (targetIndex != -1)
                            {
                                // Determine if we should insert before or after based on mouse position
                                bool insertBefore = (mousePos.y - rowRect.y) < (rowRect.height * 0.5f);
                                insertIndex = insertBefore ? targetIndex : targetIndex + 1;
                            }
                        }
                    }
                    else if (targetItem?.type == LevelHierarchyItem.ItemType.Group)
                    {
                        // Dropping on a group - add to the end of the group
                        targetGroup = targetItem.groupAsset;
                        insertIndex = targetGroup?.levels.Count ?? 0;
                    }
                    break;
                }
            }
            
            var sourceGroup = GetLevelGroup(draggedLevel);

            // If no specific target found, check if we're in an area where we can still drop
            if (targetGroup == null || insertIndex == -1)
            {
                // Check if we're below all rows - allow dropping at the end of the last group
                if (rows.Count > 0)
                {
                    var lastRowRect = GetRowRect(rows.Count - 1);
                    if (mousePos.y > lastRowRect.yMax)
                    {
                        // Find the last group in the hierarchy
                        for (int i = rows.Count - 1; i >= 0; i--)
                        {
                            var item = rows[i] as LevelHierarchyItem;
                            if (item?.type == LevelHierarchyItem.ItemType.Group)
                            {
                                targetGroup = item.groupAsset;
                                insertIndex = targetGroup?.levels.Count ?? 0;
                                break;
                            }
                            else if (item?.type == LevelHierarchyItem.ItemType.Level)
                            {
                                var group = GetLevelGroup(item);
                                if (group != null)
                                {
                                    targetGroup = group;
                                    insertIndex = group.levels.Count;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Handle both reordering within the same group and moving between groups
            if (targetGroup != null && insertIndex != -1)
            {
                // Handle the actual drop
                if (args.performDrop)
                {
                    // Show progress bar during the move operation
                    string progressTitle = sourceGroup == targetGroup ? "Reordering Level..." : "Moving Level...";
                    EditorUtility.DisplayProgressBar(progressTitle, "Preparing level move operation...", 0.1f);
                    
                    try
                    {
                        if (sourceGroup == targetGroup)
                        {
                            // Reordering within the same group
                            EditorUtility.DisplayProgressBar(progressTitle, "Reordering level within group...", 0.5f);
                            ReorderLevelInGroup(draggedLevel, insertIndex, targetGroup);
                        }
                        else
                        {
                            // Moving to a different group
                            EditorUtility.DisplayProgressBar(progressTitle, "Recording undo operations...", 0.2f);
                            Undo.RecordObject(sourceGroup, "Move Level - Remove from source");
                            Undo.RecordObject(targetGroup, "Move Level - Add to target");
                            Undo.RecordObject(draggedLevel.levelAsset, "Update Level Background and ColorsTile");

                            EditorUtility.DisplayProgressBar(progressTitle, "Removing level from source group...", 0.3f);
                            // Remove from source group
                            sourceGroup.levels.Remove(draggedLevel.levelAsset);
                            EditorUtility.SetDirty(sourceGroup);

                            EditorUtility.DisplayProgressBar(progressTitle, "Adding level to target group...", 0.5f);
                            // Add to target group at specific position
                            if (targetGroup.levels == null)
                                targetGroup.levels = new List<Level>();

                            targetGroup.levels.Insert(insertIndex, draggedLevel.levelAsset);

                            EditorUtility.DisplayProgressBar(progressTitle, "Updating level properties...", 0.7f);
                            // Update level's background and colorsTile from new group
                            if (targetGroup.background != null)
                                draggedLevel.levelAsset.background = targetGroup.background;
                            if (targetGroup.colorsTile != null)
                                draggedLevel.levelAsset.colorsTile = targetGroup.colorsTile;

                            EditorUtility.SetDirty(targetGroup);
                            EditorUtility.SetDirty(draggedLevel.levelAsset);

                            EditorUtility.DisplayProgressBar(progressTitle, "Updating level numbers...", 0.8f);
                            // Update level numbers in both groups
                            UpdateLevelNumbers(sourceGroup);
                            UpdateLevelNumbers(targetGroup);
                        }

                        EditorUtility.DisplayProgressBar(progressTitle, "Reloading hierarchy...", 0.9f);
                        Reload();
                        
                        // Find and select the level at its new position after reload
                        foreach (var item in m_IdToItem.Values)
                        {
                            if (item.type == LevelHierarchyItem.ItemType.Level && 
                                item.levelAsset == draggedLevel.levelAsset)
                            {
                                SetSelection(new[] { item.id }, TreeViewSelectionOptions.RevealAndFrame);
                                break;
                            }
                        }
                        
                        EditorUtility.DisplayProgressBar(progressTitle, "Saving assets...", 0.95f);
                        AssetDatabase.SaveAssets();
                        OnHierarchyChanged?.Invoke();
                        
                        // Clear drag data to ensure highlighting is restored
                        EditorApplication.delayCall += () => {
                            DragAndDrop.PrepareStartDrag(); // This clears the drag data
                            Repaint(); // Force repaint to restore normal highlighting
                        };
                    }
                    finally
                    {
                        // Always clear the progress bar, even if an exception occurs
                        EditorUtility.ClearProgressBar();
                    }
                }
                                
                return DragAndDropVisualMode.Move;
            }
                            
            return DragAndDropVisualMode.None;
        }

        // Helper method to get content indent for drawing insertion line
        private float GetInsertionLineIndent(TreeViewItem item)
        {
            return GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
        }

        // Helper method to check for circular references
        private bool WouldCreateCircularReference(LevelHierarchyItem draggedItem, LevelHierarchyItem newParentItem)
        {
            // If dragging to root, no circular reference is possible
            if (newParentItem == null)
                return false;
            
            // Check if target is trying to become child of itself or its child
            LevelHierarchyItem current = newParentItem;
            while (current != null)
            {
                if (current.id == draggedItem.id)
                    return true;
                
                // Find the parent of current item
                current = GetParentItem(current);
            }
            
            return false;
        }

        // Helper method to find the parent item of a group
        private LevelHierarchyItem GetParentItem(LevelHierarchyItem item)
        {
            if (item == null || item.type != LevelHierarchyItem.ItemType.Group || 
                item.groupAsset == null || item.groupAsset.parentGroup == null)
                return null;
            
            // Find the hierarchy item for this parent group
            foreach (var entry in m_IdToItem)
            {
                if (entry.Value.type == LevelHierarchyItem.ItemType.Group && 
                    entry.Value.groupAsset == item.groupAsset.parentGroup)
                {
                    return entry.Value;
                }
            }
            
            return null;
        }

        // Helper method to update the parent of a group
        private void UpdateGroupParent(LevelHierarchyItem groupItem, LevelHierarchyItem newParentItem)
        {
            if (groupItem == null || groupItem.type != LevelHierarchyItem.ItemType.Group || groupItem.groupAsset == null)
                return;
            
            // Prepare for undo
            Undo.RecordObject(groupItem.groupAsset, "Change Group Parent");
            
            // Update parent reference
            if (newParentItem != null && newParentItem.type == LevelHierarchyItem.ItemType.Group)
            {
                groupItem.groupAsset.parentGroup = newParentItem.groupAsset;
            }
            else
            {
                // Dragging to root (no parent)
                groupItem.groupAsset.parentGroup = null;
            }
            
            // Save changes to asset
            EditorUtility.SetDirty(groupItem.groupAsset);
            AssetDatabase.SaveAssets();
        }

        // Helper method to update the parent of a level
        private void UpdateLevelParent(LevelHierarchyItem levelItem, LevelHierarchyItem newParentItem)
        {
            if (levelItem == null || levelItem.type != LevelHierarchyItem.ItemType.Level || 
                levelItem.levelAsset == null || newParentItem == null || 
                newParentItem.type != LevelHierarchyItem.ItemType.Group || 
                newParentItem.groupAsset == null)
                return;
            
            // Find the current parent group
            LevelGroup currentGroup = null;
            foreach (var entry in m_IdToItem)
            {
                if (entry.Value.type == LevelHierarchyItem.ItemType.Group && 
                    entry.Value.groupAsset != null && 
                    entry.Value.groupAsset.levels != null)
                {
                    if (entry.Value.groupAsset.levels.Contains(levelItem.levelAsset))
                    {
                        currentGroup = entry.Value.groupAsset;
                        break;
                    }
                }
            }
            
            // If the current parent is the same as the new parent, do nothing
            if (currentGroup == newParentItem.groupAsset)
                return;
            
            // Prepare for undo
            if (currentGroup != null)
                Undo.RecordObject(currentGroup, "Change Level Parent - Remove from old");
            
            Undo.RecordObject(newParentItem.groupAsset, "Change Level Parent - Add to new");
            Undo.RecordObject(levelItem.levelAsset, "Update Level Background and ColorsTile");
            
            // Remove from old parent
            if (currentGroup != null && currentGroup.levels != null)
            {
                currentGroup.levels.Remove(levelItem.levelAsset);
                EditorUtility.SetDirty(currentGroup);
            }
            
            // Add to new parent
            if (newParentItem.groupAsset.levels == null)
                newParentItem.groupAsset.levels = new List<Level>();
                
            if (!newParentItem.groupAsset.levels.Contains(levelItem.levelAsset))
            {
                newParentItem.groupAsset.levels.Add(levelItem.levelAsset);
                EditorUtility.SetDirty(newParentItem.groupAsset);
            }
            
            // Update level's background and colorsTile from new parent group
            if (newParentItem.groupAsset.background != null)
            {
                levelItem.levelAsset.background = newParentItem.groupAsset.background;
            }
            
            if (newParentItem.groupAsset.colorsTile != null)
            {
                levelItem.levelAsset.colorsTile = newParentItem.groupAsset.colorsTile;
            }
            
            // Mark level as dirty to save the changes
            EditorUtility.SetDirty(levelItem.levelAsset);
            
            // Save changes to assets
            AssetDatabase.SaveAssets();
        }

        // Helper method to get the group containing a level item
        private LevelGroup GetLevelGroup(LevelHierarchyItem item)
        {
            if (item == null)
                return null;
                
            if (item.type == LevelHierarchyItem.ItemType.Group)
                return item.groupAsset;
                
            if (item.type == LevelHierarchyItem.ItemType.Level)
            {
                // Find the group that contains this level
                foreach (var entry in m_IdToItem)
                {
                    if (entry.Value.type == LevelHierarchyItem.ItemType.Group && 
                        entry.Value.groupAsset != null && 
                        entry.Value.groupAsset.levels != null &&
                        entry.Value.groupAsset.levels.Contains(item.levelAsset))
                    {
                        return entry.Value.groupAsset;
                    }
                }
            }
            
            return null;
        }
        
        // Helper method to reorder a level within its group
        private void ReorderLevelInGroup(LevelHierarchyItem draggedLevel, int insertAtIndex, LevelGroup group)
        {
            if (draggedLevel?.levelAsset == null || group?.levels == null)
                return;
                
            // Prepare for undo
            Undo.RecordObject(group, "Reorder Level");
            
            // Find current index of dragged level
            int currentIndex = group.levels.IndexOf(draggedLevel.levelAsset);
            if (currentIndex == -1)
                return;
            
            // Clamp insert index to valid range
            int newIndex = Mathf.Clamp(insertAtIndex, 0, group.levels.Count);
            
            // If dragging to the same position, do nothing
            if (currentIndex == newIndex || (currentIndex == newIndex - 1 && newIndex < group.levels.Count))
                return;
            
            // Remove from current position
            var levelToMove = group.levels[currentIndex];
            group.levels.RemoveAt(currentIndex);
            
            // Adjust new index if we removed an item before the target position
            if (currentIndex < newIndex)
                newIndex--;
            
            // Ensure we don't exceed the list bounds after removal
            newIndex = Mathf.Clamp(newIndex, 0, group.levels.Count);
            
            // Insert at new position
            group.levels.Insert(newIndex, levelToMove);
            
            // Update level numbers based on new positions
            UpdateLevelNumbers(group);
            
            // Mark as dirty
            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssets();
        }
        
        // Helper method to update level numbers based on their position in the group
        private void UpdateLevelNumbers(LevelGroup group)
        {
            if (group?.levels == null)
                return;
                
            for (int i = 0; i < group.levels.Count; i++)
            {
                if (group.levels[i] != null)
                {
                    // Prepare for undo
                    Undo.RecordObject(group.levels[i], "Update Level Number");
                    
                    // Set level number based on position (1-indexed)
                    int oldNumber = group.levels[i].number;
                    group.levels[i].number = i + 1;
                    
                    // Mark as dirty
                    EditorUtility.SetDirty(group.levels[i]);
                }
            }
        }

        public TreeViewItem FindItemWrapper(int id)
        {
            return FindItem(id, rootItem);
        }
        
        /// <summary>
        /// Creates a new root-level LevelGroup asset via save panel and reloads the tree.
        /// </summary>
        public void CreateRootGroup()
        {
            OnCreateSubgroup?.Invoke(null);
        }

        public LevelHierarchyItem GetSelectedItem()
        {
            var selection = GetSelection();
            if (m_IdToItem != null && m_IdToItem.Count > 0)
            {
                return selection is { Count: > 0 } ? m_IdToItem[selection[0]] : null;
            }
            return null;
        }

        // Enable single selection for better drag and drop experience
        protected override bool CanMultiSelect(TreeViewItem item)
        {
            // Allow multi-select for groups but prefer single selection for levels to make reordering easier
            if (item is LevelHierarchyItem hierarchyItem)
            {
                return hierarchyItem.type == LevelHierarchyItem.ItemType.Group;
            }
            return base.CanMultiSelect(item);
        }
    }
}
