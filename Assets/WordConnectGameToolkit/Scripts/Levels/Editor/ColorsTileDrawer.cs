using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.Levels.Editor
{
    [CustomPropertyDrawer(typeof(ColorsTile))]
    public class ColorsTileDrawer : PropertyDrawer
    {
        private const float previewSize = 20f;

        // Add a static event to notify when a color tile is selected
        public static global::System.Action<ColorsTile> OnColorTileSelected;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create the root container
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.justifyContent = Justify.FlexStart;
            container.style.height = EditorGUIUtility.singleLineHeight;

            // Create label
            var label = new Label(property.displayName);
            label.style.paddingRight = 20;
            container.Add(label);

            // Create color preview
            var colorPreview = new VisualElement();
            colorPreview.style.width = previewSize;
            colorPreview.style.height = previewSize;
            colorPreview.style.borderTopWidth = 1;
            colorPreview.style.borderBottomWidth = 1;
            colorPreview.style.borderLeftWidth = 1;
            colorPreview.style.borderRightWidth = 1;
            colorPreview.style.borderTopColor = Color.white;
            colorPreview.style.borderBottomColor = Color.white;
            colorPreview.style.borderLeftColor = Color.white;
            colorPreview.style.borderRightColor = Color.white;
            container.Add(colorPreview);

            // Update color preview based on current value
            UpdateColorPreview(colorPreview, property);

            // Make the entire container clickable
            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) // Left mouse button
                {
                    var rect = new Rect(container.worldBound.x, container.worldBound.y, 
                                      container.worldBound.width, container.worldBound.height);
                    
                    ColorsTilePopupWindow.Show(rect, property, () =>
                    {
                        UpdateColorPreview(colorPreview, property);
                        OnColorTileSelected?.Invoke(property.objectReferenceValue as ColorsTile);
                    });
                    
                    evt.StopPropagation();
                }
            });

            // Listen for property changes to update the preview
            container.TrackPropertyValue(property, prop =>
            {
                UpdateColorPreview(colorPreview, prop);
            });

            return container;
        }

        private void UpdateColorPreview(VisualElement colorPreview, SerializedProperty property)
        {
            ColorsTile selectedTile = property.objectReferenceValue as ColorsTile;
            if (selectedTile != null)
            {
                colorPreview.style.backgroundColor = selectedTile.faceColor;
            }
            else
            {
                colorPreview.style.backgroundColor = Color.gray;
            }
        }
    }
}