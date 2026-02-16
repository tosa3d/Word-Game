using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WordsToolkit.Scripts.Levels.Editor
{
    // [CustomPropertyDrawer(typeof(LanguageData))]
    public class LanguageDataPropertyDrawer : PropertyDrawer
    {
        // Track expanded state for each property path
        private static Dictionary<string, bool> expandedState = new Dictionary<string, bool>();

        // Reference to language configuration (cached)
        private LanguageConfiguration languageConfig;

        // Heights for different elements
        private const float HeaderHeight = 22f;
        private const float PropertyHeight = 18f;
        private const float PropertyMargin = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            string propertyPath = property.propertyPath;
            bool isExpanded = IsExpanded(propertyPath);

            // Base height for the header
            float height = HeaderHeight;

            // Add height for expanded properties
            if (isExpanded)
            {
                // Basic properties (language, letters, wordsAmount)
                height += (PropertyHeight + PropertyMargin) * 3;

                // Words array
                SerializedProperty wordsProperty = property.FindPropertyRelative("words");
                height += EditorGUI.GetPropertyHeight(wordsProperty, true);
                height += PropertyMargin * 2;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string propertyPath = property.propertyPath;
            SerializedProperty languageProp = property.FindPropertyRelative("language");
            SerializedProperty lettersProp = property.FindPropertyRelative("letters");
            SerializedProperty wordsAmountProp = property.FindPropertyRelative("wordsAmount");
            SerializedProperty wordsProp = property.FindPropertyRelative("words");

            // Get language display name
            string languageCode = languageProp.stringValue;
            string displayName = GetLanguageDisplayName(languageCode);

            // Draw header with foldout
            Rect headerRect = new Rect(position.x, position.y, position.width, HeaderHeight);

            // Background for header
            Color headerColor = IsExpanded(propertyPath) ? new Color(0.7f, 0.7f, 0.7f, 0.3f) : new Color(0.6f, 0.6f, 0.6f, 0.2f);
            EditorGUI.DrawRect(headerRect, headerColor);

            // Foldout and header content
            Rect foldoutRect = new Rect(headerRect.x + 10, headerRect.y, headerRect.width - 60, headerRect.height);

            // Create a proper label with flag icon if available
            GUIContent headerContent = new GUIContent(displayName);

            bool newExpanded = EditorGUI.Foldout(foldoutRect, IsExpanded(propertyPath), headerContent, true);
            if (newExpanded != IsExpanded(propertyPath))
            {
                SetExpanded(propertyPath, newExpanded);
            }

            // Delete button
            Rect deleteButtonRect = new Rect(headerRect.xMax - 30, headerRect.y + 2, 25, 18);
            if (UnityEngine.GUI.Button(deleteButtonRect, "X"))
            {
                // Schedule deletion to avoid modifying collection during iteration
                // Find the parent array
                string parentPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf('.'));
                SerializedProperty parentArray = property.serializedObject.FindProperty(parentPath);
                int index = int.Parse(property.propertyPath.Substring(property.propertyPath.LastIndexOf('[') + 1).Replace("]", ""));
                
                if (EditorUtility.DisplayDialog("Remove Language", 
                    $"Are you sure you want to remove {displayName} language?", "Yes", "No"))
                {
                    parentArray.DeleteArrayElementAtIndex(index);
                    property.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI(); // Exit the GUI to avoid errors
                }
            }
            
            // Draw properties if expanded
            if (IsExpanded(propertyPath))
            {
                // Calculate positions for properties
                float y = position.y + HeaderHeight + PropertyMargin;
                float indent = 15f;
                Rect propRect = new Rect(position.x + indent, y, position.width - indent, PropertyHeight);
                
                // Draw language code
                EditorGUI.BeginDisabledGroup(true); // Make language code read-only
                EditorGUI.PropertyField(propRect, languageProp, new GUIContent("Language Code"));
                EditorGUI.EndDisabledGroup();
                
                // Draw letters field
                y += PropertyHeight + PropertyMargin;
                propRect.y = y;
                EditorGUI.PropertyField(propRect, lettersProp);
                
                // Draw words amount field
                y += PropertyHeight + PropertyMargin;
                propRect.y = y;
                EditorGUI.PropertyField(propRect, wordsAmountProp);
                
                // Draw words array
                y += PropertyHeight + PropertyMargin;
                propRect.y = y;
                propRect.height = EditorGUI.GetPropertyHeight(wordsProp, true);
                EditorGUI.PropertyField(propRect, wordsProp, true);
            }
            
            EditorGUI.EndProperty();
        }
        
        private string GetLanguageDisplayName(string languageCode)
        {
            // Try to find and cache language configuration
            if (languageConfig == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:LanguageConfiguration");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    languageConfig = AssetDatabase.LoadAssetAtPath<LanguageConfiguration>(path);
                }
            }
            
            if (languageConfig != null)
            {
                var langInfo = languageConfig.GetLanguageInfo(languageCode);
                if (langInfo != null && !string.IsNullOrEmpty(langInfo.displayName))
                {
                    return $"{langInfo.displayName} ({languageCode})";
                }
            }
            
            return $"Language: {languageCode}";
        }
        
        private bool IsExpanded(string propertyPath)
        {
            if (!expandedState.ContainsKey(propertyPath))
            {
                expandedState[propertyPath] = false; // Default to collapsed
            }
            return expandedState[propertyPath];
        }
        
        private void SetExpanded(string propertyPath, bool expanded)
        {
            expandedState[propertyPath] = expanded;
        }
    }
}
