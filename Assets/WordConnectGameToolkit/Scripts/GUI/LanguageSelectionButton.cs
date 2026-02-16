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

using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.Localization;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Services;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.GUI
{
    public class LanguageSelectionButton : MonoBehaviour
    {
        public TextMeshProUGUI languageText;
        public CustomButton button;
        [Inject]
        private StateManager stateManager;
        [Inject]
        private MenuManager menuManager;
        [Inject]
        private ILanguageService languageService;

        private void OnEnable()
        {
            button.onClick.AddListener(OnClick);
            languageText.text = LocalizationManager.instance.GetLocalizedCurrentLanguage();
            EventManager.GetEvent<string>(EGameEvent.LanguageChanged).Subscribe(OnLanguageChanged);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClick);
            EventManager.GetEvent<string>(EGameEvent.LanguageChanged).Unsubscribe(OnLanguageChanged);
        }

        private void OnLanguageChanged(string obj)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            languageText.text = LocalizationManager.instance.GetLocalizedCurrentLanguage();
        }

        private void OnClick()
        {
            ShowLanguageSelector();
        }

        public void ShowLanguageSelector()
        {
            menuManager.ShowPopup<LanguageSelectionGame>(null, result => { UpdateText(); });
        }
    }
}