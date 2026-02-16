using UnityEditor;
using UnityEngine;

namespace WordsToolkit.Scripts.Levels.Editor
{
    [CustomEditor(typeof(WordEmbeddingModel))]
    public class WordEmbeddingModelEditor : UnityEditor.Editor
    {
        private bool showTestSection = false;
        private string testLetters = "";
        private string testLanguage = "en";
        private int testWordCount = 10;
        private string[] testResults = new string[0];
        
        public override void OnInspectorGUI()
        {
            WordEmbeddingModel model = (WordEmbeddingModel)target;
            
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            // Button to load all dictionaries
            if (GUILayout.Button("Reload All Dictionaries"))
            {
                // Force reload by setting isLoaded to false for all
                foreach (var dict in model.dictionaries)
                {
                    dict.isLoaded = false;
                }
                
                // Call Awake to reload
                model.enabled = false;
                model.enabled = true;
            }
            
            EditorGUILayout.Space();
            
            // Test section
            showTestSection = EditorGUILayout.Foldout(showTestSection, "Test Word Generation", true);
            if (showTestSection)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                testLanguage = EditorGUILayout.TextField("Language Code", testLanguage);
                testLetters = EditorGUILayout.TextField("Letters", testLetters);
                testWordCount = EditorGUILayout.IntSlider("Word Count", testWordCount, 1, 20);
                
                if (GUILayout.Button("Test Find Words"))
                {
                    if (string.IsNullOrEmpty(testLetters))
                    {
                        EditorUtility.DisplayDialog("Invalid Input", "Please enter some letters to test with.", "OK");
                    }
                    else
                    {
                        testResults = model.FindWordsFromSymbols(testLetters, testWordCount, testLanguage).ToArray();
                    }
                }
                
                if (testResults.Length > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Found {testResults.Length} Words:", EditorStyles.boldLabel);
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    foreach (string word in testResults)
                    {
                        EditorGUILayout.LabelField(word);
                    }
                    EditorGUILayout.EndVertical();
                }
                else if (testResults != null)
                {
                    EditorGUILayout.HelpBox("No valid words found with these letters.", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
    }
}
