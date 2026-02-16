using UnityEngine;
using UnityEditor;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.Levels;
using System.Linq;

namespace WordsToolkit.Scripts.Settings.Editor
{
    [CustomEditor(typeof(DebugSettings))]
    public class DebugSettingsEditor : UnityEditor.Editor
    {
        private LanguageConfiguration languageConfig;
        private string[] availableLanguageCodes;
        private string[] availableLanguageNames;
        private int selectedLanguageIndex = 0;

        void OnEnable()
        {
            LoadLanguageConfiguration();
        }

        void LoadLanguageConfiguration()
        {
            // Find LanguageConfiguration asset in the project
            var guids = AssetDatabase.FindAssets("t:LanguageConfiguration");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                languageConfig = AssetDatabase.LoadAssetAtPath<LanguageConfiguration>(path);
                
                if (languageConfig != null && languageConfig.languages != null)
                {
                    availableLanguageCodes = languageConfig.languages.Select(l => l.code).ToArray();
                    availableLanguageNames = languageConfig.languages.Select(l => $"{l.displayName} ({l.code})").ToArray();
                    
                    // Find current selection
                    var debugSettings = (DebugSettings)target;
                    selectedLanguageIndex = global::System.Array.IndexOf(availableLanguageCodes, debugSettings.TestLanguageCode);
                    if (selectedLanguageIndex < 0) selectedLanguageIndex = 0;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var debugSettings = (DebugSettings)target;
            
            // Refresh language configuration if it's null or changed
            if (languageConfig == null || availableLanguageCodes == null)
            {
                LoadLanguageConfiguration();
            }
            
            serializedObject.Update();

            // Draw default fields
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableHotkeys"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Hotkeys", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Win"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Lose"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Back"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Restart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SimulateDuplicate"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Language Settings", EditorStyles.boldLabel);
            
            // Custom dropdown for test language
            if (languageConfig != null && availableLanguageCodes != null && availableLanguageCodes.Length > 0)
            {
                EditorGUI.BeginChangeCheck();
                selectedLanguageIndex = EditorGUILayout.Popup("Test Language", selectedLanguageIndex, availableLanguageNames);
                
                if (EditorGUI.EndChangeCheck())
                {
                    debugSettings.TestLanguageCode = availableLanguageCodes[selectedLanguageIndex];
                    EditorUtility.SetDirty(debugSettings);
                }
            }
            else
            {
                // Fallback to text field if no LanguageConfiguration found
                EditorGUILayout.PropertyField(serializedObject.FindProperty("TestLanguageCode"), new GUIContent("Test Language Code"));
                EditorGUILayout.HelpBox("No LanguageConfiguration found. Create one to enable language dropdown.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}