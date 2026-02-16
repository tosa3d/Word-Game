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
using DG.Tweening;
using UnityEngine.UI;

namespace WordsToolkit.Scripts.GUI
{
    public class WordBubble : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI wordText;
        public float shakeDuration = 0.5f;
        public float shakeMagnitude = 0.1f;
        public int vibratoCount = 10;
        public float randomness = 90f;
        public float destroyDelay = 0.2f;

        private RectTransform _rectTransform;
        private ContentSizeFitter _contentSizeFitter;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _contentSizeFitter = GetComponent<ContentSizeFitter>();
        }

        public void SetWord(string word)
        {
            wordText.text = word;

            // Force content size fitter and layout update
            if (_contentSizeFitter != null)
            {
                Canvas.ForceUpdateCanvases();
                _contentSizeFitter.SetLayoutHorizontal();
                _contentSizeFitter.SetLayoutVertical();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            }

            Shake();
        }

        private void Shake()
        {
            if (_rectTransform != null)
            {
                // Create sequence for shake and fade out
                Sequence sequence = DOTween.Sequence();

                // Add shake animation
                sequence.Append(_rectTransform.DOShakeAnchorPos(
                    duration: shakeDuration,
                    strength: shakeMagnitude * 100f,
                    vibrato: vibratoCount,
                    randomness: randomness,
                    fadeOut: true
                ));

                // Add fade out and destroy
                sequence.AppendInterval(destroyDelay);
                sequence.Append(wordText.DOFade(0, 0.2f));
                sequence.OnComplete(() => {
                    Destroy(gameObject);
                });
            }
        }

        private void OnDisable()
        {
            _rectTransform?.DOKill();
            wordText?.DOKill();
        }
    }
}

