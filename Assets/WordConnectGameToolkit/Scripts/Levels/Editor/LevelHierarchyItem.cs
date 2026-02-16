using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace WordsToolkit.Scripts.Levels.Editor
{
    // Custom TreeViewItem for level hierarchy
    public class LevelHierarchyItem : TreeViewItem
    {
        public enum ItemType { Collection, Group, Level }
        
        public ItemType type;
        public string folderPath; // For collections
        public LevelGroup groupAsset; // For groups
        public Level levelAsset; // For levels
        public string assetPath;
        public new Texture2D icon;
    }
}
