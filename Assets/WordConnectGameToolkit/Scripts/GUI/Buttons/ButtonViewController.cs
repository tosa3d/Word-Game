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

namespace WordsToolkit.Scripts.GUI.Buttons
{
    public interface IShowable
    {
        void Show();
    }

    public interface IHideable
    {
        void Hide();
        void InstantHide();
    }

    public interface IHideableForWin
    {
        void HideForWin();
    }

    public interface IFadeable : IShowable, IHideable{}

    public class ButtonViewController
    {
        private HashSet<IShowable> buttons;

        public void RegisterButton(IShowable button)
        {
            if (buttons == null)
            {
                buttons = new HashSet<IShowable>();
            }
            buttons.Add(button);
            if (button is IHideable hideable)
            {
                hideable.Hide();
            }
        }

        public void HideOtherButtons(IShowable except)
        {
            foreach (var button in buttons)
            {
                if (!ReferenceEquals(button, except) && button is IHideable hideable)
                {
                    hideable.Hide();
                }
            }
        }

        public void ShowButtons()
        {
            foreach (var button in buttons)
            {
                button.Show();
            }
        }

        public void HideAllForWin()
        {
            if (buttons == null) return;

            foreach (var button in buttons)
            {
                if (button is IHideableForWin buttonForWin)
                {
                    buttonForWin.HideForWin();
                }
                else if(button is IHideable hideable)
                {
                    hideable.Hide();
                }
            }
        }
    }
}