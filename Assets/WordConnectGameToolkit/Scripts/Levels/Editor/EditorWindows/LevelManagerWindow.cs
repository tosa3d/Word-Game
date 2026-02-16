using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.IMGUI.Controls;
using System;

namespace WordsToolkit.Scripts.Levels.Editor.EditorWindows
{
    [ExecuteAlways]
    public class LevelManagerWindow : EditorWindow
    {
        // Static field to track all windows and handle script recompilation
        private static List<LevelManagerWindow> activeWindows = new List<LevelManagerWindow>();
        
        // Static flag to track if Unity is quitting
        private static bool isQuitting = false;
        
        // Static event for when hierarchy selection changes - other windows can subscribe to this
        public static event Action<LevelHierarchyItem> OnHierarchySelectionChanged;
        
        // Tree view for hierarchy
        private LevelHierarchyTreeView m_HierarchyTree;
        private TreeViewState m_TreeViewState;
        private MultiColumnHeaderState m_MultiColumnHeaderState;

        // UI Elements
        public VisualElement m_RightPanel;
        public VisualElement m_InspectorContainer;
        private Label m_HeaderLabel;
        private TextField m_LevelNumberField;
        private VisualElement m_LevelTitleContainer;
        public VisualElement m_ActionButtonsContainer;

        // Currently selected item
        private LevelHierarchyItem m_SelectedItem;

        // Selected language for level editing
        private string m_SelectedLanguage;
        private const string SELECTED_LANGUAGE_KEY = "WordsToolkit_SelectedLanguage";
        
        // Selection persistence
        private const string SELECTED_ASSET_GUID_KEY = "WordsToolkit_SelectedAssetGUID";
        private const string LATEST_LEVEL_GUID_KEY = "WordsToolkit_LatestLevelGUID";

        // Scroll position for the inspector scrollview
        private Vector2 inspectorScrollPosition;
        
        // Cache for editors to prevent GC allocations, exposed publicly to allow ref access
        public UnityEditor.Editor cachedEditor;

        [MenuItem("WordConnect/Editor/Level Editor _C", false, 1000)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelManagerWindow>();
            window.titleContent = new GUIContent("Level Editor");
            window.minSize = new Vector2(1000, 500);
        }

        private void OnEnable()
        {
            // Register this window instance
            if (!activeWindows.Contains(this))
            {
                activeWindows.Add(this);
            }
            
            // Subscribe to Unity quitting event
            EditorApplication.quitting += OnUnityQuitting;
            
            // Initialize UI
            LevelManagerWindowUI.InitializeUI(this);

            // Load selected language preference
            m_SelectedLanguage = EditorPrefs.GetString(SELECTED_LANGUAGE_KEY, LevelEditorUtility.GetDefaultLanguage());
            
            InitializeTreeView();
        }

        private void OnDisable()
        {
            // Save the current selection when window is closed or disabled
            LevelEditorUtility.SaveSelectedItem(m_SelectedItem);
            
            // Save selected language preference
            if (!string.IsNullOrEmpty(m_SelectedLanguage))
                EditorPrefs.SetString(SELECTED_LANGUAGE_KEY, m_SelectedLanguage);
                
            // Unsubscribe from Unity quitting event
            EditorApplication.quitting -= OnUnityQuitting;
                
            // Unregister this window instance
            activeWindows.Remove(this);
        }

        private static void OnUnityQuitting()
        {
            isQuitting = true;
            // Dispose the editor scope container to prevent crashes
            EditorScope.Dispose();
        }

        // Public static property to check if Unity is quitting
        public static bool IsQuitting => isQuitting;

        private void InitializeTreeView()
        {
            // Create tree view state if needed
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            // Column header
            var headerState = LevelHierarchyTreeView.CreateDefaultMultiColumnHeaderState();
            if (m_MultiColumnHeaderState == null)
                m_MultiColumnHeaderState = headerState;

            var multiColumnHeader = new MultiColumnHeader(headerState);
            multiColumnHeader.ResizeToFit();

            m_HierarchyTree = new LevelHierarchyTreeView(m_TreeViewState, multiColumnHeader);
            m_HierarchyTree.OnSelectionChanged += OnTreeSelectionChanged;
            m_HierarchyTree.OnDeleteItem += OnDeleteRequested;
            m_HierarchyTree.OnCreateSubgroup += OnTreeViewCreateSubgroup;

            m_HierarchyTree.OnCreateLevel += OnTreeViewCreateLevel;
            m_HierarchyTree.OnHierarchyChanged += OnHierarchyChanged;

            // Refresh the tree
            m_HierarchyTree.Reload();
            m_HierarchyTree.ExpandAll();

            // Auto-select the latest level, but only if not during compilation
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += () =>
                {
                    // Double check we're still not compiling after the delay
                    if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                    {
                        Level latestLevel = GetLastWorkedOnLevel();
                        if (latestLevel != null)
                        {
                            m_HierarchyTree.SelectAsset(latestLevel);
                        }
                    }
                    
                    // Trigger selection changed event to update UI
                    m_HierarchyTree.OnSelectionChanged?.Invoke(m_HierarchyTree.GetSelectedItem());
                };
            }
            else
            {
                // If compiling, just trigger the selection event without auto-selecting
                m_HierarchyTree.OnSelectionChanged?.Invoke(m_HierarchyTree.GetSelectedItem());
            }
        }

        private void OnHierarchyChanged()
        {
            LevelEditorUtility.RefreshHierarchy(m_HierarchyTree);
        }

