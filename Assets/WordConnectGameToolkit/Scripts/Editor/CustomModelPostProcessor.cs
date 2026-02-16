using System;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace WordsToolkit.Scripts.Editor
{
    public class CustomModelPostProcessor : AssetPostprocessor
    {
        private static readonly string SOURCE_PATH = "Assets/WordsToolkit/model/custom";
        private static readonly string TARGET_PATH = "Assets/StreamingAssets/WordConnectGameToolkit/model/custom";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool hasCustomModelChanges = false;

            // Check imported assets
            foreach (string assetPath in importedAssets)
            {
                if (IsCustomModelFile(assetPath))
                {
                    hasCustomModelChanges = true;
                    break;
                }
            }

            // Check moved assets
            if (!hasCustomModelChanges)
            {
                foreach (string assetPath in movedAssets)
                {
                    if (IsCustomModelFile(assetPath))
                    {
                        hasCustomModelChanges = true;
                        break;
                    }
                }
            }

            if (hasCustomModelChanges)
            {
                CopyCustomModelsToStreamingAssets();
            }
        }

        private static bool IsCustomModelFile(string assetPath)
        {
            return assetPath.StartsWith(SOURCE_PATH) && 
                   (assetPath.EndsWith(".bin") || assetPath.EndsWith(".json") || assetPath.EndsWith(".txt"));
        }

        private static void CopyCustomModelsToStreamingAssets()
        {
            if (!Directory.Exists(SOURCE_PATH))
            {
                return;
            }

            // Create target directory
            Directory.CreateDirectory(TARGET_PATH);

            // Get all custom model files
            string[] files = Directory.GetFiles(SOURCE_PATH, "*", SearchOption.AllDirectories);

            foreach (string sourceFile in files)
            {
                // Skip .meta files
                if (sourceFile.EndsWith(".meta"))
                    continue;

                // Calculate relative path from source directory
                string relativePath = Path.GetRelativePath(SOURCE_PATH, sourceFile);
                string targetFile = Path.Combine(TARGET_PATH, relativePath);

                // Create target subdirectory if needed
                string targetDir = Path.GetDirectoryName(targetFile);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                try
                {
                    // Copy file if it doesn't exist or is newer
                    if (!File.Exists(targetFile) || File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(targetFile))
                    {
                        File.Copy(sourceFile, targetFile, true);
                        Debug.Log($"[CustomModelPostProcessor] Copied {relativePath} to StreamingAssets");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CustomModelPostProcessor] Failed to copy {sourceFile}: {e.Message}");
                }
            }

            // Refresh the asset database so Unity sees the new files
            AssetDatabase.Refresh();
        }

        [MenuItem("WordToolkit/Copy Custom Models to StreamingAssets")]
        private static void ManualCopyCustomModels()
        {
            CopyCustomModelsToStreamingAssets();
            Debug.Log("[CustomModelPostProcessor] Manual copy completed");
        }

        [MenuItem("WordToolkit/Clean Custom Models from StreamingAssets")]
        private static void CleanCustomModelsFromStreamingAssets()
        {
            if (Directory.Exists(TARGET_PATH))
            {
                try
                {
                    Directory.Delete(TARGET_PATH, true);
                    Debug.Log("[CustomModelPostProcessor] Cleaned custom models from StreamingAssets");
                    AssetDatabase.Refresh();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CustomModelPostProcessor] Failed to clean StreamingAssets: {e.Message}");
                }
            }
        }
    }
}