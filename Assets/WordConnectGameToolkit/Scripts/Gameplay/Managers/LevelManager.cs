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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.System;
using Object = UnityEngine.Object;
using VContainer;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Infrastructure.Service;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.Gameplay.Managers
{
    public partial class LevelManager : MonoBehaviour
    {
        public int currentLevel;
        private Level _levelData;
        private FieldManager field;
        public UnityEvent<Level> OnLevelLoaded;
        protected float gameTimer = 0f;
        private bool isTimerRunning = false;
        // Dictionary to store special items in the level
        private Dictionary<Vector2Int, GameObject> specialItems = new Dictionary<Vector2Int, GameObject>();

        // Event that fires when a special item is collected
        public UnityEvent<Vector2Int> OnSpecialItemCollected = new UnityEvent<Vector2Int>();

        [SerializeField]
        private Transform giftButton;

        public bool hammerMode;

        public float GameTime { get => gameTimer; private set => gameTimer = value; }
        public bool HasTimer { get; private set; }

        // Timer limit in seconds, -1 means unlimited time
        public float TimerLimit { get; private set; } = -1f;
        
        // Event that fires when the timer runs out
        public UnityEvent OnTimerExpired = new UnityEvent();
        private StateManager stateManager;
        private SceneLoader sceneLoader;
        private GameManager gameManager;
        private DebugSettings debugSettings;
        private ILevelLoaderService levelLoaderService;
        private ButtonViewController buttonController;
        private GameSettings gameSettings;
        [SerializeField]
        private Transform bubbleAnchor;

        private ICustomWordRepository customWordRepository;

        // Debug panel for web demo
        private bool showDebugPanel = true;
        private bool isDebugPanelExpanded = false;
        private Vector2 scrollPosition = Vector2.zero;

        [Inject]
        public void Construct(FieldManager fieldManager, StateManager stateManager,
            SceneLoader sceneLoader, GameManager gameManager, DebugSettings debugSettings, ILevelLoaderService levelLoaderService, ButtonViewController buttonController, GameSettings gameSettings, ICustomWordRepository customWordRepository)
        {
            this.debugSettings = debugSettings;
            this.gameManager = gameManager;
            this.field = fieldManager;
            this.stateManager = stateManager;
            this.sceneLoader = sceneLoader;
            this.levelLoaderService = levelLoaderService;
            this.buttonController = buttonController;
            this.gameSettings = gameSettings;
            this.customWordRepository = customWordRepository;
        }
        
        private void OnEnable()
        {
            EventManager.GameStatus = EGameState.PrepareGame;
            EventManager.GetEvent(EGameEvent.RestartLevel).Subscribe(RestartLevel);
            EventManager.OnGameStateChanged += HandleGameStateChange;
            
            if (field != null)
            {
                field.OnAllTilesOpened.AddListener(HandleAllTilesOpened);
                field.OnAllRequiredWordsFound.AddListener(HandleAllRequiredWordsFound);
            }

            Load();

        }

        private void OnDisable()
        {
            EventManager.GetEvent(EGameEvent.RestartLevel).Unsubscribe(RestartLevel);
            EventManager.OnGameStateChanged -= HandleGameStateChange;
            
            if (field != null)
            {
                field.OnAllTilesOpened.RemoveListener(HandleAllTilesOpened);
                field.OnAllRequiredWordsFound.RemoveListener(HandleAllRequiredWordsFound);
            }

            if (stateManager != null)
            {
                stateManager.OnStateChanged.RemoveListener(HandleStateChanged);
            }
        }

        private void HandleAllTilesOpened()
        {
            SetWin();
        }

        private void RestartLevel()
        {
            GameDataManager.SetLevel(_levelData);
            sceneLoader.StartGameScene();
        }

        public void Load()
        {
            // check the level is loaded
            if (EventManager.GameStatus == EGameState.Playing)
            {
                return;
            }
            field.Clear();
            // currentLevel = GameDataManager.GetLevelNum();
            _levelData = GameDataManager.GetLevel();
            currentLevel = _levelData.number;
            if (_levelData == null)
            {
                // Try to find previous level
                int previousLevel = currentLevel - 1;
                while (previousLevel > 0)
                {
                    GameDataManager.SetLevelNum(previousLevel);
                    _levelData = GameDataManager.GetLevel();
                    if (_levelData != null)
                    {
                        currentLevel = previousLevel;
                        break;
                    }
                    previousLevel--;
                }

                // If still null after trying previous levels
                if (_levelData == null)
                {
                    return;
                }
            }

            // Clear special items collection when loading a new level
            ClearSpecialItems();

            levelLoaderService.NotifyBeforeLevelLoaded(_levelData);
            LoadLevel(_levelData);
            Invoke(nameof(StartGame), 0.5f);
        }

        private void StartGame()
        {
            buttonController.ShowButtons();
            levelLoaderService.NotifyLevelLoaded(_levelData);
            EventManager.GameStatus = EGameState.Playing;
            EventManager.GetEvent<Level>(EGameEvent.Play).Invoke(_levelData);
            if (stateManager != null)
            {
                stateManager.OnStateChanged.RemoveListener(HandleStateChanged);
                stateManager.OnStateChanged.AddListener(HandleStateChanged);
            }
        }

        public void LoadLevel(Level levelData)
        {
            // Get the current language setting
            string language = gameManager.language;

            // Check if level data contains saved crossword for this language
            var languageData = levelData.GetLanguageData(language);

            // Generate the field with level data
            // Use the new specialItems list instead of filtering placements
            var specialItems = languageData?.crosswordData?.specialItems ?? new List<SerializableSpecialItem>();
            field.GenerateWithSpecialItems(levelData, language, specialItems);


            // Initialize timer settings from level data
            HasTimer = levelData.enableTimer;
            TimerLimit = levelData.enableTimer ? levelData.timerDuration : -1f;
            
            // Reset timer for new level
            ResetTimer();
        }

        public void SetWin()
        {
            customWordRepository.ClearExtraWords();
            GameDataManager.UnlockLevel(currentLevel + 1);
            EventManager.GameStatus = EGameState.PreWin;
        }

        private void PanelWinAnimation()
        {
            buttonController.HideAllForWin();
        }

        private void SetLose()
        {
            EventManager.GameStatus = EGameState.PreFailed;
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current[debugSettings.Win].wasPressedThisFrame)
                {
                    SetWin();
                }

                if (Keyboard.current[debugSettings.Lose].wasPressedThisFrame)
                {
                    SetLose();
                }

                if (Keyboard.current[debugSettings.Restart].wasPressedThisFrame)
                {
                    gameManager.RestartLevel();
                }

#if UNITY_WEBGL && !UNITY_EDITOR
                // Quick win shortcut for web demo testing
                if (Keyboard.current[Key.W].wasPressedThisFrame)
                {
                    SetWin();
                }
                
                // Toggle debug panel with T key
                if (Keyboard.current[Key.T].wasPressedThisFrame)
                {
                    showDebugPanel = !showDebugPanel;
                }
#endif

            }
            
            // Update timer if it's running
            if (isTimerRunning && HasTimer && !menuManager.IsAnyPopupOpened())
            {
                // Timer now decreases instead of increases
                gameTimer += Time.deltaTime;
                
                // Check if timer has expired (if there's a limit)
                if (TimerLimit > 0 && gameTimer >= TimerLimit)
                {
                    TimerExpired();
                }
            }
        }

        private void StartTimer()
        {
            isTimerRunning = true;
        }

        private void StopTimer()
        {
            isTimerRunning = false;
        }

        private void ResetTimer()
        {
            gameTimer = 0f;
            isTimerRunning = false;
        }

        private void TimerExpired()
        {
            // Stop the timer
            StopTimer();
            
            // Notify listeners that the timer has expired
            OnTimerExpired.Invoke();
            
            // Just check if all tiles are opened
            if (field != null && field.AreAllTilesOpen())
            {
                SetWin();
                return;
            }
            
            // If not all tiles are opened, player loses
            SetLose();
        }

        public Level GetCurrentLevel()
        {
            return _levelData;
        }

        public string GetCurrentLanguage()
        {
            return gameManager?.language ?? "en";
        }

        private void HandleAllRequiredWordsFound()
        {
            EventManager.GameStatus = EGameState.PreWin;
        }
        // Register a special item instance with its position
        public void RegisterSpecialItem(Vector2Int position, GameObject itemInstance)
        {
            if (itemInstance == null)
                return;

            specialItems[position] = itemInstance;
        }

        // Collect a special item at the given position
        public bool CollectSpecialItem(Vector2Int position)
        {
            if (specialItems.TryGetValue(position, out GameObject item))
            {
                // Fire event before removing the item
                OnSpecialItemCollected.Invoke(position);

                // Remove from dictionary and destroy the instance
                specialItems.Remove(position);
                Destroy(item);

                return true;
            }

            return false;
        }

        // Clear all special items
        private void ClearSpecialItems()
        {
            foreach (var item in specialItems.Values)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }

            specialItems.Clear();
        }

        // Keep the SerializableStringArray class as it might be used elsewhere or for future serialization
        [Serializable]
        private class SerializableStringArray
        {
            public string[] words;
        }

        public Vector3 GetSpecialItemCollectionPoint()
        {
            return this.giftButton.transform.position;
        }

        private void HandleStateChanged(EScreenStates newState)
        {
            if (newState == EScreenStates.Game)
            {
                Load();
            }
        }
    }
}