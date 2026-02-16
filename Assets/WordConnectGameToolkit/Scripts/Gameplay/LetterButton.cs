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
using UnityEngine.EventSystems;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.System.Haptic;

namespace WordsToolkit.Scripts.Gameplay
{
    public class LetterButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        public TextMeshProUGUI letterText;
        public Image circleImage;

        private WordSelectionManager wordSelectionManager;
        private bool isSelected = false;
        private Color color;
        private string originalLetter; // Store the original letter for validation

        private void Awake()
        {
            // Get a reference to the WordSelectionManager
            wordSelectionManager = GetComponentInParent<WordSelectionManager>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Start selection process when the user taps/clicks on a letter
            if (wordSelectionManager != null && EventManager.GameStatus == EGameState.Playing)
            {
                wordSelectionManager.StartSelection(this);
                SetSelected(true);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // When dragging over this letter, add it to the selection
            if (wordSelectionManager != null && wordSelectionManager.IsSelecting && EventManager.GameStatus == EGameState.Playing)
            {
                wordSelectionManager.AddToSelection(this);
                SetSelected(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // End selection process when the user lifts finger/mouse
            if (wordSelectionManager != null && EventManager.GameStatus == EGameState.Playing)
            {
                wordSelectionManager.EndSelection();
            }
        }

        public void SetSelected(bool selected)
        {
            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticForce.Light);
            isSelected = selected;
            circleImage.color = new Color(color.r, color.g, color.b, selected ? 1f : 0);
            letterText.color = selected ? Color.white : Color.black;
        }

        public string GetLetter()
        {
            return originalLetter ?? letterText.text;
        }

        public void SetText(string toString)
        {
           originalLetter = toString; // Store the original letter
           letterText.text = toString.ToUpper(); // Display in uppercase
        }

        public void SetColor(Color color)
        {
            this.color = color;
        }
    }
}