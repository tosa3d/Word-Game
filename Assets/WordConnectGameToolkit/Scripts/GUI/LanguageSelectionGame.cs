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
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.Localization;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Services;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.GUI
{
    public class LanguageSelectionGame : Popup
    {
        private List<LanguageConfiguration.LanguageInfo> languages;
        [SerializeField] private LanguageSelectionElement languageButtonPrefab;
        [SerializeField] private Transform languageButtonContainer;
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private TextMeshProUGUI selectedLanguageText;
        public CurtureTuple[] extraLanguages;
        [SerializeField]
        private LanguageConfiguration languageConfiguration;
        
        [Inject]
        private ILanguageService languageService;
        
        private Button[] languageButtons;
        private int currentSelectedIndex = -1;

        private void Start()
        {
            // Check for layout component on container
            if (languageButtonContainer.GetComponent<LayoutGroup>() == null)
            {
                Debug.LogWarning("No LayoutGroup component found on languageButtonContainer. Elements may overlap.");
            }
            
            // Get enabled languages from configuration
            languages = languageConfiguration.GetEnabledLanguages();

            // Add extra languages if configured
            if (extraLanguages != null && extraLanguages.Length > 0)
            {
                foreach (var extraLang in extraLanguages)
                {
                    var langInfo = languageConfiguration.GetLanguageInfo(extraLang.culture);
                    if (langInfo != null && !languages.Contains(langInfo))
                    {
                        languages.Add(langInfo);
                    }
                }
            }
            
            LocalizationManager.instance.InitializeLocalization();
            PopulateLanguageButtons();
        }

        private void PopulateLanguageButtons()
        {
            // Clear existing buttons if any
            foreach (Transform child in languageButtonContainer)
            {
                Destroy(child.gameObject);
            }

            var currentLanguageCode = languageService.GetCurrentLanguageCode();
            currentSelectedIndex = languages.FindIndex(l => l.code == currentLanguageCode);

            languageButtons = new Button[languages.Count];

            for (int i = 0; i < languages.Count; i++)
            {
                var buttonObj = _container.Instantiate(languageButtonPrefab, languageButtonContainer);
                var button = buttonObj.GetComponent<CustomButton>();
                buttonObj.SetLanguageName(languages[i].localizedName.ToUpper());

                // Ensure proper positioning in layout
                RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
                rectTransform.localScale = Vector3.one;
                
                int index = i; // Capture the index for the lambda
                button.onClick.AddListener(() =>
                {
                    SelectLanguage(index);
                });
                
                languageButtons[i] = button;
                // if the language is selected, set the checkmark active
                buttonObj.SetCheckMarkActive(i == currentSelectedIndex);
            }
            
            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(languageButtonContainer.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
            
            // Make sure the content size fitter is updated if present
            if (languageButtonContainer.GetComponent<ContentSizeFitter>() != null)
            {
                languageButtonContainer.GetComponent<ContentSizeFitter>().SetLayoutVertical();
            }
        }

        private void SelectLanguage(int index)
        {
            if (index < 0 || index >= languages.Count)
                return;

            // First deactivate all checkmarks
            for (int i = 0; i < languageButtons.Length; i++)
            {
                if (languageButtons[i].GetComponent<LanguageSelectionElement>() != null)
                {
                    languageButtons[i].GetComponent<LanguageSelectionElement>().SetCheckMarkActive(false);
                }
            }
            
            // Then activate only the selected one
            if (languageButtons[index].GetComponent<LanguageSelectionElement>() != null)
            {
                languageButtons[index].GetComponent<LanguageSelectionElement>().SetCheckMarkActive(true);
            }

            // Update visual selection
            if (currentSelectedIndex >= 0 && currentSelectedIndex < languageButtons.Length)
            {
                // Reset previous selection visual if needed
                ColorBlock colors = languageButtons[currentSelectedIndex].colors;
                colors.normalColor = Color.white;
                languageButtons[currentSelectedIndex].colors = colors;
            }

            currentSelectedIndex = index;
            
            // Scroll to the selected button
            Canvas.ForceUpdateCanvases();

            var selectedLanguage = languages[index];
            
            // Use LanguageService to set the language
            languageService.SetLanguage(selectedLanguage.code);

            // find all objects of LocalizationTextMeshProUGUI
            var localizationTextObjects = FindObjectsOfType<LocalizedTextMeshProUGUI>();
            foreach (var localizationText in localizationTextObjects)
            {
                localizationText.UpdateText();
            }
        }

        public void OnChangeLanguage()
        {
            if (currentSelectedIndex >= 0)
            {
                var selectedLanguage = languages[currentSelectedIndex];
                if (selectedLanguage.localizationBase != null)
                {
                    LocalizationManager.instance.LoadLanguageFromBase(selectedLanguage.localizationBase);
                }
            }
        }

        private LanguageConfiguration.LanguageInfo GetSelectedLanguage()
        {
            return languages[currentSelectedIndex];
        }
    }

    [Serializable]
    public struct CurtureTuple
    {
        public string culture;
        public string name;
    }
}