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

namespace WordsToolkit.Scripts.GUI.ExtraWordBar
{
    public class SliderExtraWordsProgressBar : BaseExtraWordsProgressBar
    {
        [Tooltip("Slider component used as progress bar")]
        public Slider progressSlider;
        
        protected override void Start()
        {
            base.Start();
            
            // Configure the slider's min value
            if (progressSlider != null)
            {
                progressSlider.minValue = 0;
            }
        }

        protected override void UpdateProgressDisplay(float progress)
        {
            if (progressSlider != null)
            {
                progressSlider.value = PlayerPrefs.GetInt("ExtraWordsCollected");
            }
        }
        
        // Apply max value changes to the slider component
        protected override void ApplyMaxValue()
        {
            if (progressSlider != null)
            {
                progressSlider.maxValue = maxValue;
            }
        }
    }
}