#nullable enable
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace WordsToolkit.Scripts.NLP.Editor
{
    public class MakeEmbeddingModel : EditorWindow
    {
        private string selectedModelPath = "";
        private string selectedTxtPath = "";
        private Vector2 scrollPosition;

        [MenuItem("WordConnect/NLP/Build Custom Model From TXT")]
        static void ShowWindow()
        {
            var window = GetWindow<MakeEmbeddingModel>("Embedding Model Builder");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Embedding Model Builder", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Model file selection
            GUILayout.Label("Select existing model file to replace (optional):", EditorStyles.label);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Model File:", selectedModelPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var path = EditorUtility.OpenFilePanel("Select model file to replace", "Assets/WordConnectGameToolkit/model", "bin");
                if (!string.IsNullOrEmpty(path))
                {
                    selectedModelPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Text file selection
            GUILayout.Label("Select training text file:", EditorStyles.label);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Text File:", selectedTxtPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                var path = EditorUtility.OpenFilePanel("Select word list text file", "", "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    selectedTxtPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            // Build button
            UnityEngine.GUI.enabled = !string.IsNullOrEmpty(selectedTxtPath);
            if (GUILayout.Button("Build Model", GUILayout.Height(30)))
            {
                BuildModel();
            }
            UnityEngine.GUI.enabled = true;

            GUILayout.Space(10);

            // Instructions
            GUILayout.Label("Instructions:", EditorStyles.boldLabel);
            GUILayout.Label("1. Optionally select an existing .bin model file to replace");
            GUILayout.Label("2. Select a .txt file containing word list (one word per line)");
            GUILayout.Label("3. Click 'Build Model' to generate random embeddings");
            GUILayout.Label("4. The model will be saved to Assets/WordConnectGameToolkit/model/");

            EditorGUILayout.EndScrollView();
        }

        private void BuildModel()
        {
            if (string.IsNullOrEmpty(selectedTxtPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a text file.", "OK");
                return;
            }

            try
            {
                // ------------------------------------------------ 1️⃣ read the TXT
                var words = new List<string>();
                var floats = new List<float>();

                using var sr = new StreamReader(selectedTxtPath);
                var inv = CultureInfo.InvariantCulture;
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line) || line[0] == '#')
                        continue;

                    words.Add(line.Trim());
                    const int dd = 128;
                    var rng = new Random();
                    for (int i = 0; i < dd; ++i)
                        floats.Add((float)(rng.NextDouble() * 2.0 - 1.0));  // repeat per word
                }

                int vocab = words.Count;
                int dim = floats.Count / vocab;

                // ------------------------------------------------ 2️⃣ Save as .bin file
                string fileName;
                if (!string.IsNullOrEmpty(selectedModelPath))
                {
                    fileName = Path.GetFileName(selectedModelPath);
                }
                else
                {
                    fileName = Path.GetFileNameWithoutExtension(selectedTxtPath) + "_model.bin";
                }

                string dirRel = "Assets/WordConnectGameToolkit/model";
                string dirFull = Path.Combine(Application.dataPath, "WordConnectGameToolkit/model");
                Directory.CreateDirectory(dirFull);

                string binFullPath = Path.Combine(dirFull, fileName);
                SaveModelBinary(binFullPath, words, floats.ToArray(), dim, vocab);

                // Tell Unity there is a new asset
                AssetDatabase.ImportAsset(Path.Combine(dirRel, fileName));

                EditorUtility.DisplayDialog("Success", 
                    $"Model saved to: {Path.Combine(dirRel, fileName)}\n" +
                    $"Vocabulary size: {vocab}\n" +
                    $"Vector dimension: {dim}", "OK");
                
                Debug.Log($"✅  Model saved to: {Path.Combine(dirRel, fileName)}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to build model: {ex.Message}", "OK");
                Debug.LogError($"Failed to build model: {ex}");
            }
        }

        private static void SaveModelBinary(string filePath, List<string> words, float[] embeddings, int vectorDim, int vocabSize)
        {
            using var bw = new BinaryWriter(File.Create(filePath));
            
            // Magic header
            bw.Write(0x564F4342); // "BOCV"
            
            // Metadata
            bw.Write(vocabSize);
            bw.Write(vectorDim);
            
            // Write vocabulary
            foreach (var word in words)
            {
                bw.Write(word);
            }
            
            // Write embeddings
            foreach (var value in embeddings)
            {
                bw.Write(value);
            }
        }
    }
}
#endif