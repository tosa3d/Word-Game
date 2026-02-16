using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using WordsToolkit.Scripts.Gameplay;
using WordsToolkit.Scripts.Settings;

public class ColorsTilePopupWindow : EditorWindow
{
    private List<ColorsTile> colorTiles = new List<ColorsTile>();
    private SerializedProperty targetProperty;
    private float tileSize = 20f;
    private float spacing = 3f;
    private float padding = 5f;
    private System.Action onSelectionChanged;

    public static void Show(Rect activatorRect, SerializedProperty property, System.Action onSelectionChanged)
    {
        ColorsTilePopupWindow window = CreateInstance<ColorsTilePopupWindow>();
        window.titleContent = new GUIContent("Select Color Tile");
        window.targetProperty = property;
        window.onSelectionChanged = onSelectionChanged;
        window.LoadColorTiles();

        var screenRect = GUIUtility.GUIToScreenRect(activatorRect);
        Vector2 position = new Vector2(screenRect.x, screenRect.yMax);

        float rowWidth = 6 * (window.tileSize + window.spacing) - window.spacing;
        int totalTiles = window.colorTiles.Count + 1;
        int rows = Mathf.CeilToInt((float)totalTiles / 6);

        float contentWidth = rowWidth + (window.padding * 2);
        float contentHeight = (rows * (window.tileSize + window.spacing)) - window.spacing + (window.padding * 2) + 25;

        window.position = new Rect(position, new Vector2(contentWidth, contentHeight));
        window.ShowPopup();
        window.Focus();
    }

    private void LoadColorTiles()
    {
        colorTiles.Clear();
        string[] guids = AssetDatabase.FindAssets("t:ColorsTile");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ColorsTile colorTile = AssetDatabase.LoadAssetAtPath<ColorsTile>(assetPath);

            if (colorTile != null)
            {
                colorTiles.Add(colorTile);
            }
        }
    }

    private void OnLostFocus()
    {
        Close();
    }

    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingTop = padding;
        root.style.paddingBottom = padding;
        root.style.paddingLeft = padding;
        root.style.paddingRight = padding;
        root.style.flexDirection = FlexDirection.Column;
        root.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);

        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.flexWrap = Wrap.Wrap;
        container.style.justifyContent = Justify.FlexStart;
        container.style.alignContent = Align.FlexStart;

        float rowWidth = 6 * (tileSize + spacing) - spacing;
        container.style.width = rowWidth;

        foreach (var tile in colorTiles)
        {
            var tileButton = CreateColorTileButton(tile);
            container.Add(tileButton);
        }

        var addButton = CreateAddButton();
        container.Add(addButton);

        root.Add(container);
    }

    private Button CreateColorTileButton(ColorsTile tile)
    {
        var button = new Button(() => SelectTile(tile));
        button.style.width = tileSize;
        button.style.height = tileSize;
        button.style.marginRight = spacing;
        button.style.marginBottom = spacing;
        button.style.backgroundColor = tile.faceColor;
        button.style.borderTopWidth = 0;
        button.style.borderBottomWidth = 0;
        button.style.borderLeftWidth = 0;
        button.style.borderRightWidth = 0;
        button.style.flexShrink = 0;
        button.style.flexGrow = 0;

        bool isSelected = targetProperty != null && targetProperty.objectReferenceValue == tile;
        if (isSelected)
        {
            var dot = new VisualElement();
            float dotSize = tileSize * 0.4f;
            dot.style.width = dotSize;
            dot.style.height = dotSize;
            dot.style.backgroundColor = Color.white;
            dot.style.position = Position.Absolute;
            dot.style.left = (tileSize - dotSize) / 2;
            dot.style.top = (tileSize - dotSize) / 2;
            dot.style.flexShrink = 0;
            button.Add(dot);
        }

        return button;
    }

    private Button CreateAddButton()
    {
        var button = new Button(() => CreateNewColorsTile());
        button.text = "+";
        button.style.width = tileSize;
        button.style.height = tileSize;
        button.style.marginRight = spacing;
        button.style.marginBottom = spacing;
        button.style.flexShrink = 0;
        button.style.flexGrow = 0;

        return button;
    }

    private void SelectTile(ColorsTile tile)
    {
        targetProperty.objectReferenceValue = tile;
        targetProperty.serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(targetProperty.serializedObject.targetObject);
        onSelectionChanged?.Invoke();
        Close();
    }

    private void CreateNewColorsTile()
    {
        // Make sure the directory exists
        string folderPath = "Assets/WordConnectGameToolkit/Resources/ColorsTile";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        string baseName = "NewColorsTile";
        string fileName = baseName;
        int counter = 1;
        string path = $"{folderPath}/{fileName}.asset";

        while (System.IO.File.Exists(path))
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
        else
        {
            Debug.LogError($"Could not load Tile prefab at path: {prefabPath}");
        }

        AssetDatabase.CreateAsset(newColorsTile, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SelectTile(newColorsTile);

        // Select and highlight the new asset in the Project window
        Selection.activeObject = newColorsTile;
        EditorGUIUtility.PingObject(newColorsTile);
    }
}
