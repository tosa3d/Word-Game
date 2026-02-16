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

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.System;
using VContainer;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.GUI;

namespace WordsToolkit.Scripts.Popups
{
    public class MenuPlay : Popup
    {
        public Image[] backgroundImages;
        public Slider scrollBar;
        public TextMeshProUGUI counter;
        public Button play;

        private LevelGroup currentGroup;
        private Level currentLevel;

        [Inject]
        private SceneLoader sceneLoader;
        [Inject]
        private BackgroundChanger backgroundChanger;
        [SerializeField]
        private GameObject hardLabel;

        private void OnEnable()
        {
            stateManager.HideMain();

            play.onClick.AddListener(Play);

            // Get the current level from GameDataManager
            currentLevel = GameDataManager.GetLevel();
            // If current level is null, try to find previous level
            if (currentLevel == null)
            {
                TryLoadPreviousLevel();
            }

            currentGroup = currentLevel?.GetGroup();
            // Try to find a background sprite following the hierarchy
            Sprite backgroundToUse = GetBackgroundFromHierarchy();

            backgroundChanger.SetBackground(backgroundToUse);

            if (backgroundToUse != null && backgroundImages.Length > 0)
            {
                // Set sprite for all background images
                foreach (var bg in backgroundImages)
                {
                    bg.sprite = backgroundToUse;
                }
            }

            // Update progress UI elements
            UpdateProgressUI();
        }

        public override void AfterShowAnimation()
        {
            base.AfterShowAnimation();
            if (currentLevel.isHardLevel)
            {
                hardLabel.SetActive(true); hardLabel.GetComponent<Animator>().Play("HardLabel");
            }
        }

        private void TryLoadPreviousLevel()
        {
            Debug.LogWarning("Current level is null, trying to load previous level");

            // Get current level number
            int currentLevelNum = GameDataManager.GetLevelNum();

            // Try to find a valid previous level
            int previousLevel = currentLevelNum - 1;
            while (previousLevel > 0)
            {
                GameDataManager.SetLevelNum(previousLevel);
                Level levelData = GameDataManager.GetLevel();
                if (levelData != null)
                {
                    currentLevel = levelData;
                    Debug.Log($"Loaded previous level: {previousLevel}");
                    break;
                }
                previousLevel--;
            }

            // If we still couldn't find a valid level, log an error
            if (currentLevel == null)
            {
                Debug.LogError("Could not find any valid level to load");
            }
        }

        private void Play()
        {
            // Make sure we have a valid level to play
            if (currentLevel == null)
            {
                Debug.LogWarning("Attempting to play with null level, trying to load previous level");
                TryLoadPreviousLevel();

                // If still null, we can't play
                if (currentLevel == null)
                {
                    Debug.LogError("No valid level to play");
                    return;
                }
            }

            GameDataManager.SetLevel(currentLevel);
            sceneLoader.StartGameScene();
            Close();
        }

        private void UpdateProgressUI()
        {
            if (counter != null && currentLevel != null)
            {
                // Check if we have a valid group
                if (currentGroup == null || currentGroup.levels == null)
                {
                    counter.text = "0/0";
                    scrollBar.minValue = 0;
                    scrollBar.maxValue = 1;
                    scrollBar.value = 0;
                    return;
                }

                // Get the total number of levels in the group
                int totalLevels = currentGroup.levels.Count;

                // Find the index of current level in the group
                int currentIndex = currentGroup.levels.IndexOf(currentLevel);

                // If level is not found in the group, set index to 0
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                }

                // Update counter and scrollbar
                counter.text = $"{currentIndex}/{totalLevels}";
                scrollBar.minValue = 0;
                scrollBar.maxValue = totalLevels;
                scrollBar.value = currentIndex;
            }
        }

        private Sprite GetBackgroundFromHierarchy()
        {
            if (currentGroup == null) return null;

            // Start from the top-most parent
            LevelGroup current = currentGroup;
            while (current.parentGroup != null)
            {
                if (current.parentGroup.background != null)
                {
                    return current.parentGroup.background;
                }
                current = current.parentGroup;
            }

            // Then check current group
            if (currentGroup.background != null)
            {
                return currentGroup.background;
            }

            // Finally check the level itself
            if (currentLevel != null && currentLevel.background != null)
            {
                return currentLevel.background;
            }

            return null;
        }
    }
}