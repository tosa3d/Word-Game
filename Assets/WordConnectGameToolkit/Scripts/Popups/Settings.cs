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

using UnityEngine;
using UnityEngine.UI;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.System;
using VContainer;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Services;

namespace WordsToolkit.Scripts.Popups
{
    public class Settings : PopupWithCurrencyLabel
    {
        [SerializeField]
        private CustomButton privacypolicy;

        [SerializeField]
        private CustomButton googleUMPConsent;

        [SerializeField]
        private Button restorePurchase;

        [SerializeField]
        private Slider vibrationSlider;

        private const string VibrationPrefKey = "VibrationLevel";


        protected virtual void OnEnable()
        {
            privacypolicy?.onClick.AddListener(PrivacyPolicy);
            googleUMPConsent?.onClick.AddListener(ReconsiderGoogleUMPConsent);
            LoadVibrationLevel();
            vibrationSlider.onValueChanged.AddListener(SaveVibrationLevel);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(BackToGame);
            restorePurchase?.onClick.AddListener(RestorePurchase);
            restorePurchase?.gameObject.SetActive(gameSettings.enableInApps);
        }

        private void RestorePurchase()
        {
            gameManager.RestorePurchases(((b, list) =>
            {
                if (b)
                    Close();
            }));
        }

        private void BackToGame()
        {
            DisablePause();
            Close();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            vibrationSlider.onValueChanged.RemoveListener(SaveVibrationLevel);
        }

        private void SaveVibrationLevel(float value)
        {
            PlayerPrefs.SetFloat(VibrationPrefKey, value);
            PlayerPrefs.Save();
        }

        private void LoadVibrationLevel()
        {
            if (PlayerPrefs.HasKey(VibrationPrefKey))
            {
                vibrationSlider.value = PlayerPrefs.GetFloat(VibrationPrefKey);
            }
            else
            {
                vibrationSlider.value = 1.0f;
                SaveVibrationLevel(1.0f);
            }
        }

        private void PrivacyPolicy()
        {
            StopInteration();
            DisablePause();
            menuManager.ShowPopup<GDPR>();
            Close();
        }

        private void ReconsiderGoogleUMPConsent()
        {
            StopInteration();
            DisablePause();
            adsManager.ReconsiderUMPConsent();
            Close();
        }

        private void DisablePause()
        {
            if (stateManager.CurrentState == EScreenStates.Game)
            {
                EventManager.GameStatus = EGameState.Playing;
            }
        }
    }
}