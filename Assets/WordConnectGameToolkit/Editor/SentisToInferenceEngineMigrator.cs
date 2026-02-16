using UnityEngine;
using UnityEditor;
using System.IO;

namespace WordsToolkit.Editor
{
    [InitializeOnLoad]
    public static class SentisToInferenceEngineMigrator
    {
        private const string MIGRATION_KEY = "SentisToInferenceEngineMigrated";
        
        static SentisToInferenceEngineMigrator()
        {
            if (!SessionState.GetBool(MIGRATION_KEY, false))
            {
                EditorApplication.delayCall += PerformMigration;
            }
        }
        
        private static void PerformMigration()
        {
            bool migrationPerformed = false;
            
            // Force remove any Sentis references from PackageCache
            string packageCachePath = Path.Combine(Application.dataPath, "..", "Library", "PackageCache");
            if (Directory.Exists(packageCachePath))
            {
                var sentisDirectories = Directory.GetDirectories(packageCachePath, "com.unity.sentis*");
                foreach (var dir in sentisDirectories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        Debug.Log($"Removed Sentis package cache: {dir}");
                        migrationPerformed = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Could not remove Sentis cache directory {dir}: {e.Message}");
                    }
                }
            }
            
            // Clean up any remaining Sentis meta files
            CleanupMetaFiles();
            
            if (migrationPerformed)
            {
                Debug.Log("Sentis to Inference Engine migration completed automatically. Restarting Unity...");
                AssetDatabase.Refresh();
                
                // Restart Unity to ensure clean state
                EditorApplication.delayCall += () => {
                    EditorApplication.OpenProject(System.IO.Path.GetDirectoryName(Application.dataPath));
                };
            }
            
            SessionState.SetBool(MIGRATION_KEY, true);
        }
        
        private static void ForceMigrationAndRestart()
        {
            SessionState.SetBool(MIGRATION_KEY, false);
            PerformMigration();
        }
        
        private static void RestartUnity()
        {
            Debug.Log("Restarting Unity...");
            EditorApplication.OpenProject(System.IO.Path.GetDirectoryName(Application.dataPath));
        }
        
        private static void CleanupMetaFiles()
        {
            string[] searchPaths = {
                Path.Combine(Application.dataPath, "Scripts"),
                Path.Combine(Application.dataPath, "WordConnectGameToolkit")
            };
            
            foreach (string searchPath in searchPaths)
            {
                if (Directory.Exists(searchPath))
                {
                    var metaFiles = Directory.GetFiles(searchPath, "*.meta", SearchOption.AllDirectories);
                    foreach (var metaFile in metaFiles)
                    {
                        string content = File.ReadAllText(metaFile);
                        if (content.Contains("com.unity.sentis"))
                        {
                            try
                            {
                                content = content.Replace("com.unity.sentis", "com.unity.ai.inference");
                                File.WriteAllText(metaFile, content);
                                Debug.Log($"Updated meta file: {metaFile}");
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"Could not update meta file {metaFile}: {e.Message}");
                            }
                        }
                    }
                }
            }
        }
    }
}