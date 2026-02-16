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
using DG.Tweening;
using System;
using UnityEngine.UI;
using VContainer;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay.Pool;

namespace WordsToolkit.Scripts.Gameplay
{
    public class SpecialItem : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float moveDuration = 0.7f; // Matched with AnimateExtraWord
        [SerializeField] private float fadeOutDuration = 0.2f;
        [SerializeField] private AudioClip collectSound; // Sound to play when item is collected
        [SerializeField] private AudioClip bounceSound; // Sound to play on bounce effect
        [Inject]
        private IAudioService audioService; // Audio service for playing sounds
        private Sequence animationSequence;
        [SerializeField]
        private ParticleSystem collectEffect; // Optional particle effect to play on collection
        
        /// <summary>
        /// Animates the special item to a target position using an arc path animation
        /// </summary>
        /// <param name="targetPosition">The world position to move to</param>
        /// <param name="onComplete">Optional callback when animation completes</param>
        public void FlyToPosition(Vector3 targetPosition, Action onComplete = null)
        {
            // Kill any existing animation
            animationSequence?.Kill();
            
            // Make sure the item is visible
            gameObject.SetActive(true);

            // audioService?.PlaySound(collectSound);
            // Create animation sequence
            animationSequence = DOTween.Sequence();
            
            // Store original scale
            Vector3 originalScale = transform.localScale;
            audioService?.PlaySound(bounceSound);
            
            // Initial move up animation
            Vector3 startPosition = transform.position;
            animationSequence.Append(transform.DOMoveY(startPosition.y + 0.5f, 0.3f)
                .SetEase(Ease.OutCubic));
            
            // Add delay after move up
            animationSequence.AppendInterval(0.2f);
            
            // Calculate a mid-point for the arc
            startPosition = transform.position;
            Vector3 midPoint = (startPosition + targetPosition) / 2f;
            
            // Determine arc height based on the distance between points (30% of distance)
            float arcHeight = Vector3.Distance(startPosition, targetPosition) * 0.3f;
            midPoint.y += arcHeight;
            
            // Calculate half duration for scaling
            float halfDuration = moveDuration / 2f;

            // Create path animation
            var pathTween = transform.DOMove(
                targetPosition,
                moveDuration)
                .SetEase(Ease.OutQuad);

            // Add the path animation to the sequence
            animationSequence.Append(pathTween);

            // Scale down during the second half (matches AnimateExtraWord)
            animationSequence.Append(transform.DOScale(originalScale * 0.8f, .2f).SetLoops(1, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad));
            // Fade out at the end
            Image image = GetComponent<Image>();
            if (image != null)
            {
                animationSequence.Append(image.DOFade(0, fadeOutDuration)
                    .SetEase(Ease.InQuad));
            }
            
            // Set completion callback
            animationSequence.OnComplete(() => {
                // Fire event to notify that a special item was collected
                EventManager.GetEvent(EGameEvent.SpecialItemCollected).Invoke();

                var fx = PoolObject.GetObject(collectEffect.gameObject, transform.position);

                // Invoke the callback if provided
                onComplete?.Invoke();
                audioService?.PlaySound(collectSound);

                // Clean up
                Destroy(gameObject);
            });
        }



        // Stop any ongoing animation when destroyed
        private void OnDestroy()
        {
            animationSequence?.Kill();
        }
    }
}