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

using System;
using TMPro;
using UnityEngine;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.Settings;
using VContainer;
using WordsToolkit.Scripts.NLP;

namespace WordsToolkit.Scripts.GUI.ExtraWordBar
{
    // Abstract base class for progress bars
    public abstract class BaseExtraWordsProgressBar : MonoBehaviour
    {
        [Tooltip("Optional text display for progress")]
        public TextMeshProUGUI progressText;
        
        [Tooltip("Format string for progress text. Use {0} for current, {1} for target")]
        public string textFormat = "{0}/{1}";

        // Singleton instance for easy access from other scripts
        public static BaseExtraWordsProgressBar Instance { get; private set; }
        
        // Protected field for max value that can be set by derived classes
        protected float maxValue = 1f;
        private LevelManager levelManager;
        private GameManager gameManager;
        private GameSettings gameSettings;
        private ICustomWordRepository wordRepository;

        [Inject]
        public void Construct(LevelManager levelManager, GameManager gameManager, GameSettings gameSettings, ICustomWordRepository wordRepository)
        {
            this.levelManager = levelManager;
            this.gameManager = gameManager;
            this.gameSettings = gameSettings;
            this.wordRepository = wordRepository;
        }

        protected virtual void Awake()
        {
            Instance = this;
        }
        
        protected virtual void Start()
        {
            UpdateMaxValueFromLevel();
        }

        protected virtual void OnEnable()
        {

            EventManager.GetEvent<Level>(EGameEvent.LevelLoaded).Subscribe(OnLevelLoaded);
            // Make sure to update max value when enabled
            UpdateMaxValueFromLevel();
            UpdateProgressBar();
        }

        private void OnExtraWordClaimed()
        {
            UpdateProgressBar();
        }

        private void OnLevelLoaded(Level level)
        {
            EventManager.GetEvent<string>(EGameEvent.ExtraWordFound).Subscribe(OnExtraWordFound);
            EventManager.GetEvent(EGameEvent.ExtraWordClaimed).Subscribe(OnExtraWordClaimed);
        }

        protected virtual void OnDisable()
        {
            EventManager.GetEvent<string>(EGameEvent.ExtraWordFound).Unsubscribe(OnExtraWordFound);
            EventManager.GetEvent(EGameEvent.ExtraWordClaimed).Unsubscribe(OnExtraWordClaimed);
            EventManager.GetEvent<Level>(EGameEvent.LevelLoaded).Unsubscribe(OnLevelLoaded);
        }

        protected virtual void OnExtraWordFound(string word)
        {
            // Update the progress bar when a new extra word is found
            UpdateProgressBar();
        }

        public virtual void UpdateProgressBar()
        {
            if (levelManager == null)
                return;

            // Get the current counts
            int targetExtraWords = GetTargetExtraWordsCount();
            int currentExtraWords = GetCurrentExtraWordsCount();

            var currentValue = Mathf.Min(targetExtraWords, PlayerPrefs.GetInt("ExtraWordsCollected"));
            // Calculate the normalized progress value (0-1)
            float normalizedProgress = CalculateProgress(currentValue, targetExtraWords);
            // Update the specific UI component (each implementation can scale as needed)
            UpdateProgressDisplay(normalizedProgress);

            // Update the text if available
            UpdateTextDisplay(currentValue, targetExtraWords);
        }

        // Abstract method that must be implemented by derived classes
        protected abstract void UpdateProgressDisplay(float progress);

        // New method to update max value from level data
        protected virtual void UpdateMaxValueFromLevel()
        {
            int targetExtraWords = GetTargetExtraWordsCount();
            SetMaxValue(targetExtraWords);
        }

        // Method to set the max value for this progress bar
        public virtual void SetMaxValue(float value)
        {
            if (value <= 0)
                value = 1f; // Ensure we don't have invalid values
                
            maxValue = value;
            
            // Call implementation-specific method to apply the new max value
            ApplyMaxValue();
        }
        
        // Virtual method that derived classes will override to apply max value to their specific component
        protected virtual void ApplyMaxValue() 
        {
            // Base implementation does nothing
        }

        // Gets the target extra words count from the current level's group or fallback to game settings
        protected int GetTargetExtraWordsCount()
        {
            var currentLevelGroup =  GameDataManager.GetLevel().GetGroup();
            return Mathf.Max(1, currentLevelGroup.targetExtraWords); // Ensure it's at least 1 to prevent division by zero
        }

        // Gets the current count of extra words found
        protected int GetCurrentExtraWordsCount()
        {
            return wordRepository.GetExtraWordsCount();
        }
        
        // Calculate normalized progress as a value between 0 and 1
        protected float CalculateProgress(int current, int target)
        {
            return Mathf.Clamp01((float)current / target);
        }
        
        // Updates the text display if available
        protected void UpdateTextDisplay(int currentValue, int targetValue)
        {
            if (progressText != null)
            {
                progressText.text = string.Format(textFormat, currentValue, targetValue);
            }
        }
    }
}