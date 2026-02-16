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

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using VContainer;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.GUI.Tutorials
{
    public class TutorialWordSubstitution : TutorialPopupBase
    {
        [SerializeField]
        private GameObject hand;

        protected void ReplaceWordForTutorial(string word)
        {
            localizationManager.SetPairPlaceholder("word", word.ToUpper());
            SetTitle(localizationManager.GetText(tutorialData.kind.ToString(), "Tutorial"));
        }

        protected void SelectWordAnimation(string wordForTutorial)
        {
            var selection = FindObjectOfType<WordSelectionManager>();
            var letters = selection.GetLetters(wordForTutorial);
            AnimateHandToLetters(letters);
            EventManager.GetEvent<string>(EGameEvent.WordOpened).Subscribe(OnWordOpened);
        }

        private void OnWordOpened(string obj)
        {
            EventManager.GetEvent<string>(EGameEvent.WordOpened).Unsubscribe(OnWordOpened);
            Close();
        }

        private void OnDestroy()
        {
            if (hand != null)
            {
                hand.SetActive(false);
                hand.transform.DOKill();
            }
        }

        private void AnimateHandToLetters(List<LetterButton> letters)
        {
            var path = new List<Vector3>();
            foreach (var letter in letters)
            {
                // Skip null letters (in case a word can't be formed with available letters)
                if (letter != null)
                {
                    path.Add(letter.transform.position + new Vector3(.5f, -0.8f, 0)); // Adjusting position to avoid overlap with letter
                }
            }
            if (path.Count > 0)
            {
                if (hand != null)
                {
                    hand.SetActive(true);
                    hand.transform.position = path[0];
                    hand.transform.DOPath(path.ToArray(), 2f, PathType.CatmullRom)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Restart);
                }
            }
        }
    }
}