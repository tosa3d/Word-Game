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

namespace WordsToolkit.Scripts.Popups
{
    public class Finish : Popup
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI description;
        private Level currentLevel;
        [SerializeField]
        private AudioClip swish;

        [SerializeField]
        private AudioClip cheers;

        [SerializeField]
        private Image background;

        private void OnEnable()
        {
            currentLevel = levelManager.GetCurrentLevel();

            SetFinishText(currentLevel.GetTitle(gameManager.language),
                currentLevel.GetText(gameManager.language));

            background.sprite = levelManager.GetCurrentLevel().background;
        }

        private void SetFinishText(string titleText, string descriptionText)
        {
            if (titleText != "")
            {
                title.text = titleText;
            }

            if (descriptionText != "")
            {
                description.text = descriptionText;
            }
        }

        public override void ShowAnimationSound()
        {
            base.ShowAnimationSound();
            audioService.PlaySound(cheers);
        }
    }
}