using UnityEditor;
using UnityEngine;
using WordsToolkit.Scripts.Levels;

namespace WordsToolkit.Scripts.Levels.Editor.EditorWindows
{
    public class CollectionStats
    {
        public int groupCount;
        public int levelCount;

        public static CollectionStats GetCollectionStats(string folderPath)
        {
            var stats = new CollectionStats();
            
            // Get all level groups in the folder
            var groupGuids = AssetDatabase.FindAssets("t:LevelGroup", new[] { folderPath });
            stats.groupCount = groupGuids.Length;
            
            // Count total levels across all groups
            foreach (var guid in groupGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var group = AssetDatabase.LoadAssetAtPath<LevelGroup>(path);
                if (group != null && group.levels != null)
                {
                    stats.levelCount += group.levels.Count;
                }
            }
            
            return stats;
        }
    }
}
