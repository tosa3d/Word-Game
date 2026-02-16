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
using UnityEngine;
using VContainer;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.GUI.Buttons
{
    public class BaseGUIButton : CustomButton, IFadeable
    {
        protected LevelManager levelManager;
        protected GameSettings gameSettings;
        protected ResourceManager resourceManager;
        protected ButtonViewController buttonViewController;
        public RectTransform rectTransform;
        public Vector2 savePosition;
        public Vector2 targetPosition;

        [Inject]
        public void Construct(LevelManager levelManager, GameSettings gameSettings, ResourceManager resourceManager, ButtonViewController buttonViewController)
        {
            this.gameSettings = gameSettings;
            this.levelManager = levelManager;
            this.resourceManager = resourceManager;
            this.buttonViewController = buttonViewController;
            this.buttonViewController.RegisterButton(this);
        }

        public void Hide()
        {
            animator.enabled = false;
            rectTransform.DOAnchorPos( targetPosition, 0.5f);
        }

        public void InstantHide()
        {
            rectTransform.anchoredPosition = targetPosition;
        }

        public void HideForWin()
        {
            Hide();
        }

        public void Show()
        {
            animator.enabled = true;
            rectTransform.DOAnchorPos(savePosition, 0.5f).OnComplete(ShowCallback);
        }

        protected virtual void ShowCallback()
        {

        }
    }
}