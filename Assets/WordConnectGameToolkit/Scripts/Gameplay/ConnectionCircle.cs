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
using DG.Tweening;

namespace WordsToolkit.Scripts.Gameplay
{
    [RequireComponent(typeof(RectTransform))]
    public class ConnectionCircle : MonoBehaviour
    {
        [Header("Animation Settings")]
        public float pulseDuration = 0.5f;
        public float pulseScale = 1.2f;
        public float appearDuration = 0.2f;

        private RectTransform rectTransform;
        private Image image;
        private Sequence pulseSequence;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            
            if (image == null)
                image = gameObject.AddComponent<Image>();
                
            // Start with circle invisible
            if (image != null)
            {
                Color color = image.color;
                color.a = 0;
                image.color = color;
            }
        }
        
        public void Appear()
        {
            // Stop any running animations
            if (pulseSequence != null)
                pulseSequence.Kill();
                
            // Reset scale
            rectTransform.localScale = Vector3.zero;
            
            // Make sure it's visible
            gameObject.SetActive(true);
            
            // Animate appear with pop effect
            rectTransform.DOScale(Vector3.one, appearDuration)
                .SetEase(Ease.OutBack);
                
            // Fade in
            if (image != null)
            {
                Color color = image.color;
                Color targetColor = new Color(color.r, color.g, color.b, 1f);
                image.DOColor(targetColor, appearDuration);
            }
            
            // Start pulsing
            StartPulse();
        }
        
        public void Disappear()
        {
            // Stop any running animations
            if (pulseSequence != null)
                pulseSequence.Kill();
                
            // Animate disappear
            rectTransform.DOScale(Vector3.zero, appearDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));
                
            // Fade out
            if (image != null)
            {
                Color color = image.color;
                Color targetColor = new Color(color.r, color.g, color.b, 0f);
                image.DOColor(targetColor, appearDuration);
            }
        }
        
        private void StartPulse()
        {
            // Create a gentle pulse animation
            pulseSequence = DOTween.Sequence();
            
            pulseSequence.Append(rectTransform.DOScale(Vector3.one * pulseScale, pulseDuration / 2)
                .SetEase(Ease.InOutSine));
                
            pulseSequence.Append(rectTransform.DOScale(Vector3.one, pulseDuration / 2)
                .SetEase(Ease.InOutSine));
                
            pulseSequence.SetLoops(-1); // Loop indefinitely
        }
        
        private void OnDisable()
        {
            // Kill animations when disabled
            if (pulseSequence != null)
                pulseSequence.Kill();
        }
        
        // Set circle color
        public void SetColor(Color newColor)
        {
            if (image != null)
                image.color = newColor;
        }
        
        // Set circle size
        public void SetSize(float size)
        {
            if (rectTransform != null)
                rectTransform.sizeDelta = new Vector2(size, size);
        }
    }
}
