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
using UnityEngine;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.Popups;
using UnityEngine.UI;
using VContainer;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.GUI
{
    public class UIManager : MonoBehaviour
    {
        [Inject]
        private MenuManager menuManager;
        [Inject]
        private IObjectResolver resolver;
        [Inject]
        private GameSettings gameSettings;
        [Inject]
        private FieldManager fieldManager;

        public ButtonPopup[] buttonsToOpenPopups;

        [SerializeField] private CustomButton openRandomTileButton;
        [SerializeField] private CustomButton openSelectedTileButton;
        [SerializeField] private CustomButton giftButton;
        [SerializeField] private TimerDisplay timerDisplay;
        
        private LevelManager levelManager;

        private const string GIFT_BUTTON_SHOWN_KEY = "GiftButtonShown";
        
        private void Awake()
        {
            foreach (var buttonPopup in buttonsToOpenPopups)
            {
                resolver.Inject(buttonPopup);
                buttonPopup.Init();
            }

            // Initialize gift button visibility
            if (giftButton != null)
            {
                giftButton.gameObject.SetActive(false);
            }
        }

        private void SaveGiftButtonState(bool shown)
        {
            PlayerPrefs.SetInt(GIFT_BUTTON_SHOWN_KEY, shown ? 1 : 0);
            PlayerPrefs.Save();
        }

        private bool LoadGiftButtonState()
        {
            return PlayerPrefs.GetInt(GIFT_BUTTON_SHOWN_KEY, 0) == 1;
        }

        private void UpdateGiftButtonVisibility(bool hasSpecialItems)
        {
            if (giftButton != null)
            {
                bool shouldShow = hasSpecialItems || LoadGiftButtonState();
                giftButton.gameObject.SetActive(shouldShow);
                if (shouldShow)
                {
                    SaveGiftButtonState(true);
                }
            }
        }

        private void OnEnable()
        {
            EventManager.GetEvent<Level>(EGameEvent.LevelLoaded).Subscribe(OnLevelLoaded);
            EventManager.OnGameStateChanged += HandleGameStateChange;
            if (fieldManager != null && giftButton != null)
            {
                UpdateGiftButtonVisibility(fieldManager.HasSpecialItems());
            }
        }

        private void OnDisable()
        {
            EventManager.GetEvent<Level>(EGameEvent.LevelLoaded).Unsubscribe(OnLevelLoaded);
            EventManager.OnGameStateChanged -= HandleGameStateChange;
        }

        private void OnLevelLoaded(Level obj)
        {
            foreach (var boost in gameSettings.boostLevels)
            {
                if(openRandomTileButton.CompareTag(boost.tag))
                {
                    openRandomTileButton.gameObject.SetActive(boost.level <= obj.number);
                }
                if(openSelectedTileButton.CompareTag(boost.tag))
                {
                    openSelectedTileButton.gameObject.SetActive(boost.level <= obj.number);
                }
            }

            // Check for special items and update gift button visibility
            if (giftButton != null)
            {
                UpdateGiftButtonVisibility(fieldManager.HasSpecialItems());
            }
        }

        private void HandleGameStateChange(EGameState newState)
        {
            UpdateButtonInteractability(newState);
        }

        private void UpdateButtonInteractability(EGameState gameState)
        {
            if (buttonsToOpenPopups == null) return;
            
            bool isInteractable = gameState == EGameState.Playing;
            
            foreach (var buttonPopup in buttonsToOpenPopups)
            {
                if (buttonPopup.button != null)
                {
                    buttonPopup.button.interactable = isInteractable;
                }
            }
        }

    }
    [Serializable]
    public class ButtonPopup
    {
        public CustomButton button;
        public Popup popupRef;
        public Button.ButtonClickedEvent onClick => button.onClick;
        [Inject]
        private MenuManager menuManager;

        public void Init()
        {
            button?.onClick.AddListener(() =>
            {
                if (popupRef)
                {
                    this.menuManager.ShowPopup(popupRef);
                }
            });
        }
    }
}