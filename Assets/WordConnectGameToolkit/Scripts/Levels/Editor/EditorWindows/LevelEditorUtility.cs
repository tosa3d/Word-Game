using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WordsToolkit.Scripts.Levels.Editor.EditorWindows
{
    public static class LevelEditorUtility
    {
        private const string SELECTED_ASSET_GUID_KEY = "WordsToolkit_SelectedAssetGUID";
        private const string LATEST_LEVEL_GUID_KEY = "WordsToolkit_LatestLevelGUID";

        public static void SaveSelectedItem(LevelHierarchyItem selectedItem)
        {
            if (selectedItem == null) return;

            string path = string.Empty;
            Object asset = null;

            switch (selectedItem.type)
            {
                case LevelHierarchyItem.ItemType.Level:
                    asset = selectedItem.levelAsset;
                    break;
                case LevelHierarchyItem.ItemType.Group:
                    asset = selectedItem.groupAsset;
                    break;
            }

            if (asset != null)
            {
                path = AssetDatabase.GetAssetPath(asset);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid))
                {
                    EditorPrefs.SetString(SELECTED_ASSET_GUID_KEY, guid);
                }
            }
        }

        public static void SaveLatestLevelSelection(LevelHierarchyItem selectedItem)
        {
            if (selectedItem?.type == LevelHierarchyItem.ItemType.Level && selectedItem.levelAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(selectedItem.levelAsset);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid))
                {
                    EditorPrefs.SetString(LATEST_LEVEL_GUID_KEY, guid);
                }
            }
        }

        public static void RestoreSelectedItem()
        {
            string guid = EditorPrefs.GetString(SELECTED_ASSET_GUID_KEY, string.Empty);
            if (string.IsNullOrEmpty(guid)) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;

            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null)
            {
                Selection.activeObject = asset;
            }
        }

        public static string GetDefaultLanguage()
        {
            // Implement your default language logic here
            return "en";
        }

        public static string GetLanguageCodeForLevel(Level level)
        {
            if (level == null || level.languages == null || level.languages.Count == 0)
                return null;

            // Get currently selected language tab from EditorPrefs (same as Open Grid button)
            int selectedTabIndex = EditorPrefs.GetInt("WordsToolkit_SelectedLanguageTab", 0);
            
            // Get the language code for the selected tab
            if (selectedTabIndex >= 0 && selectedTabIndex < level.languages.Count)
            {
                return level.languages[selectedTabIndex].language;
            }
            
            // Fallback to first available language
            return level.languages[0].language;
        }

        public static void DeleteGroup(LevelGroup group)
        {
            if (group == null) return;

            string assetPath = AssetDatabase.GetAssetPath(group);
            if (string.IsNullOrEmpty(assetPath)) return;

            var allGroups = AssetDatabase.FindAssets("t:LevelGroup")
                .Select(guid => AssetDatabase.LoadAssetAtPath<LevelGroup>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(g => g != null && g != group)
                .ToList();

            if (group.levels != null && group.levels.Count > 0)
            {
                var levelsToDelete = new List<Level>(group.levels);

                foreach (var otherGroup in allGroups)
                {
                    bool modified = false;
                    for (int i = otherGroup.levels.Count - 1; i >= 0; i--)
                    {
                        if (levelsToDelete.Contains(otherGroup.levels[i]))
                        {
                            otherGroup.levels.RemoveAt(i);
                            modified = true;
                        }
                    }
                    if (modified)
                    {
                        EditorUtility.SetDirty(otherGroup);
                    }
                }

                foreach (var level in levelsToDelete)
                {
                    if (level != null)
                    {
                        string levelPath = AssetDatabase.GetAssetPath(level);
                        if (!string.IsNullOrEmpty(levelPath))
                        {
                            AssetDatabase.DeleteAsset(levelPath);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
            }

            AssetDatabase.DeleteAsset(assetPath);
        }

        public static void DeleteLevel(Level level)
        {
            if (level == null) return;

            var parentGroup = FindParentGroup(level);
            if (parentGroup != null)
            {
                var serializedObject = new SerializedObject(parentGroup);
                var levelsProperty = serializedObject.FindProperty("levels");

                for (int i = 0; i < levelsProperty.arraySize; i++)
                {
                    var elementProperty = levelsProperty.GetArrayElementAtIndex(i);
                    if (elementProperty.objectReferenceValue == level)
                    {
                        levelsProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(parentGroup);
                AssetDatabase.SaveAssets();
            }

            string assetPath = AssetDatabase.GetAssetPath(level);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        public static LevelGroup FindParentGroup(Level level)
        {
            if (level == null) return null;

            var groups = AssetDatabase.FindAssets("t:LevelGroup")
                .Select(guid => AssetDatabase.LoadAssetAtPath<LevelGroup>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(g => g != null)
                .ToList();

            return groups.FirstOrDefault(g => g.levels != null && g.levels.Contains(level));
        }

        public static void RefreshHierarchy(LevelHierarchyTreeView m_HierarchyTree)
        {
            // Refresh assets and rebuild tree
            AssetDatabase.Refresh();
            m_HierarchyTree.Reload();

            // Renumber levels sequentially based on current tree order
            RenumberLevelsByOrder(m_HierarchyTree);

            // Reload to update display names after renumbering
            m_HierarchyTree.Reload();

            // Restore previous selection after hierarchy refresh
            // RestoreSelectedItem();
        }

        /// <summary>
        /// Renumber all levels sequentially based on the hierarchy order, considering all levels even if groups are collapsed.
        /// </summary>
        /// <param name="m_HierarchyTree"></param>
        private static void RenumberLevelsByOrder(LevelHierarchyTreeView m_HierarchyTree)
        {
            // Get all items regardless of collapsed state and sort them properly
            var allLevels = GetAllLevelsInHierarchyOrder(m_HierarchyTree);
            
            int number = 1;
            foreach (var level in allLevels)
            {
                // Record undo for renaming
                Undo.RecordObject(level, "Renumber Levels");
                level.number = number;
                EditorUtility.SetDirty(level);
                number++;
            }
            // Save all changes to assets
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Gets all levels in proper hierarchy order, regardless of collapsed state.
        /// </summary>
        /// <param name="hierarchyTree"></param>
        /// <returns>All levels sorted in hierarchy display order</returns>
        private static List<Level> GetAllLevelsInHierarchyOrder(LevelHierarchyTreeView hierarchyTree)
        {
            var allItems = hierarchyTree.GetAllItems();
            var levelItems = allItems.Where(item => item.type == LevelHierarchyItem.ItemType.Level && item.levelAsset != null).ToList();
            
            // Sort level items by their hierarchy order:
            // 1. Group levels by their parent group
            // 2. Sort groups by name/hierarchy
            // 3. Within each group, sort levels by their current number
            var groupedLevels = levelItems
                .GroupBy(item => FindParentGroup(item.levelAsset))
                .OrderBy(group => group.Key?.name ?? "")
                .SelectMany(group => group.OrderBy(item => item.levelAsset.number))
                .Select(item => item.levelAsset)
                .ToList();
            
            return groupedLevels;
        }
    }
}
