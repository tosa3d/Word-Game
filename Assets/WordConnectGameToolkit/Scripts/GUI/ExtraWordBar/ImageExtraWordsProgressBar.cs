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
using UnityEngine.UI;
using WordsToolkit.Scripts.GUI.Buttons;

namespace WordsToolkit.Scripts.GUI.ExtraWordBar
{
    public class ImageExtraWordsProgressBar : BaseExtraWordsProgressBar
    {
        [Tooltip("Image component used as progress bar")]
        public Image progressBar;
        
        [Tooltip("Speed of the fill animation (higher is faster)")]
        [Range(1f, 10f)]
        public float interpolationSpeed = 5f;
        
        // Current displayed fill amount (for interpolation)
        private float _currentFill;
        
        // Target fill amount we're interpolating toward
        private float _targetFill;
        [SerializeField]
        private ExtraWordsButton extraWordsButton;

        protected override void UpdateProgressDisplay(float progress)
        {
            if (progressBar != null)
            {
                // Set the target fill amount instead of directly setting fillAmount
                _targetFill = progress;
                extraWordsButton.PulseAnimation(progress == 1);
            }
        }
        
        private void Update()
        {
            // Smoothly interpolate the current fill amount towards the target
            if (progressBar != null && !Mathf.Approximately(_currentFill, _targetFill))
            {
                _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * interpolationSpeed);
                
                // If we're very close to the target, snap to it
                if (Mathf.Abs(_currentFill - _targetFill) < 0.005f)
                {
                    _currentFill = _targetFill;
                }
                
                progressBar.fillAmount = _currentFill;
            }
        }
    }
}