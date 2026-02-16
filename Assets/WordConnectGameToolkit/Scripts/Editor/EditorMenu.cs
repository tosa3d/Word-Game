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

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Editor
{
    public static class EditorMenu
    {
        public static string WordConnect = "WordConnect";
        private static string WordConnectPath = "Assets/WordConnectGameToolkit";

        [MenuItem( nameof(WordConnect) + "/Settings/Shop settings")]
        public static void IAPProducts()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/CoinsShopSettings.asset");
        }

        [MenuItem( nameof(WordConnect) + "/Settings/Ads settings")]
        public static void AdsSettings()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/AdsSettings.asset");
        }

        //DailyBonusSettings
        [MenuItem( nameof(WordConnect) + "/Settings/Daily bonus settings")]
        public static void DailyBonusSettings()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/DailyBonusSettings.asset");
        }

        //GameSettings
        [MenuItem( nameof(WordConnect) + "/Settings/Game settings")]
        public static void GameSettings()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/GameSettings.asset");
        }

        //SpinSettings
        [MenuItem( nameof(WordConnect) + "/Settings/Spin settings")]
        public static void SpinSettings()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/SpinSettings.asset");
        }

        //DebugSettings
        [MenuItem( nameof(WordConnect) + "/Settings/Debug settings")]
        public static void DebugSettings()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/DebugSettings.asset");
        }

        [MenuItem( nameof(WordConnect) + "/Settings/Crossword config")]
        public static void CrosswordConfig()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/CrosswordConfig.asset");
        }

        [MenuItem( nameof(WordConnect) + "/Settings/Tutorial settings")]
        public static void TutorialSettings()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/TutorialSettings.asset");
        }

        [MenuItem( nameof(WordConnect) + "/Settings/Language configuration")]
        public static void LanguageConfiguration()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/LanguageConfiguration.asset");
        }

        [MenuItem( nameof(WordConnect) + "/Settings/Gift settings")]
        public static void GiftSettings()
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(WordConnectPath + "/Resources/Settings/GiftsSettings.asset");
        }

        [MenuItem( nameof(WordConnect) + "/Scenes/Main scene &1", priority = 0)]
        public static void MainScene()
        {
            EditorSceneManager.OpenScene(WordConnectPath + "/Scenes/main.unity");
            var stateManager = Object.FindObjectOfType<StateManager>();
            if (stateManager != null)
            {
                stateManager.CurrentState = EScreenStates.MainMenu;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem( nameof(WordConnect) + "/Scenes/Game scene &2")]
        public static void GameScene()
        {
            var stateManager = Object.FindObjectOfType<StateManager>();
            stateManager.CurrentState = EScreenStates.Game;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem( nameof(WordConnect) + "/Editor/Tile editor", priority = 1)]
        public static void ColorEditor()
        {
            string folderPath = WordConnectPath + "/Resources/ColorsTile";

            // Get all tile assets in the folder
            string[] guids = AssetDatabase.FindAssets("t:Object", new[] { folderPath });
            if (guids.Length == 0)
            {
                Debug.LogWarning($"No tile assets found in: {folderPath}");
                return;
            }

            // Select a random tile asset
            string randomGuid = guids[Random.Range(0, guids.Length)];
            string assetPath = AssetDatabase.GUIDToAssetPath(randomGuid);
            var tileAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            // Select and ping the tile asset in the Project window
            Selection.activeObject = tileAsset;
            EditorGUIUtility.PingObject(tileAsset);
        }

        [MenuItem( nameof(WordConnect) + "/Documentation/Main", priority = 2)]
        public static void MainDoc()
        {
            Application.OpenURL("https://candy-smith.gitbook.io/main");
        }

        [MenuItem( nameof(WordConnect) + "/Documentation/ADS/Setup ads")]
        public static void UnityadsDoc()
        {
            Application.OpenURL("https://candy-smith.gitbook.io/bubble-shooter-toolkit/tutorials/ads-setup/");
        }

        [MenuItem( nameof(WordConnect) + "/Documentation/Unity IAP (in-apps)")]
        public static void Inapp()
        {
            Application.OpenURL("https://candy-smith.gitbook.io/main/block-puzzle-game-toolkit/setting-up-in-app-purchase-products");
        }

        [MenuItem( nameof(WordConnect) + "/NLP/Training Language Model")]
        public static void TrainingModel()
        {
            Application.OpenURL("https://colab.research.google.com/drive/199zNcB3FPfnrD6E7OiwmwCcf27jMnY1b?usp=sharing");
        }


        [MenuItem( nameof(WordConnect) + "/Reset PlayerPrefs &e")]
        private static void ResetPlayerPrefs()
        {
            GameDataManager.ClearALlData();
            PlayerPrefs.DeleteKey("GameState");
            Debug.Log("PlayerPrefs are reset");
        }
    }
}
