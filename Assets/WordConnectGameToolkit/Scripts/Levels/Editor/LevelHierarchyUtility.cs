using System.IO;
using UnityEditor;
using UnityEngine;
using WordsToolkit.Scripts.Levels.Editor.EditorWindows;

namespace WordsToolkit.Scripts.Levels.Editor
{
    public static class LevelHierarchyUtility
    {
        [MenuItem("Assets/Create/Game/Level Collection", false, 81)]
        public static void CreateLevelCollection()
        {
            // Find the currently selected folder in the Project window
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(selectedPath))
            {
                // Create in a Levels folder if no selection
                string levelsFolder = "Assets/WordConnectGameToolkit/Levels";
                if (!Directory.Exists(levelsFolder))
                {
                    string parentFolder = "Assets";
                    string folderName = "Levels";
                    AssetDatabase.CreateFolder(parentFolder, folderName);
                    AssetDatabase.Refresh();
                }
                selectedPath = levelsFolder;
            }
            else if (!Directory.Exists(selectedPath))
            {
                selectedPath = Path.GetDirectoryName(selectedPath);
            }

            // Create a new folder for the level collection
            string collectionFolderPath = AssetDatabase.GenerateUniqueAssetPath($"{selectedPath}/LevelCollection");
            AssetDatabase.CreateFolder(Path.GetDirectoryName(collectionFolderPath), Path.GetFileName(collectionFolderPath));
            
            // Create a main level group asset
            LevelGroup mainGroup = ScriptableObject.CreateInstance<LevelGroup>();
            mainGroup.groupName = "Main Levels";
            
            string mainGroupPath = $"{collectionFolderPath}/MainLevels.asset";
            AssetDatabase.CreateAsset(mainGroup, mainGroupPath);
            
            // Create a bonus level group asset
            LevelGroup bonusGroup = ScriptableObject.CreateInstance<LevelGroup>();
            bonusGroup.groupName = "Bonus Levels";
            
            string bonusGroupPath = $"{collectionFolderPath}/BonusLevels.asset";
            AssetDatabase.CreateAsset(bonusGroup, bonusGroupPath);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the new folder in the Project window
            var folderObject = AssetDatabase.LoadAssetAtPath<Object>(collectionFolderPath);
            Selection.activeObject = folderObject;
            
            // Log success message
            Debug.Log($"Created level collection at {collectionFolderPath}");
        }
        
        [MenuItem("Assets/Open in Level Manager", true)]
        private static bool ValidateOpenInLevelManager()
        {
            var selection = Selection.activeObject;
            return selection is LevelGroup || selection is Level;
        }
        
        [MenuItem("Assets/Open in Level Manager", false, 30)]
        public static void OpenInLevelManager()
        {
            var window = EditorWindow.GetWindow<LevelManagerWindow>();
            window.Show();
            
            // Focus on the selected asset
            EditorApplication.delayCall += () => {
                var selection = Selection.activeObject;
                var treeView = window.GetHierarchyTree();
                    
                if (treeView != null)
                {
                    treeView.SelectAsset(selection);
                }
            };
        }
        
        // Get the highest level number across all levels in the Resources/Levels folder
        public static int GetHighestLevelNumber()
        {
            int highestNumber = 0;
            Level[] allLevels = Resources.LoadAll<Level>("Levels");
            
            foreach (Level level in allLevels)
            {
                if (level != null && level.number > highestNumber)
                {
                    highestNumber = level.number;
                }
            }
            
            return highestNumber;
        }
    }
}