        private void OnTreeViewCreateLevel(LevelHierarchyItem parentItem)
        {
            if (parentItem?.groupAsset == null) return;

            var newItem = m_HierarchyTree.CreateLevel(parentItem);
            if (newItem != null)
            {
                EditorApplication.delayCall += () =>
                {
                    m_HierarchyTree.SetSelection(new List<int> { newItem.id }, TreeViewSelectionOptions.RevealAndFrame);
                    OnTreeSelectionChanged(newItem);
                };
            }
        }

        private void OnTreeViewCreateSubgroup(LevelHierarchyItem parentItem)
        {
            // Use CreateSubGroup which automatically creates a level inside the new group
            var newItem = m_HierarchyTree.CreateSubGroup(parentItem);
        }

        private void OnLostFocus()
        {
            // // Save current state when window loses focus
            // if (m_SelectedItem != null && m_SelectedItem.levelAsset != null)
            // {
            //     // Notify LevelDataEditor that it needs to update this level
            //     LevelDataEditor.NotifyLevelNeedsUpdate(m_SelectedItem.levelAsset);
            // }
            
            LevelEditorUtility.SaveSelectedItem(m_SelectedItem);
            if (!string.IsNullOrEmpty(m_SelectedLanguage))
                EditorPrefs.SetString(SELECTED_LANGUAGE_KEY, m_SelectedLanguage);
        }

        private void OnDeleteRequested(LevelHierarchyItem item)
        {
            if (item == null) return;

            switch (item.type)
            {
                case LevelHierarchyItem.ItemType.Group:
                    if (EditorUtility.DisplayDialog("Delete Group",
                        "Are you sure you want to delete this group and all its levels?", "Yes", "No"))
                    {
                        LevelEditorUtility.DeleteGroup(item.groupAsset);
                    }
                    break;

                case LevelHierarchyItem.ItemType.Level:
                    if (EditorUtility.DisplayDialog("Delete Level",
                        "Are you sure you want to delete this level?", "Yes", "No"))
                    {
                        LevelEditorUtility.DeleteLevel(item.levelAsset);
                    }
                    break;
            }

            LevelEditorUtility.RefreshHierarchy(m_HierarchyTree);
        }

        private void OnTreeSelectionChanged(LevelHierarchyItem selectedItem)
        {
            m_SelectedItem = selectedItem;
            inspectorScrollPosition = Vector2.zero;
            LevelManagerWindowUI.UpdateInspector(this, m_SelectedItem);

            // Save selections
            LevelEditorUtility.SaveSelectedItem(selectedItem);
            LevelEditorUtility.SaveLatestLevelSelection(selectedItem);
            
            // Trigger static event for other windows to listen to
            OnHierarchySelectionChanged?.Invoke(selectedItem);
        }

        // Expose necessary properties for other classes
        public VisualElement RightPanel => m_RightPanel;
        public VisualElement ActionButtonsContainer => m_ActionButtonsContainer;
        public VisualElement LevelTitleContainer
        {
            get => m_LevelTitleContainer;
            set => m_LevelTitleContainer = value;
        }

        public Label HeaderLabel
        {
            get => m_HeaderLabel;
            set => m_HeaderLabel = value;
        }

        public TextField LevelNumberField
        {
            get => m_LevelNumberField;
            set => m_LevelNumberField = value;
        }

        // Editor field is exposed publicly to allow ref access in other classes
        public Vector2 InspectorScrollPosition { get => inspectorScrollPosition; set => inspectorScrollPosition = value; }
        public LevelHierarchyTreeView HierarchyTree => m_HierarchyTree;
        public LevelHierarchyItem SelectedItem { get => m_HierarchyTree?.GetSelectedItem(); set => m_SelectedItem = value; }
        public VisualElement InspectorContainer => m_InspectorContainer;

        public LevelHierarchyTreeView GetHierarchyTree()
        {
            return m_HierarchyTree;
        }
        
        /// <summary>
        /// Static method to refresh inspector in all open LevelManagerWindow instances
        /// Called from LevelDataEditor.HandleLevelUpdate when available words are updated
        /// </summary>
        public static void RefreshInspectorForLevel(Level level)
        {
            if (level == null) return;
            
            foreach (var window in activeWindows)
            {
                if (window != null && 
                    window.SelectedItem != null && 
                    window.SelectedItem.type == LevelHierarchyItem.ItemType.Level &&
                    window.SelectedItem.levelAsset == level)
                {
                    // Refresh the inspector for this level
                    LevelManagerWindowUI.UpdateInspector(window, window.SelectedItem);
                }
            }
        }
        
        /// <summary>
        /// Public method to trigger selection changed event and update UI.
        /// Used by NavigateLevel to ensure proper event propagation to other windows.
        /// </summary>
        public void TriggerSelectionChanged(LevelHierarchyItem selectedItem)
        {
            // Update internal state
            m_SelectedItem = selectedItem;
            inspectorScrollPosition = Vector2.zero;
            
            // Update the inspector
            LevelManagerWindowUI.UpdateInspector(this, m_SelectedItem);

            // Save selections
            LevelEditorUtility.SaveSelectedItem(selectedItem);
            LevelEditorUtility.SaveLatestLevelSelection(selectedItem);
            
            // Trigger static event for other windows to listen to
            OnHierarchySelectionChanged?.Invoke(selectedItem);
        }

        private Level GetLastWorkedOnLevel()
        {
            string guid = EditorPrefs.GetString(LATEST_LEVEL_GUID_KEY, string.Empty);
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var level = AssetDatabase.LoadAssetAtPath<Level>(path);
                    if (level != null)
                        return level;
                }
            }

            // Fallback: Find the level with the highest number (most recently created)
            Level[] allLevels = Resources.LoadAll<Level>("Levels");
            if (allLevels.Length > 0)
            {
                return allLevels.OrderByDescending(l => l.number).FirstOrDefault();
            }

            return null;
        }
    }
}
