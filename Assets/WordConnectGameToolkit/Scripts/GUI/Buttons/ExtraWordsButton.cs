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
using System.Collections;

namespace WordsToolkit.Scripts.GUI.Buttons
{
    public class ExtraWordsButton : BaseGUIButton
    {
        private Coroutine occasionalPulseCoroutine;
        private bool pulseEnabled;
        private bool animated;

        public void PulseAnimation(bool b)
        {
            pulseEnabled = b;
            if (b)
            {
                animator.Play($"PulseLoop");
            }
            else
            {
                animator.SetTrigger($"Pulse");
            }
        }


        protected override void ShowCallback()
        {
            base.ShowCallback();
            if (pulseEnabled)
            {
                PulseAnimation(true);
            }
        }
    }
}