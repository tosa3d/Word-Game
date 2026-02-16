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
using System.Linq;
using DG.Tweening;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.GUI.Tutorials;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.Localization;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Gameplay.Managers
{
    public class TutorialManager : IStartable, IDisposable
    {
        private readonly TutorialSettings settings;
        private readonly MenuManager menuManager;
        private readonly ILocalizationService localizationManager;
        private readonly GameManager gameManager;
        private readonly IObjectResolver _resolver;
        private TutorialPopupBase tutorial;

        public TutorialManager(
            TutorialSettings settings,
            MenuManager menuManager,
            ILocalizationService localizationManager,
            GameManager gameManager,
            IObjectResolver resolver)
        {
            this.settings = settings;
            this.menuManager = menuManager;
            this.localizationManager = localizationManager;
            this.gameManager = gameManager;
            this._resolver = resolver;
        }

        public void Start()
        {
            EventManager.GetEvent<Level>( EGameEvent.Play).Subscribe(OnLevelLoaded);
            EventManager.GetEvent(EGameEvent.WordAnimated).Subscribe(OnWordOpened);
            EventManager.GetEvent<string>(EGameEvent.ExtraWordFound).Subscribe(ExtraWordFound);
            EventManager.GetEvent(EGameEvent.SpecialItemCollected).Subscribe(OnSpecialItemCollected);
            EventManager.GetEvent<CustomButton>( EGameEvent.ButtonClicked).Subscribe(OnCustomButtonClicked);
        }


        public void Dispose()
        {
            EventManager.GetEvent<Level>( EGameEvent.Play).Unsubscribe(OnLevelLoaded);
            EventManager.GetEvent(EGameEvent.WordAnimated).Unsubscribe(OnWordOpened);
            EventManager.GetEvent<string>(EGameEvent.ExtraWordFound).Unsubscribe(ExtraWordFound);
            EventManager.GetEvent(EGameEvent.SpecialItemCollected).Unsubscribe(OnSpecialItemCollected);
            EventManager.GetEvent<CustomButton>( EGameEvent.ButtonClicked).Unsubscribe(OnCustomButtonClicked);
            
            if (tutorial != null)
            {
                tutorial.OnCloseAction -= OnTutorialClosed;
                tutorial = null;
            }
        }

        private void OnSpecialItemCollected()
        {
            ShowTutorialPopup(t => t.showCondition.showCondition == ETutorialShowCondition.Event && t.kind == TutorialKind.GiftButton);
        }

        private void OnCustomButtonClicked(CustomButton obj)
        {
            CloseTutorial();
        }

        private void CloseTutorial()
        {
            if (tutorial != null)
            {
                tutorial.Close();
            }
        }

        private void ExtraWordFound(string obj)
        {
            ShowTutorialPopup(t => t.showCondition.showCondition == ETutorialShowCondition.Event && t.kind == TutorialKind.ExtraWordsButton);
        }

        private void OnLevelLoaded(Level obj)
        {
            DOVirtual.DelayedCall(0.2f, () => UpdateTutorialAppearance(obj), false);
        }

        private void UpdateTutorialAppearance(Level obj)
        {
            var tutorialShown = ShowTutorialPopup(t => t.showCondition.showCondition == ETutorialShowCondition.Level && t.showCondition.level == obj.number|| t.showCondition.showCondition == ETutorialShowCondition.FirstAppearance);
            if (tutorialShown)
                return;
            var hasSpecialItem = obj.GetLanguageData(gameManager.language).crosswordData.placements.Any(i => i.isSpecialItem);
            if (hasSpecialItem)
            {
                ShowTutorialPopup(t => t.showCondition.showCondition == ETutorialShowCondition.FirstAppearance && t.kind == TutorialKind.RedGem);
            }
        }

        private void OnWordOpened()
        {
            UpdateTutorialAppearance(GameDataManager.GetLevel());
        }

        private bool ShowTutorialPopup(Func<TutorialSettingsData, bool> predicate)
        {
            // Only show tutorials when game is in playing state
            if (EventManager.GameStatus != EGameState.Playing)
                return false;

            var tutorialDatas = settings.tutorialSettings.Where(predicate);
            foreach (var tutorialData in tutorialDatas)
            {
                bool notShow = false;
                foreach (var tag in tutorialData.tagsToShow)
                {
                    var obj = GameObject.FindGameObjectWithTag(tag);
                    if (obj == null || !obj.activeSelf || (obj.TryGetComponent(out CanvasGroup cg) && cg.alpha <= 0))
                    {
                        notShow = true;
                        break; // If any tag is not active, do not show the tutorial
                    }
                }
                if (notShow)
                    continue; // Skip to the next tutorial if any tag is not active
                if (!PlayerPrefs.HasKey(tutorialData.GetID()) || PlayerPrefs.GetInt(tutorialData.GetID()) != 1)
                {
                    ShowTutorial(tutorialData);
                    return true; // Indicate that a tutorial was shown
                }
            }
            return false; // No tutorial was shown
        }

        private void ShowTutorial(TutorialSettingsData tutorialData)
        {
            if (tutorialData != null)
            {
                tutorial = (TutorialPopupBase)menuManager.ShowPopup(tutorialData.popup);
                tutorial.SetData(tutorialData);
                tutorial.SetTitle(localizationManager.GetText(tutorialData.kind.ToString(), "Use this booster"));
                tutorial.OnCloseAction += OnTutorialClosed;
            }
        }

        private void OnTutorialClosed(EPopupResult obj)
        {
            if (tutorial != null)
            {
                PlayerPrefs.SetInt(tutorial.GetData().GetID(), 1); // Mark as shown
                PlayerPrefs.Save();
                tutorial.OnCloseAction -= OnTutorialClosed;
                if (tutorial)
                {
                    tutorial = null; // Clear the reference
                }
            }
        }
    }
}