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
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VContainer;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.Gameplay.Pool;

namespace WordsToolkit.Scripts.GUI.Labels
{
    public class LabelAnim : MonoBehaviour
    {
        public GameObject prefab;
        public Transform targetTransform;
        public ResourceObject associatedResource;

        [SerializeField]
        protected GameObject fxPrefab;

        [SerializeField]
        protected TextMeshProUGUI coinsTextPrefab;

        protected Tweener doPunchScale;

        [Inject]
        protected IAudioService _audioService;

        protected IAnimationSource animatedObjectSource;

        [SerializeField]
        protected UnityEvent OnAnimationComplete;
        protected GameObject GetAnimatedObjectSource(GameObject o)
        {
            animatedObjectSource ??= new PooledAnimationSource(o??prefab, transform);
            return animatedObjectSource.GetAnimatedObject();
        }

        protected void Release(GameObject o)
        {
            animatedObjectSource?.ReleaseObject(o);
        }

        protected void PopupText(Vector3 transformPosition, string rewardDataCount)
        {
            var coinsText = PoolObject.GetObject(coinsTextPrefab.gameObject, transformPosition).GetComponent<TextMeshProUGUI>();
            coinsText.transform.position = transformPosition;
            coinsText.text = rewardDataCount;
            coinsText.alpha = 0;

            var sequence = DOTween.Sequence();
            sequence.Append(coinsText.DOFade(1, 0.2f));
            sequence.Join(coinsText.transform.DOMoveY(coinsText.transform.position.y + .5f, .5f)).OnComplete(() => { PoolObject.Return(coinsText.gameObject); });
            sequence.Append(coinsText.DOFade(0, 0.2f));
        }
    }
}