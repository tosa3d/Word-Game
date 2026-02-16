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
using UnityEngine.UI;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.System;
using VContainer;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.Popups
{
    public class MainMenu : Popup
    {
        public CustomButton settingsButton;
        public CustomButton luckySpin;

        public Button playButton;

        [SerializeField]
        private GameObject freeSpinMarker;

        [SerializeField]
        private Image background;

        public Action OnAnimationEnded;

        private const string LastFreeSpinTimeKey = "LastFreeSpinTime";

        private void Start()
        {
            settingsButton.onClick.AddListener(SettingsButtonClicked);
            luckySpin.onClick.AddListener(LuckySpinButtonClicked);
            UpdateFreeSpinMarker();
            var levelsCount = Resources.LoadAll<Level>("Levels").Length;
            luckySpin.gameObject.SetActive(gameSettings.enableLuckySpin);
            playButton.onClick.AddListener(PlayButtonClicked);
        }

        private void PlayButtonClicked()
        {
            menuManager.ShowPopup<MenuPlay>();
        }

        private bool CanUseFreeSpinToday()
        {
            if (!PlayerPrefs.HasKey(LastFreeSpinTimeKey))
            {
                return true;
            }

            var lastFreeSpinTimeStr = PlayerPrefs.GetString(LastFreeSpinTimeKey);
            var lastFreeSpinTime = DateTime.Parse(lastFreeSpinTimeStr);
            return DateTime.Now.Date > lastFreeSpinTime.Date;
        }

        private void UpdateFreeSpinMarker()
        {
            var isFreeSpinAvailable = CanUseFreeSpinToday();
            if (freeSpinMarker != null)
            {
                freeSpinMarker.SetActive(isFreeSpinAvailable);
            }
        }

        private void SettingsButtonClicked()
        {
            menuManager.ShowPopup<Settings>();
        }

        private void LuckySpinButtonClicked()
        {
            menuManager.ShowPopup<LuckySpin>(null, _ => UpdateFreeSpinMarker());
        }

        public void OnAnimationEnd()
        {
            OnAnimationEnded?.Invoke();
        }
    }
}