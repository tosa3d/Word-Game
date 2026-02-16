// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System.Linq;
using UnityEditor;
using UnityEngine;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Levels;


namespace WordsToolkit.Scripts.System
{
    public static class GameDataManager
    {
        // Note: We're removing the static _level cache to avoid state issues

        public static bool isTestPlay = false;

        // Track if we're in test mode and which level is being tested
        private static Level _testLevel;

        public static void ClearPlayerProgress()
        {
            PlayerPrefs.DeleteKey("Level");
            PlayerPrefs.Save();
        }

        public static void ClearALlData()
        {
            #if UNITY_EDITOR
            // clear variables ResourceObject from Resources/Variables
            var resourceObjects = Resources.LoadAll<ResourceObject>("Variables");
            foreach (var resourceObject in resourceObjects)
            {
                resourceObject.Set(0);
            }

            AssetDatabase.SaveAssets();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            #endif
        }

        public static void UnlockLevel(int currentLevel)
        {
            isTestPlay = false;
            // Store the level number in PlayerPrefs
            PlayerPrefs.SetInt("Level", currentLevel);
            PlayerPrefs.Save();

            // No need to load and cache the level here
        }

        public static int GetLevelNum()
        {
            // Always get the current level from PlayerPrefs
            return PlayerPrefs.GetInt("Level", 1);
        }

        public static Level GetLevel()
        {
            // For test play, return the test level if it exists
            if (isTestPlay && _testLevel != null)
            {
                return _testLevel;
            }

            // Get current level number from PlayerPrefs
            int currentLevelNum = GetLevelNum();

            // Load the level directly from Resources
            Level level = Resources.LoadAll<Level>("Levels").FirstOrDefault(i=>i.number == currentLevelNum);

            // Debug level loading
            if (level == null)
            {
                Debug.LogWarning($"Level 'Levels/level_{currentLevelNum}' not found");
            }

            // If level is null, LevelManager's fallback logic will handle finding a previous level
            return level;
        }

        public static void SetLevel(Level level)
        {
            if (isTestPlay)
            {
                // In test mode, we store the level reference
                _testLevel = level;
                return;
            }

            // In normal mode, find the level number and store it in PlayerPrefs
            if (level != null)
            {
                // Try to find the level's index in the Resources folder
                Level[] allLevels = Resources.LoadAll<Level>("Levels").OrderBy(i => i.number).ToArray();
                for (int i = 0; i < allLevels.Length; i++)
                {
                    if (allLevels[i] == level)
                    {
                        // Found the level, store its index+1 as the level number
                        SetLevelNum(i + 1);
                        break;
                    }
                }
            }
        }

        public static EGameMode GetGameMode()
        {
            return (EGameMode)PlayerPrefs.GetInt("GameMode");
        }

        public static void SetGameMode(EGameMode gameMode)
        {
            PlayerPrefs.SetInt("GameMode", (int)gameMode);
            PlayerPrefs.Save();
        }

        public static void SetAllLevelsCompleted()
        {
            var levels = Resources.LoadAll<Level>("Levels").Length;
            PlayerPrefs.SetInt("Level", levels);
            PlayerPrefs.Save();
        }

        internal static bool HasMoreLevels()
        {
            int currentLevel = GetLevelNum();
            int totalLevels = Resources.LoadAll<Level>("Levels").Length;
            return currentLevel < totalLevels;
        }

        public static void SetLevelNum(int stateCurrentLevel)
        {
            // Store the level in PlayerPrefs immediately
            PlayerPrefs.SetInt("Level", stateCurrentLevel);
            PlayerPrefs.Save();

            // No need to clear cached level since we don't cache it anymore
        }

        public static void CleanupAfterTest()
        {
            isTestPlay = false;
            _testLevel = null;
        }
    }
}