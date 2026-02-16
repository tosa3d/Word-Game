using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.Gameplay;
using System.Linq;
using System.IO;

namespace WordsToolkit.Scripts.Levels.Editor
{
    [CustomEditor(typeof(ColorsTile))]
    public class ColorsTileInspector : UnityEditor.Editor
    {
        private ColorsTile[] allColorsTiles;
        private int currentIndex;

        private void OnEnable()
        {
            RefreshColorsTilesList();
        }

        private void RefreshColorsTilesList()
        {
            // Find all ColorsTile assets in the project
            string[] guids = AssetDatabase.FindAssets("t:ColorsTile");
            allColorsTiles = guids.Select(guid => AssetDatabase.LoadAssetAtPath<ColorsTile>(AssetDatabase.GUIDToAssetPath(guid)))
                                 .Where(tile => tile != null)
                                 .OrderBy(tile => tile.name)
                                 .ToArray();

            // Find current index
            currentIndex = global::System.Array.IndexOf(allColorsTiles, target);
            if (currentIndex == -1) currentIndex = 0;
        }

        public override VisualElement CreateInspectorGUI()
        {
            // Create root container
            var root = new VisualElement();

            // Add custom management panel at the top
            var managementPanel = CreateManagementPanel();
            root.Add(managementPanel);

            // Add default property fields
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            return root;
        }

        private VisualElement CreateManagementPanel()
        {
            var panel = new VisualElement();
            panel.style.marginBottom = 15;
            panel.style.paddingBottom = 10;
            panel.style.borderBottomWidth = 1;
            panel.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            var label = new Label("Tile editor");
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 10;
            panel.Add(label);

            // Navigation and name editing section
            var navigationContainer = new VisualElement();
            navigationContainer.style.flexDirection = FlexDirection.Row;
            navigationContainer.style.alignItems = Align.Center;
            navigationContainer.style.marginBottom = 10;

            var prevButton = new Button(() => NavigateToAsset(-1));
            prevButton.text = "<<";
            prevButton.style.width = 30;
            prevButton.style.marginRight = 5;
            navigationContainer.Add(prevButton);

            var nameField = new TextField();
            nameField.value = target.name;
            nameField.style.width = 100;
            nameField.style.marginRight = 5;
            nameField.RegisterValueChangedCallback(evt => {
                if (!string.IsNullOrEmpty(evt.newValue) && evt.newValue != target.name)
                {
                    string assetPath = AssetDatabase.GetAssetPath(target);
                    AssetDatabase.RenameAsset(assetPath, evt.newValue);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    target.name = evt.newValue;
                    EditorUtility.SetDirty(target);
                    RefreshColorsTilesList();
                }
            });
            navigationContainer.Add(nameField);

            var nextButton = new Button(() => NavigateToAsset(1));
            nextButton.text = ">>";
            nextButton.style.width = 30;
            navigationContainer.Add(nextButton);

            var createButton = new Button(() => CreateNewColorsTile());
            createButton.text = "+";
            createButton.style.width = 25;
            createButton.style.marginRight = 2;
            navigationContainer.Add(createButton);

            var deleteButton = new Button(() => DeleteCurrentColorsTile());
            deleteButton.text = "-";
            deleteButton.style.width = 25;
            deleteButton.style.marginRight = 5;
            navigationContainer.Add(deleteButton);

            panel.Add(navigationContainer);


            // Utility buttons
            var buttonsContainer = new VisualElement();
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.style.marginTop = 5;

            var resetButton = new Button(() => {
                var colorsTile = target as ColorsTile;
                if (colorsTile != null)
                {
                    serializedObject.FindProperty("faceColor").colorValue = Color.white;
                    serializedObject.FindProperty("topColor").colorValue = Color.white;
                    serializedObject.FindProperty("bottomColor").colorValue = Color.white;
                    serializedObject.ApplyModifiedProperties();
                }
            });

            panel.Add(buttonsContainer);

            return panel;
        }

        private void CreateNewColorsTile()
        {
            // Make sure the directory exists
            string folderPath = "Assets/WordConnectGameToolkit/Resources/ColorsTile";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            string baseName = "NewColorsTile";
            string fileName = baseName;
            int counter = 1;
            string path = $"{folderPath}/{fileName}.asset";
            
            while (File.Exists(path))
            {
                fileName = $"{baseName}_{counter}";
                path = $"{folderPath}/{fileName}.asset";
                counter++;
            }
                
            ColorsTile newColorsTile = ScriptableObject.CreateInstance<ColorsTile>();
            
            // Load and assign the tile prefab
            string prefabPath = "Assets/WordConnectGameToolkit/Prefabs/Game/Tile.prefab";
            var tilePrefab = AssetDatabase.LoadAssetAtPath<FillAndPreview>(prefabPath);
            if (tilePrefab != null)
            {
                newColorsTile.prefab = tilePrefab;
            }

            // Set default colors
            newColorsTile.faceColor = Color.white;
            newColorsTile.topColor = Color.white;
            newColorsTile.bottomColor = Color.white;

            AssetDatabase.CreateAsset(newColorsTile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the new asset
            Selection.activeObject = newColorsTile;
            EditorGUIUtility.PingObject(newColorsTile);

            // Refresh the list
            RefreshColorsTilesList();
        }

        private void DeleteCurrentColorsTile()
        {
            if (target == null) return;

            string assetPath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(assetPath)) return;

            if (EditorUtility.DisplayDialog("Delete ColorsTile", 
                $"Are you sure you want to delete '{target.name}'?\nThis action cannot be undone.", 
                "Delete", "Cancel"))
            {
                // Navigate to next asset before deleting
                if (allColorsTiles != null && allColorsTiles.Length > 1)
                {
                    int nextIndex = currentIndex + 1;
                    if (nextIndex >= allColorsTiles.Length) nextIndex = currentIndex - 1;
                    if (nextIndex >= 0 && nextIndex < allColorsTiles.Length && nextIndex != currentIndex)
                    {
                        var nextAsset = allColorsTiles[nextIndex];
                        if (nextAsset != null)
                        {
                            Selection.activeObject = nextAsset;
                        }
                    }
                }

                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                RefreshColorsTilesList();
            }
        }

        private void NavigateToAsset(int direction)
        {
            if (allColorsTiles == null || allColorsTiles.Length <= 1) return;

            int newIndex = currentIndex + direction;
            if (newIndex < 0) newIndex = allColorsTiles.Length - 1;
            if (newIndex >= allColorsTiles.Length) newIndex = 0;

            var newAsset = allColorsTiles[newIndex];
            if (newAsset != null)
            {
                Selection.activeObject = newAsset;
                EditorGUIUtility.PingObject(newAsset);
            }
        }

        public override void OnInspectorGUI()
        {
            // Handle object picker for copy functionality
            if (Event.current.commandName == "ObjectSelectorClosed")
            {
                var selectedObject = EditorGUIUtility.GetObjectPickerObject() as ColorsTile;
                if (selectedObject != null)
                {
                    var colorsTile = target as ColorsTile;
                    if (colorsTile != null)
                    {
                        serializedObject.FindProperty("faceColor").colorValue = selectedObject.faceColor;
                        serializedObject.FindProperty("topColor").colorValue = selectedObject.topColor;
                        serializedObject.FindProperty("bottomColor").colorValue = selectedObject.bottomColor;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }
    }
}
