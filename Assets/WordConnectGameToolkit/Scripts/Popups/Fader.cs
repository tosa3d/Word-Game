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
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WordsToolkit.Scripts.Popups
{
    public class Fader : MonoBehaviour
    {
        public Image fader;
        private readonly float fadeTime = .3f;
        private readonly float maxValue = .993f;

        public bool IsFaded()
        {
            return fader.color.a >= 0;
        }

        public void FadeIn(float fadeAlpha, Action action = null)
        {
            fader.gameObject.SetActive(true);
            fader.DOFade(fadeAlpha, fadeTime).OnComplete(() => action?.Invoke());
        }

        public void FadeOut()
        {
            fader.DOFade(0, fadeTime).OnComplete(() => fader.gameObject.SetActive(false));
        }

        public void FadeAfterLoadingScene()
        {
            FadeOut();
        }
    }
}